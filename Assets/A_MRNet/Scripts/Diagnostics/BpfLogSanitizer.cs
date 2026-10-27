using System.Text.RegularExpressions;

namespace Bpf.Diagnostics
{
    /// <summary>
    /// Applies the default BPF log redaction rules and length limits. The same
    /// sanitizer runs for direct <see cref="BpfLog"/> calls, captured Unity
    /// messages and structured fields, so no sink can persist a secret.
    /// </summary>
    public static class BpfLogSanitizer
    {
        /// <summary>Replacement text for a masked value.</summary>
        public const string Mask = "***";

        /// <summary>Prefix used when a value is cut to the configured limit.</summary>
        public const string TruncationMarker = "...[truncated ";

        private const RegexOptions Options = RegexOptions.CultureInvariant;

        private const RegexOptions OptionsIgnoreCase =
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;

        /// <summary>
        /// Masks <c>key=value</c>, <c>key: value</c> and JSON
        /// <c>"key":"value"</c> pairs for token, password, authorization,
        /// cookie, API key, session and user identifier names.
        /// </summary>
        private static readonly Regex SecretAssignment = new Regex(
            @"(?<key>""?\b(?:access[_-]?token|refresh[_-]?token|id[_-]?token|token|password|passwd|pwd|secret|app[_-]?secret|client[_-]?secret|authorization|auth|cookie|set[_-]?cookie|api[_-]?key|apikey|session[_-]?id|user[_-]?id|userid|openid|unionid|phone|mobile|email|device[_-]?id)\b""?)(?<separator>\s*[:=]\s*)(?<value>""[^""]*""|'[^']*'|(?:Bearer|Basic)\s+\S+|[^,;\s&}\]]+)",
            OptionsIgnoreCase);

        /// <summary>Masks <c>Bearer</c> and <c>Basic</c> authorization headers.</summary>
        private static readonly Regex AuthorizationScheme = new Regex(
            @"(?<scheme>\b(?:Bearer|Basic))\s+[A-Za-z0-9\-._~+/=]{8,}",
            OptionsIgnoreCase);

        /// <summary>Masks signature and credential URL query parameters.</summary>
        private static readonly Regex QueryCredential = new Regex(
            @"(?<prefix>[?&](?:token|access_token|refresh_token|sign|signature|key|apikey|password|pwd|ticket|auth|sessionid)=)[^&#\s]+",
            OptionsIgnoreCase);

        /// <summary>Masks JWT style blobs.</summary>
        private static readonly Regex JsonWebToken = new Regex(
            @"\beyJ[A-Za-z0-9_\-]{6,}\.[A-Za-z0-9_\-]{6,}\.[A-Za-z0-9_\-]{2,}\b",
            Options);

        /// <summary>Replaces long raw request or response bodies with a mask.</summary>
        private static readonly Regex RawBody = new Regex(
            @"(?<prefix>\b(?:request|response|body|payload|content)\b\s*[:=]\s*)[^\s]{64,}",
            OptionsIgnoreCase);

        /// <summary>Sanitizes a message and applies the message length limit.</summary>
        public static string Sanitize(string value)
        {
            return Sanitize(value, BpfLogOptions.MaxMessageCharacters);
        }

        /// <summary>Sanitizes a field value and applies the field length limit.</summary>
        public static string SanitizeField(string value)
        {
            return Sanitize(value, BpfLogOptions.MaxFieldCharacters);
        }

        /// <summary>Sanitizes a stack trace and applies the stack length limit.</summary>
        public static string SanitizeStackTrace(string value)
        {
            return Sanitize(value, BpfLogOptions.MaxStackTraceCharacters);
        }

        /// <summary>Applies every masking rule, then truncates to the limit.</summary>
        public static string Sanitize(string value, int maxCharacters)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            string result = SecretAssignment.Replace(value, MaskSecretAssignment);
            result = AuthorizationScheme.Replace(result, match => match.Groups["scheme"].Value + " " + Mask);
            result = QueryCredential.Replace(result, match => match.Groups["prefix"].Value + Mask);
            result = JsonWebToken.Replace(result, Mask);
            result = RawBody.Replace(result, match => match.Groups["prefix"].Value + Mask);
            return Truncate(result, maxCharacters);
        }

        /// <summary>
        /// Masks the value of a secret assignment while keeping its key, the
        /// separator and any surrounding quotes readable.
        /// </summary>
        private static string MaskSecretAssignment(Match match)
        {
            string key = match.Groups["key"].Value;
            string separator = match.Groups["separator"].Value;
            string value = match.Groups["value"].Value;
            bool quoted = value.Length > 1 && (value[0] == '"' || value[0] == '\'');
            if (quoted)
            {
                char quote = value[0];
                return key + separator + quote + Mask + quote;
            }

            return key + separator + Mask;
        }

        /// <summary>Cuts a value to the limit and records how much was removed.</summary>
        public static string Truncate(string value, int maxCharacters)
        {
            if (string.IsNullOrEmpty(value) || maxCharacters <= 0 || value.Length <= maxCharacters)
            {
                return value;
            }

            return value.Substring(0, maxCharacters) +
                   TruncationMarker +
                   (value.Length - maxCharacters) +
                   " chars]";
        }

        /// <summary>True when the value contains a mask produced by this type.</summary>
        public static bool ContainsMask(string value)
        {
            return !string.IsNullOrEmpty(value) && value.Contains(Mask);
        }
    }
}
