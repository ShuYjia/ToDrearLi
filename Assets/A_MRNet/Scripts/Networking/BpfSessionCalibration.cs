using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Bpf.Networking
{
    /// <summary>
    /// 三点校准流程（挂在 BpfSessionRoot 上，Host 权威）。
    ///
    /// 流程：
    ///   1. Host 点"开始校准" → 阶段切到 Calibrating，清空已上报数据；
    ///   2. Host 先采集三个物理标记点（这三个点的 Host 本机坐标即"会话空间"基准）；
    ///   3. 其余各端依次触摸同样的三个物理点，上报自己的本机坐标；
    ///   4. Host 收齐后为每一端解出「本机空间 → 会话空间」刚体变换并广播；
    ///   5. 全员拿到自己的变换 → 阶段切到 Playing。
    ///
    /// 校准完成前，业务交互请求应被拒（阶段 3 的校验会读 SessionPhase）。
    /// </summary>
    [DisallowMultipleComponent]
    public class BpfSessionCalibration : NetworkBehaviour
    {
        private const string LogTag = "Bpf|Calibration";

        /// <summary>本机 → 会话空间的变换（校准完成后有效；未校准按单位变换处理）。</summary>
        public static BpfSessionTransform LocalTransform { get; private set; }

        /// <summary>UI 提示：需要采集第几个点（1..3），0 表示无需采集。</summary>
        public static event System.Action<int> PointHint;

        /// <summary>校准阶段变化（大厅 UI 订阅）。</summary>
        public static event System.Action<BpfSessionPhase> PhaseChanged;

        /// <summary>
        /// Host 权威判断。不能用组件自身的 IsServer：本组件若在生成之后才
        /// AddComponent 上去，NGO 不会把它算作已生成的网络行为（IsSpawned=false、
        /// IsServer=false）。统一以 NetworkManager 为准。
        /// </summary>
        private bool HasAuthority
        {
            get
            {
                if (IsSpawned && IsServer)
                {
                    return true;
                }

                NetworkManager manager = NetworkObject != null && NetworkObject.NetworkManager != null
                    ? NetworkObject.NetworkManager
                    : NetworkManager.Singleton;
                return manager != null && manager.IsServer;
            }
        }

        // Host 侧收集表
        private readonly Dictionary<ulong, Vector3[]> reports = new Dictionary<ulong, Vector3[]>();
        private Vector3[] sessionAnchors;

        private BpfSessionState sessionState;

        private void Awake()
        {
            sessionState = GetComponent<BpfSessionState>();
        }

        public override void OnNetworkSpawn()
        {
            reports.Clear();
            LocalTransform = default;
            BpfNetLog.Info(LogTag, "spawned", "校准组件已同步（isServer=" + IsServer + "）");
        }

        // ------------------------------------------------------------ 流程

        /// <summary>Host 专用：进入校准阶段。</summary>
        public void BeginCalibration()
        {
            if (!HasAuthority)
            {
                BpfNetLog.Warning(LogTag, "begin_denied", "只有 Host 可以开始校准");
                return;
            }

            reports.Clear();
            sessionAnchors = null;

            if (sessionState != null)
            {
                sessionState.ServerSetPhase(BpfSessionPhase.Calibrating);
            }

            RaisePhaseChanged(BpfSessionPhase.Calibrating);
            RaisePointHint(1);
            BpfNetLog.Info(LogTag, "begin", "已进入校准阶段：Host 先采集三个标记点");
        }

        /// <summary>
        /// 各端（含 Host）上报自己采集的三个物理点（本机空间坐标）。
        /// 不在校准阶段或数量不对时直接拒绝。
        /// </summary>
        public void SubmitLocalPoints(Vector3 p0, Vector3 p1, Vector3 p2)
        {
            if (NetworkManager == null || !NetworkManager.IsListening)
            {
                return;
            }

            CustomMessagingManager messaging = NetworkManager.CustomMessagingManager;
            if (messaging == null)
            {
                return;
            }

            BpfCalibrationReportPayload payload = new BpfCalibrationReportPayload
            {
                clientId = NetworkManager.LocalClientId,
                p0x = p0.x, p0y = p0.y, p0z = p0.z,
                p1x = p1.x, p1y = p1.y, p1z = p1.z,
                p2x = p2.x, p2y = p2.y, p2z = p2.z,
            };

            try
            {
                using (FastBufferWriter writer = new FastBufferWriter(128, Allocator.Temp))
                {
                    writer.WriteNetworkSerializable(payload);
                    // 各端上报只到 Host（NGO 客户端不能群发），见 BpfNetSend。
                    BpfNetSend.ToHostOrAll(
                        NetworkManager, BpfNetMessage.CalibrationReport, writer, NetworkDelivery.Reliable);
                }

                BpfNetLog.Info(LogTag, "reported",
                    "已上报三个校准点 clientId=" + payload.clientId);
            }
            catch (System.Exception e)
            {
                BpfNetLog.Warning(LogTag, "report_failed", e.Message);
            }
        }

        // ------------------------------------------------------------ Host 侧

        /// <summary>由消息路由分发：收到某端的校准点上报。</summary>
        public static void HandleCalibrationReport(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadNetworkSerializable(out BpfCalibrationReportPayload payload);

            BpfSessionCalibration self = Resolve();
            if (self == null || !self.HasAuthority)
            {
                return;
            }

            if (self.sessionState != null && self.sessionState.Phase != BpfSessionPhase.Calibrating)
            {
                BpfNetLog.Warning(LogTag, "report_ignored", "不在校准阶段，忽略上报");
                return;
            }

            Vector3[] points =
            {
                new Vector3(payload.p0x, payload.p0y, payload.p0z),
                new Vector3(payload.p1x, payload.p1y, payload.p1z),
                new Vector3(payload.p2x, payload.p2y, payload.p2z),
            };

            self.reports[payload.clientId] = points;

            // Host 自己的三个点即会话空间基准
            if (self.sessionAnchors == null && payload.clientId == NetworkManager.Singleton.LocalClientId)
            {
                self.sessionAnchors = points;
                BpfNetLog.Info(LogTag, "anchors_set",
                    "会话空间基准已由 Host 建立：" + V3(points[0]) + " / " + V3(points[1]) + " / " + V3(points[2]));
            }

            self.TryFinishCalibration();
        }

        /// <summary>
        /// 收齐全部在线客户端的上报后解算并广播。
        /// 条件：基准已建立 且 每个已连接客户端都上报过。
        /// </summary>
        private void TryFinishCalibration()
        {
            if (sessionAnchors == null || NetworkManager == null)
            {
                return;
            }

            IReadOnlyList<ulong> clients = NetworkManager.ConnectedClientsIds;
            foreach (ulong id in clients)
            {
                if (!reports.ContainsKey(id))
                {
                    return; // 还有端没上报
                }
            }

            CustomMessagingManager messaging = NetworkManager.CustomMessagingManager;
            if (messaging == null)
            {
                return;
            }

            foreach (ulong id in clients)
            {
                Vector3[] local = reports[id];

                BpfSessionTransform transform;
                if (id == NetworkManager.LocalClientId)
                {
                    // Host 自身：会话空间就是它的本机空间 → 单位变换
                    transform = BpfSessionTransform.Identity;
                }
                else if (!BpfSessionSpace.TrySolve(
                    local[0], local[1], local[2],
                    sessionAnchors[0], sessionAnchors[1], sessionAnchors[2],
                    out transform))
                {
                    BpfNetLog.Error(LogTag, "solve_failed",
                        "客户端 " + id + " 的校准点退化（三点共线或重合），未生成变换");
                    transform = default;
                }

                BpfCalibrationResultPayload result = new BpfCalibrationResultPayload
                {
                    clientId = id,
                    posX = transform.position.x,
                    posY = transform.position.y,
                    posZ = transform.position.z,
                    rotX = transform.rotation.x,
                    rotY = transform.rotation.y,
                    rotZ = transform.rotation.z,
                    rotW = transform.rotation.w,
                    valid = transform.valid,
                };

                try
                {
                    using (FastBufferWriter writer = new FastBufferWriter(128, Allocator.Temp))
                    {
                        writer.WriteNetworkSerializable(result);
                        messaging.SendNamedMessageToAll(
                            BpfNetMessage.CalibrationComplete, writer, NetworkDelivery.Reliable);
                    }
                }
                catch (System.Exception e)
                {
                    BpfNetLog.Warning(LogTag, "result_send_failed", e.Message);
                }

                BpfNetLog.Info(LogTag, "solved",
                    "clientId=" + id + " -> " + BpfSessionSpace.Describe(transform));
            }

            if (sessionState != null)
            {
                sessionState.ServerSetPhase(BpfSessionPhase.Playing);
            }

            RaisePhaseChanged(BpfSessionPhase.Playing);
            RaisePointHint(0);
            BpfNetLog.Info(LogTag, "completed", "三点校准完成，进入协作阶段");
        }

        // ------------------------------------------------------------ 接收端

        /// <summary>由消息路由分发：收到校准结果，只采纳自己那条。</summary>
        public static void HandleCalibrationResult(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadNetworkSerializable(out BpfCalibrationResultPayload payload);

            if (NetworkManager.Singleton != null
                && payload.clientId != NetworkManager.Singleton.LocalClientId)
            {
                return;
            }

            LocalTransform = new BpfSessionTransform
            {
                position = new Vector3(payload.posX, payload.posY, payload.posZ),
                rotation = new Quaternion(payload.rotX, payload.rotY, payload.rotZ, payload.rotW).normalized,
                valid = payload.valid,
            };

            BpfNetLog.Info(LogTag, "received",
                "本机会话变换已就绪：" + BpfSessionSpace.Describe(LocalTransform));
            RaisePhaseChanged(BpfSessionPhase.Playing);
        }

        // ------------------------------------------------------------ 工具

        private static BpfSessionCalibration Resolve()
        {
            if (BpfSessionState.Instance != null)
            {
                return BpfSessionState.Instance.GetComponent<BpfSessionCalibration>();
            }

            return null;
        }

        private static void RaisePointHint(int index)
        {
            PointHint?.Invoke(index);
        }

        /// <summary>外部（如校准采集器）推进提示索引时走这里，避免跨类型 invoke 事件。</summary>
        public static void NotifyPointHint(int index)
        {
            PointHint?.Invoke(index);
        }

        private static void RaisePhaseChanged(BpfSessionPhase phase)
        {
            PhaseChanged?.Invoke(phase);
        }

        private static string V3(Vector3 v)
        {
            return "(" + v.x.ToString("F2") + ", " + v.y.ToString("F2") + ", " + v.z.ToString("F2") + ")";
        }
    }
}
