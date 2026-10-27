using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Bpf.Diagnostics.Tests
{
    /// <summary>
    /// EditMode coverage for the JSONL record shape, the redaction rules and
    /// the classification of captured Unity console messages.
    /// </summary>
    [TestFixture]
    public sealed class BpfLogRecordTests
    {
        private string tempDirectory;
        private bool previousIgnoreFailingMessages;
        private bool previousCaptureUnityLogs;

        [SetUp]
        public void SetUp()
        {
            previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            previousCaptureUnityLogs = BpfLogOptions.CaptureUnityLogs;

            tempDirectory = Path.Combine(Path.GetTempPath(), "bpf-log-record-tests-" + Guid.NewGuid().ToString("N"));
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
            BpfLogOptions.CaptureUnityLogs = previousCaptureUnityLogs;
            LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;

            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }

        [Test]
        public void JsonLineCarriesEveryFixedField()
        {
            LogAssert.ignoreFailingMessages = true;
            BpfLog.SetScene("JsonProbeScene");
            BpfLog.Info(
                BpfLogCategory.UI,
                "test.json.fields",
                "message body",
                null,
                BpfLogField.Of("topic", "05开启服务器"),
                BpfLogField.Of("count", 3));

            BpfLogRecord record = Single("test.json.fields");
            string line = record.ToJsonLine();

            Assert.That(line, Does.StartWith("{"));
            Assert.That(line, Does.EndWith("}"));
            Assert.That(line, Does.Contain("\"timestamp\":\"" + record.TimestampText + "\""));
            Assert.That(line, Does.Contain("\"level\":\"Info\""));
            Assert.That(line, Does.Contain("\"category\":\"UI\""));
            Assert.That(line, Does.Contain("\"event\":\"test.json.fields\""));
            Assert.That(line, Does.Contain("\"message\":\"message body\""));
            Assert.That(line, Does.Contain("\"scene\":\"JsonProbeScene\""));
            Assert.That(line, Does.Contain("\"sessionId\":\"" + BpfLog.SessionId + "\""));
            Assert.That(line, Does.Contain("\"frame\":"));
            Assert.That(line, Does.Contain("\"stackTrace\":"));
            Assert.That(line, Does.Contain("\"fields\":{\"topic\":\"05开启服务器\",\"count\":\"3\"}"));
            Assert.That(line, Does.Contain("\"captured\":false"));
        }

        [Test]
        public void JsonEscapesQuotesBackslashesAndNewlines()
        {
            LogAssert.ignoreFailingMessages = true;
            BpfLog.Info(BpfLogCategory.TexCore, "test.json.escape", "quote \" slash \\ newline \n end");

            BpfLogRecord record = Single("test.json.escape");
            string line = record.ToJsonLine();

            Assert.That(line, Does.Contain("quote \\\" slash \\\\ newline \\n end"));
            Assert.That(line, Does.Not.Contain("newline \n end"));
        }

        [Test]
        public void ConsoleLineKeepsCategoryAndEvent()
        {
            LogAssert.ignoreFailingMessages = true;
            BpfLog.Warning(BpfLogCategory.XR, "test.console", "hand tracking missing", null, BpfLogField.Of("hand", "left"));

            BpfLogRecord record = Single("test.console");
            string line = record.ToConsoleLine();

            Assert.That(line, Is.EqualTo("[Bpf][Warning][XR] test.console: hand tracking missing {hand=left}"));
        }

        [Test]
        public void CapturedUnityMessageIsClassifiedAsUnityExternal()
        {
            LogAssert.ignoreFailingMessages = true;
            BpfLogOptions.CaptureUnityLogs = true;

            BpfLog.HandleUnityLog("external warning text", "at Bpf.Sample.Throw()", LogType.Warning);

            List<BpfLogRecord> records = BpfLog.Snapshot(
                BpfLogLevel.Debug,
                BpfLogCategory.UnityExternal,
                "external warning text",
                10);

            Assert.That(records, Has.Count.EqualTo(1));
            Assert.That(records[0].Level, Is.EqualTo(BpfLogLevel.Warning));
            Assert.That(records[0].Event, Is.EqualTo("unity.warning"));
            Assert.That(records[0].Captured, Is.True);
            Assert.That(records[0].StackTrace, Does.Contain("Bpf.Sample.Throw"));
        }

        [Test]
        public void CapturedUnityMessagesHonourLevelFilterAndCategorySwitch()
        {
            LogAssert.ignoreFailingMessages = true;
            BpfLogOptions.CaptureUnityLogs = true;
            BpfLog.MinimumLevel = BpfLogLevel.Error;

            BpfLog.HandleUnityLog("filtered warning text", string.Empty, LogType.Warning);
            Assert.That(BpfLog.Snapshot(BpfLogLevel.Debug, BpfLogCategory.UnityExternal, "filtered warning text", 10), Is.Empty);

            BpfLog.MinimumLevel = BpfLogLevel.Debug;
            BpfLog.SetCategoryEnabled(BpfLogCategory.UnityExternal, false);
            BpfLog.HandleUnityLog("muted warning text", string.Empty, LogType.Warning);
            Assert.That(BpfLog.Snapshot(BpfLogLevel.Debug, null, "muted warning text", 10), Is.Empty);
        }

        [Test]
        public void ExceptionRecordsCarryTheExceptionStack()
        {
            LogAssert.ignoreFailingMessages = true;
            BpfLog.Exception(
                BpfLogCategory.Network,
                "test.exception",
                new InvalidOperationException("socket closed"),
                "upload failed");

            BpfLogRecord record = Single("test.exception");

            Assert.That(record.Level, Is.EqualTo(BpfLogLevel.Exception));
            Assert.That(record.Message, Is.EqualTo("upload failed"));
            Assert.That(record.StackTrace, Does.Contain("socket closed"));
            Assert.That(record.ToJsonLine(), Does.Contain("\"level\":\"Exception\""));
        }

        [Test]
        public void SanitizerMasksCredentialsAndIdentifiers()
        {
            LogAssert.ignoreFailingMessages = true;
            string result = BpfLogSanitizer.Sanitize(
                "token=abc123&user_id=42&password: hunter2&email=bob@example.com");

            Assert.That(result, Does.Not.Contain("abc123"));
            Assert.That(result, Does.Not.Contain("hunter2"));
            Assert.That(result, Does.Not.Contain("bob@example.com"));
            Assert.That(result, Does.Contain("token=***"));
            Assert.That(result, Does.Contain("user_id=***"));
        }

        [Test]
        public void SanitizerMasksJsonAssignmentsAndAuthorizationHeaders()
        {
            LogAssert.ignoreFailingMessages = true;
            string json = BpfLogSanitizer.Sanitize("{\"access_token\":\"ya29.abcdef\",\"nickname\":\"bob\"}");
            Assert.That(json, Does.Contain("\"access_token\":\"***\""));
            Assert.That(json, Does.Contain("\"nickname\":\"bob\""));
            Assert.That(json, Does.Not.Contain("ya29.abcdef"));

            string header = BpfLogSanitizer.Sanitize("Authorization: Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.abc123");
            Assert.That(header, Does.Not.Contain("eyJhbGciOiJIUzI1NiJ9"));
            Assert.That(header, Does.Contain("***"));
        }

        [Test]
        public void SanitizerMasksUrlCredentialsAndJwt()
        {
            LogAssert.ignoreFailingMessages = true;
            string url = BpfLogSanitizer.Sanitize("https://host/api?sign=deadbeef&room=7");
            Assert.That(url, Does.Not.Contain("deadbeef"));
            Assert.That(url, Does.Contain("sign=***"));
            Assert.That(url, Does.Contain("room=7"));

            string jwt = BpfLogSanitizer.Sanitize("jwt eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.abc123 done");
            Assert.That(jwt, Does.Not.Contain("eyJhbGciOiJIUzI1NiJ9"));
        }

        [Test]
        public void SanitizerTruncatesLongValuesAndKeepsPlainText()
        {
            LogAssert.ignoreFailingMessages = true;
            string longValue = new string('x', 120);
            string truncated = BpfLogSanitizer.Sanitize(longValue, 40);

            Assert.That(truncated, Does.Contain("truncated"));
            Assert.That(truncated.Length, Is.LessThan(longValue.Length));

            const string plain = "M05 ready: laser, marking, draw and QR interactions bound.";
            Assert.That(BpfLogSanitizer.Sanitize(plain), Is.EqualTo(plain));
        }

        [Test]
        public void SanitizerRunsForMessagesAndFields()
        {
            LogAssert.ignoreFailingMessages = true;
            BpfLog.Info(
                BpfLogCategory.Network,
                "test.sanitized_record",
                "http://host/login password=p@ssw0rd",
                null,
                BpfLogField.Of("cookie", "sessionid=abcdef123456"));

            BpfLogRecord record = Single("test.sanitized_record");

            Assert.That(record.Message, Does.Not.Contain("p@ssw0rd"));
            Assert.That(record.Fields[0].Value, Does.Not.Contain("abcdef123456"));
        }

        private static BpfLogRecord Single(string eventName)
        {
            List<BpfLogRecord> records = BpfLog.Snapshot(BpfLogLevel.Debug, null, eventName, 50);
            Assert.That(records, Has.Count.EqualTo(1), "expected exactly one record for " + eventName);
            return records[0];
        }
    }
}
