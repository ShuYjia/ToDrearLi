using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Bpf.Diagnostics.Tests
{
    /// <summary>
    /// PlayMode coverage for the runtime bootstrap: session files, capture of
    /// direct Unity log calls and the development diagnostics panel.
    /// </summary>
    [TestFixture]
    public sealed class BpfLogPlayModeTests
    {
        private const string StartupScene = "01StartUp";
        private const string SingleplayerScene = "03SingleplayerScene";

        private string tempDirectory;
        private bool previousIgnoreFailingMessages;

        [SetUp]
        public void SetUp()
        {
            previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            tempDirectory = Path.Combine(Path.GetTempPath(), "bpf-log-playmode-" + Guid.NewGuid().ToString("N"));

            BpfLog.Shutdown();
            BpfLog.ResetStatics();
            BpfLogOptions.LogDirectoryOverride = tempDirectory;
            BpfLogOptions.CaptureUnityLogs = true;
            BpfLogOptions.EnableRateLimiting = false;
            BpfLog.EnsureInitialized();
            BpfLog.ClearRecords();
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = true;
            BpfLogOptions.EnableRateLimiting = true;
            BpfLogOptions.LogDirectoryOverride = null;
            BpfLog.Shutdown();
            BpfLog.ResetStatics();
            LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;

            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }

        [UnityTest]
        public IEnumerator UnityNativeLogsAreCapturedIntoTheSessionFile()
        {
            LogAssert.ignoreFailingMessages = true;
            Debug.Log("bpf playmode log probe");
            yield return null;

            BpfLogRecord record = SingleExternal("bpf playmode log probe");
            Assert.That(record.Captured, Is.True);
            Assert.That(record.Level, Is.EqualTo(BpfLogLevel.Info));
            Assert.That(record.Event, Is.EqualTo("unity.log"));

            BpfLog.Flush();

            Assert.That(BpfLog.CurrentLogFilePath, Is.Not.Null);
            Assert.That(File.Exists(BpfLog.CurrentLogFilePath), Is.True);
            Assert.That(ReadAllTextShared(BpfLog.CurrentLogFilePath), Does.Contain("bpf playmode log probe"));
        }

        [UnityTest]
        public IEnumerator WarningErrorAndExceptionAreCapturedWithTheirLevels()
        {
            LogAssert.ignoreFailingMessages = true;
            Debug.LogWarning("bpf playmode warning probe");
            Debug.LogError("bpf playmode error probe");
            Debug.LogException(new InvalidOperationException("bpf playmode exception probe"));
            yield return null;

            BpfLogRecord warning = SingleExternal("bpf playmode warning probe");
            Assert.That(warning.Level, Is.EqualTo(BpfLogLevel.Warning));
            Assert.That(warning.Event, Is.EqualTo("unity.warning"));

            BpfLogRecord error = SingleExternal("bpf playmode error probe");
            Assert.That(error.Level, Is.EqualTo(BpfLogLevel.Error));
            Assert.That(error.Event, Is.EqualTo("unity.error"));

            BpfLogRecord exception = SingleExternal("bpf playmode exception probe");
            Assert.That(exception.Level, Is.EqualTo(BpfLogLevel.Exception));
            Assert.That(exception.Event, Is.EqualTo("unity.exception"));
        }

        [UnityTest]
        public IEnumerator DiagnosticsPanelFiltersPausesAndExportsRecords()
        {
            LogAssert.ignoreFailingMessages = true;
            BpfDiagnosticsOverlay overlay = UnityEngine.Object.FindObjectOfType<BpfDiagnosticsOverlay>();
            Assert.That(overlay, Is.Not.Null, "the development overlay must exist in the Editor play session");
            Assert.That(BpfDiagnosticsOverlay.IsVisible, Is.False, "the overlay starts hidden");

            yield return null;

            BpfLog.ClearRecords();
            BpfLog.Info(BpfLogCategory.UI, "test.overlay.info", "alpha record");
            BpfLog.Warning(BpfLogCategory.Network, "test.overlay.warning", "bravo record");

            BpfDiagnosticsPanel panel = overlay.Panel;

            panel.SetKeyword("alpha");
            Assert.That(Events(panel), Does.Contain("test.overlay.info"));
            Assert.That(Events(panel), Does.Not.Contain("test.overlay.warning"));

            panel.SetKeyword(string.Empty);
            panel.SetLevelFilter(BpfLogLevel.Warning);
            Assert.That(Events(panel), Does.Contain("test.overlay.warning"));
            Assert.That(Events(panel), Does.Not.Contain("test.overlay.info"));

            panel.SetLevelFilter(BpfLogLevel.Debug);
            panel.SetCategory(BpfLogCategory.UI);
            Assert.That(panel.GetRecords().All(record => record.Category == BpfLogCategory.UI), Is.True);

            panel.SetCategory(BpfLogCategory.All);
            panel.SetPaused(true);
            int frozenCount = panel.GetRecords().Count;
            BpfLog.Info(BpfLogCategory.UI, "test.overlay.after_pause", "charlie record");
            Assert.That(panel.GetRecords().Count, Is.EqualTo(frozenCount));

            panel.SetPaused(false);
            Assert.That(Events(panel), Does.Contain("test.overlay.after_pause"));

            string exportPath = panel.ExportRecent();
            Assert.That(exportPath, Is.Not.Null);
            Assert.That(File.Exists(exportPath), Is.True);
            Assert.That(File.ReadAllText(exportPath), Does.Contain("test.overlay.info"));

            BpfDiagnosticsOverlay.SetVisible(true);
            yield return null;
            Assert.That(BpfDiagnosticsOverlay.IsVisible, Is.True);

            BpfDiagnosticsOverlay.SetVisible(false);
            yield return null;
            Assert.That(BpfDiagnosticsOverlay.IsVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator SceneFieldFollowsLoadedScenes()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return LoadScene(StartupScene);

            Assert.That(BpfLog.CurrentScene, Is.EqualTo(StartupScene));
            Assert.That(
                BpfLog.Snapshot(BpfLogLevel.Debug, BpfLogCategory.Scene, "scene.loaded", 100)
                    .Any(record => record.Scene == StartupScene),
                Is.True,
                "01StartUp must produce a scene.loaded record");

            yield return LoadScene(SingleplayerScene);

            Assert.That(BpfLog.CurrentScene, Is.EqualTo(SingleplayerScene));
            Assert.That(
                BpfLog.Snapshot(BpfLogLevel.Debug, BpfLogCategory.Scene, "scene.loaded", 100)
                    .Any(record => record.Scene == SingleplayerScene),
                Is.True,
                "03SingleplayerScene must produce a scene.loaded record");

            Scene startup = SceneManager.GetSceneByName(StartupScene);
            if (startup.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(startup);
            }

            Scene singleplayer = SceneManager.GetSceneByName(SingleplayerScene);
            if (singleplayer.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(singleplayer);
            }
        }

        private static IEnumerator LoadScene(string sceneName)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            Assert.That(operation, Is.Not.Null, sceneName + " must be enabled in Build Settings");

            float deadline = Time.realtimeSinceStartup + 90f;
            while (!operation.isDone && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(operation.isDone, Is.True, sceneName + " did not finish loading in time");
            yield return null;
        }

        private static string ReadAllTextShared(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        private static BpfLogRecord SingleExternal(string text)
        {
            List<BpfLogRecord> records = BpfLog.Snapshot(
                BpfLogLevel.Debug,
                BpfLogCategory.UnityExternal,
                text,
                20);
            Assert.That(records, Has.Count.GreaterThanOrEqualTo(1), "expected a captured record for: " + text);
            return records[records.Count - 1];
        }

        private static List<string> Events(BpfDiagnosticsPanel panel)
        {
            return panel.GetRecords().Select(record => record.Event).ToList();
        }
    }
}
