using System.Collections;
using System.Collections.Generic;
using MixedReality.Toolkit.SpatialManipulation;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Bpf.Networking
{
    /// <summary>
    /// 客户端路径自测（开发用）：单机环境下所有实例都是 Host，客户端 origin
    /// 的消息处理路径（Host 代执行、占用仲裁、位姿转发、镜像接收）从未被
    /// 真正跑过。本协程用伪造的 sender/clientId 直接调各静态 handler，
    /// 把这些"真机才走"的路径在编辑器里验证一遍。
    ///
    /// 覆盖：
    ///   C1  客户端抓取请求 → Host 批准 → 占用表 owner=请求方 + 本机 Manipulator 禁用
    ///   C2  第二个客户端抓取同一物体 → 拒绝，占用表不变
    ///   C3  客户端位姿流 → Host 转发 + 本机插值跟随（物体移动）
    ///   C4  客户端释放请求 → 占用表清空 + Manipulator 归还
    ///   C5  客户端业务点击请求 → Host 代执行（重入抑制，业务分发真正跑）
    ///   C6  第二个客户端点击已占用对象 → 拒绝，占用表不变
    ///   C7  非 Playing 阶段的业务点击请求 → Host 侧阶段门拒绝
    ///   C8  客户端收到面板状态镜像 → 入库（hasRemoteTopicState）
    ///
    /// 单协程原因同其他自测：规避 play 会话中途强制重编译。
    /// </summary>
    [DisallowMultipleComponent]
    public class BpfClientPathSelfTest : MonoBehaviour
    {
        private const string LogTag = "Bpf|ClientPathTest";
        private const string GrabObjectName = "BPFTestGrab";
        private const string ClickObjectName = "BPFTestClick";
        private const string ClickObject2Name = "BPFTestClick2";
        private const ulong ClientA = 42;
        private const ulong ClientB = 43;

        private int passed;
        private int failed;

        public static void Run()
        {
            if (Object.FindObjectOfType<BpfClientPathSelfTest>() != null)
            {
                BpfNetLog.Warning(LogTag, "already_running", "自测已在进行中");
                return;
            }

            GameObject go = new GameObject("BpfClientPathSelfTest");
            go.AddComponent<BpfClientPathSelfTest>();
        }

        private IEnumerator Start()
        {
            yield return Execute();
        }

        private IEnumerator Execute()
        {
            BpfNetLog.Info(LogTag, "begin", "===== 客户端路径自测开始 =====");

            BpfNetworkBootstrap root = BpfNetworkBootstrap.EnsureRuntimeRoot();
            NetworkManager nm = root.NetworkManager;

            BpfInteractiveObject grabObj = SpawnGrabObject();
            GameObject clickObj = SpawnPlainObject(ClickObjectName);
            GameObject clickObj2 = SpawnPlainObject(ClickObject2Name);
            BpfObjectId clickId = new BpfObjectId(gameObject.scene.name, ClickObjectName);
            BpfObjectId click2Id = new BpfObjectId(gameObject.scene.name, ClickObject2Name);

            root.RoomService.LocalPlayerName = "ClientPathHost";
            root.RoomService.CreateRoom("客户端路径自测");
            yield return new WaitForSeconds(1f);

            BpfSessionState session = BpfSessionState.Instance;
            if (session != null)
            {
                session.ServerSetPhase(BpfSessionPhase.Playing);
            }

            yield return new WaitForSeconds(0.3f);
            Check("准备：Host 监听且 Playing",
                nm != null && nm.IsListening && nm.IsHost && root.SessionPhase == BpfSessionPhase.Playing,
                "phase=" + root.SessionPhase);

            // ---------------------------------------------------------- C1 抓取批准
            SendInteractiveRequest(ClientA, grabObj.Id, true);
            yield return null;

            BpfObjectStateRegistry.TryGet(grabObj.Id, out BpfObjectState afterGrab);
            ObjectManipulator manipulator = grabObj.GetComponent<ObjectManipulator>();
            Check("C1 客户端抓取被批准（占用表 owner=请求方）",
                afterGrab.used && afterGrab.ownerClientId == ClientA,
                "used=" + afterGrab.used + " owner=" + afterGrab.ownerClientId);
            Check("C1 本机 Manipulator 禁用（物体被远端占着）",
                manipulator == null || !manipulator.enabled,
                manipulator != null ? manipulator.enabled.ToString() : "no-manipulator");
            Check("C1 本机不认为自己在抓", !grabObj.LocalHeld, "held=" + grabObj.LocalHeld);

            // ---------------------------------------------------------- C2 抓取冲突
            SendInteractiveRequest(ClientB, grabObj.Id, true);
            yield return null;
            BpfObjectStateRegistry.TryGet(grabObj.Id, out BpfObjectState afterConflict);
            Check("C2 第二个客户端抓取被拒，占用表不变",
                afterConflict.used && afterConflict.ownerClientId == ClientA,
                "owner=" + afterConflict.ownerClientId);

            // ---------------------------------------------------------- C3 位姿流
            Vector3 streamTarget = new Vector3(2f, 1.5f, 1.5f);
            SendInteractivePose(ClientA, grabObj.Id, streamTarget, true);
            yield return new WaitForSeconds(0.6f);
            Check("C3 客户端位姿流驱动本机插值（物体被搬动）",
                grabObj.transform.position.x > 1f,
                grabObj.transform.position.ToString("F3"));

            // ---------------------------------------------------------- C4 释放
            SendInteractiveRequest(ClientA, grabObj.Id, false);
            yield return null;
            BpfObjectStateRegistry.TryGet(grabObj.Id, out BpfObjectState afterRelease);
            Check("C4 客户端释放后占用表清空", !afterRelease.used, "used=" + afterRelease.used);
            Check("C4 释放后 Manipulator 归还可用",
                manipulator == null || manipulator.enabled,
                manipulator != null ? manipulator.enabled.ToString() : "no-manipulator");

            // ---------------------------------------------------------- C5 代执行业务点击
            int executedBefore = BpfTexCoreNetworkBridge.ClickExecuted;
            int rejectedBefore = BpfTexCoreNetworkBridge.ClickRejected;
            SendBusinessClickRequest(ClientA, clickId);
            yield return null;

            BpfObjectStateRegistry.TryGet(clickId, out BpfObjectState afterClick);
            int executedDelta = BpfTexCoreNetworkBridge.ClickExecuted - executedBefore;
            int rejectedDelta = BpfTexCoreNetworkBridge.ClickRejected - rejectedBefore;
            Check("C5 客户端点击请求入占用表（owner=请求方）",
                afterClick.used && afterClick.ownerClientId == ClientA,
                "used=" + afterClick.used + " owner=" + afterClick.ownerClientId);
            Check("C5 Host 代执行放行了一次（重入抑制生效）", executedDelta == 1,
                "executedDelta=" + executedDelta);
            Check("C5 代执行过程没有误拒", rejectedDelta == 0, "rejectedDelta=" + rejectedDelta);

            // ---------------------------------------------------------- C6 业务点击冲突
            SendBusinessClickRequest(ClientB, clickId);
            yield return null;
            BpfObjectStateRegistry.TryGet(clickId, out BpfObjectState afterClick2);
            Check("C6 第二个客户端点击已占用对象被拒，占用表不变",
                afterClick2.used && afterClick2.ownerClientId == ClientA,
                "owner=" + afterClick2.ownerClientId);

            // ---------------------------------------------------------- C7 阶段门
            if (session != null)
            {
                session.ServerSetPhase(BpfSessionPhase.Lobby);
            }

            yield return new WaitForSeconds(0.2f);
            int countBeforeStageBlock = BpfObjectStateRegistry.Count;
            SendBusinessClickRequest(ClientA, click2Id);
            yield return null;
            Check("C7 非 Playing 的业务点击请求被 Host 阶段门拒绝",
                BpfObjectStateRegistry.Count == countBeforeStageBlock,
                "before=" + countBeforeStageBlock + " after=" + BpfObjectStateRegistry.Count);

            if (session != null)
            {
                session.ServerSetPhase(BpfSessionPhase.Playing);
            }

            yield return new WaitForSeconds(0.2f);

            // ---------------------------------------------------------- C8 镜像接收
            SendTopicState(ClientA, 3, 77);
            yield return null;
            bool mirrorStored = ReadBridgeFlag("hasRemoteTopicState");
            Check("C8 客户端侧收到面板状态镜像并入库", mirrorStored,
                "hasRemoteTopicState=" + mirrorStored);

            // ---------------------------------------------------------- 收尾
            root.RoomService.LeaveRoom();
            yield return new WaitForSeconds(1f);
            Check("收尾：离开后占用表清空", BpfObjectStateRegistry.Count == 0,
                "count=" + BpfObjectStateRegistry.Count);

            BpfNetLog.Info(LogTag, "summary",
                "===== 客户端路径自测结束：PASS " + passed + " / FAIL " + failed + " =====");

            Destroy(grabObj.gameObject);
            Destroy(clickObj);
            Destroy(clickObj2);
            Destroy(gameObject);
        }

        // ------------------------------------------------------------ 消息构造

        private static void SendInteractiveRequest(ulong clientId, BpfObjectId id, bool grab)
        {
            BpfInteractiveRequestPayload payload = new BpfInteractiveRequestPayload
            {
                clientId = clientId,
                id = id,
                grab = grab,
            };

            using (FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp))
            {
                writer.WriteNetworkSerializable(payload);
                byte[] bytes = writer.ToArray();
                FastBufferReader reader = new FastBufferReader(bytes, Allocator.Temp);
                BpfInteractiveObject.HandleInteractiveRequest(clientId, reader);
                reader.Dispose();
            }
        }

        private static void SendInteractivePose(
            ulong clientId, BpfObjectId id, Vector3 position, bool held)
        {
            BpfInteractivePosePayload payload = new BpfInteractivePosePayload
            {
                id = id,
                posX = position.x,
                posY = position.y,
                posZ = position.z,
                rotX = 0f,
                rotY = 0f,
                rotZ = 0f,
                rotW = 1f,
                ownerClientId = clientId,
                held = held,
            };

            using (FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp))
            {
                writer.WriteNetworkSerializable(payload);
                byte[] bytes = writer.ToArray();
                FastBufferReader reader = new FastBufferReader(bytes, Allocator.Temp);
                BpfInteractiveObject.HandleInteractivePose(clientId, reader);
                reader.Dispose();
            }
        }

        private static void SendBusinessClickRequest(ulong clientId, BpfObjectId id)
        {
            BpfInteractionRequestPayload payload = new BpfInteractionRequestPayload
            {
                clientId = clientId,
                id = id,
            };

            using (FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp))
            {
                writer.WriteNetworkSerializable(payload);
                byte[] bytes = writer.ToArray();
                FastBufferReader reader = new FastBufferReader(bytes, Allocator.Temp);
                BpfTexCoreNetworkBridge.HandleInteractionRequest(clientId, reader);
                reader.Dispose();
            }
        }

        private static void SendTopicState(ulong senderClientId, int stepIndex, int haveScore)
        {
            BpfTopicStatePayload payload = new BpfTopicStatePayload
            {
                epoch = 7,
                stepIndex = stepIndex,
                haveScore = haveScore,
                errorNum = 1,
                isSubmit = false,
                topicName = new FixedString64Bytes("自测题目"),
            };

            using (FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp))
            {
                writer.WriteNetworkSerializable(payload);
                byte[] bytes = writer.ToArray();
                FastBufferReader reader = new FastBufferReader(bytes, Allocator.Temp);
                BpfTexCoreNetworkBridge.HandleTopicState(senderClientId, reader);
                reader.Dispose();
            }
        }

        // ------------------------------------------------------------ 工具

        private static BpfInteractiveObject SpawnGrabObject()
        {
            GameObject existing = GameObject.Find(GrabObjectName);
            if (existing != null)
            {
                BpfInteractiveObject existingComponent = existing.GetComponent<BpfInteractiveObject>();
                if (existingComponent != null)
                {
                    return existingComponent;
                }
            }

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = GrabObjectName;
            go.transform.position = new Vector3(0f, 1.2f, 1.5f);
            go.transform.localScale = Vector3.one * 0.15f;
            go.AddComponent<ObjectManipulator>();
            return go.AddComponent<BpfInteractiveObject>();
        }

        private static GameObject SpawnPlainObject(string name)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null)
            {
                return existing;
            }

            GameObject go = new GameObject(name);
            go.transform.position = new Vector3(0.5f, 1.2f, 1.5f);
            return go;
        }

        private static bool ReadBridgeFlag(string fieldName)
        {
            BpfTexCoreNetworkBridge bridge = BpfTexCoreNetworkBridge.Instance;
            if (bridge == null)
            {
                return false;
            }

            var field = bridge.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field != null && (bool)field.GetValue(bridge);
        }

        private void Check(string name, bool ok, string detail)
        {
            if (ok)
            {
                passed++;
                BpfNetLog.Info(LogTag, "pass", name + " | " + detail);
            }
            else
            {
                failed++;
                BpfNetLog.Error(LogTag, "fail", name + " | " + detail);
            }
        }
    }
}
