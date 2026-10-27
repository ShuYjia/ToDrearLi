using MixedReality.Toolkit.SpatialManipulation;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Bpf.Networking
{
    /// <summary>
    /// 抓取物运行时注册表（阶段 4）。
    ///
    /// 职责：
    ///   - 维护 BpfObjectId → BpfInteractiveObject 的运行时映射（消息路由用）；
    ///   - 场景加载后扫描带 ObjectManipulator 的物体自动挂载组件
    ///     （场景文件零改动，运行为期装配）；
    ///   - Host 侧断线释放：某客户端掉线时释放它占用的物体并广播；
    ///   - 晚加入补齐：把各抓取物当前位姿/占用一次性发给新客户端。
    /// </summary>
    public static class BpfInteractiveRegistry
    {
        private const string LogTag = "Bpf|InteractiveRegistry";

        private static readonly Dictionary<BpfObjectId, BpfInteractiveObject> Objects =
            new Dictionary<BpfObjectId, BpfInteractiveObject>();

        public static int Count => Objects.Count;

        public static void Register(BpfInteractiveObject obj)
        {
            if (obj == null)
            {
                return;
            }

            Objects[obj.Id] = obj;
        }

        public static void Unregister(BpfInteractiveObject obj)
        {
            if (obj == null)
            {
                return;
            }

            BpfInteractiveObject current;
            if (Objects.TryGetValue(obj.Id, out current) && current == obj)
            {
                Objects.Remove(obj.Id);
            }
        }

        public static BpfInteractiveObject Find(BpfObjectId id)
        {
            BpfInteractiveObject obj;
            Objects.TryGetValue(id, out obj);
            return obj;
        }

        public static void ApplyRemotePose(BpfInteractivePosePayload payload)
        {
            BpfInteractiveObject obj = Find(payload.id);
            if (obj != null)
            {
                obj.FeedRemotePose(payload);
            }
        }

        public static void ApplyResult(BpfObjectId id, bool granted, bool held, ulong ownerClientId)
        {
            BpfInteractiveObject obj = Find(id);
            if (obj != null)
            {
                obj.ApplyResult(granted, held, ownerClientId);
            }
        }

        /// <summary>
        /// 扫描当前场景：给带 ObjectManipulator 且没有 BpfInteractiveObject
        /// 的物体自动挂载。包含未激活物体（业务脚本可能稍后激活）。
        /// </summary>
        public static void ScanActiveScene()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ObjectManipulator[] manipulators = Object.FindObjectsOfType<ObjectManipulator>(true);
            int attached = 0;

            foreach (ObjectManipulator manipulator in manipulators)
            {
                if (manipulator == null)
                {
                    continue;
                }

                GameObject go = manipulator.gameObject;
                if (go.GetComponent<BpfInteractiveObject>() != null)
                {
                    continue;
                }

                go.AddComponent<BpfInteractiveObject>();
                attached++;
            }

            if (attached > 0)
            {
                BpfNetLog.Info(LogTag, "scene_scanned",
                    "已为 " + attached + " 个抓取物自动挂载联机组件（共注册 " + Count + " 个）");
            }
        }

        /// <summary>
        /// Host 侧断线释放：某个客户端掉线时，释放它占用的所有抓取物并广播。
        /// 非 Host 上为空操作（占用表本来就是 Host 权威数据的镜像）。
        /// </summary>
        public static void ReleaseByOwner(ulong clientId)
        {
            NetworkManager manager = BpfNetworkBootstrap.Instance != null
                ? BpfNetworkBootstrap.Instance.NetworkManager
                : null;
            if (manager == null || !manager.IsServer)
            {
                return;
            }

            List<BpfObjectState> snapshot = BpfObjectStateRegistry.Snapshot();
            int released = 0;

            foreach (BpfObjectState state in snapshot)
            {
                if (!state.used || state.ownerClientId != clientId)
                {
                    continue;
                }

                BpfObjectStateRegistry.Set(state.id, false, 0, 0);
                released++;

                // 本地视图同步：占用者的抓取流已停止，通知全员重获抓取权
                BpfInteractiveObject obj = Find(state.id);
                if (obj != null)
                {
                    obj.ApplyResult(true, false, 0);
                }

                SendResult(state.id, true, false, 0);
            }

            if (released > 0)
            {
                BpfNetLog.Info(LogTag, "released_by_owner",
                    "客户端 " + clientId + " 掉线，已释放 " + released + " 个抓取物");
            }
        }

        /// <summary>
        /// 晚加入补齐（Host 调用）：把各抓取物当前位姿与占用发给新客户端。
        /// 与 BpfTexCoreNetworkBridge.SendSnapshotTo 配套——那条补业务占用表，
        /// 这条补抓取物位姿（场景物体的 Transform 不会随 NGO 快照恢复）。
        /// </summary>
        public static void SendSnapshotTo(ulong clientId)
        {
            NetworkManager manager = BpfNetworkBootstrap.Instance != null
                ? BpfNetworkBootstrap.Instance.NetworkManager
                : null;
            if (manager == null || !manager.IsServer)
            {
                return;
            }

            List<BpfInteractiveSnapshotItem> items = new List<BpfInteractiveSnapshotItem>();
            foreach (KeyValuePair<BpfObjectId, BpfInteractiveObject> pair in Objects)
            {
                BpfInteractiveObject obj = pair.Value;
                if (obj == null)
                {
                    continue;
                }

                BpfSessionTransform sessionTransform = BpfSessionCalibration.LocalTransform;
                Vector3 position = sessionTransform.valid
                    ? sessionTransform.ApplyToPosition(obj.transform.position)
                    : obj.transform.position;
                Quaternion rotation = sessionTransform.valid
                    ? sessionTransform.ApplyToRotation(obj.transform.rotation)
                    : obj.transform.rotation;

                bool held = false;
                ulong owner = 0;
                if (BpfObjectStateRegistry.TryGet(pair.Key, out BpfObjectState state))
                {
                    held = state.used;
                    owner = state.ownerClientId;
                }

                items.Add(new BpfInteractiveSnapshotItem
                {
                    id = pair.Key,
                    posX = position.x,
                    posY = position.y,
                    posZ = position.z,
                    rotX = rotation.x,
                    rotY = rotation.y,
                    rotZ = rotation.z,
                    rotW = rotation.w,
                    held = held,
                    ownerClientId = owner,
                });
            }

            try
            {
                using (FastBufferWriter writer = new FastBufferWriter(1024, Allocator.Temp))
                {
                    writer.WriteValueSafe(items.Count);
                    foreach (BpfInteractiveSnapshotItem item in items)
                    {
                        writer.WriteNetworkSerializable(item);
                    }

                    manager.CustomMessagingManager.SendNamedMessage(
                        BpfNetMessage.InteractiveSnapshot, clientId, writer, NetworkDelivery.Reliable);
                }

                BpfNetLog.Info(LogTag, "snapshot_sent",
                    "已向 clientId=" + clientId + " 发送抓取物快照（" + items.Count + " 个）");
            }
            catch (System.Exception e)
            {
                BpfNetLog.Warning(LogTag, "snapshot_failed", e.Message);
            }
        }

        /// <summary>Client 收到抓取物快照：位姿硬对齐 + 占用状态套用。</summary>
        public static void HandleInteractiveSnapshot(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int count);

            for (int i = 0; i < count; i++)
            {
                reader.ReadNetworkSerializable(out BpfInteractiveSnapshotItem item);

                BpfInteractiveObject obj = Find(item.id);
                if (obj == null)
                {
                    continue;
                }

                BpfSessionTransform sessionTransform = BpfSessionCalibration.LocalTransform;
                Vector3 rawPosition = new Vector3(item.posX, item.posY, item.posZ);
                Quaternion rawRotation = new Quaternion(
                    item.rotX, item.rotY, item.rotZ, item.rotW).normalized;

                Vector3 position = sessionTransform.valid
                    ? sessionTransform.ApplyInverseToPosition(rawPosition)
                    : rawPosition;
                Quaternion rotation = sessionTransform.valid
                    ? sessionTransform.ApplyInverseToRotation(rawRotation)
                    : rawRotation;

                obj.transform.SetPositionAndRotation(position, rotation);
                obj.ApplyResult(true, item.held, item.ownerClientId);
            }

            BpfNetLog.Info(LogTag, "snapshot_applied", "已补齐 " + count + " 个抓取物状态");
        }

        private static void SendResult(BpfObjectId id, bool granted, bool held, ulong ownerClientId)
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
    }
}
