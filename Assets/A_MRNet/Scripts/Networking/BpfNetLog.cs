using Bpf.Diagnostics;
using UnityEngine;

namespace Bpf.Networking
{
    /// <summary>
    /// 联机模块统一日志门面。
    ///
    /// 全部转发到工程统一日志 <see cref="BpfLog"/>（category = Network），
    /// 事件名统一带 `Bpf|` 前缀，便于在 logcat / Console 里按前缀过滤：
    ///   `adb logcat -v time | Select-String "Bpf|Netcode|UnityTransport"`
    ///
    /// 网络线程禁止调用本类的任何方法（BpfLog 会碰 Unity API），
    /// 网络线程只允许把文本塞进队列，由主线程消费后调用。
    /// </summary>
    public static class BpfNetLog
    {
        private const string Category = BpfLogCategory.Network;

        public static void Info(string tag, string code, string message = null)
        {
            BpfLog.Info(Category, BuildEventName(tag, code), message);
        }

        public static void Warning(string tag, string code, string message = null)
        {
            BpfLog.Warning(Category, BuildEventName(tag, code), message);
        }

        public static void Error(string tag, string code, string message = null)
        {
            BpfLog.Error(Category, BuildEventName(tag, code), message);
        }

        /// <summary>事件名格式：`Bpf|&lt;tag&gt;.&lt;code&gt;`，例如 `Bpf|Bootstrap.phase_changed`。</summary>
        private static string BuildEventName(string tag, string code)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return "Bpf|" + code;
            }

            return "Bpf|" + tag + "." + code;
        }

        /// <summary>Unity Console 直出（调试期临时用，正式路径走上面三个方法）。</summary>
        public static void Raw(string tag, string message)
        {
            Debug.Log("[" + tag + "] " + message);
        }
    }
}
