namespace Bpf.Diagnostics
{
    /// <summary>
    /// Static tuning values for the BPF unified log. Values are intentionally
    /// plain static fields so tests and device debugging can adjust them
    /// without rebuilding a configuration asset.
    /// </summary>
    public static class BpfLogOptions
    {
        /// <summary>Folder created below <c>Application.persistentDataPath</c>.</summary>
        public const string LogDirectoryName = "BpfLogs";

        /// <summary>File name prefix of every session log file.</summary>
        public const string FileNamePrefix = "bpf-";

        /// <summary>File extension of every session log file.</summary>
        public const string FileExtension = ".jsonl";

        /// <summary>
        /// True in the Unity Editor and in development players. Controls the
        /// default level, the memory buffer and the diagnostics overlay.
        /// </summary>
        public static readonly bool IsDevelopmentBuild =
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            true;
#else
            false;
#endif

        /// <summary>Default level filter for development players.</summary>
        public const BpfLogLevel DevelopmentMinimumLevel = BpfLogLevel.Debug;

        /// <summary>Default level filter for release players.</summary>
        public const BpfLogLevel ReleaseMinimumLevel = BpfLogLevel.Warning;

        /// <summary>Maximum sanitized message length before truncation.</summary>
        public static int MaxMessageCharacters = 4000;

        /// <summary>Maximum sanitized field value length before truncation.</summary>
        public static int MaxFieldCharacters = 512;

        /// <summary>Maximum sanitized stack trace length before truncation.</summary>
        public static int MaxStackTraceCharacters = 4000;

        /// <summary>Rotate to the next part file once a file exceeds this size.</summary>
        public static long MaxFileBytes = 5L * 1024L * 1024L;

        /// <summary>Number of newest session files kept on disk.</summary>
        public static int MaxRetainedFiles = 10;

        /// <summary>Retained records in the in-memory diagnostics buffer.</summary>
        public static int MemoryBufferCapacity = 2000;

        /// <summary>Capture Unity console messages as <c>unity.external</c>.</summary>
        public static bool CaptureUnityLogs = true;

        /// <summary>Suppress repeated identical records inside a time window.</summary>
        public static bool EnableRateLimiting = true;

        /// <summary>Length of the repeated-record window.</summary>
        public static double RateLimitWindowSeconds = 1.0;

        /// <summary>Duplicates per key allowed inside one window.</summary>
        public static int RateLimitMaxRecordsPerWindow = 5;

        /// <summary>Seconds between automatic queue drains.</summary>
        public static float FlushIntervalSeconds = 0.5f;

        /// <summary>Records kept for the diagnostics overlay.</summary>
        public static int OverlayRetainedRecords = 300;

        /// <summary>Fixed Editor hotkey used to toggle the diagnostics overlay.</summary>
        public const string OverlayToggleKey = "F9";

        /// <summary>Input System binding for the overlay toggle (Editor).</summary>
        public const string OverlayToggleBinding = "<Keyboard>/f9";

        /// <summary>Input System binding for the overlay toggle (PICO device).</summary>
        public const string OverlayControllerToggleBinding = "<XRController>{LeftHand}/menuButton";

        /// <summary>Overrides the log directory. Used by tests; null in normal runs.</summary>
        public static string LogDirectoryOverride;

        /// <summary>Captures caller stack traces for Warning and above in dev builds.</summary>
        public static bool CaptureStackTraceForWarnings = true;

        /// <summary>The level filter a fresh session starts with.</summary>
        public static BpfLogLevel DefaultMinimumLevel
        {
            get { return IsDevelopmentBuild ? DevelopmentMinimumLevel : ReleaseMinimumLevel; }
        }
    }
}
