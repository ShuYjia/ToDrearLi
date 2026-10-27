using System.Collections.Generic;

namespace Bpf.Diagnostics
{
    /// <summary>
    /// Filter, pause, clear and export state shared by the Editor overlay and
    /// the in-device development panel. Keeping the behaviour here means both
    /// views show exactly the same records.
    /// </summary>
    public sealed class BpfDiagnosticsPanel
    {
        private BpfLogRecord[] frozen;

        public BpfDiagnosticsPanel()
        {
            MinimumLevel = BpfLogLevel.Debug;
            Category = BpfLogCategory.All;
            Keyword = string.Empty;
            MaxRecords = BpfLogOptions.OverlayRetainedRecords;
        }

        /// <summary>Lowest level shown by the panel.</summary>
        public BpfLogLevel MinimumLevel { get; private set; }

        /// <summary>Category filter, or <see cref="BpfLogCategory.All"/>.</summary>
        public string Category { get; private set; }

        /// <summary>Case-insensitive keyword filter.</summary>
        public string Keyword { get; private set; }

        /// <summary>True while the visible list is frozen.</summary>
        public bool IsPaused { get; private set; }

        /// <summary>Maximum records returned by <see cref="GetRecords"/>.</summary>
        public int MaxRecords { get; set; }

        public long TotalRecords
        {
            get { return BpfLog.RecordCount; }
        }

        public long ErrorCount
        {
            get { return BpfLog.ErrorCount; }
        }

        public long WarningCount
        {
            get { return BpfLog.WarningCount; }
        }

        public long RateLimitedCount
        {
            get { return BpfLog.RateLimitedRecordCount; }
        }

        /// <summary>Path of the JSONL session file, shown by both views.</summary>
        public string LogFilePath
        {
            get { return BpfLog.CurrentLogFilePath; }
        }

        public string LogDirectory
        {
            get { return BpfLog.LogDirectory; }
        }

        public void SetLevelFilter(BpfLogLevel level)
        {
            MinimumLevel = level;
        }

        /// <summary>Moves the level filter by one step, used by device input.</summary>
        public void CycleLevelFilter(int direction)
        {
            BpfLogLevel[] levels = BpfLogLevelExtensions.All;
            int index = 0;
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] == MinimumLevel)
                {
                    index = i;
                    break;
                }
            }

            index = (index + (direction >= 0 ? 1 : levels.Length - 1)) % levels.Length;
            MinimumLevel = levels[index];
        }

        public void SetCategory(string category)
        {
            Category = category ?? BpfLogCategory.All;
        }

        /// <summary>Moves the category filter by one step, used by device input.</summary>
        public void CycleCategory(int direction)
        {
            string[] categories = CategoryOptions;
            int index = 0;
            for (int i = 0; i < categories.Length; i++)
            {
                if (string.Equals(categories[i], Category, System.StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }

            index = (index + (direction >= 0 ? 1 : categories.Length - 1)) % categories.Length;
            Category = categories[index];
        }

        public void SetKeyword(string keyword)
        {
            Keyword = keyword ?? string.Empty;
        }

        /// <summary>Freezes or resumes the visible list.</summary>
        public void SetPaused(bool paused)
        {
            if (paused == IsPaused)
            {
                return;
            }

            IsPaused = paused;
            frozen = paused ? BpfLog.Snapshot(BpfLogLevel.Debug, null, null, MaxRecords).ToArray() : null;
        }

        public void TogglePause()
        {
            SetPaused(!IsPaused);
        }

        /// <summary>Clears the retained records and every counter.</summary>
        public void Clear()
        {
            frozen = null;
            IsPaused = false;
            BpfLog.ClearRecords();
        }

        /// <summary>Exports the newest retained records and returns the file path.</summary>
        public string ExportRecent()
        {
            return BpfLog.ExportRecent(MaxRecords);
        }

        /// <summary>Records currently shown by the panel, oldest first.</summary>
        public List<BpfLogRecord> GetRecords()
        {
            if (!IsPaused || frozen == null)
            {
                return BpfLog.Snapshot(MinimumLevel, Category, Keyword, MaxRecords);
            }

            List<BpfLogRecord> matches = new List<BpfLogRecord>(frozen.Length);
            for (int i = 0; i < frozen.Length; i++)
            {
                if (frozen[i] != null && frozen[i].Matches(MinimumLevel, Category, Keyword))
                {
                    matches.Add(frozen[i]);
                }
            }

            if (MaxRecords > 0 && matches.Count > MaxRecords)
            {
                matches.RemoveRange(0, matches.Count - MaxRecords);
            }

            return matches;
        }

        /// <summary>Category values cycled by the device panel filter.</summary>
        public static readonly string[] CategoryOptions = BuildCategoryOptions();

        private static string[] BuildCategoryOptions()
        {
            string[] options = new string[BpfLogCategory.Documented.Length + 1];
            options[0] = BpfLogCategory.All;
            for (int i = 0; i < BpfLogCategory.Documented.Length; i++)
            {
                options[i + 1] = BpfLogCategory.Documented[i];
            }

            return options;
        }
    }
}
