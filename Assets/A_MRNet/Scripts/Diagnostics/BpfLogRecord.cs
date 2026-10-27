using System;
using System.Globalization;
using System.Text;

namespace Bpf.Diagnostics
{
    /// <summary>
    /// One structured BPF log entry. Every record always carries the fixed
    /// fields <c>timestamp</c>, <c>level</c>, <c>category</c>, <c>event</c>,
    /// <c>message</c>, <c>scene</c>, <c>sessionId</c>, <c>frame</c> and
    /// <c>stackTrace</c>, plus an optional <c>fields</c> object.
    /// </summary>
    public sealed class BpfLogRecord
    {
        private static readonly BpfLogField[] NoFields = new BpfLogField[0];

        public BpfLogRecord(BpfLogLevel level, string category, string eventName, string message)
        {
            TimestampUtc = DateTime.UtcNow;
            Level = level;
            Category = category ?? string.Empty;
            Event = eventName ?? string.Empty;
            Message = message ?? string.Empty;
            Scene = string.Empty;
            SessionId = string.Empty;
            StackTrace = string.Empty;
            Fields = NoFields;
        }

        /// <summary>UTC creation time.</summary>
        public DateTime TimestampUtc { get; set; }

        public BpfLogLevel Level { get; set; }

        public string Category { get; set; }

        /// <summary>Stable dotted event name, for example <c>scene.loaded</c>.</summary>
        public string Event { get; set; }

        public string Message { get; set; }

        /// <summary>Active scene name at the time the record was created.</summary>
        public string Scene { get; set; }

        /// <summary>Identifier shared by every record of one application session.</summary>
        public string SessionId { get; set; }

        /// <summary>Unity frame number.</summary>
        public int Frame { get; set; }

        /// <summary>Exception or captured Unity stack trace (may be empty).</summary>
        public string StackTrace { get; set; }

        /// <summary>Optional structured fields.</summary>
        public BpfLogField[] Fields { get; set; }

        /// <summary>
        /// True when the record was produced by capturing a Unity console
        /// message instead of a direct <see cref="BpfLog"/> call.
        /// </summary>
        public bool Captured { get; set; }

        /// <summary>UTC timestamp in round-trip format with a Z suffix.</summary>
        public string TimestampText
        {
            get { return TimestampUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture); }
        }

        /// <summary>One JSON object without a trailing newline.</summary>
        public string ToJsonLine()
        {
            StringBuilder builder = new StringBuilder(256);
            bool first = true;
            builder.Append('{');

            BpfLogJson.AppendName(builder, "timestamp", ref first);
            BpfLogJson.AppendString(builder, TimestampText);

            BpfLogJson.AppendName(builder, "level", ref first);
            BpfLogJson.AppendString(builder, Level.ToTag());

            BpfLogJson.AppendName(builder, "category", ref first);
            BpfLogJson.AppendString(builder, Category);

            BpfLogJson.AppendName(builder, "event", ref first);
            BpfLogJson.AppendString(builder, Event);

            BpfLogJson.AppendName(builder, "message", ref first);
            BpfLogJson.AppendString(builder, Message);

            BpfLogJson.AppendName(builder, "scene", ref first);
            BpfLogJson.AppendString(builder, Scene);

            BpfLogJson.AppendName(builder, "sessionId", ref first);
            BpfLogJson.AppendString(builder, SessionId);

            BpfLogJson.AppendName(builder, "frame", ref first);
            BpfLogJson.AppendLiteral(builder, Frame.ToString(CultureInfo.InvariantCulture));

            BpfLogJson.AppendName(builder, "stackTrace", ref first);
            BpfLogJson.AppendString(builder, StackTrace);

            BpfLogJson.AppendName(builder, "fields", ref first);
            AppendFields(builder);

            BpfLogJson.AppendName(builder, "captured", ref first);
            BpfLogJson.AppendLiteral(builder, Captured ? "true" : "false");

            builder.Append('}');
            return builder.ToString();
        }

        /// <summary>Single-line console form that keeps the category visible.</summary>
        public string ToConsoleLine()
        {
            StringBuilder builder = new StringBuilder(192);
            builder.Append("[Bpf][");
            builder.Append(Level.ToTag());
            builder.Append("][");
            builder.Append(Category);
            builder.Append("] ");

            if (!string.IsNullOrEmpty(Event))
            {
                builder.Append(Event);
                if (!string.IsNullOrEmpty(Message))
                {
                    builder.Append(": ");
                }
            }

            builder.Append(Message);

            if (Fields != null && Fields.Length > 0)
            {
                builder.Append(" {");
                for (int i = 0; i < Fields.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(Fields[i].Key);
                    builder.Append('=');
                    builder.Append(Fields[i].Value);
                }

                builder.Append('}');
            }

            return builder.ToString();
        }

        /// <summary>True when the record passes the given filter.</summary>
        public bool Matches(BpfLogLevel minimumLevel, string category, string keyword)
        {
            if (Level < minimumLevel)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(category) &&
                !string.Equals(Category, category, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.IsNullOrEmpty(keyword))
            {
                return true;
            }

            if (Contains(Message, keyword) ||
                Contains(Event, keyword) ||
                Contains(Category, keyword) ||
                Contains(Level.ToTag(), keyword))
            {
                return true;
            }

            if (Fields != null)
            {
                for (int i = 0; i < Fields.Length; i++)
                {
                    if (Contains(Fields[i].Key, keyword) || Contains(Fields[i].Value, keyword))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool Contains(string value, string keyword)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void AppendFields(StringBuilder builder)
        {
            builder.Append('{');
            if (Fields != null)
            {
                for (int i = 0; i < Fields.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(',');
                    }

                    BpfLogJson.AppendString(builder, Fields[i].Key);
                    builder.Append(':');
                    BpfLogJson.AppendString(builder, Fields[i].Value);
                }
            }

            builder.Append('}');
        }
    }
}
