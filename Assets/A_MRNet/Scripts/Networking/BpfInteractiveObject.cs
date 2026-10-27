using MixedReality.Toolkit.SpatialManipulation;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace Bpf.Networking
{
    /// <summary>
    /// 可抓取业务物体（阶段 4：抓取平滑）。
    ///
    /// 挂接方式：运行期由 <see cref="BpfInteractiveRegistry.ScanActiveScene"/>
    /// 给场景里带 ObjectManipulator（MRTK3，基于 XRI）的物体自动挂载；
    /// 业务脚本也可以手动 AddComponent。场景文件零改动。
    ///
    /// 权威模型（Host 仲裁占用 + 拥有者流式位姿）：
    ///   1. 抓取/释放先过 Host 仲裁（复用 <see cref="BpfObjectStateRegistry"/>
    ///      占用表——与业务点击同一张表，同一物体同一时刻只允许一个操作者）；
    ///   2. 占用成立后由「拥有者」以低频（默认 10Hz）把位姿发往 Host，
    ///      Host 再广播全员（NGO 纯客户端不能直接群发，见 BpfNetSend）；
    ///   3. 非拥有者把位姿喂进双快照插值缓冲，每帧连续跟随，无跳变；
    ///   4. 位姿一律先归一化到会话空间（三点校准），双设备坐标才一致；
    ///   5. 释放时拥有者补发一帧 held=false 的可靠位姿做落点硬对齐。
    ///
    /// 本地输入：自动挂 ObjectManipulator 的 firstSelectEntered /
    /// lastSelectExited；无 MRTK 环境（验证台/单测）可直接调用公开的
    /// <see cref="BeginLocalGrab"/> / <see cref="EndLocalRelease"/>。
    ///
    /// 已知限制（与旧框架一致，阶段 5 双机复测后处理）：
    ///   - Client 抓取被 Host 拒绝时，本机 MRTK 已经抓起物体，只能就地
    ///     停放并停止流式发送，不做强制回弹；
    ///   - 未校准（非 Playing）阶段拒绝抓取，语义与业务点击相同。
    /// </summary>
    [DisallowMultipleComponent]
    public class BpfInteractiveObject : MonoBehaviour
    {
        private const string LogTag = "Bpf|Interactive";

        [Tooltip("位姿流发送频率（Hz）。低频权威写入，接收端插值补平滑。")]
        [SerializeField] private float sendHz = 10f;

        [Tooltip("插值窗口（秒）：接收端按此刻前的历史位姿渲染，约等于 1.5 倍发送间隔。")]
        [SerializeField] private float interpolationWindow = 0.15f;

        private ObjectManipulator manipulator;
        private BpfObjectId id;
        private bool registered;

        // 本地是否为当前拥有者（Host 仲裁通过后才置位）
        private bool localHeld;

        // 拥有者发送节流
        private float sendInterval;
        private float sendTimer;

        // 接收端双快照插值缓冲（会话空间已转本机空间）
        private bool hasPrevious;
        private Vector3 previousPosition;
        private Quaternion previousRotation;
        private Vector3 latestPosition;
        private Quaternion latestRotation;
        private float previousReceiveTime;
        private float latestReceiveTime;

        public BpfObjectId Id => id;

        /// <summary>本机是否为该物体的当前拥有者。</summary>
        public bool LocalHeld => localHeld;

        /// <summary>诊断用：本会话已发送的位姿流帧数（含落点帧）。</summary>
        public static int PoseFramesSent { get; private set; }

        /// <summary>诊断/调参用：插值窗口（秒）。自测可临时改大以绕过编辑器低帧率。</summary>
        public float InterpolationWindow
        {
            get => interpolationWindow;
            set => interpolationWindow = Mathf.Max(0.01f, value);
        }

        private void Awake()
        {
            manipulator = GetComponent<ObjectManipulator>();
            id = new BpfObjectId(gameObject.scene.name, gameObject.name);
        }

        private void OnEnable()
        {
            BpfInteractiveRegistry.Register(this);
            registered = true;
            HookManipulator(true);
        }

        private void OnDisable()
        {
            HookManipulator(false);
            if (registered)
            {
                BpfInteractiveRegistry.Unregister(this);
                registered = false;
            }
        }

        private void Update()
        {
            BpfNetworkBootstrap boot = BpfNetworkBootstrap.Instance;
            if (boot == null || boot.NetworkManager == null || !boot.NetworkManager.IsListening)
            {
                return;
            }

            if (localHeld)
            {
                UpdateOwnerStream();
                return;
            }

            UpdateRemoteInterpolation();
        }

        // ------------------------------------------------------------ 本地输入

        private void HookManipulator(bool hook)
        {
            if (!Application.isPlaying || manipulator == null)
            {
                return;
            }

            if (hook)
            {
                manipulator.firstSelectEntered.AddListener(HandleManipulatorGrabbed);
                manipulator.lastSelectExited.AddListener(HandleManipulatorReleased);
            }
            else
            {
                manipulator.firstSelectEntered.RemoveListener(HandleManipulatorGrabbed);
                manipulator.lastSelectExited.RemoveListener(HandleManipulatorReleased);
            }
        }

        private void HandleManipulatorGrabbed(SelectEnterEventArgs args)
        {
            BeginLocalGrab();
        }

        private void HandleManipulatorReleased(SelectExitEventArgs args)
        {
            EndLocalRelease();
        }

        /// <summary>
        /// 本地用户发起抓取（MRTK 选择事件或验证台手动调用）。
        /// 返回 true = 请求已被网络层受理（Host 本地获批 / Client 已上报）。
        /// </summary>
        public bool BeginLocalGrab()
        {
            BpfNetworkBootstrap boot = BpfNetworkBootstrap.Instance;
            if (boot == null || boot.RoomService == null || !boot.RoomService.IsInRoom)
            {
                return false; // 单机：不拦，抓取完全走本地
            }

            NetworkManager manager = boot.NetworkManager;
            if (manager == null || !manager.IsListening)
            {
                return false;
            }

            if (boot.SessionPhase != BpfSessionPhase.Playing)
            {
                BpfNetLog.Warning(LogTag, "grab_blocked",
                    "会话阶段为 " + boot.SessionPhase + "，抓取暂不可用（需先完成校准）");
                return false;
            }

            if (manager.IsHost)
            {
                // Host 本地仲裁
                if (BpfObjectStateRegistry.TryGet(id, out BpfObjectState existing)
                    && existing.used && existing.ownerClientId != manager.LocalClientId)
                {
                    BpfNetLog.Warning(LogTag, "grab_rejected",
                        "对象已被占用：" + id + "（owner=" + existing.ownerClientId + "）");
                    return false;
                }

                BpfObjectStateRegistry.Set(id, true, manager.LocalClientId, 1);
                localHeld = true;
                ClearInterpolationBuffer();
                ApplyManipulatorOwnership();
                BroadcastResult(true, true, manager.LocalClientId);
                BpfNetLog.Info(LogTag, "grab_executed", "Host 本地抓取：" + id);
                return true;
            }

            // Client：只发请求，等 Host 占用广播后开始流式发送
            BpfInteractiveRequestPayload payload = new BpfInteractiveRequestPayload
            {
                clientId = manager.LocalClientId,
                id = id,
                grab = true,
            };

            try
            {
                using (FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp))
                {
                    writer.WriteNetworkSerializable(payload);
                    BpfNetSend.ToHostOrAll(
                        manager, BpfNetMessage.InteractiveRequest, writer, NetworkDelivery.Reliable);
                }

                BpfNetLog.Info(LogTag, "grab_requested", "已上报抓取请求：" + id);
            }
            catch (System.Exception e)
            {
                BpfNetLog.Warning(LogTag, "grab_request_failed", e.Message);
            }

            return true;
        }

        /// <summary>本地用户释放（MRTK 选择退出或手动调用）。</summary>
        public void EndLocalRelease()
        {
            if (!localHeld)
            {
                return;
            }

            localHeld = false;
            ClearInterpolationBuffer();

            BpfNetworkBootstrap boot = BpfNetworkBootstrap.Instance;
            NetworkManager manager = boot != null ? boot.NetworkManager : null;
            if (manager == null || !manager.IsListening)
            {
                ApplyManipulatorOwnership();
                return;
            }

            // 补发一帧可靠落点（held=false），接收端硬对齐，避免停在插值中间态
            SendPoseFrame(true);

            if (manager.IsHost)
            {
                BpfObjectStateRegistry.Set(id, false, 0, 0);
                ApplyManipulatorOwnership();
                BroadcastResult(true, false, 0);
                BpfNetLog.Info(LogTag, "release_executed", "Host 本地释放：" + id);
                return;
            }

            BpfInteractiveRequestPayload payload = new BpfInteractiveRequestPayload
            {
                clientId = manager.LocalClientId,
                id = id,
                grab = false,
            };

            try
            {
                using (FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp))
                {
                    writer.WriteNetworkSerializable(payload);
                    BpfNetSend.ToHostOrAll(
                        manager, BpfNetMessage.InteractiveRequest, writer, NetworkDelivery.Reliable);
                }
            }
            catch (System.Exception e)
            {
                BpfNetLog.Warning(LogTag, "release_request_failed", e.Message);
            }

            ApplyManipulatorOwnership();
        }

        // ------------------------------------------------------------ 拥有者流

        /// <summary>
        /// 拥有者侧：以 sendHz 采样本机 Transform，归一化到会话空间后发出。
        /// Host 拥有者直接广播全员；Client 拥有者发给 Host 由其转发。
        /// </summary>
        private void UpdateOwnerStream()
        {
            sendInterval = 1f / Mathf.Max(1f, sendHz);
            sendTimer += Time.deltaTime;
            if (sendTimer < sendInterval)
            {
                return;
            }

            sendTimer = 0f;
            SendPoseFrame(false);
        }

        private void SendPoseFrame(bool reliable)
        {
            NetworkManager manager = BpfNetworkBootstrap.Instance != null
                ? BpfNetworkBootstrap.Instance.NetworkManager
                : null;
            if (manager == null || !manager.IsListening)
            {
                return;
            }

            // 本机空间 → 会话空间（校准未完成按单位变换直发）
            BpfSessionTransform sessionTransform = BpfSessionCalibration.LocalTransform;
            Vector3 position = sessionTransform.valid
                ? sessionTransform.ApplyToPosition(transform.position)
                : transform.position;
            Quaternion rotation = sessionTransform.valid
                ? sessionTransform.ApplyToRotation(transform.rotation)
                : transform.rotation;

            BpfInteractivePosePayload payload = new BpfInteractivePosePayload
            {
                id = id,
                posX = position.x,
                posY = position.y,
                posZ = position.z,
                rotX = rotation.x,
                rotY = rotation.y,
                rotZ = rotation.z,
                rotW = rotation.w,
                ownerClientId = manager.LocalClientId,
                held = reliable ? false : true,
            };

            try
            {
                using (FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp))
                {
                    writer.WriteNetworkSerializable(payload);
                    BpfNetSend.ToHostOrAll(
                        manager, BpfNetMessage.InteractivePose, writer,
                        reliable ? NetworkDelivery.Reliable : NetworkDelivery.Unreliable);
                }

                PoseFramesSent++;
            }
            catch (System.Exception e)
            {
                BpfNetLog.Warning(LogTag, "pose_send_failed", e.Message);
            }
        }

        // ------------------------------------------------------------ 消息处理

        /// <summary>Host 收到抓取/释放请求：仲裁后广播占用变更。</summary>
        public static void HandleInteractiveRequest(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadNetworkSerializable(out BpfInteractiveRequestPayload payload);

            BpfNetworkBootstrap boot = BpfNetworkBootstrap.Instance;
            NetworkManager manager = boot != null ? boot.NetworkManager : null;
            if (manager == null || !manager.IsServer)
            {
                return; // 只有 Host 裁决
            }

            BpfInteractiveObject target = BpfInteractiveRegistry.Find(payload.id);
            if (target == null)
            {
                BpfNetLog.Warning(LogTag, "request_missing",
                    "Host 没有该交互物体：" + payload.id);
                SendResult(payload.id, false, false, 0);
                return;
            }

            bool granted = true;
            bool held = false;
            ulong owner = 0;

            if (payload.grab)
            {
                if (BpfObjectStateRegistry.TryGet(payload.id, out BpfObjectState existing)
                    && existing.used && existing.ownerClientId != payload.clientId)
                {
                    granted = false; // 被其他客户端占着
                    BpfNetLog.Warning(LogTag, "request_rejected",
                        "对象已占用，拒绝 clientId=" + payload.clientId + "：" + payload.id);
                }
                else
                {
                    BpfObjectStateRegistry.Set(payload.id, true, payload.clientId, 1);
                    held = true;
                    owner = payload.clientId;
                    BpfNetLog.Info(LogTag, "request_granted",
                        "批准 clientId=" + payload.clientId + " 抓取：" + payload.id);
                }
            }
            else
            {
                // 释放：仅拥有者可释放；未占用时幂等成功
                if (BpfObjectStateRegistry.TryGet(payload.id, out BpfObjectState existing)
                    && existing.used && existing.ownerClientId == payload.clientId)
                {
                    BpfObjectStateRegistry.Set(payload.id, false, 0, 0);
                    BpfNetLog.Info(LogTag, "request_released",
                        "clientId=" + payload.clientId + " 释放：" + payload.id);
                }

                granted = true;
                held = false;
                owner = 0;
            }

            SendResult(payload.id, granted, held, owner);
        }

        /// <summary>全员收到占用变更广播。</summary>
        public static void HandleInteractiveResult(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadNetworkSerializable(out BpfInteractiveResultPayload payload);
            BpfInteractiveRegistry.ApplyResult(
                payload.id, payload.granted, payload.held, payload.ownerClientId);
        }

        /// <summary>
        /// 位姿流接收。Host 收到 Client 拥有者的流后负责转发全员
        /// （NGO 纯客户端不能群发）；拥有者本机跳过自己的回声。
        /// </summary>
        public static void HandleInteractivePose(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadNetworkSerializable(out BpfInteractivePosePayload payload);

            BpfNetworkBootstrap boot = BpfNetworkBootstrap.Instance;
            NetworkManager manager = boot != null ? boot.NetworkManager : null;
            if (manager == null || !manager.IsListening)
            {
                return;
            }

            // Host 转发：Client 拥有者的位姿流由 Host 广播给全员
            // （NGO 纯客户端不能群发）。只转发"从远端客户端收到的"那一跳——
            // Host 的 ToAll 会把消息本地直送回本机客户端（sender=本机），
            // 若不排除这个回声就会无限自我转发直到栈溢出。
            if (manager.IsServer
                && senderClientId != manager.LocalClientId
                && payload.ownerClientId != manager.LocalClientId)
            {
                try
                {
                    using (FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp))
                    {
                        writer.WriteNetworkSerializable(payload);
                        manager.CustomMessagingManager.SendNamedMessageToAll(
                            BpfNetMessage.InteractivePose, writer, NetworkDelivery.Unreliable);
                    }
                }
                catch (System.Exception e)
                {
                    BpfNetLog.Warning(LogTag, "pose_relay_failed", e.Message);
                }
            }

            if (payload.ownerClientId == manager.LocalClientId)
            {
                return; // 拥有者本机的回声，不插值自己的物体
            }

            BpfInteractiveRegistry.ApplyRemotePose(payload);
        }

        /// <summary>Host 广播占用变更（本地仲裁路径也走它，保持单一出口）。</summary>
        private static void SendResult(
            BpfObjectId id, bool granted, bool held, ulong ownerClientId)
        {
            NetworkManager manager = BpfNetworkBootstrap.Instance != null
                ? BpfNetworkBootstrap.Instance.NetworkManager
                : null;
            if (manager == null || !manager.IsServer)
            {
                return;
            }

            BpfInteractiveResultPayload payload = new BpfInteractiveResultPayload
            {
                id = id,
                granted = granted,
                held = held,
                ownerClientId = ownerClientId,
            };

            try
            {
                using (FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp))
                {
                    writer.WriteNetworkSerializable(payload);
                    manager.CustomMessagingManager.SendNamedMessageToAll(
                        BpfNetMessage.InteractiveResult, writer, NetworkDelivery.Reliable);
                }
            }
            catch (System.Exception e)
            {
                BpfNetLog.Warning(LogTag, "result_send_failed", e.Message);
            }
        }

        private void BroadcastResult(bool granted, bool held, ulong ownerClientId)
        {
            SendResult(id, granted, held, ownerClientId);
        }

        // ------------------------------------------------------------ 状态应用

        /// <summary>应用 Host 的占用变更广播。</summary>
        public void ApplyResult(bool granted, bool held, ulong ownerClientId)
        {
            NetworkManager manager = NetworkManager.Singleton;
            ulong localId = manager != null && manager.IsListening ? manager.LocalClientId : 0;

            bool wasHeld = localHeld;
            localHeld = held && ownerClientId == localId;

            // 所有权变化或物体已释放：丢弃旧缓冲，避免下一段抓取从旧轨迹起跳
            if (!held || localHeld || wasHeld != localHeld)
            {
                ClearInterpolationBuffer();
            }

            ApplyManipulatorOwnership();

            if (!granted)
            {
                BpfNetLog.Warning(LogTag, "result_denied", "抓取请求被 Host 拒绝：" + id);
            }
        }

        /// <summary>
        /// 非拥有者收到位姿：会话空间转本机空间后喂双快照缓冲。
        /// held=false 的释放帧直接硬对齐。
        ///
        /// 插值模型（延后渲染）：渲染时刻取 Time.time - interpolationWindow，
        /// 在前后两帧快照之间 Lerp。空闲后第一帧把"当前状态"当作
        /// interpolationWindow 秒前的快照，保证从原位平滑出发；
        /// 直接按"当前时刻"渲染会让首帧瞬移到目标。
        /// </summary>
        public void FeedRemotePose(BpfInteractivePosePayload payload)
        {
            if (localHeld)
            {
                return; // 自己抓着，以本机 MRTK 驱动为准
            }

            BpfSessionTransform sessionTransform = BpfSessionCalibration.LocalTransform;
            Vector3 rawPosition = new Vector3(payload.posX, payload.posY, payload.posZ);
            Quaternion rawRotation = new Quaternion(
                payload.rotX, payload.rotY, payload.rotZ, payload.rotW).normalized;

            Vector3 position = sessionTransform.valid
                ? sessionTransform.ApplyInverseToPosition(rawPosition)
                : rawPosition;
            Quaternion rotation = sessionTransform.valid
                ? sessionTransform.ApplyInverseToRotation(rawRotation)
                : rawRotation;

            if (!payload.held)
            {
                // 释放落点：硬对齐，不留插值尾巴
                previousPosition = latestPosition = position;
                previousRotation = latestRotation = rotation;
                previousReceiveTime = latestReceiveTime = Time.time;
                hasPrevious = true;
                transform.SetPositionAndRotation(position, rotation);
                return;
            }

            if (!hasPrevious)
            {
                // 空闲后第一帧：当前状态视为一个插值窗口前的快照
                previousPosition = transform.position;
                previousRotation = transform.rotation;
                previousReceiveTime = Time.time - interpolationWindow;
            }
            else
            {
                previousPosition = latestPosition;
                previousRotation = latestRotation;
                previousReceiveTime = latestReceiveTime;
            }

            latestPosition = position;
            latestRotation = rotation;
            latestReceiveTime = Time.time;
            hasPrevious = true;
        }

        /// <summary>接收端连续插值：延后一个窗口，在双快照之间 Lerp/Slerp。</summary>
        private void UpdateRemoteInterpolation()
        {
            if (!hasPrevious)
            {
                return;
            }

            float renderTime = Time.time - interpolationWindow;
            float span = latestReceiveTime - previousReceiveTime;
            float t = span > 0.0001f
                ? Mathf.Clamp01((renderTime - previousReceiveTime) / span)
                : 1f;

            Vector3 position = Vector3.Lerp(previousPosition, latestPosition, t);
            Quaternion rotation = Quaternion.Slerp(previousRotation, latestRotation, t);
            transform.SetPositionAndRotation(position, rotation);
        }

        private void ClearInterpolationBuffer()
        {
            hasPrevious = false;
            previousPosition = latestPosition = transform.position;
            previousRotation = latestRotation = transform.rotation;
            previousReceiveTime = latestReceiveTime = Time.time;
        }

        /// <summary>
        /// 被其他客户端占着时禁用本机 Manipulator（本地抢不到，也不和
        /// 插值打架）；空闲或自己占着时启用。
        /// </summary>
        private void ApplyManipulatorOwnership()
        {
            if (manipulator == null)
            {
                return;
            }

            NetworkManager manager = NetworkManager.Singleton;
            ulong localId = manager != null && manager.IsListening ? manager.LocalClientId : 0;

            bool heldByOther = false;
            if (BpfObjectStateRegistry.TryGet(id, out BpfObjectState state))
            {
                heldByOther = state.used && state.ownerClientId != localId;
            }

            manipulator.enabled = !heldByOther;
        }
    }
}
