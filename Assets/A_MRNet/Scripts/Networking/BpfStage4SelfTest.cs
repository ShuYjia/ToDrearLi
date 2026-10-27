using System.Collections;
using System.Collections.Generic;
using MixedReality.Toolkit.SpatialManipulation;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Bpf.Networking
{
    /// <summary>
    /// 阶段 4 单机自测（开发用，不进正式流程）。
    ///
    /// 为什么是单协程：实测本环境会在 play 会话中途强制重编译
    /// （Editor.log 可见 "forced synchronous recompile" + domain reload），
    /// 跨 execute_code 调用的运行期状态会丢。一个协程内部按帧推进，
    /// 不受调用间隙影响；真被 reload 打断也能从日志看出断点。
    ///
    /// 覆盖项：
    ///   G1 未校准（非 Playing）抓取被拒
    ///   G2 Host 抓取仲裁：占用表写入 + 本地持有 + 占用广播
    ///   G3 拥有者位姿流：10Hz 发送无异常（帧数计数器）
    ///   G4 远端位姿接收 + 双快照插值：首帧不瞬移、随后收敛
    ///   G5 释放落点硬对齐
    ///   G6 断线释放（ReleaseByOwner）+ Manipulator 归还
    ///   G7 晚加入快照补齐（SendSnapshotTo 自收）
    ///   A1 音视频拦截器安装 + Host 广播（EventsBroadcast）
    ///   A2 Client 排程到点播放（EventsExecuted）
    ///   X  离开房间：占用表清空、NGO 停止
    ///
    /// 入口：BpfStage4SelfTest.Run()（验证台「阶段4自测」按钮同此）。
    /// </summary>
    [DisallowMultipleComponent]
    public class BpfStage4SelfTest : MonoBehaviour
    {
        private const string LogTag = "Bpf|Stage4Test";
        private const string TestObjectName = "BPFTestGrab";
        private const ulong FakeRemoteClient = 42;

        private int passed;
        private int failed;

        public static void Run()
        {
            if (Object.FindObjectOfType<BpfStage4SelfTest>() != null)
            {
                BpfNetLog.Warning(LogTag, "already_running", "自测已在进行中");
                return;
            }

            GameObject go = new GameObject("BpfStage4SelfTest");
            go.AddComponent<BpfStage4SelfTest>();
        }

        private IEnumerator Start()
        {
            yield return Execute();
        }

        private IEnumerator Execute()
        {
            BpfNetLog.Info(LogTag, "begin", "===== 阶段 4 单机自测开始 =====");

            // ---------------------------------------------------------- 准备
            BpfNetworkBootstrap root = BpfNetworkBootstrap.EnsureRuntimeRoot();
            BpfInteractiveObject io = SpawnTestObject();
            Check("G0 测试抓取物已注册", io != null && BpfInteractiveRegistry.Find(io.Id) == io,
                "count=" + BpfInteractiveRegistry.Count);

            root.RoomService.LocalPlayerName = "HostStage4Test";
            root.RoomService.CreateRoom("BPF阶段4自测");
            yield return new WaitForSeconds(1f);

            NetworkManager nm = root.NetworkManager;
            Check("G0 Host 已监听", nm != null && nm.IsListening && nm.IsHost,
                "listening=" + (nm != null && nm.IsListening));

            BpfSessionState session = BpfSessionState.Instance;
            Check("G0 会话状态对象已生成", session != null,
                session != null ? session.Phase.ToString() : "null");

            // ---------------------------------------------------------- G1
            bool grabBeforePlaying = io.BeginLocalGrab();
            Check("G1 未校准抓取被拒", !grabBeforePlaying && !io.LocalHeld,
                "returned=" + grabBeforePlaying);

            // ---------------------------------------------------------- 进入 Playing
            if (session != null)
            {
                session.ServerSetPhase(BpfSessionPhase.Playing);
            }

            yield return new WaitForSeconds(0.3f);
            Check("G2 阶段权威与镜像一致（Playing）",
                root.SessionPhase == BpfSessionPhase.Playing,
                "boot=" + root.SessionPhase);

            // ---------------------------------------------------------- G2 抓取
            bool grabAccepted = io.BeginLocalGrab();
            BpfObjectStateRegistry.TryGet(io.Id, out BpfObjectState grabbed);
            Check("G2 Host 抓取仲裁通过",
                grabAccepted && io.LocalHeld && grabbed.used && grabbed.ownerClientId == nm.LocalClientId,
                "accepted=" + grabAccepted + " held=" + io.LocalHeld + " owner=" + grabbed.ownerClientId);

            // ---------------------------------------------------------- G3 位姿流
            int framesBefore = BpfInteractiveObject.PoseFramesSent;
            Vector3 movedTo = new Vector3(1f, 1.5f, 1.5f);
            io.transform.position = movedTo;
            yield return new WaitForSeconds(0.7f); // 10Hz ≈ 7 帧
            int framesDelta = BpfInteractiveObject.PoseFramesSent - framesBefore;
            Check("G3 拥有者位姿流持续发送（10Hz）", framesDelta >= 5,
                "frames=" + framesDelta);
            Check("G3 发送不影响本机 Transform",
                Vector3.Distance(io.transform.position, movedTo) < 0.001f,
                io.transform.position.ToString("F3"));

            // ---------------------------------------------------------- 释放后测远端插值
            io.EndLocalRelease();
            yield return new WaitForSeconds(0.2f);
            Check("G5 释放后本地持有清除", !io.LocalHeld, "held=" + io.LocalHeld);

            BpfObjectStateRegistry.TryGet(io.Id, out BpfObjectState released);
            Check("G5 释放后占用表清空", !released.used, "used=" + released.used);

            // ---------------------------------------------------------- G4 远端位姿 + 插值
            Vector3 restPosition = io.transform.position;
            Vector3 remoteTarget = new Vector3(2f, 1.5f, 1.5f);
            FeedRemotePose(io.Id, remoteTarget, Quaternion.identity, FakeRemoteClient, true);

            // 连续采样 8 帧轨迹：期望从出发点渐进逼近目标，而不是第一帧就到
            var trajectory = new List<float>();
            for (int i = 0; i < 8; i++)
            {
                trajectory.Add(io.transform.position.x);
                yield return null;
            }

            float firstFrameX = trajectory[0];
            float lastFrameX = trajectory[trajectory.Count - 1];
            float firstJump = Mathf.Abs(firstFrameX - remoteTarget.x);
            float residual = Mathf.Abs(lastFrameX - remoteTarget.x);
            bool progressive = firstFrameX < remoteTarget.x - 0.01f && lastFrameX > firstFrameX;

            BpfNetLog.Info(LogTag, "g4_trace",
                "window=" + io.InterpolationWindow.ToString("F2")
                + " rest=" + restPosition.x.ToString("F3")
                + " traj=[" + string.Join(",", trajectory.ConvertAll(f => f.ToString("F3"))) + "]");

            Check("G4 首帧不瞬移", firstJump > 0.01f,
                "firstJump=" + firstJump.ToString("F3"));
            Check("G4 轨迹渐进", progressive, "progressive=" + progressive);
            Check("G4 随后收敛到目标", residual < 0.05f,
                "residual=" + residual.ToString("F3"));

            // ---------------------------------------------------------- G5 落点硬对齐
            Vector3 releaseTarget = new Vector3(2.5f, 1.5f, 1.5f);
            FeedRemotePose(io.Id, releaseTarget, Quaternion.identity, FakeRemoteClient, false);
            yield return null;
            Check("G5 释放落点硬对齐",
                Vector3.Distance(io.transform.position, releaseTarget) < 0.001f,
                io.transform.position.ToString("F3"));

            // ---------------------------------------------------------- G6 断线释放
            BpfObjectStateRegistry.Set(io.Id, true, FakeRemoteClient, 1);
            BpfInteractiveRegistry.ReleaseByOwner(FakeRemoteClient);
            BpfObjectStateRegistry.TryGet(io.Id, out BpfObjectState afterDrop);
            ObjectManipulator manipulator = io.GetComponent<ObjectManipulator>();
            Check("G6 断线释放占用表", !afterDrop.used, "used=" + afterDrop.used);
            Check("G6 断线释放后 Manipulator 归还可用",
                manipulator == null || manipulator.enabled,
                manipulator != null ? manipulator.enabled.ToString() : "no-manipulator");

            // ---------------------------------------------------------- G7 晚加入快照
            Vector3 snapshotPose = new Vector3(3f, 1.5f, 1.5f);
            io.transform.position = snapshotPose;
            BpfObjectStateRegistry.Set(io.Id, true, FakeRemoteClient, 1);
            BpfInteractiveRegistry.SendSnapshotTo(nm.LocalClientId);
            yield return null;
            Check("G7 晚加入快照自收：位姿保持",
                Vector3.Distance(io.transform.position, snapshotPose) < 0.01f,
                io.transform.position.ToString("F3"));
            BpfInteractiveRegistry.ReleaseByOwner(FakeRemoteClient);

            // ---------------------------------------------------------- A1 广播
            int broadcastBefore = BpfSessionAVSync.EventsBroadcast;
            TexCore_AudioManager audio = CreateTestAudioManager();
            audio.audioClips = new List<AudioClip>();
            audio.PlayAudio("av_selftest");
            yield return null;
            int broadcastDelta = BpfSessionAVSync.EventsBroadcast - broadcastBefore;
            Check("A1 播放入口触发 Host 广播", broadcastDelta >= 1,
                "events=" + broadcastDelta);

            // ---------------------------------------------------------- A2 排程播放
            int executedBefore = BpfSessionAVSync.EventsExecuted;
            double fireAt = nm.ServerTime.Time + 0.3f;
            FeedAvEvent("audio", "av_selftest_scheduled", 0, fireAt);
            yield return new WaitForSeconds(0.6f);
            int executedDelta = BpfSessionAVSync.EventsExecuted - executedBefore;
            Check("A2 排程事件到点执行", executedDelta >= 1,
                "executed=" + executedDelta);

            // ---------------------------------------------------------- X 离开
            root.RoomService.LeaveRoom();
            yield return new WaitForSeconds(1f);
            Check("X 离开后 NGO 停止", !nm.IsListening, "listening=" + nm.IsListening);
            Check("X 离开后占用表清空", BpfObjectStateRegistry.Count == 0,
                "count=" + BpfObjectStateRegistry.Count);

            // ---------------------------------------------------------- 汇总
            BpfNetLog.Info(LogTag, "summary",
                "===== 阶段 4 单机自测结束：PASS " + passed + " / FAIL " + failed + " =====");

            Destroy(audio.gameObject);
            if (io != null)
            {
                Destroy(io.gameObject);
            }

            Destroy(gameObject);
        }

        // ------------------------------------------------------------ 工具

        private BpfInteractiveObject SpawnTestObject()
        {
            GameObject existing = GameObject.Find(TestObjectName);
            if (existing != null)
            {
                BpfInteractiveObject existingComponent = existing.GetComponent<BpfInteractiveObject>();
                if (existingComponent != null)
                {
                    return existingComponent;
                }
            }

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = TestObjectName;
            go.transform.position = new Vector3(0f, 1.2f, 1.5f);
            go.transform.localScale = Vector3.one * 0.15f;
            go.AddComponent<ObjectManipulator>();
            return go.AddComponent<BpfInteractiveObject>();
        }

        private TexCore_AudioManager CreateTestAudioManager()
        {
            GameObject go = new GameObject("AVTestAudio");
            go.AddComponent<AudioSource>();
            TexCore_AudioManager audio = go.AddComponent<TexCore_AudioManager>();
            return audio;
        }

        /// <summary>构造一条位姿流消息并直接喂给接收 handler（模拟远端拥有者）。</summary>
        private static void FeedRemotePose(
            BpfObjectId id, Vector3 position, Quaternion rotation, ulong owner, bool held)
        {
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

        /// <summary>构造一条音视频事件消息并直接喂给接收 handler（模拟 Host 广播）。</summary>
        private static void FeedAvEvent(string kind, string assetName, double argument, double startServerTime)
        {
            byte kindCode;
            byte state;
            switch (kind)
            {
                case "audio": kindCode = 0; state = 0; break;
                case "bgm": kindCode = 1; state = 0; break;
                case "stepvoice": kindCode = 2; state = 0; break;
                default: kindCode = 0; state = 0; break;
            }

            BpfAvEventPayload payload = new BpfAvEventPayload
            {
                eventId = 9001,
                startServerTime = startServerTime,
                state = state,
                kind = kindCode,
                argument = argument,
                assetName = new FixedString64Bytes(assetName ?? string.Empty),
            };

            using (FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp))
            {
                writer.WriteNetworkSerializable(payload);
                byte[] bytes = writer.ToArray();
                FastBufferReader reader = new FastBufferReader(bytes, Allocator.Temp);
                BpfSessionAVSync.HandleAvEvent(FakeRemoteClient, reader);
                reader.Dispose();
            }
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
