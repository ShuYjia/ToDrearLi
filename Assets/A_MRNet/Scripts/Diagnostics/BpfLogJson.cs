using System.Text;

namespace Bpf.Diagnostics
{
    /// <summary>
    /// Minimal JSONL writer used by the BPF log. Hand-rolled so the log layer
    /// has no dependency on the Unity serializer and can run on any thread.
    /// </summary>
    public static class BpfLogJson
    {
        /// <summary>Appends <c>"name":</c> (with a leading comma when needed).</summary>
        public static void AppendName(StringBuilder builder, string name, ref bool first)
        {
            if (!first)
            {
                builder.Append(',');
            }

            first = false;
            AppendString(builder, name);
            builder.Append(':');
        }

        /// <summary>Appends a quoted, escaped JSON string.</summary>
        public static void AppendString(StringBuilder builder, string value)
        {
            builder.Append('"');
            AppendEscaped(builder, value);
            builder.Append('"');
        }

        /// <summary>Appends the escaped body of a JSON string (no quotes).</summary>
        public static void AppendEscaped(StringBuilder builder, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                switch (character)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (character < ' ')
                        {
                            builder.Append("\\u");
                            builder.Append(((int)character).ToString("x4"));
                        }
                        else
                        {
                            builder.Append(character);
                        }

                        break;
                }
            }
        }

        /// <summary>Appends a raw JSON literal such as a number or boolean.</summary>
        public static void AppendLiteral(StringBuilder builder, string literal)
        {
            builder.Append(string.IsNullOrEmpty(literal) ? "null" : literal);
        }
    }
}
