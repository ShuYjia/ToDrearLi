using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Bpf.Networking
{
    /// <summary>
    /// 玩家姿态同步（挂在 BpfPlayerAvatar 预制体上）。
    ///
    /// 拥有者（Owner）：25Hz 采样本机 Rig，经命名消息不可靠广播给全员。
    /// 非拥有者：接收姿态，用双快照插值平滑应用到可视化节点。
    ///
    /// 阶段 1 直接发送世界坐标；阶段 2 三点校准上线后，
    /// 发送前会做 LocalPoseToSession 归一化（接口已留位）。
    /// </summary>
    [DisallowMultipleComponent]
    public class BpfPlayerPoseSync : NetworkBehaviour
    {
        private const string LogTag = "Bpf|PoseSync";

        [Tooltip("发送频率（Hz）。")]
        [SerializeField] private float sendHz = 25f;

        // 可视化节点（由 BpfPlayerAvatar 在生成可视化时注入）
        private Transform headVisual;
        private Transform leftVisual;
        private Transform rightVisual;

        private float sendInterval;
        private float sendTimer;

        private bool hasPrevious;
        private BpfPosePayload previousPose;
        private BpfPosePayload latestPose;
        private float previousReceiveTime;
        private float latestReceiveTime;

        /// <summary>可视化节点注入（头像生成头/手/名牌时调用）。</summary>
        public void BindVisuals(Transform head, Transform left, Transform right)
        {
            headVisual = head;
            leftVisual = left;
            rightVisual = right;
        }

        private void Update()
        {
            if (NetworkManager == null || !NetworkManager.IsListening)
            {
                return;
            }

            if (IsOwner)
            {
                UpdateOwnerSend();
                ApplyLocalRigToVisuals();
                return;
            }

            UpdateRemoteInterpolation();
        }

        // ------------------------------------------------------------ Owner

        private void UpdateOwnerSend()
        {
            sendInterval = 1f / Mathf.Max(1f, sendHz);
            sendTimer += Time.deltaTime;
            if (sendTimer < sendInterval)
            {
                return;
            }

            sendTimer = 0f;

            if (!BpfLocalRigPoseSource.TryCapturePose(out BpfPosePayload pose))
            {
                return;
            }

            pose.clientId = NetworkObject.OwnerClientId;

            CustomMessagingManager messaging = NetworkManager.CustomMessagingManager;
            if (messaging == null)
            {
                return;
            }

            // 统一发到会话空间：两台设备的世界原点不同，必须先归一化。
            // 校准完成前 LocalTransform 无效，按单位变换直发（同机联调可用）。
            BpfPosePayload outgoing = LocalPoseToSession(pose);

            try
            {
                using (FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp))
                {
                    writer.WriteValueSafe(outgoing.clientId);
                    writer.WriteNetworkSerializable(outgoing);
                    // Client 拥有者只能发给 Host（NGO 客户端不能群发），
                    // Host 拥有者直接广播全员；见 BpfNetSend。
                    BpfNetSend.ToHostOrAll(
                        NetworkManager, BpfNetMessage.PlayerPose, writer, NetworkDelivery.Unreliable);
                }
            }
            catch (System.Exception e)
            {
                BpfNetLog.Warning(LogTag, "pose_send_failed", e.Message);
            }
        }

        /// <summary>
        /// 本机姿态 → 会话空间姿态。校准未完成时按单位变换处理。
        /// </summary>
        private static BpfPosePayload LocalPoseToSession(in BpfPosePayload local)
        {
            BpfSessionTransform transform = BpfSessionCalibration.LocalTransform;
            if (!transform.valid)
            {
                return local;
            }

            BpfPosePayload result = local;

            Vector3 headPos = transform.ApplyToPosition(
                new Vector3(local.headPosX, local.headPosY, local.headPosZ));
            result.headPosX = headPos.x;
            result.headPosY = headPos.y;
            result.headPosZ = headPos.z;

            Quaternion headRot = transform.ApplyToRotation(
                new Quaternion(local.headRotX, local.headRotY, local.headRotZ, local.headRotW));
            result.headRotX = headRot.x;
            result.headRotY = headRot.y;
            result.headRotZ = headRot.z;
            result.headRotW = headRot.w;

            if (local.hasLeft)
            {
                Vector3 leftPos = transform.ApplyToPosition(
                    new Vector3(local.leftPosX, local.leftPosY, local.leftPosZ));
                result.leftPosX = leftPos.x;
                result.leftPosY = leftPos.y;
                result.leftPosZ = leftPos.z;

                Quaternion leftRot = transform.ApplyToRotation(
                    new Quaternion(local.leftRotX, local.leftRotY, local.leftRotZ, local.leftRotW));
                result.leftRotX = leftRot.x;
                result.leftRotY = leftRot.y;
                result.leftRotZ = leftRot.z;
                result.leftRotW = leftRot.w;
            }

            if (local.hasRight)
            {
                Vector3 rightPos = transform.ApplyToPosition(
                    new Vector3(local.rightPosX, local.rightPosY, local.rightPosZ));
                result.rightPosX = rightPos.x;
                result.rightPosY = rightPos.y;
                result.rightPosZ = rightPos.z;

                Quaternion rightRot = transform.ApplyToRotation(
                    new Quaternion(local.rightRotX, local.rightRotY, local.rightRotZ, local.rightRotW));
                result.rightRotX = rightRot.x;
                result.rightRotY = rightRot.y;
                result.rightRotZ = rightRot.z;
                result.rightRotW = rightRot.w;
            }

            return result;
        }

        /// <summary>自己的头像直接贴在本机 Rig 上（无插值，所见即所发）。</summary>
        private void ApplyLocalRigToVisuals()
        {
            if (!BpfLocalRigPoseSource.TryCapturePose(out BpfPosePayload pose))
            {
                return;
            }

            ApplyToVisuals(in pose);
        }

        // ------------------------------------------------------------ Remote

        /// <summary>由 BpfNetMessageRouter 分发的姿态消息（主线程调用）。</summary>
        public static void HandlePoseMessage(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out ulong clientId);
            reader.ReadNetworkSerializable(out BpfPosePayload pose);

            if (!BpfPlayerRegistry.TryFind(clientId, out BpfPlayerAvatar avatar) || avatar == null)
            {
                return;
            }

            BpfPlayerPoseSync sync = avatar.PoseSync;
            if (sync == null)
            {
                return;
            }

            sync.ApplyRemotePose(pose);
        }

        private void ApplyRemotePose(BpfPosePayload pose)
        {
            previousPose = latestPose;
            previousReceiveTime = latestReceiveTime;
            latestPose = pose;
            latestReceiveTime = Time.time;
            hasPrevious = true;
        }

        private void UpdateRemoteInterpolation()
        {
            if (!hasPrevious)
            {
                return;
            }

            float span = latestReceiveTime - previousReceiveTime;
            float t = span > 0.0001f
                ? Mathf.Clamp01((Time.time - previousReceiveTime) / span)
                : 1f;

            BpfPosePayload blended = Blend(previousPose, latestPose, t);
            ApplyToVisuals(in blended);
        }

        private static BpfPosePayload Blend(in BpfPosePayload a, in BpfPosePayload b, float t)
        {
            BpfPosePayload result = b;
            result.headPosX = Mathf.Lerp(a.headPosX, b.headPosX, t);
            result.headPosY = Mathf.Lerp(a.headPosY, b.headPosY, t);
            result.headPosZ = Mathf.Lerp(a.headPosZ, b.headPosZ, t);
            result.headRotX = Mathf.Lerp(a.headRotX, b.headRotX, t);
            result.headRotY = Mathf.Lerp(a.headRotY, b.headRotY, t);
            result.headRotZ = Mathf.Lerp(a.headRotZ, b.headRotZ, t);
            result.headRotW = Mathf.Lerp(a.headRotW, b.headRotW, t);

            if (b.hasLeft)
            {
                result.hasLeft = true;
                result.leftPosX = Mathf.Lerp(a.leftPosX, b.leftPosX, t);
                result.leftPosY = Mathf.Lerp(a.leftPosY, b.leftPosY, t);
                result.leftPosZ = Mathf.Lerp(a.leftPosZ, b.leftPosZ, t);
                result.leftRotX = Mathf.Lerp(a.leftRotX, b.leftRotX, t);
                result.leftRotY = Mathf.Lerp(a.leftRotY, b.leftRotY, t);
                result.leftRotZ = Mathf.Lerp(a.leftRotZ, b.leftRotZ, t);
                result.leftRotW = Mathf.Lerp(a.leftRotW, b.leftRotW, t);
            }

            if (b.hasRight)
            {
                result.hasRight = true;
                result.rightPosX = Mathf.Lerp(a.rightPosX, b.rightPosX, t);
                result.rightPosY = Mathf.Lerp(a.rightPosY, b.rightPosY, t);
                result.rightPosZ = Mathf.Lerp(a.rightPosZ, b.rightPosZ, t);
                result.rightRotX = Mathf.Lerp(a.rightRotX, b.rightRotX, t);
                result.rightRotY = Mathf.Lerp(a.rightRotY, b.rightRotY, t);
                result.rightRotZ = Mathf.Lerp(a.rightRotZ, b.rightRotZ, t);
                result.rightRotW = Mathf.Lerp(a.rightRotW, b.rightRotW, t);
            }

            return result;
        }

        // ------------------------------------------------------------ 应用

        private void ApplyToVisuals(in BpfPosePayload pose)
        {
            if (headVisual != null)
            {
                headVisual.position = new Vector3(pose.headPosX, pose.headPosY, pose.headPosZ);
                headVisual.rotation = new Quaternion(
                    pose.headRotX, pose.headRotY, pose.headRotZ, pose.headRotW).normalized;
            }

            if (leftVisual != null && pose.hasLeft)
            {
                leftVisual.position = new Vector3(pose.leftPosX, pose.leftPosY, pose.leftPosZ);
                leftVisual.rotation = new Quaternion(
                    pose.leftRotX, pose.leftRotY, pose.leftRotZ, pose.leftRotW).normalized;
            }

            if (rightVisual != null && pose.hasRight)
            {
                rightVisual.position = new Vector3(pose.rightPosX, pose.rightPosY, pose.rightPosZ);
                rightVisual.rotation = new Quaternion(
                    pose.rightRotX, pose.rightRotY, pose.rightRotZ, pose.rightRotW).normalized;
            }
        }
    }
}
