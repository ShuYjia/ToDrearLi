using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Bpf.Networking.Tests
{
    /// <summary>
    /// 联机模块 EditMode 测试（放在 Editor 文件夹，编入 Assembly-CSharp-Editor，
    /// 无需 asmdef——联机主代码与 TexCore 互相引用，拆 asmdef 会成环）。
    ///
    /// 覆盖核心纯逻辑：三点解算数学（Gate E 跨设备对齐的根基，此前只验证过
    /// 单位变换一种情况）、变换互逆、命名消息载荷序列化往返、占用表状态机。
    /// </summary>
    [TestFixture]
    public sealed class BpfSessionSpaceTests
    {
        private static readonly Vector3 Q0 = new Vector3(0f, 0f, 0f);
        private static readonly Vector3 Q1 = new Vector3(1f, 0f, 0f);
        private static readonly Vector3 Q2 = new Vector3(0f, 1f, 0f);

        private const float AngleTolerance = 1e-2f;
        private const float PositionTolerance = 1e-3f;

        [Test]
        public void Solve_IdenticalPoints_ReturnsIdentity()
        {
            bool ok = BpfSessionSpace.TrySolve(Q0, Q1, Q2, Q0, Q1, Q2, out BpfSessionTransform t);

            Assert.IsTrue(ok);
            Assert.IsTrue(t.valid);
            Assert.LessOrEqual(t.position.magnitude, PositionTolerance);
            Assert.LessOrEqual(Quaternion.Angle(t.rotation, Quaternion.identity), AngleTolerance);
        }

        [Test]
        public void Solve_PureTranslation_RecoversOffset()
        {
            Vector3 offset = new Vector3(5f, -2f, 3f);

            bool ok = BpfSessionSpace.TrySolve(
                Q0, Q1, Q2, Q0 + offset, Q1 + offset, Q2 + offset, out BpfSessionTransform t);

            Assert.IsTrue(ok);
            Assert.LessOrEqual(Vector3.Distance(t.position, offset), PositionTolerance);
            Assert.LessOrEqual(Quaternion.Angle(t.rotation, Quaternion.identity), AngleTolerance);
        }

        [Test]
        public void Solve_RotationAroundY_RecoversRotation()
        {
            Quaternion expected = Quaternion.Euler(0f, 90f, 0f);

            bool ok = BpfSessionSpace.TrySolve(
                Q0, Q1, Q2, expected * Q0, expected * Q1, expected * Q2, out BpfSessionTransform t);

            Assert.IsTrue(ok);
            Assert.LessOrEqual(Quaternion.Angle(t.rotation, expected), AngleTolerance);
            Assert.LessOrEqual(t.position.magnitude, PositionTolerance);
        }

        [Test]
        public void Solve_RotationPlusTranslation_RoundTripsArbitraryPoint()
        {
            Quaternion rotation = Quaternion.Euler(15f, 40f, -25f);
            Vector3 offset = new Vector3(1.5f, 0.5f, -2f);
            System.Func<Vector3, Vector3> map = q => rotation * q + offset;

            bool ok = BpfSessionSpace.TrySolve(
                Q0, Q1, Q2, map(Q0), map(Q1), map(Q2), out BpfSessionTransform t);

            Assert.IsTrue(ok);
            Assert.LessOrEqual(Quaternion.Angle(t.rotation, rotation), AngleTolerance);
            Assert.LessOrEqual(Vector3.Distance(t.position, offset), PositionTolerance);

            // 第四点才是实际用途：变换必须对任意点成立，不只校准用的三个点
            Vector3 arbitrary = new Vector3(0.3f, -0.7f, 1.1f);
            Assert.LessOrEqual(Vector3.Distance(t.ApplyToPosition(arbitrary), map(arbitrary)), PositionTolerance);
        }

        [Test]
        public void Solve_CollinearPoints_Rejected()
        {
            bool ok = BpfSessionSpace.TrySolve(
                new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 1f), new Vector3(0f, 0f, 2f),
                new Vector3(1f, 0f, 0f), new Vector3(1f, 0f, 1f), new Vector3(1f, 0f, 2f),
                out _);

            Assert.IsFalse(ok);
        }

        [Test]
        public void Solve_CoincidentPoints_Rejected()
        {
            bool ok = BpfSessionSpace.TrySolve(Q0, Q0, Q0, Q0, Q1, Q2, out _);

            Assert.IsFalse(ok);
        }
    }

    /// <summary>
    /// <see cref="BpfSessionTransform"/> 正逆变换互逆（姿态同步与抓取同步
    /// 都依赖它：发送端 ApplyTo*，接收端 ApplyInverseTo*，任一侧错位
    /// 都会让远端物体整体偏移或镜像）。
    /// </summary>
    [TestFixture]
    public sealed class BpfSessionTransformTests
    {
        [Test]
        public void Inverse_RoundTripsPositionAndRotation()
        {
            BpfSessionTransform t = new BpfSessionTransform
            {
                position = new Vector3(2f, -1f, 0.5f),
                rotation = Quaternion.Euler(10f, 70f, -30f),
                valid = true,
            };

            Vector3 point = new Vector3(0.4f, 1.2f, -0.8f);
            Vector3 positionRoundTrip = t.ApplyInverseToPosition(t.ApplyToPosition(point));
            Assert.LessOrEqual(Vector3.Distance(positionRoundTrip, point), 1e-4f);

            Quaternion rotation = Quaternion.Euler(45f, 0f, 90f);
            Quaternion rotationRoundTrip = t.ApplyInverseToRotation(t.ApplyToRotation(rotation));
            Assert.LessOrEqual(Quaternion.Angle(rotationRoundTrip, rotation), 1e-2f);
        }

        [Test]
        public void Identity_ApplyIsNoOp()
        {
            Vector3 point = new Vector3(1f, 2f, 3f);
            Quaternion rotation = Quaternion.Euler(30f, 60f, 90f);

            Assert.LessOrEqual(Vector3.Distance(BpfSessionTransform.Identity.ApplyToPosition(point), point), 1e-6f);
            Assert.LessOrEqual(
                Quaternion.Angle(BpfSessionTransform.Identity.ApplyToRotation(rotation), rotation), 1e-4f);
        }
    }

    /// <summary>
    /// 命名消息载荷的序列化往返。读写顺序逐字段对齐是协议契约：
    /// 任何一侧字段增删造成错位，这里必须红。
    /// </summary>
    [TestFixture]
    public sealed class BpfPayloadSerializationTests
    {
        private const float FloatTolerance = 1e-4f;

        private static T RoundTrip<T>(T value) where T : struct, INetworkSerializable
        {
            using (FastBufferWriter writer = new FastBufferWriter(1024, Allocator.Temp))
            {
                writer.WriteNetworkSerializable(value);
                byte[] bytes = writer.ToArray();
                FastBufferReader reader = new FastBufferReader(bytes, Allocator.Temp);
                reader.ReadNetworkSerializable(out T result);
                reader.Dispose();
                return result;
            }
        }

        [Test]
        public void ObjectId_RoundTrips()
        {
            BpfObjectId id = new BpfObjectId("03SingleplayerScene", "A.选项一");

            BpfObjectId copy = RoundTrip(id);

            Assert.AreEqual(id.sceneName, copy.sceneName);
            Assert.AreEqual(id.objectName, copy.objectName);
        }

        [Test]
        public void ObjectState_RoundTrips()
        {
            BpfObjectState state = new BpfObjectState
            {
                id = new BpfObjectId("S", "O"),
                used = true,
                ownerClientId = 42,
                value = 7,
            };

            BpfObjectState copy = RoundTrip(state);

            Assert.AreEqual(state.id, copy.id);
            Assert.AreEqual(state.used, copy.used);
            Assert.AreEqual(state.ownerClientId, copy.ownerClientId);
            Assert.AreEqual(state.value, copy.value);
        }

        [Test]
        public void InteractionRequest_RoundTrips()
        {
            BpfInteractionRequestPayload payload = new BpfInteractionRequestPayload
            {
                clientId = 7,
                id = new BpfObjectId("S", "O"),
            };

            BpfInteractionRequestPayload copy = RoundTrip(payload);

            Assert.AreEqual(payload.clientId, copy.clientId);
            Assert.AreEqual(payload.id, copy.id);
        }

        [Test]
        public void InteractiveRequest_RoundTrips()
        {
            BpfInteractiveRequestPayload payload = new BpfInteractiveRequestPayload
            {
                clientId = 9,
                id = new BpfObjectId("S", "Grab"),
                grab = true,
            };

            BpfInteractiveRequestPayload copy = RoundTrip(payload);

            Assert.AreEqual(payload.clientId, copy.clientId);
            Assert.AreEqual(payload.id, copy.id);
            Assert.AreEqual(payload.grab, copy.grab);
        }

        [Test]
        public void InteractiveResult_RoundTrips()
        {
            BpfInteractiveResultPayload payload = new BpfInteractiveResultPayload
            {
                id = new BpfObjectId("S", "Grab"),
                granted = true,
                held = false,
                ownerClientId = 13,
            };

            BpfInteractiveResultPayload copy = RoundTrip(payload);

            Assert.AreEqual(payload.id, copy.id);
            Assert.AreEqual(payload.granted, copy.granted);
            Assert.AreEqual(payload.held, copy.held);
            Assert.AreEqual(payload.ownerClientId, copy.ownerClientId);
        }

        [Test]
        public void InteractivePose_RoundTrips()
        {
            BpfInteractivePosePayload payload = new BpfInteractivePosePayload
            {
                id = new BpfObjectId("S", "Grab"),
                posX = 0.25f,
                posY = -1.5f,
                posZ = 2.75f,
                rotX = 0.1f,
                rotY = 0.2f,
                rotZ = 0.3f,
                rotW = 0.9f,
                ownerClientId = 21,
                held = true,
            };

            BpfInteractivePosePayload copy = RoundTrip(payload);

            Assert.AreEqual(payload.id, copy.id);
            Assert.AreEqual(payload.posX, copy.posX, FloatTolerance);
            Assert.AreEqual(payload.posY, copy.posY, FloatTolerance);
            Assert.AreEqual(payload.posZ, copy.posZ, FloatTolerance);
            Assert.AreEqual(payload.rotX, copy.rotX, FloatTolerance);
            Assert.AreEqual(payload.rotY, copy.rotY, FloatTolerance);
            Assert.AreEqual(payload.rotZ, copy.rotZ, FloatTolerance);
            Assert.AreEqual(payload.rotW, copy.rotW, FloatTolerance);
            Assert.AreEqual(payload.ownerClientId, copy.ownerClientId);
            Assert.AreEqual(payload.held, copy.held);
        }

        [Test]
        public void InteractiveSnapshotItem_RoundTrips()
        {
            BpfInteractiveSnapshotItem item = new BpfInteractiveSnapshotItem
            {
                id = new BpfObjectId("S", "Grab"),
                posX = 1f,
                posY = 2f,
                posZ = 3f,
                rotW = 1f,
                held = true,
                ownerClientId = 5,
            };

            BpfInteractiveSnapshotItem copy = RoundTrip(item);

            Assert.AreEqual(item.id, copy.id);
            Assert.AreEqual(item.posX, copy.posX, FloatTolerance);
            Assert.AreEqual(item.held, copy.held);
            Assert.AreEqual(item.ownerClientId, copy.ownerClientId);
        }

        [Test]
        public void TopicState_RoundTrips()
        {
            BpfTopicStatePayload payload = new BpfTopicStatePayload
            {
                epoch = 12,
                stepIndex = 3,
                haveScore = 80,
                errorNum = 2,
                isSubmit = true,
                topicName = new FixedString64Bytes("01检查机器人"),
            };

            BpfTopicStatePayload copy = RoundTrip(payload);

            Assert.AreEqual(payload.epoch, copy.epoch);
            Assert.AreEqual(payload.stepIndex, copy.stepIndex);
            Assert.AreEqual(payload.haveScore, copy.haveScore);
            Assert.AreEqual(payload.errorNum, copy.errorNum);
            Assert.AreEqual(payload.isSubmit, copy.isSubmit);
            Assert.AreEqual(payload.topicName, copy.topicName);
        }

        [Test]
        public void AvEvent_RoundTrips()
        {
            BpfAvEventPayload payload = new BpfAvEventPayload
            {
                eventId = 99,
                startServerTime = 123.456,
                state = 1,
                kind = 2,
                argument = 4,
                assetName = new FixedString64Bytes("step_voice_03"),
            };

            BpfAvEventPayload copy = RoundTrip(payload);

            Assert.AreEqual(payload.eventId, copy.eventId);
            Assert.AreEqual(payload.startServerTime, copy.startServerTime, 1e-6);
            Assert.AreEqual(payload.state, copy.state);
            Assert.AreEqual(payload.kind, copy.kind);
            Assert.AreEqual(payload.argument, copy.argument, 1e-6);
            Assert.AreEqual(payload.assetName, copy.assetName);
        }
    }

    /// <summary>
    /// 占用表状态机。<see cref="BpfObjectId"/> 是结构体、按内容做字典键——
    /// "同物体两次独立构造 id 必须命中同一条"这条锁住键语义，否则仲裁
    /// 会出现幽灵重复项（同一物体被两人同时"占用"）。
    /// </summary>
    [TestFixture]
    public sealed class BpfObjectStateRegistryTests
    {
        [SetUp]
        public void SetUp()
        {
            BpfObjectStateRegistry.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            BpfObjectStateRegistry.Clear();
        }

        [Test]
        public void SetThenTryGet_ReturnsSameState()
        {
            BpfObjectId id = new BpfObjectId("S", "O");

            BpfObjectStateRegistry.Set(id, true, 7, 3);

            Assert.IsTrue(BpfObjectStateRegistry.TryGet(id, out BpfObjectState state));
            Assert.IsTrue(state.used);
            Assert.AreEqual(7ul, state.ownerClientId);
            Assert.AreEqual(3, state.value);
        }

        [Test]
        public void Key_IsContentBased_NotReferenceBased()
        {
            BpfObjectStateRegistry.Set(new BpfObjectId("S", "O"), true, 1, 1);

            bool found = BpfObjectStateRegistry.TryGet(new BpfObjectId("S", "O"), out BpfObjectState state);

            Assert.IsTrue(found);
            Assert.AreEqual(1ul, state.ownerClientId);
            Assert.AreEqual(1, BpfObjectStateRegistry.Count);
        }

        [Test]
        public void DifferentObjects_DoNotCollide()
        {
            BpfObjectStateRegistry.Set(new BpfObjectId("S", "A"), true, 1, 1);
            BpfObjectStateRegistry.Set(new BpfObjectId("S", "B"), true, 2, 1);

            Assert.AreEqual(2, BpfObjectStateRegistry.Count);
            Assert.IsTrue(BpfObjectStateRegistry.TryGet(new BpfObjectId("S", "A"), out BpfObjectState a));
            Assert.IsTrue(BpfObjectStateRegistry.TryGet(new BpfObjectId("S", "B"), out BpfObjectState b));
            Assert.AreEqual(1ul, a.ownerClientId);
            Assert.AreEqual(2ul, b.ownerClientId);
        }

        [Test]
        public void Snapshot_ReflectsAllEntries()
        {
            BpfObjectStateRegistry.Set(new BpfObjectId("S", "A"), true, 1, 5);
            BpfObjectStateRegistry.Set(new BpfObjectId("S", "B"), false, 0, 0);

            List<BpfObjectState> snapshot = BpfObjectStateRegistry.Snapshot();

            Assert.AreEqual(2, snapshot.Count);
        }

        [Test]
        public void ApplySnapshot_ReplacesContent()
        {
            BpfObjectStateRegistry.Set(new BpfObjectId("S", "A"), true, 1, 1);

            List<BpfObjectState> incoming = new List<BpfObjectState>
            {
                new BpfObjectState
                {
                    id = new BpfObjectId("S", "X"),
                    used = true,
                    ownerClientId = 9,
                    value = 4,
                },
            };
            BpfObjectStateRegistry.ApplySnapshot(incoming);

            Assert.AreEqual(1, BpfObjectStateRegistry.Count);
            Assert.IsTrue(BpfObjectStateRegistry.TryGet(new BpfObjectId("S", "X"), out BpfObjectState state));
            Assert.AreEqual(9ul, state.ownerClientId);
            Assert.IsFalse(BpfObjectStateRegistry.TryGet(new BpfObjectId("S", "A"), out _));
        }

        [Test]
        public void Clear_EmptiesRegistry()
        {
            BpfObjectStateRegistry.Set(new BpfObjectId("S", "A"), true, 1, 1);

            BpfObjectStateRegistry.Clear();

            Assert.AreEqual(0, BpfObjectStateRegistry.Count);
        }

        [Test]
        public void TryGet_MissingEntry_ReturnsFalse()
        {
            bool found = BpfObjectStateRegistry.TryGet(new BpfObjectId("S", "Nope"), out _);

            Assert.IsFalse(found);
        }
    }
}
