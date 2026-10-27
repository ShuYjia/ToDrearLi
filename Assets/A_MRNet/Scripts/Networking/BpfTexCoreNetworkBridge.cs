using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bpf.Networking
{
    /// <summary>
    /// TexCore 业务桥接：把业务点击收口到"Client 只发请求、Host 裁决执行"。
    ///
    /// 挂接方式（最小侵入）：
    ///   TexCore_BusinessManager.ClickInterceptor = HandleClick
    /// 单机（无会话）时拦截器返回 false，业务点击仍走原来的本地分发，
    /// 行为与改造前逐字节一致。
    ///
    /// 联机且已进入协作阶段（Playing）时：
    ///   Client → 发 InteractionRequest，本地不执行（返回 true 拦住）；
    ///   Host   → 收到请求，做占用仲裁后放行，让原逻辑在本地跑（返回 false），
    ///             再把执行后的面板状态以 0.5s 镜像广播给全员。
    /// 未校准/未完成（非 Playing）→ 直接拦住并提示，业务操作不生效。
    ///
    /// 分数只由 Host 计算：Client 从不执行业务分发，只接收镜像后的状态。
    /// </summary>
    [DisallowMultipleComponent]
    public class BpfTexCoreNetworkBridge : MonoBehaviour
    {
        private const string LogTag = "Bpf|TexCoreBridge";

        [Tooltip("面板状态镜像间隔（秒）。")]
        [SerializeField] private float mirrorInterval = 0.5f;

        public static BpfTexCoreNetworkBridge Instance { get; private set; }

        /// <summary>诊断用：Host 放行执行（含代客户端执行）的业务点击次数。</summary>
        public static int ClickExecuted { get; private set; }

        /// <summary>诊断用：被拦截器拒绝的点击次数（占用冲突 / 阶段不符）。</summary>
        public static int ClickRejected { get; private set; }

        /// <summary>
        /// Host 代客户端执行业务点击时置位。占用表里 owner 是请求方客户端，
        /// 不置位的话 Host 本机这次重入会被自己的仲裁拒掉——业务分发永远
        /// 不会真正执行（分数/步骤都不动）。
        /// </summary>
        private static bool executingForClient;

        private float nextMirrorTime;
        private int stateEpoch;
        private int lastMirroredEpoch = -1;

        // Client 侧：收到的最新面板状态（等本地面板就绪后套用）
        private bool hasRemoteTopicState;
        private BpfTopicStatePayload remoteTopicState;

        private void Awake()
        {
            Instance = this;
            TexCore_BusinessManager.ClickInterceptor = HandleClick;
            BpfNetLog.Info(LogTag, "interceptor_installed", "业务点击拦截器已安装");
        }

        private void OnDestroy()
        {
            if (TexCore_BusinessManager.ClickInterceptor != null
                && TexCore_BusinessManager.ClickInterceptor.Equals((System.Func<GameObject, string, bool>)HandleClick))
            {
                TexCore_BusinessManager.ClickInterceptor = null;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            BpfNetworkBootstrap boot = BpfNetworkBootstrap.Instance;
            if (boot == null || boot.NetworkManager == null || !boot.NetworkManager.IsListening)
            {
                return;
            }

            if (boot.NetworkManager.IsHost)
            {
                MirrorTopicStateToClients();
            }
            else
            {
                ApplyRemoteTopicState();
            }
        }

        // ------------------------------------------------------------ 点击

        /// <summary>
        /// 业务点击收口。返回 true = 已由网络层接管（本地不要执行原逻辑）。
        /// </summary>
        private bool HandleClick(GameObject interactionObj, string sceneName)
        {
            // Host 代客户端执行的重入：占用表里 owner 是请求方客户端，
            // 不走下面的仲裁（否则会被自己拒掉），直接放行给原逻辑。
            // 阶段门由 HandleInteractionRequest 负责。
            if (executingForClient)
            {
                ClickExecuted++;
                return false;
            }

            BpfNetworkBootstrap boot = BpfNetworkBootstrap.Instance;
            if (boot == null || boot.RoomService == null || !boot.RoomService.IsInRoom)
            {
                return false; // 单机：不拦
            }

            if (interactionObj == null)
            {
                return false;
            }

            NetworkManager manager = boot.NetworkManager;
            if (manager == null || !manager.IsListening)
            {
                return false;
            }

            // 未校准/未完成：拦住业务操作，避免两端各自算分
            if (boot.SessionPhase != BpfSessionPhase.Playing)
            {
                BpfNetLog.Warning(LogTag, "click_blocked",
                    "会话阶段为 " + boot.SessionPhase + "，业务操作暂不可用（需先完成校准）");
                ClickRejected++;
                return true;
            }

            BpfObjectId id = new BpfObjectId(
                sceneName ?? SceneManager.GetActiveScene().name, interactionObj.name);

            if (manager.IsHost)
            {
                // Host：占用仲裁后放行，让原来的业务分发在本地执行。
                BpfObjectStateRegistry.TryGet(id, out BpfObjectState existing);
                if (existing.used && existing.ownerClientId != manager.LocalClientId)
                {
                    BpfNetLog.Warning(LogTag, "click_rejected",
                        "对象已被占用：" + id + "（占用者 clientId=" + existing.ownerClientId + "）");
                    ClickRejected++;
                    return true;
                }

                BpfObjectStateRegistry.Set(id, true, manager.LocalClientId, existing.value + 1);
                ClickExecuted++;
                BpfNetLog.Info(LogTag, "click_executed", "Host 执行业务点击：" + id);
                return false; // 放行：交给 TexCore 原逻辑执行
            }

            // Client：只发请求，本地不执行
            SendInteractionRequest(id);

            // 本机点击反馈：请求已上报，原逻辑不在本机跑，补一次本地交互音
            PlayLocalClickFeedback();
            return true;
        }

        private void SendInteractionRequest(BpfObjectId id)
        {
            NetworkManager manager = BpfNetworkBootstrap.Instance.NetworkManager;
            CustomMessagingManager messaging = manager != null ? manager.CustomMessagingManager : null;
            if (messaging == null)
            {
                return;
            }

            BpfInteractionRequestPayload payload = new BpfInteractionRequestPayload
            {
                clientId = manager.LocalClientId,
                id = id,
            };

            try
            {
                using (FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp))
                {
                    writer.WriteNetworkSerializable(payload);
                    // 请求只到 Host（NGO 客户端不能群发），见 BpfNetSend。
                    BpfNetSend.ToHostOrAll(
                        manager, BpfNetMessage.InteractionRequest, writer, NetworkDelivery.Reliable);
                }
            }
            catch (System.Exception e)
            {
                BpfNetLog.Warning(LogTag, "request_failed", e.Message);
            }
        }

        /// <summary>
        /// Client 点击被网络层接管时补一次本地交互音。
        /// 原逻辑不在本机执行，点击音也不会响——不补的话用户"点了没反应"。
        /// 走 TexCore 事件中心，单机/联机行为都不变。
        /// </summary>
        private static void PlayLocalClickFeedback()
        {
            TexCore_EventCenter.Broadcast<string>(TexCore_EventType.交互音频, "Click");
        }

        // ------------------------------------------------------------ 消息

        /// <summary>Host 收到某端的点击请求：执行原逻辑并把结果镜像出去。</summary>
        public static void HandleInteractionRequest(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadNetworkSerializable(out BpfInteractionRequestPayload payload);

            BpfNetworkBootstrap boot = BpfNetworkBootstrap.Instance;
            NetworkManager manager = boot != null ? boot.NetworkManager : null;
            if (manager == null || !manager.IsServer)
            {
                return;
            }

            // 阶段门：客户端本地拦截已经挡过一轮，这里再挡乱序/伪造消息。
            if (boot.SessionPhase != BpfSessionPhase.Playing)
            {
                BpfNetLog.Warning(LogTag, "request_blocked",
                    "会话阶段为 " + boot.SessionPhase + "，拒绝 clientId=" + payload.clientId
                    + " 的请求：" + payload.id);
                return;
            }

            // 仲裁
            if (BpfObjectStateRegistry.TryGet(payload.id, out BpfObjectState existing)
                && existing.used)
            {
                BpfNetLog.Warning(LogTag, "request_rejected",
                    "对象已占用，拒绝 clientId=" + payload.clientId + " 的点击：" + payload.id);
                return;
            }

            // Host 代为执行：直接调用原来的业务入口。
            // 占用表里 owner 是请求方客户端，必须抑制拦截器的重入仲裁，
            // 否则 Host 本机这次调用会被自己拒掉，业务分发根本不执行。
            GameObject target = FindObjectByName(payload.id.objectName.ToString());
            if (target == null)
            {
                BpfNetLog.Warning(LogTag, "target_missing",
                    "Host 找不到点击目标：" + payload.id);
                return;
            }

            BpfObjectStateRegistry.Set(payload.id, true, payload.clientId, existing.value + 1);

            bool previousExecuting = executingForClient;
            executingForClient = true;
            try
            {
                TexCore_BusinessManager.OnClick(target, payload.id.sceneName.ToString());
            }
            finally
            {
                executingForClient = previousExecuting;
            }

            BpfNetLog.Info(LogTag, "request_executed",
                "Host 代 clientId=" + payload.clientId + " 执行：" + payload.id);

            // 立刻镜像一次，避免等下一个 0.5s 周期
            if (Instance != null)
            {
                Instance.MirrorTopicStateToClients();
            }
        }

        /// <summary>Client 收到面板状态镜像。</summary>
        public static void HandleTopicState(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadNetworkSerializable(out BpfTopicStatePayload payload);

            if (Instance == null)
            {
                return;
            }

            if (payload.epoch < Instance.lastMirroredEpoch)
            {
                return; // 乱序旧包
            }

            Instance.lastMirroredEpoch = payload.epoch;
            Instance.remoteTopicState = payload;
            Instance.hasRemoteTopicState = true;
        }

        // ------------------------------------------------------------ 镜像

        private void MirrorTopicStateToClients()
        {
            if (Time.time < nextMirrorTime)
            {
                return;
            }

            nextMirrorTime = Time.time + mirrorInterval;

            NetworkManager manager = BpfNetworkBootstrap.Instance.NetworkManager;
            CustomMessagingManager messaging = manager != null ? manager.CustomMessagingManager : null;
            if (messaging == null)
            {
                return;
            }

            PanelTopicBase panel = ResolveActiveTopicPanel();

            BpfTopicStatePayload payload = new BpfTopicStatePayload
            {
                epoch = ++stateEpoch,
                stepIndex = panel != null ? panel.stepIndex : 0,
                haveScore = panel != null ? panel.haveScore : 0,
                errorNum = panel != null ? panel.errorNum : 0,
                isSubmit = panel != null && panel.isSubmit,
                // 题目名取面板自身的数据类名（TexCore_DataClass.name）；
                // DataClass_Topic 继承的 SelfDataBase 没有名字字段。
                topicName = new FixedString64Bytes(panel != null && panel.datas != null
                    ? panel.datas.name ?? string.Empty
                    : string.Empty),
            };

            try
            {
                using (FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp))
                {
                    writer.WriteNetworkSerializable(payload);
                    messaging.SendNamedMessageToAll(
                        BpfNetMessage.TopicState, writer, NetworkDelivery.Unreliable);
                }
            }
            catch (System.Exception e)
            {
                BpfNetLog.Warning(LogTag, "mirror_failed", e.Message);
            }
        }

        /// <summary>Client：把镜像到的状态套到本地活动面板上。</summary>
        private void ApplyRemoteTopicState()
        {
            if (!hasRemoteTopicState)
            {
                return;
            }

            PanelTopicBase panel = ResolveActiveTopicPanel();
            if (panel == null)
            {
                return; // 面板还没就绪，下一帧再试
            }

            if (panel.stepIndex == remoteTopicState.stepIndex
                && panel.haveScore == remoteTopicState.haveScore
                && panel.isSubmit == remoteTopicState.isSubmit)
            {
                return; // 已一致
            }

            panel.stepIndex = remoteTopicState.stepIndex;
            panel.haveScore = remoteTopicState.haveScore;
            panel.errorNum = remoteTopicState.errorNum;
            panel.isSubmit = remoteTopicState.isSubmit;

            BpfNetLog.Info(LogTag, "state_applied",
                "已同步题目状态：step=" + panel.stepIndex + " score=" + panel.haveScore
                + " submit=" + panel.isSubmit + "（epoch=" + remoteTopicState.epoch + "）");
        }

        // ------------------------------------------------------------ 晚加入

        /// <summary>Host 在有新客户端加入时调用：把对象占用表补给它。</summary>
        public void SendSnapshotTo(ulong clientId)
        {
            NetworkManager manager = BpfNetworkBootstrap.Instance.NetworkManager;
            CustomMessagingManager messaging = manager != null ? manager.CustomMessagingManager : null;
            if (messaging == null || !manager.IsServer)
            {
                return;
            }

            List<BpfObjectState> snapshot = BpfObjectStateRegistry.Snapshot();

            try
            {
                using (FastBufferWriter writer = new FastBufferWriter(1024, Allocator.Temp))
                {
                    writer.WriteValueSafe(snapshot.Count);
                    writer.WriteValueSafe(stateEpoch);
                    foreach (BpfObjectState state in snapshot)
                    {
                        writer.WriteNetworkSerializable(state);
                    }

                    messaging.SendNamedMessage(
                        BpfNetMessage.ObjectSnapshot, clientId, writer, NetworkDelivery.Reliable);
                }

                BpfNetLog.Info(LogTag, "snapshot_sent",
                    "已向 clientId=" + clientId + " 发送对象快照（" + snapshot.Count + " 条）");
            }
            catch (System.Exception e)
            {
                BpfNetLog.Warning(LogTag, "snapshot_failed", e.Message);
            }
        }

        /// <summary>Client 收到对象快照。</summary>
        public static void HandleObjectSnapshot(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int count);
            reader.ReadValueSafe(out int epoch);

            List<BpfObjectState> states = new List<BpfObjectState>();
            for (int i = 0; i < count; i++)
            {
                reader.ReadNetworkSerializable(out BpfObjectState state);
                states.Add(state);
            }

            BpfObjectStateRegistry.ApplySnapshot(states);
            BpfNetLog.Info(LogTag, "snapshot_applied",
                "已补齐 " + states.Count + " 条对象状态（epoch=" + epoch + "）");
        }

        // ------------------------------------------------------------ 工具

        private static PanelTopicBase ResolveActiveTopicPanel()
        {
            TexCore_PlaneManager manager = TexCore_PlaneManager.self;
            if (manager == null || manager.activePlane == null)
            {
                return null;
            }

            return manager.activePlane as PanelTopicBase;
        }

        private static GameObject FindObjectByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            GameObject[] all = Object.FindObjectsOfType<GameObject>(true);
            foreach (GameObject candidate in all)
            {
                if (candidate.name == name)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
