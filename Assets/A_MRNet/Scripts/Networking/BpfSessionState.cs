using Unity.Netcode;
using UnityEngine;

namespace Bpf.Networking
{
    /// <summary>
    /// 会话权威状态（阶段 0 最小桩，阶段 2 充实校准与业务镜像）。
    ///
    /// 挂在 `Resources/Networking/BpfSessionRoot` 预制体上，
    /// 由 Host 在 StartHost 时生成（destroyWithScene=false 跨场景常驻）。
    /// 唯一写者是 Host；Client 只读 NetworkVariable。
    /// </summary>
    /// <summary>
    /// 会话权威状态（Host 写，全员读）。挂在 Resources/Networking/BpfSessionRoot 上。
    ///
    /// 阶段 2 起成为会话阶段的唯一权威源：Offline → Lobby → Calibrating → Playing。
    /// 校准组件（BpfSessionCalibration）负责推动校准与完成。
    /// </summary>
    [DisallowMultipleComponent]
    public class BpfSessionState : NetworkBehaviour
    {
        /// <summary>NGO 的 NetworkVariable&lt;T&gt; 要求 T : unmanaged，枚举不满足，
        /// 因此用 int 承载，对外暴露枚举。</summary>
        private readonly NetworkVariable<int> phaseValue = new NetworkVariable<int>(
            (int)BpfSessionPhase.Lobby, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<bool> calibrationDone = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public static BpfSessionState Instance { get; private set; }

        /// <summary>权威会话阶段（Host 写，全员读）。</summary>
        public BpfSessionPhase Phase => (BpfSessionPhase)phaseValue.Value;

        /// <summary>三点校准是否完成（未完成时业务交互应被拒）。</summary>
        public bool CalibrationDone => calibrationDone.Value;

        private BpfSessionCalibration calibration;

        public override void OnNetworkSpawn()
        {
            Instance = this;
            phaseValue.OnValueChanged += HandlePhaseChanged;
            calibration = GetComponent<BpfSessionCalibration>();
            if (calibration == null)
            {
                calibration = gameObject.AddComponent<BpfSessionCalibration>();
            }

            BpfNetLog.Info("Bpf|Session", "spawned",
                "会话状态对象已同步（阶段=" + Phase + "，isServer=" + IsServer + "）");
        }

        public override void OnNetworkDespawn()
        {
            phaseValue.OnValueChanged -= HandlePhaseChanged;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Host 专用：推进会话阶段。</summary>
        public void ServerSetPhase(BpfSessionPhase next)
        {
            if (!IsServer)
            {
                BpfNetLog.Warning("Bpf|Session", "write_denied", "只有 Host 可以修改会话阶段");
                return;
            }

            phaseValue.Value = (int)next;
            calibrationDone.Value = next == BpfSessionPhase.Playing;
        }

        /// <summary>取校准组件（大厅/校准流程用）。</summary>
        public BpfSessionCalibration Calibration => calibration;

        private void HandlePhaseChanged(int previous, int current)
        {
            BpfNetLog.Info("Bpf|Session", "phase_synced",
                (BpfSessionPhase)previous + " -> " + (BpfSessionPhase)current);

            // 会话阶段的唯一权威在本组件（NetworkVariable）。把变化镜像回网络根，
            // 让大厅 UI、连接审批（BpfConnectionApproval）与业务桥接
            // （BpfTexCoreNetworkBridge）等既有消费方读到真实阶段，
            // 否则校准完成后它们仍停留在 Lobby，业务点击会被误拦。
            if (BpfNetworkBootstrap.Instance != null)
            {
                BpfNetworkBootstrap.Instance.SetSessionPhase((BpfSessionPhase)current);
            }
        }
    }
}
