using System;

namespace Bpf.Diagnostics
{
    /// <summary>
    /// Severity of a <see cref="BpfLogRecord"/>. The numeric order is the
    /// filter order used by <see cref="BpfLog.MinimumLevel"/>.
    /// </summary>
    public enum BpfLogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Exception = 4,
        Fatal = 5,
    }

    /// <summary>Shared helpers for <see cref="BpfLogLevel"/>.</summary>
    public static class BpfLogLevelExtensions
    {
        /// <summary>Stable tag written into JSONL and the console prefix.</summary>
        public static string ToTag(this BpfLogLevel level)
        {
            switch (level)
            {
                case BpfLogLevel.Debug:
                    return "Debug";
                case BpfLogLevel.Info:
                    return "Info";
                case BpfLogLevel.Warning:
                    return "Warning";
                case BpfLogLevel.Error:
                    return "Error";
                case BpfLogLevel.Exception:
                    return "Exception";
                case BpfLogLevel.Fatal:
                    return "Fatal";
                default:
                    return "Info";
            }
        }

        /// <summary>Parses a tag produced by <see cref="ToTag"/>.</summary>
        public static bool TryParse(string value, out BpfLogLevel level)
        {
            level = BpfLogLevel.Info;
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            foreach (BpfLogLevel candidate in All)
            {
                if (string.Equals(candidate.ToTag(), value, StringComparison.OrdinalIgnoreCase))
                {
                    level = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>True for Error, Exception and Fatal.</summary>
        public static bool IsFailure(this BpfLogLevel level)
        {
            return level >= BpfLogLevel.Error;
        }

        /// <summary>All levels from least to most severe.</summary>
        public static readonly BpfLogLevel[] All =
        {
            BpfLogLevel.Debug,
            BpfLogLevel.Info,
            BpfLogLevel.Warning,
            BpfLogLevel.Error,
            BpfLogLevel.Exception,
            BpfLogLevel.Fatal,
        };
    }
}
