using System.Collections;
using System.Collections.Generic;
using MixedReality.Toolkit.SpatialManipulation;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Bpf.Networking
{
    /// <summary>
    /// 联机系统预检自测（开发用）：阶段 5 收尾后的一次全链路回归。
    ///
    /// 背景：阶段 5 改动了共享基础设施（Bootstrap 新增组件、Calibration
    /// 新增 NotifyPointHint、LobbyPanel 重写），需要确认阶段 3/4 的既有
    /// 行为没有被带坏。本协程按真实使用顺序把各阶段可单机验证的验收项
    /// 串起来跑一遍，输出统一 PASS/FAIL 清单——通过即视为"可上设备"。
    ///
    /// 单协程的原因同 <see cref="BpfStage4SelfTest"/>：规避 play 会话中途
    /// 强制重编译导致跨调用状态丢失。
    ///
    /// 覆盖：
    ///   P1  无会话点击直通（阶段 3：单机行为不变）
    ///   P2  建房 → Lobby，面板创建
    ///   P3  未校准抓取被拒（阶段 4 G1）
    ///   P4  扳机采集三点 → 上报 → 解算 → Playing（阶段 5 采集入口）
    ///   P5  Host 点击仲裁 + 0.5s 镜像回环（阶段 3）
    ///   P6  Host 抓取仲裁 + 10Hz 位姿流（阶段 4 G2/G3）
    ///   P7  远端位姿插值无跳变 + 收敛 + 落点硬对齐（阶段 4 G4/G5）
    ///   P8  断线释放 + Manipulator 归还（阶段 4 G6）
    ///   P9  晚加入抓取物快照（阶段 4 G7）
    ///   P10 非 Playing 点击被拦（阶段 3）
    ///   P11 面板紧凑模式 + 校准区可见性（阶段 5 UI）
    ///   P12 离开清理：占用表/监听/会话根（阶段 3 X）
    /// </summary>
    [DisallowMultipleComponent]
    public class BpfPreflightSelfTest : MonoBehaviour
    {
        private const string LogTag = "Bpf|Preflight";
        private const string GrabObjectName = "BPFTestGrab";
        private const string ClickObjectName = "BPFTestClick";
        private const string ClickObject2Name = "BPFTestClick2";
        private const ulong FakeRemoteClient = 42;

        private int passed;
        private int failed;

        public static void Run()
        {
            if (Object.FindObjectOfType<BpfPreflightSelfTest>() != null)
            {
                BpfNetLog.Warning(LogTag, "already_running", "预检已在进行中");
                return;
            }

            GameObject go = new GameObject("BpfPreflightSelfTest");
            go.AddComponent<BpfPreflightSelfTest>();
        }

        private IEnumerator Start()
        {
            yield return Execute();
        }

        private IEnumerator Execute()
        {
            BpfNetLog.Info(LogTag, "begin", "===== 联机系统预检开始 =====");

            // ---------------------------------------------------------- 准备
            BpfNetworkBootstrap root = BpfNetworkBootstrap.EnsureRuntimeRoot();

            // P1：无会话点击直通
            GameObject clickObj = SpawnClickObject(ClickObjectName);
            TexCore_BusinessManager.OnClick(clickObj, gameObject.scene.name);
            int countBeforeRoom = BpfObjectStateRegistry.Count;
            Check("P1 无会话点击直通（占用表不记）", countBeforeRoom == 0,
                "count=" + countBeforeRoom);

            // P2：建房
            root.RoomService.LocalPlayerName = "Preflight";
            root.RoomService.CreateRoom("预检房间");
            yield return new WaitForSeconds(1f);

            NetworkManager nm = root.NetworkManager;
            Check("P2 Host 监听", nm != null && nm.IsListening && nm.IsHost,
                "listening=" + (nm != null && nm.IsListening));

            BpfSessionState session = BpfSessionState.Instance;
            Check("P2 会话状态对象", session != null, session != null ? session.Phase.ToString() : "null");

            BpfLanLobbyPanel panel = BpfLanLobbyPanel.EnsureFor();
            Check("P2 大厅面板已创建", panel != null, panel != null ? panel.gameObject.name : "null");

            BpfInteractiveObject grabObj = SpawnGrabObject();

            // P3：未校准抓取被拒
            bool grabBeforePlaying = grabObj.BeginLocalGrab();
            Check("P3 未校准抓取被拒", !grabBeforePlaying, "returned=" + grabBeforePlaying);

            // P11a：面板模式（当前场景非大厅场景 → 紧凑）+ 校准区在大堂可见
            RectTransform panelRect = (RectTransform)panel.transform;
            Check("P11a 面板紧凑模式（非大厅场景）", Mathf.Approximately(panelRect.sizeDelta.y, 340f),
                "height=" + panelRect.sizeDelta.y);
            Transform calSection = panel.transform.Find("校准区");
            Check("P11b 校准区在 Lobby 可见",
                calSection != null && calSection.gameObject.activeSelf,
                calSection != null ? calSection.gameObject.activeSelf.ToString() : "missing");

            // ---------------------------------------------------------- P4 校准采集
            SpawnDummyHands();
            yield return new WaitForSeconds(2.5f); // 等 Rig 重扫解析到手

            if (session != null)
            {
                session.Calibration.BeginCalibration();
            }

            BpfCalibrationCapture capture = root.CalibrationCapture;
            bool c1 = capture != null && capture.CaptureNextPoint(true);
            bool c2 = capture != null && capture.CaptureNextPoint(true);
            bool c3 = capture != null && capture.CaptureNextPoint(true);
            Check("P4 扳机采集三点均被接受", c1 && c2 && c3,
                c1 + "/" + c2 + "/" + c3);

            yield return new WaitForSeconds(0.5f);
            Check("P4 采集后进入 Playing",
                session != null && session.Phase == BpfSessionPhase.Playing,
                session != null ? session.Phase.ToString() : "null");
            Check("P4 Host 自身变换为单位变换",
                BpfSessionCalibration.LocalTransform.valid
                && BpfSessionCalibration.LocalTransform.position.magnitude < 0.001f,
                BpfSessionSpace.Describe(BpfSessionCalibration.LocalTransform));

            // ---------------------------------------------------------- P5 点击桥接
            int countBeforeClick = BpfObjectStateRegistry.Count;
            TexCore_BusinessManager.OnClick(clickObj, gameObject.scene.name);
            BpfObjectStateRegistry.TryGet(
                new BpfObjectId(gameObject.scene.name, ClickObjectName), out BpfObjectState clicked);
            Check("P5 Host 点击放行入占用表",
                clicked.used && clicked.ownerClientId == nm.LocalClientId,
                "used=" + clicked.used + " owner=" + clicked.ownerClientId);

            yield return new WaitForSeconds(0.7f); // 0.5s 镜像周期
            bool mirrorRoundTrip = ReadBridgeFlag("hasRemoteTopicState");
            Check("P5 面板状态镜像回环（本机客户端收到）", mirrorRoundTrip,
                "hasRemoteTopicState=" + mirrorRoundTrip);

            // ---------------------------------------------------------- P6 抓取
            bool grabAccepted = grabObj.BeginLocalGrab();
            BpfObjectStateRegistry.TryGet(grabObj.Id, out BpfObjectState grabbed);
            Check("P6 Host 抓取仲裁通过",
                grabAccepted && grabObj.LocalHeld && grabbed.used,
                "accepted=" + grabAccepted + " held=" + grabObj.LocalHeld);

            int framesBefore = BpfInteractiveObject.PoseFramesSent;
            grabObj.transform.position = new Vector3(1f, 1.5f, 1.5f);
            yield return new WaitForSeconds(0.7f);
            int framesDelta = BpfInteractiveObject.PoseFramesSent - framesBefore;
            Check("P6 位姿流持续发送（10Hz）", framesDelta >= 5, "frames=" + framesDelta);

            grabObj.EndLocalRelease();
            yield return new WaitForSeconds(0.2f);
            Check("P6 释放后占用表清空",
                !BpfObjectStateRegistry.TryGet(grabObj.Id, out BpfObjectState rel) || !rel.used,
                "used=" + (BpfObjectStateRegistry.TryGet(grabObj.Id, out BpfObjectState r2) && r2.used));

            // ---------------------------------------------------------- P7 插值
            // 编辑器帧率可能低于插值窗口（0.15s），首帧采样会直接落在目标上。
            // 自测时把窗口临时放到 1s，保证任意帧率下都能观察到渐进过程。
            float originalWindow = grabObj.InterpolationWindow;
            grabObj.InterpolationWindow = 1f;

            Vector3 remoteTarget = new Vector3(2f, 1.5f, 1.5f);
            FeedRemotePose(grabObj.Id, remoteTarget, FakeRemoteClient, true);

            var trajectory = new List<float>();
            for (int i = 0; i < 8; i++)
            {
                trajectory.Add(grabObj.transform.position.x);
                yield return null;
            }

            float firstX = trajectory[0];
            float lastX = trajectory[trajectory.Count - 1];
            grabObj.InterpolationWindow = originalWindow;

            yield return new WaitForSeconds(1.2f); // 等插值走完
            float convergedX = grabObj.transform.position.x;

            Check("P7 远端位姿首帧不瞬移", Mathf.Abs(firstX - remoteTarget.x) > 0.01f,
                "firstX=" + firstX.ToString("F3") + " target=" + remoteTarget.x.ToString("F3"));
            Check("P7 插值轨迹渐进", lastX > firstX + 0.001f,
                "traj=[" + string.Join(",", trajectory.ConvertAll(f => f.ToString("F3"))) + "]");
            Check("P7 随后收敛到目标", Mathf.Abs(convergedX - remoteTarget.x) < 0.05f,
                "convergedX=" + convergedX.ToString("F3"));

            Vector3 releaseTarget = new Vector3(2.5f, 1.5f, 1.5f);
            FeedRemotePose(grabObj.Id, releaseTarget, FakeRemoteClient, false);
            yield return null;
            Check("P7 释放落点硬对齐",
                Vector3.Distance(grabObj.transform.position, releaseTarget) < 0.001f,
                grabObj.transform.position.ToString("F3"));

            // ---------------------------------------------------------- P8 断线释放
            BpfObjectStateRegistry.Set(grabObj.Id, true, FakeRemoteClient, 1);
            BpfInteractiveRegistry.ReleaseByOwner(FakeRemoteClient);
            BpfObjectStateRegistry.TryGet(grabObj.Id, out BpfObjectState afterDrop);
            ObjectManipulator manipulator = grabObj.GetComponent<ObjectManipulator>();
            Check("P8 断线释放占用表", !afterDrop.used, "used=" + afterDrop.used);
            Check("P8 断线释放后 Manipulator 归还",
                manipulator == null || manipulator.enabled,
                manipulator != null ? manipulator.enabled.ToString() : "no-manipulator");

            // ---------------------------------------------------------- P9 晚加入快照
            Vector3 snapshotPose = new Vector3(3f, 1.5f, 1.5f);
            grabObj.transform.position = snapshotPose;
            BpfObjectStateRegistry.Set(grabObj.Id, true, FakeRemoteClient, 1);
            BpfInteractiveRegistry.SendSnapshotTo(nm.LocalClientId);
            yield return null;
            Check("P9 晚加入快照自收（位姿保持）",
                Vector3.Distance(grabObj.transform.position, snapshotPose) < 0.01f,
                grabObj.transform.position.ToString("F3"));
            BpfInteractiveRegistry.ReleaseByOwner(FakeRemoteClient);

            // ---------------------------------------------------------- P10 非 Playing 拦截
            if (session != null)
            {
                session.ServerSetPhase(BpfSessionPhase.Lobby);
            }

            yield return new WaitForSeconds(0.2f);
            GameObject clickObj2 = SpawnClickObject(ClickObject2Name);
            int countBeforeBlocked = BpfObjectStateRegistry.Count;
            TexCore_BusinessManager.OnClick(clickObj2, gameObject.scene.name);
            Check("P10 非 Playing 点击被拦（占用表不增长）",
                BpfObjectStateRegistry.Count == countBeforeBlocked,
                "before=" + countBeforeBlocked + " after=" + BpfObjectStateRegistry.Count);

            if (session != null)
            {
                session.ServerSetPhase(BpfSessionPhase.Playing);
            }

            yield return new WaitForSeconds(0.2f);
            Check("P11c Playing 后校准区收起",
                calSection != null && !calSection.gameObject.activeSelf,
                calSection != null ? calSection.gameObject.activeSelf.ToString() : "missing");

            // ---------------------------------------------------------- P12 离开
            root.RoomService.LeaveRoom();
            yield return new WaitForSeconds(1f);
            Check("P12 离开后 NGO 停止", !nm.IsListening, "listening=" + nm.IsListening);
            Check("P12 离开后占用表清空", BpfObjectStateRegistry.Count == 0,
                "count=" + BpfObjectStateRegistry.Count);
            Check("P12 离开后会话根销毁", BpfSessionState.Instance == null,
                BpfSessionState.Instance != null ? "alive" : "destroyed");
            Check("P12 离开后阶段回 Offline", root.SessionPhase == BpfSessionPhase.Offline,
                root.SessionPhase.ToString());

            // ---------------------------------------------------------- 汇总
            BpfNetLog.Info(LogTag, "summary",
                "===== 预检结束：PASS " + passed + " / FAIL " + failed + " =====");

            Destroy(clickObj);
            Destroy(clickObj2);
            if (grabObj != null)
            {
                Destroy(grabObj.gameObject);
            }

            Destroy(gameObject);
        }

        // ------------------------------------------------------------ 工具

        private BpfInteractiveObject SpawnGrabObject()
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

        private static GameObject SpawnClickObject(string name)
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

        /// <summary>编辑器无手柄：造假手柄让 Rig 解析到手（校准采集需要）。</summary>
        private static void SpawnDummyHands()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            Transform rigRoot = cam.transform.root;
            if (rigRoot.Find("Left Controller") != null)
            {
                return;
            }

            GameObject left = new GameObject("Left Controller");
            left.transform.SetParent(rigRoot, false);
            left.transform.localPosition = new Vector3(-0.25f, -0.25f, 0.4f);

            GameObject right = new GameObject("Right Controller");
            right.transform.SetParent(rigRoot, false);
            right.transform.localPosition = new Vector3(0.25f, -0.25f, 0.4f);
        }

        private static void FeedRemotePose(BpfObjectId id, Vector3 position, ulong owner, bool held)
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
                ownerClientId = owner,
                held = held,
            };

            using (FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp))
            {
                writer.WriteNetworkSerializable(payload);
                byte[] bytes = writer.ToArray();
                FastBufferReader reader = new FastBufferReader(bytes, Allocator.Temp);
                BpfInteractiveObject.HandleInteractivePose(FakeRemoteClient, reader);
                reader.Dispose();
            }
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
