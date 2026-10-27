using System.Globalization;

namespace Bpf.Diagnostics
{
    /// <summary>
    /// One structured key/value pair attached to a <see cref="BpfLogRecord"/>.
    /// Values are sanitized and length-limited by <see cref="BpfLogSanitizer"/>.
    /// </summary>
    public readonly struct BpfLogField
    {
        public readonly string Key;
        public readonly string Value;

        public BpfLogField(string key, string value)
        {
            Key = string.IsNullOrEmpty(key) ? "field" : key;
            Value = value;
        }

        /// <summary>Creates a field, converting the value to invariant text.</summary>
        public static BpfLogField Of(string key, object value)
        {
            return new BpfLogField(key, ToText(value));
        }

        /// <summary>Invariant, log-friendly text for any value.</summary>
        public static string ToText(object value)
        {
            if (value == null)
            {
                return "null";
            }

            switch (value)
            {
                case string text:
                    return text;
                case bool flag:
                    return flag ? "true" : "false";
                case float single:
                    return single.ToString("0.###", CultureInfo.InvariantCulture);
                case double number:
                    return number.ToString("0.###", CultureInfo.InvariantCulture);
                case System.IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return value.ToString();
            }
        }
    }
}
