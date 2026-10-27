namespace Bpf.Diagnostics
{
    /// <summary>
    /// Category names used by the BPF unified log. Categories are free-form
    /// strings so business code can add one without touching this type, but
    /// these are the documented values used by the current BPF mainline.
    /// </summary>
    public static class BpfLogCategory
    {
        public const string Startup = "Startup";
        public const string Scene = "Scene";
        public const string TexCore = "TexCore";
        public const string UI = "UI";
        public const string XR = "XR";
        public const string Input = "Input";
        public const string Network = "Network";
        public const string Asset = "Asset";
        public const string Performance = "Performance";
        public const string Diagnostics = "Diagnostics";

        /// <summary>
        /// Category assigned to Unity console messages that were not produced
        /// by <see cref="BpfLog"/> itself.
        /// </summary>
        public const string UnityExternal = "unity.external";

        /// <summary>Value used by the panel category filter to mean "any".</summary>
        public const string All = "";

        /// <summary>Documented category list, used by filters and validation.</summary>
        public static readonly string[] Documented =
        {
            Startup,
            Scene,
            TexCore,
            UI,
            XR,
            Input,
            Network,
            Asset,
            Performance,
            Diagnostics,
            UnityExternal,
        };
    }
}
