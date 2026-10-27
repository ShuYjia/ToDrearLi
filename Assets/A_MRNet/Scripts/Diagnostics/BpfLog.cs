using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bpf.Diagnostics
{
    /// <summary>
    /// The BPF unified log facade. Business code calls this type instead of
    /// <c>UnityEngine.Debug</c> or <c>print</c> so that every record carries a
    /// level, a category, a stable event name, the active scene, the session
    /// identifier and optional structured fields, and so that the same record
    /// reaches the Unity console, the JSONL session file and the development
    /// diagnostics panel.
    /// </summary>
    public static class BpfLog
    {
        /// <summary>Category assigned to captured Unity console messages.</summary>
        public const string UnityExternalCategory = BpfLogCategory.UnityExternal;

        private static readonly object Gate = new object();
        private static readonly HashSet<string> DisabledCategories =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, RateWindow> RateWindows =
            new Dictionary<string, RateWindow>(StringComparer.Ordinal);
        private static readonly BpfLogField[] NoFields = new BpfLogField[0];
        private static readonly System.Diagnostics.Stopwatch Clock = System.Diagnostics.Stopwatch.StartNew();

        [ThreadStatic]
        private static int forwardingDepth;

        private static BpfLogMemoryBuffer memoryBuffer;
        private static BpfLogFileWriter fileWriter;
        private static string logDirectory = string.Empty;
        private static string sessionId = string.Empty;
        private static string currentScene = string.Empty;
        private static BpfLogLevel minimumLevel = BpfLogOptions.DefaultMinimumLevel;
        private static int mainThreadId;
        private static int currentFrame;
        private static float flushTimer;
        private static bool initialized;
        private static bool captureHooked;
        private static bool unhandledHooked;
        private static bool stopped;
        private static bool emitFaultReported;
        private static long recordCount;
        private static long errorCount;
        private static long warningCount;
        private static long rateLimitedCount;

        /// <summary>Raised for every record that reached the sinks.</summary>
        public static event Action<BpfLogRecord> RecordWritten;

        /// <summary>Master switch. When false every log call is ignored.</summary>
        public static bool IsEnabled = true;

        /// <summary>Records below this level are dropped.</summary>
        public static BpfLogLevel MinimumLevel
        {
            get { return minimumLevel; }
            set { minimumLevel = value; }
        }

        /// <summary>Identifier shared by every record of the running session.</summary>
        public static string SessionId
        {
            get { return sessionId; }
        }

        /// <summary>Active scene name recorded on new records.</summary>
        public static string CurrentScene
        {
            get { return currentScene; }
        }

        /// <summary>Directory holding the JSONL session files.</summary>
        public static string LogDirectory
        {
            get { return logDirectory; }
        }

        /// <summary>Path of the JSONL file currently being written.</summary>
        public static string CurrentLogFilePath
        {
            get { return fileWriter == null ? null : fileWriter.CurrentPath; }
        }

        /// <summary>True once <see cref="EnsureInitialized"/> has run.</summary>
        public static bool IsInitialized
        {
            get { return initialized; }
        }

        /// <summary>True while Unity console messages are captured.</summary>
        public static bool IsCaptureHooked
        {
            get { return captureHooked; }
        }

        /// <summary>Records emitted since the last <see cref="ClearRecords"/>.</summary>
        public static long RecordCount
        {
            get { return Interlocked.Read(ref recordCount); }
        }

        /// <summary>Error, Exception and Fatal records since the last clear.</summary>
        public static long ErrorCount
        {
            get { return Interlocked.Read(ref errorCount); }
        }

        /// <summary>Warning records since the last clear.</summary>
        public static long WarningCount
        {
            get { return Interlocked.Read(ref warningCount); }
        }

        /// <summary>Duplicate records dropped by the rate limiter.</summary>
        public static long RateLimitedRecordCount
        {
            get { return Interlocked.Read(ref rateLimitedCount); }
        }

        /// <summary>Text of the first internal emit failure, if any.</summary>
        public static string LastEmitError { get; private set; }

        // ---------------------------------------------------------------- API

        public static void Debug(string category, string eventName, string message = null, UnityEngine.Object context = null, params BpfLogField[] fields)
        {
            Write(BpfLogLevel.Debug, category, eventName, message, context, null, fields, false, null);
        }

        public static void Info(string category, string eventName, string message = null, UnityEngine.Object context = null, params BpfLogField[] fields)
        {
            Write(BpfLogLevel.Info, category, eventName, message, context, null, fields, false, null);
        }

        public static void Warning(string category, string eventName, string message = null, UnityEngine.Object context = null, params BpfLogField[] fields)
        {
            Write(BpfLogLevel.Warning, category, eventName, message, context, null, fields, false, null);
        }

        public static void Error(string category, string eventName, string message = null, UnityEngine.Object context = null, params BpfLogField[] fields)
        {
            Write(BpfLogLevel.Error, category, eventName, message, context, null, fields, false, null);
        }

        public static void Error(string category, string eventName, System.Exception exception, string message = null, UnityEngine.Object context = null, params BpfLogField[] fields)
        {
            Write(BpfLogLevel.Error, category, eventName, message, context, exception, fields, false, null);
        }

        public static void Exception(string category, string eventName, System.Exception exception, string message = null, UnityEngine.Object context = null, params BpfLogField[] fields)
        {
            Write(BpfLogLevel.Exception, category, eventName, message, context, exception, fields, false, null);
        }

        public static void Fatal(string category, string eventName, string message = null, UnityEngine.Object context = null, params BpfLogField[] fields)
        {
            Write(BpfLogLevel.Fatal, category, eventName, message, context, null, fields, false, null);
        }

        // ------------------------------------------------------- configuration

        /// <summary>Turns one category on or off at runtime.</summary>
        public static void SetCategoryEnabled(string category, bool enabled)
        {
            if (string.IsNullOrEmpty(category))
            {
                return;
            }

            lock (Gate)
            {
                if (enabled)
                {
                    DisabledCategories.Remove(category);
                }
                else
                {
                    DisabledCategories.Add(category);
                }
            }
        }

        /// <summary>True when the category is currently recorded.</summary>
        public static bool IsCategoryEnabled(string category)
        {
            if (string.IsNullOrEmpty(category))
            {
                return true;
            }

            lock (Gate)
            {
                return !DisabledCategories.Contains(category);
            }
        }

        /// <summary>Re-enables every category.</summary>
        public static void ResetCategoryFilters()
        {
            lock (Gate)
            {
                DisabledCategories.Clear();
            }
        }

        /// <summary>Sets the scene name recorded on new records.</summary>
        public static void SetScene(string sceneName)
        {
            currentScene = sceneName ?? string.Empty;
        }

        // -------------------------------------------------------- lifecycle

        /// <summary>
        /// Creates the session file, the memory buffer and the Unity console
        /// capture hook. Safe to call repeatedly; the runtime host and tests
        /// both use it.
        /// </summary>
        public static void EnsureInitialized()
        {
            lock (Gate)
            {
                if (initialized)
                {
                    return;
                }

                initialized = true;
                stopped = false;
                emitFaultReported = false;
                mainThreadId = Thread.CurrentThread.ManagedThreadId;
                currentFrame = 0;
                flushTimer = 0f;
                sessionId = BuildSessionId();
                logDirectory = ResolveLogDirectory();
                currentScene = SceneManager.GetActiveScene().name ?? string.Empty;

                if (BpfLogOptions.IsDevelopmentBuild)
                {
                    memoryBuffer = new BpfLogMemoryBuffer(BpfLogOptions.MemoryBufferCapacity);
                }

                try
                {
                    fileWriter = new BpfLogFileWriter(
                        logDirectory,
                        sessionId,
                        BpfLogOptions.MaxFileBytes,
                        BpfLogOptions.MaxRetainedFiles);
                }
                catch (System.Exception exception)
                {
                    fileWriter = null;
                    LastEmitError = exception.Message;
                }

                HookUnityCapture();
                HookUnhandledExceptions();

                Info(
                    BpfLogCategory.Diagnostics,
                    "log.session_started",
                    "BPF diagnostics session started",
                    null,
                    BpfLogField.Of("sessionId", sessionId),
                    BpfLogField.Of("logPath", CurrentLogFilePath),
                    BpfLogField.Of("developmentBuild", BpfLogOptions.IsDevelopmentBuild),
                    BpfLogField.Of("platform", Application.platform),
                    BpfLogField.Of("unityVersion", Application.unityVersion),
                    BpfLogField.Of("appVersion", Application.version),
                    BpfLogField.Of("productName", Application.productName),
                    BpfLogField.Of("deviceModel", SystemInfo.deviceModel),
                    BpfLogField.Of("deviceName", SystemInfo.deviceName));

                Flush();
            }
        }

        /// <summary>Flushes and releases the sinks and the capture hook.</summary>
        public static void Shutdown()
        {
            lock (Gate)
            {
                if (!initialized)
                {
                    return;
                }

                Info(BpfLogCategory.Diagnostics, "log.session_stopped", "BPF diagnostics session stopped");

                stopped = true;
                UnhookUnityCapture();
                UnhookUnhandledExceptions();

                if (fileWriter != null)
                {
                    fileWriter.Flush();
                    fileWriter.Close();
                }

                initialized = false;
            }
        }

        /// <summary>
        /// Clears every static field. Called before each play session so a
        /// disabled domain reload cannot leak state between runs.
        /// </summary>
        public static void ResetStatics()
        {
            lock (Gate)
            {
                UnhookUnityCapture();
                UnhookUnhandledExceptions();
                if (fileWriter != null)
                {
                    fileWriter.Dispose();
                }

                fileWriter = null;
                memoryBuffer = null;
                logDirectory = string.Empty;
                sessionId = string.Empty;
                currentScene = string.Empty;
                minimumLevel = BpfLogOptions.DefaultMinimumLevel;
                initialized = false;
                stopped = false;
                emitFaultReported = false;
                currentFrame = 0;
                flushTimer = 0f;
                IsEnabled = true;
                LastEmitError = null;
                RateWindows.Clear();
                DisabledCategories.Clear();
                Interlocked.Exchange(ref recordCount, 0);
                Interlocked.Exchange(ref errorCount, 0);
                Interlocked.Exchange(ref warningCount, 0);
                Interlocked.Exchange(ref rateLimitedCount, 0);
                RecordWritten = null;
            }
        }

        /// <summary>Drains the write queue when the interval or batch size is met.</summary>
        public static void Tick(float deltaTime)
        {
            if (!initialized || fileWriter == null)
            {
                return;
            }

            if (Thread.CurrentThread.ManagedThreadId == mainThreadId)
            {
                currentFrame = Time.frameCount;
            }

            flushTimer += deltaTime;
            int pending = fileWriter.PendingCount;
            if (flushTimer < BpfLogOptions.FlushIntervalSeconds && pending < 64)
            {
                return;
            }

            flushTimer = 0f;
            fileWriter.Drain();
        }

        /// <summary>Forces every queued record to disk.</summary>
        public static void Flush()
        {
            if (fileWriter == null)
            {
                return;
            }

            fileWriter.Flush();
        }

        /// <summary>Records that the active scene changed and logs the event.</summary>
        public static void NotifySceneLoaded(Scene scene, LoadSceneMode mode)
        {
            string previousScene = currentScene;
            SetScene(scene.IsValid() ? scene.name : string.Empty);

            Info(
                BpfLogCategory.Scene,
                "scene.loaded",
                "scene loaded",
                null,
                BpfLogField.Of("scene", currentScene),
                BpfLogField.Of("previousScene", previousScene),
                BpfLogField.Of("mode", mode),
                BpfLogField.Of("buildIndex", scene.buildIndex),
                BpfLogField.Of("isValid", scene.IsValid()));
        }

        // -------------------------------------------------------- recent data

        /// <summary>Newest-last records matching the filter.</summary>
        public static List<BpfLogRecord> Snapshot(BpfLogLevel filterLevel, string category, string keyword, int max)
        {
            if (memoryBuffer == null)
            {
                return new List<BpfLogRecord>();
            }

            return memoryBuffer.Snapshot(filterLevel, category, keyword, max);
        }

        /// <summary>Newest-last records, newest session first, capped by max.</summary>
        public static List<BpfLogRecord> GetRecentRecords(int max)
        {
            return Snapshot(BpfLogLevel.Debug, null, null, max);
        }

        /// <summary>Drops retained records and resets the counters.</summary>
        public static void ClearRecords()
        {
            if (memoryBuffer != null)
            {
                memoryBuffer.Clear();
            }

            lock (Gate)
            {
                RateWindows.Clear();
            }

            Interlocked.Exchange(ref recordCount, 0);
            Interlocked.Exchange(ref errorCount, 0);
            Interlocked.Exchange(ref warningCount, 0);
            Interlocked.Exchange(ref rateLimitedCount, 0);
        }

        /// <summary>
        /// Writes the newest retained records to a standalone JSONL file and
        /// returns its full path, or null when nothing could be exported.
        /// </summary>
        public static string ExportRecent(int maxRecords)
        {
            if (memoryBuffer == null)
            {
                return null;
            }

            List<BpfLogRecord> records = memoryBuffer.Snapshot(BpfLogLevel.Debug, null, null, maxRecords);
            if (records.Count == 0)
            {
                return null;
            }

            try
            {
                string directory = ResolveLogDirectory();
                Directory.CreateDirectory(directory);
                string path = Path.Combine(
                    directory,
                    BpfLogOptions.FileNamePrefix +
                    "export-" +
                    DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) +
                    BpfLogOptions.FileExtension);

                StringBuilder builder = new StringBuilder(records.Count * 256);
                for (int i = 0; i < records.Count; i++)
                {
                    builder.Append(records[i].ToJsonLine());
                    builder.Append('\n');
                }

                File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
                Info(
                    BpfLogCategory.Diagnostics,
                    "log.exported",
                    "recent logs exported",
                    null,
                    BpfLogField.Of("path", path),
                    BpfLogField.Of("count", records.Count));
                return path;
            }
            catch (System.Exception exception)
            {
                Error(
                    BpfLogCategory.Diagnostics,
                    "log.export_failed",
                    "recent log export failed",
                    null,
                    BpfLogField.Of("error", exception.Message));
                return null;
            }
        }

        // -------------------------------------------------------- Unity capture

        /// <summary>
        /// Handler for <c>Application.logMessageReceivedThreaded</c>. Messages
        /// forwarded by <see cref="BpfLog"/> itself are ignored, so unified
        /// records are never captured twice.
        /// </summary>
        public static void HandleUnityLog(string condition, string stackTrace, LogType type)
        {
            if (!captureHooked || stopped || !BpfLogOptions.CaptureUnityLogs)
            {
                return;
            }

            try
            {
                if (forwardingDepth > 0)
                {
                    return;
                }

                BpfLogLevel level = LevelFor(type);
                if (level < minimumLevel)
                {
                    return;
                }

                Write(level, UnityExternalCategory, EventFor(type), condition, null, null, NoFields, true, stackTrace);
            }
            catch
            {
                // Capturing must never break the caller that produced the log.
            }
        }

        private static BpfLogLevel LevelFor(LogType type)
        {
            switch (type)
            {
                case LogType.Error:
                    return BpfLogLevel.Error;
                case LogType.Assert:
                    return BpfLogLevel.Warning;
                case LogType.Warning:
                    return BpfLogLevel.Warning;
                case LogType.Exception:
                    return BpfLogLevel.Exception;
                default:
                    return BpfLogLevel.Info;
            }
        }

        private static string EventFor(LogType type)
        {
            switch (type)
            {
                case LogType.Error:
                    return "unity.error";
                case LogType.Assert:
                    return "unity.assert";
                case LogType.Warning:
                    return "unity.warning";
                case LogType.Exception:
                    return "unity.exception";
                default:
                    return "unity.log";
            }
        }

        /// <summary>
        /// Records exceptions that no Unity log callback reported, for example
        /// exceptions thrown on a managed background thread.
        /// </summary>
        private static void HandleUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            try
            {
                System.Exception exception = args.ExceptionObject as System.Exception;
                if (exception != null)
                {
                    Exception(
                        BpfLogCategory.Diagnostics,
                        "app.unhandled_exception",
                        exception,
                        "unhandled exception",
                        null,
                        BpfLogField.Of("isTerminating", args.IsTerminating));
                }
                else
                {
                    Fatal(
                        BpfLogCategory.Diagnostics,
                        "app.unhandled_exception",
                        "unhandled exception",
                        null,
                        BpfLogField.Of("payload", args.ExceptionObject),
                        BpfLogField.Of("isTerminating", args.IsTerminating));
                }

                Flush();
            }
            catch
            {
                // Reporting an unhandled exception must never throw again.
            }
        }

        private static void HookUnhandledExceptions()
        {
            if (unhandledHooked)
            {
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
            unhandledHooked = true;
        }

        private static void UnhookUnhandledExceptions()
        {
            if (!unhandledHooked)
            {
                return;
            }

            AppDomain.CurrentDomain.UnhandledException -= HandleUnhandledException;
            unhandledHooked = false;
        }

        private static void HookUnityCapture()
        {
            if (captureHooked)
            {
                return;
            }

            Application.logMessageReceivedThreaded += HandleUnityLog;
            captureHooked = true;
        }

        private static void UnhookUnityCapture()
        {
            if (!captureHooked)
            {
                return;
            }

            Application.logMessageReceivedThreaded -= HandleUnityLog;
            captureHooked = false;
        }

        // -------------------------------------------------------- write path

        private static void Write(
            BpfLogLevel level,
            string category,
            string eventName,
            string message,
            UnityEngine.Object context,
            System.Exception exception,
            BpfLogField[] fields,
            bool captured,
            string capturedStackTrace)
        {
            if (!IsEnabled || stopped)
            {
                return;
            }

            if (level < minimumLevel)
            {
                return;
            }

            if (!IsCategoryEnabled(category))
            {
                return;
            }

            if (ShouldRateLimit(level, category, eventName, message))
            {
                return;
            }

            Emit(level, category, eventName, message, context, exception, fields, captured, capturedStackTrace);
        }

        private static void Emit(
            BpfLogLevel level,
            string category,
            string eventName,
            string message,
            UnityEngine.Object context,
            System.Exception exception,
            BpfLogField[] fields,
            bool captured,
            string capturedStackTrace)
        {
            try
            {
                if (Thread.CurrentThread.ManagedThreadId == mainThreadId)
                {
                    currentFrame = Time.frameCount;
                }

                string safeMessage = BpfLogSanitizer.Sanitize(message);
                if (string.IsNullOrEmpty(safeMessage) && exception != null)
                {
                    safeMessage = BpfLogSanitizer.Sanitize(exception.Message);
                }

                string stack = exception != null
                    ? BpfLogSanitizer.SanitizeStackTrace(exception.ToString())
                    : BpfLogSanitizer.SanitizeStackTrace(capturedStackTrace);

                if (string.IsNullOrEmpty(stack) &&
                    !captured &&
                    BpfLogOptions.CaptureStackTraceForWarnings &&
                    level >= BpfLogLevel.Warning)
                {
                    stack = BpfLogSanitizer.SanitizeStackTrace(new System.Diagnostics.StackTrace(true).ToString());
                }

                BpfLogRecord record = new BpfLogRecord(level, category, eventName, safeMessage)
                {
                    Scene = currentScene,
                    SessionId = sessionId,
                    Frame = currentFrame,
                    StackTrace = stack ?? string.Empty,
                    Fields = SanitizeFields(fields),
                    Captured = captured,
                };

                if (fileWriter != null)
                {
                    fileWriter.Enqueue(record);
                }

                if (memoryBuffer != null)
                {
                    memoryBuffer.Add(record);
                }

                Interlocked.Increment(ref recordCount);
                if (level.IsFailure())
                {
                    Interlocked.Increment(ref errorCount);
                }
                else if (level == BpfLogLevel.Warning)
                {
                    Interlocked.Increment(ref warningCount);
                }

                if (!captured)
                {
                    ForwardToUnityConsole(record, context);
                }

                if (level.IsFailure() && fileWriter != null)
                {
                    fileWriter.Flush();
                }

                Action<BpfLogRecord> handler = RecordWritten;
                if (handler != null)
                {
                    handler(record);
                }
            }
            catch (System.Exception exception2)
            {
                LastEmitError = exception2.Message;
                if (!emitFaultReported)
                {
                    emitFaultReported = true;
                    UnityEngine.Debug.LogWarning("[Bpf][Warning][Diagnostics] log.emit_failed: " + exception2.Message);
                }
            }
        }

        private static BpfLogField[] SanitizeFields(BpfLogField[] fields)
        {
            if (fields == null || fields.Length == 0)
            {
                return NoFields;
            }

            BpfLogField[] safe = new BpfLogField[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                safe[i] = new BpfLogField(fields[i].Key, BpfLogSanitizer.SanitizeField(fields[i].Value));
            }

            return safe;
        }

        private static void ForwardToUnityConsole(BpfLogRecord record, UnityEngine.Object context)
        {
            string line = record.ToConsoleLine();
            forwardingDepth++;
            try
            {
                switch (record.Level)
                {
                    case BpfLogLevel.Warning:
                        UnityEngine.Debug.LogWarning(line, context);
                        break;
                    case BpfLogLevel.Error:
                    case BpfLogLevel.Exception:
                    case BpfLogLevel.Fatal:
                        UnityEngine.Debug.LogError(line, context);
                        break;
                    default:
                        UnityEngine.Debug.Log(line, context);
                        break;
                }
            }
            finally
            {
                forwardingDepth--;
            }
        }

        // -------------------------------------------------------- rate limiting

        private sealed class RateWindow
        {
            public double WindowStart;
            public int Emitted;
            public int Suppressed;
        }

        private static bool ShouldRateLimit(BpfLogLevel level, string category, string eventName, string message)
        {
            if (!BpfLogOptions.EnableRateLimiting || level.IsFailure())
            {
                return false;
            }

            double now = Clock.Elapsed.TotalSeconds;
            string key = level.ToTag() + "|" + category + "|" + eventName + "|" + message;
            lock (Gate)
            {
                RateWindow window;
                if (!RateWindows.TryGetValue(key, out window))
                {
                    window = new RateWindow { WindowStart = now };
                    RateWindows[key] = window;
                }
                else if (now - window.WindowStart >= BpfLogOptions.RateLimitWindowSeconds)
                {
                    int suppressed = window.Suppressed;
                    window.WindowStart = now;
                    window.Emitted = 0;
                    window.Suppressed = 0;
                    if (suppressed > 0)
                    {
                        Emit(
                            BpfLogLevel.Warning,
                            category,
                            eventName + ".rate_limited",
                            "suppressed duplicate records",
                            null,
                            null,
                            new[] { BpfLogField.Of("suppressed", suppressed) },
                            false,
                            null);
                    }
                }

                if (window.Emitted >= BpfLogOptions.RateLimitMaxRecordsPerWindow)
                {
                    window.Suppressed++;
                    Interlocked.Increment(ref rateLimitedCount);
                    return true;
                }

                window.Emitted++;
                PruneRateWindows(now);
                return false;
            }
        }

        private static void PruneRateWindows(double now)
        {
            if (RateWindows.Count < 256)
            {
                return;
            }

            List<string> expired = null;
            foreach (KeyValuePair<string, RateWindow> pair in RateWindows)
            {
                if (pair.Value.Suppressed == 0 &&
                    now - pair.Value.WindowStart >= BpfLogOptions.RateLimitWindowSeconds)
                {
                    if (expired == null)
                    {
                        expired = new List<string>();
                    }

                    expired.Add(pair.Key);
                }
            }

            if (expired == null)
            {
                return;
            }

            for (int i = 0; i < expired.Count; i++)
            {
                RateWindows.Remove(expired[i]);
            }
        }

        // -------------------------------------------------------- helpers

        private static string ResolveLogDirectory()
        {
            if (!string.IsNullOrEmpty(BpfLogOptions.LogDirectoryOverride))
            {
                return BpfLogOptions.LogDirectoryOverride;
            }

            return Path.Combine(Application.persistentDataPath, BpfLogOptions.LogDirectoryName);
        }

        private static string BuildSessionId()
        {
            return DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) +
                   "-" +
                   Guid.NewGuid().ToString("N").Substring(0, 6);
        }
    }
}
