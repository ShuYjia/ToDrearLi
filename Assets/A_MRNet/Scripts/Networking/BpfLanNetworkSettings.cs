using Bpf.Diagnostics;
using UnityEngine;

namespace Bpf.Networking
{
    /// <summary>
    /// 局域网多人配置资产。
    ///
    /// 资产文件位置（Unity 打开后由 execute_code 或手动创建）：
    ///   `Assets/A_MRNet/Networking/Config/BpfLanNetworkSettings.asset`
    /// 同时复制/软链一份到 `Assets/Resources/Networking/BpfLanNetworkSettings.asset`，
    /// 供运行期生成的网络根在无场景引用时解析（见 <see cref="Resolve"/>）。
    /// </summary>
    [CreateAssetMenu(menuName = "BPF/Networking/LAN Settings", fileName = "BpfLanNetworkSettings")]
    public class BpfLanNetworkSettings : ScriptableObject
    {
        [Header("房间")]
        [Tooltip("最大玩家数（含 Host）。NGO 1.5.2 的 NetworkConfig 无人数字段，上限在连接审批里校验。")]
        public int maxPlayers = 4;

        [Tooltip("首个业务场景名（与 TexCore_LoadScene.BusinessSceneName 保持一致）。")]
        public string businessSceneName = "03SingleplayerScene";

        [Header("端口")]
        [Tooltip("NGO / UnityTransport 游戏端口。")]
        public ushort gameplayPort = 7777;

        [Tooltip("UDP 房间发现端口。")]
        public ushort discoveryPort = 47777;

        [Header("版本")]
        [Tooltip("协议版本。载荷结构变更时必须 +1，旧客户端将被拒绝。")]
        public int protocolVersion = 1;

        public string appVersion = "1.0.0";
        public string contentVersion = "1.0.0";

        [Header("同步频率")]
        [Tooltip("姿态同步频率（Hz），不可靠传输。")]
        public float poseSyncHz = 25f;

        [Tooltip("Host 权威状态镜像推送周期（秒）。")]
        public float stateMirrorInterval = 0.5f;

        [Header("超时")]
        [Tooltip("加入房间超时（秒）：StartClient 后这么久没连上就报错回退。")]
        public float joinTimeoutSeconds = 15f;

        [Tooltip("UDP 扫描窗口（秒）。")]
        public float discoveryScanSeconds = 5f;

        [Tooltip("Host 房间广播周期（秒）。")]
        public float discoveryBroadcastSeconds = 1f;

        /// <summary>最近一次解析生效的实例，供 Describe() 等无实例上下文使用。</summary>
        public static BpfLanNetworkSettings Active { get; private set; }

        /// <summary>
        /// 解析生效配置：优先用场景/预制体里序列化引用的实例；
        /// 否则尝试 `Resources/Networking/BpfLanNetworkSettings`；
        /// 再否则创建纯运行时默认值实例（不落盘，仅保证流程可跑并告警）。
        /// </summary>
        public static BpfLanNetworkSettings Resolve(BpfLanNetworkSettings configured)
        {
            if (configured != null)
            {
                Active = configured;
                return configured;
            }

            BpfLanNetworkSettings fromResources =
                Resources.Load<BpfLanNetworkSettings>("Networking/BpfLanNetworkSettings");
            if (fromResources != null)
            {
                Active = fromResources;
                BpfNetLog.Warning("Settings", "resolved_from_resources",
                    "网络根未配置 Settings，已从 Resources/Networking 解析默认资产");
                return fromResources;
            }

            BpfLanNetworkSettings runtime = CreateInstance<BpfLanNetworkSettings>();
            runtime.name = "BpfLanNetworkSettings(runtime-default)";
            Active = runtime;
            BpfNetLog.Warning("Settings", "resolved_runtime_default",
                "未找到配置资产与 Resources 副本，使用运行时默认值（端口 7777/47777，协议 1）");
            return runtime;
        }

        /// <summary>配置摘要，用于日志与大厅状态栏。</summary>
        public string Describe()
        {
            return "LAN max=" + maxPlayers + " port=" + gameplayPort
                + "/" + discoveryPort + " proto=" + protocolVersion
                + " app=" + appVersion + " content=" + contentVersion
                + " pose=" + poseSyncHz + "Hz mirror=" + stateMirrorInterval + "s";
        }

        private void OnEnable()
        {
            Active = this;
        }
    }
}
