using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Bpf.Diagnostics.Tests
{
    /// <summary>
    /// EditMode coverage for level filtering, category switches, the unified
    /// log recursion guard, rate limiting and session file integration.
    /// </summary>
    [TestFixture]
    public sealed class BpfLogCoreTests
    {
        private string tempDirectory;
        private bool previousIgnoreFailingMessages;
        private bool previousEnableRateLimiting;
        private bool previousCaptureUnityLogs;
        private int previousRateLimitMax;
        private double previousRateLimitWindow;

        [SetUp]
        public void SetUp()
        {
            previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            previousEnableRateLimiting = BpfLogOptions.EnableRateLimiting;
            previousCaptureUnityLogs = BpfLogOptions.CaptureUnityLogs;
            previousRateLimitMax = BpfLogOptions.RateLimitMaxRecordsPerWindow;
            previousRateLimitWindow = BpfLogOptions.RateLimitWindowSeconds;

            tempDirectory = Path.Combine(Path.GetTempPath(), "bpf-log-tests-" + Guid.NewGuid().ToString("N"));
            BpfLog.ResetStatics();
            BpfLogOptions.LogDirectoryOverride = tempDirectory;
            BpfLogOptions.CaptureUnityLogs = false;
            BpfLog.EnsureInitialized();
            BpfLog.ClearRecords();
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = true;
            BpfLog.Shutdown();
            BpfLog.ResetStatics();
            BpfLogOptions.LogDirectoryOverride = null;
            BpfLogOptions.EnableRateLimiting = previousEnableRateLimiting;
            BpfLogOptions.CaptureUnityLogs = previousCaptureUnityLogs;
            BpfLogOptions.RateLimitMaxRecordsPerWindow = previousRateLimitMax;
            BpfLogOptions.RateLimitWindowSeconds = previousRateLimitWindow;
            LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;

            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }

        [Test]
        public void RecordsBelowMinimumLevelAreDropped()
        {
            LogAssert.ignoreFailingMessages = true;
            BpfLog.MinimumLevel = BpfLogLevel.Warning;

            BpfLog.Debug(BpfLogCategory.TexCore, "test.filter.debug", "hidden");
            BpfLog.Info(BpfLogCategory.TexCore, "test.filter.info", "hidden");
            BpfLog.Warning(BpfLogCategory.TexCore, "test.filter.warning", "visible");
            BpfLog.Error(BpfLogCategory.TexCore, "test.filter.error", "visible");

            Assert.That(Find("test.filter.debug"), Is.Empty);
            Assert.That(Find("test.filter.info"), Is.Empty);
            Assert.That(Find("test.filter.warning"), Has.Count.EqualTo(1));
            Assert.That(Find("test.filter.error"), Has.Count.EqualTo(1));
        }

        [Test]
        public void CategorySwitchSuppressesAndRestoresRecords()
        {
            LogAssert.ignoreFailingMessages = true;
            BpfLog.SetCategoryEnabled(BpfLogCategory.Network, false);

            BpfLog.Info(BpfLogCategory.Network, "test.category.off", "suppressed");
            Assert.That(Find("test.category.off"), Is.Empty);
            Assert.That(BpfLog.IsCategoryEnabled(BpfLogCategory.Network), Is.False);

            BpfLog.SetCategoryEnabled(BpfLogCategory.Network, true);
            BpfLog.Info(BpfLogCategory.Network, "test.category.on", "recorded");

            Assert.That(Find("test.category.on"), Has.Count.EqualTo(1));
            Assert.That(BpfLog.IsCategoryEnabled(BpfLogCategory.Network), Is.True);
        }

        [Test]
        public void UnifiedRecordsAreNotCapturedAgainAsUnityExternal()
        {
            LogAssert.ignoreFailingMessages = true;
            BpfLogOptions.CaptureUnityLogs = true;

            BpfLog.Info(BpfLogCategory.Diagnostics, "test.no_recursion", "single record probe");

            List<BpfLogRecord> unified = Find("test.no_recursion");
            Assert.That(unified, Has.Count.EqualTo(1));

            List<BpfLogRecord> captured = BpfLog.Snapshot(
                BpfLogLevel.Debug,
                BpfLogCategory.UnityExternal,
                "single record probe",
                50);
            Assert.That(captured, Is.Empty);
        }

        [Test]
        public void DuplicateWarningsAreRateLimited()
        {
            LogAssert.ignoreFailingMessages = true;
            BpfLogOptions.EnableRateLimiting = true;
            BpfLogOptions.RateLimitMaxRecordsPerWindow = 3;
            BpfLogOptions.RateLimitWindowSeconds = 30.0;

            for (int i = 0; i < 10; i++)
            {
                BpfLog.Warning(BpfLogCategory.Performance, "test.spam", "same warning");
            }

            Assert.That(Find("test.spam"), Has.Count.EqualTo(3));
            Assert.That(BpfLog.RateLimitedRecordCount, Is.EqualTo(7));
        }

        [Test]
        public void ErrorsAreNeverRateLimited()
        {
            LogAssert.ignoreFailingMessages = true;
            BpfLogOptions.EnableRateLimiting = true;
            BpfLogOptions.RateLimitMaxRecordsPerWindow = 2;
            BpfLogOptions.RateLimitWindowSeconds = 30.0;

            for (int i = 0; i < 8; i++)
            {
                BpfLog.Error(BpfLogCategory.TexCore, "test.error_spam", "same error");
            }

            Assert.That(Find("test.error_spam"), Has.Count.EqualTo(8));
            Assert.That(BpfLog.RateLimitedRecordCount, Is.EqualTo(0));
        }

        [Test]
        public void CountersTrackLevels()
        {
            LogAssert.ignoreFailingMessages = true;
            BpfLog.ClearRecords();
            BpfLog.Info(BpfLogCategory.UI, "test.count.info", "one");
            BpfLog.Warning(BpfLogCategory.UI, "test.count.warning", "two");
            BpfLog.Error(BpfLogCategory.UI, "test.count.error", "three");
            BpfLog.Fatal(BpfLogCategory.UI, "test.count.fatal", "four");

            Assert.That(BpfLog.RecordCount, Is.EqualTo(4));
            Assert.That(BpfLog.WarningCount, Is.EqualTo(1));
            Assert.That(BpfLog.ErrorCount, Is.EqualTo(2));

            BpfLog.ClearRecords();
            Assert.That(BpfLog.RecordCount, Is.EqualTo(0));
            Assert.That(BpfLog.ErrorCount, Is.EqualTo(0));
        }

        [Test]
        public void SceneFieldIsRecordedOnNewRecords()
        {
            LogAssert.ignoreFailingMessages = true;
            BpfLog.SetScene("BpfLogTestScene");
            BpfLog.Info(BpfLogCategory.Scene, "test.scene_field", "scene probe");

            List<BpfLogRecord> records = Find("test.scene_field");
            Assert.That(records, Has.Count.EqualTo(1));
            Assert.That(records[0].Scene, Is.EqualTo("BpfLogTestScene"));
            Assert.That(records[0].SessionId, Is.EqualTo(BpfLog.SessionId));
        }

        [Test]
        public void SessionFileIsCreatedAndReceivesJsonLines()
        {
            LogAssert.ignoreFailingMessages = true;
            BpfLog.Info(
                BpfLogCategory.Startup,
                "test.file_write",
                "written to session file",
                null,
                BpfLogField.Of("module", "M05"));
            BpfLog.Flush();

            string path = BpfLog.CurrentLogFilePath;
            Assert.That(path, Is.Not.Null);
            Assert.That(File.Exists(path), Is.True);

            string[] lines = ReadAllLinesShared(path);
            Assert.That(lines.Length, Is.GreaterThanOrEqualTo(1));
            Assert.That(string.Join("\n", lines), Does.Contain("test.file_write"));

            foreach (string line in lines)
            {
                Assert.That(line, Does.StartWith("{"));
                Assert.That(line, Does.EndWith("}"));
            }
        }

        [Test]
        public void ExportWritesRecentRecordsToAStandaloneFile()
        {
            LogAssert.ignoreFailingMessages = true;
            BpfLog.Info(BpfLogCategory.Diagnostics, "test.export", "export probe", null, BpfLogField.Of("kind", "report"));

            string path = BpfLog.ExportRecent(50);

            Assert.That(path, Is.Not.Null);
            Assert.That(File.Exists(path), Is.True);
            Assert.That(File.ReadAllText(path), Does.Contain("test.export"));
        }

        [Test]
        public void MemoryBufferHonoursCapacityAndFilters()
        {
            LogAssert.ignoreFailingMessages = true;
            BpfLogMemoryBuffer buffer = new BpfLogMemoryBuffer(3);
            buffer.Add(new BpfLogRecord(BpfLogLevel.Info, BpfLogCategory.UI, "test.buffer.1", "one"));
            buffer.Add(new BpfLogRecord(BpfLogLevel.Warning, BpfLogCategory.UI, "test.buffer.2", "two"));
            buffer.Add(new BpfLogRecord(BpfLogLevel.Error, BpfLogCategory.Network, "test.buffer.3", "three"));
            buffer.Add(new BpfLogRecord(BpfLogLevel.Info, BpfLogCategory.Network, "test.buffer.4", "four"));

            Assert.That(buffer.Count, Is.EqualTo(3));
            Assert.That(buffer.ToArray().Select(record => record.Event), Is.EqualTo(new[]
            {
                "test.buffer.2",
                "test.buffer.3",
                "test.buffer.4",
            }));

            List<BpfLogRecord> failures = buffer.Snapshot(BpfLogLevel.Error, null, null, 10);
            Assert.That(failures.Select(record => record.Event), Is.EqualTo(new[] { "test.buffer.3" }));

            List<BpfLogRecord> network = buffer.Snapshot(BpfLogLevel.Debug, BpfLogCategory.Network, null, 10);
            Assert.That(network.Select(record => record.Event), Is.EqualTo(new[] { "test.buffer.3", "test.buffer.4" }));

            List<BpfLogRecord> keyword = buffer.Snapshot(BpfLogLevel.Debug, null, "four", 10);
            Assert.That(keyword.Select(record => record.Event), Is.EqualTo(new[] { "test.buffer.4" }));
        }

        private static string[] ReadAllLinesShared(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd().Replace("\r\n", "\n").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        private static List<BpfLogRecord> Find(string eventName)
        {
            return BpfLog.Snapshot(BpfLogLevel.Debug, null, eventName, 200);
        }
    }
}
