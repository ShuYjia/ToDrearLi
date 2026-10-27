using System;
using Unity.Collections;
using Unity.Netcode;

namespace Bpf.Networking
{
    /// <summary>
    /// NGO 命名消息（CustomMessagingManager）集中定义。
    ///
    /// 消息名一旦上线不要改名：旧版本 APK 与新版本混用时靠名字路由。
    /// 载荷一律用 FastBufferWriter/Reader 的 WriteValueSafe/ReadValueSafe，
    /// 读写顺序必须逐字段对齐，两端任何一侧字段增删都视为协议变更
    /// （需要同步提升 protocolVersion）。
    /// </summary>
    public static class BpfNetMessage
    {
        // ---- 已在阶段 0 之后启用的消息（提前占位，避免后期改名） ----

        /// <summary>玩家姿态广播（不可靠传输，Client -> 所有人，Host 权威转发略）。</summary>
        public const string PlayerPose = "bpf/player_pose";

        /// <summary>晚加入者请求完整会话快照（Client -> Host）。</summary>
        public const string SessionSnapshotRequest = "bpf/session_snapshot_request";

        /// <summary>Host 下发会话快照（Host -> 请求方）。</summary>
        public const string SessionSnapshot = "bpf/session_snapshot";

        /// <summary>Host 主动通知"房主已离开"（Host -> 所有人）。</summary>
        public const string HostLeft = "bpf/host_left";

        /// <summary>业务点击请求（Client -> Host）与结果广播（Host -> 所有人）。</summary>
        public const string BusinessClick = "bpf/business_click";
        public const string BusinessResult = "bpf/business_result";

        /// <summary>交互物体抓取/释放请求（Client -> Host）与占用变更广播。</summary>
        public const string InteractiveRequest = "bpf/interactive_request";
        public const string InteractiveResult = "bpf/interactive_result";

        /// <summary>抓取物位姿流（拥有者 -> Host -> 全员，低频不可靠传输）。</summary>
        public const string InteractivePose = "bpf/interactive_pose";

        /// <summary>晚加入时 Host 下发的抓取物位姿补齐（一次性，可靠传输）。</summary>
        public const string InteractiveSnapshot = "bpf/interactive_snapshot";

        /// <summary>校准点上报（每端 -> Host）与校准完成广播（Host -> 所有人）。</summary>
        public const string CalibrationReport = "bpf/calibration_report";
        public const string CalibrationComplete = "bpf/calibration_complete";

        /// <summary>音视频事件同步广播（Host -> 所有人，携带网络起始时间）。</summary>
        public const string AvEvent = "bpf/av_event";

        /// <summary>
        /// 阶段 3 业务桥接消息：
        ///   InteractionRequest  Client -> Host：业务点击/交互占用请求（Host 裁决执行）；
        ///   TopicState          Host -> 全员：题目面板状态镜像（0.5s 周期）；
        ///   ObjectSnapshot      Host -> 晚加入 Client：对象占用表一次性补齐。
        /// </summary>
        public const string InteractionRequest = "bpf/interaction_request";
        public const string TopicState = "bpf/topic_state";
        public const string ObjectSnapshot = "bpf/object_snapshot";
    }

    /// <summary>
    /// 玩家姿态载荷：头 + 双手的位置/旋转，25Hz 不可靠传输。
    /// 阶段 1 发送本机原始姿态；阶段 2 接入三点校准后，
    /// 发送前会经过 LocalPoseToSession 归一化到会话空间。
    /// </summary>
    public struct BpfPosePayload : INetworkSerializable
    {
        public ulong clientId;
        public float headPosX, headPosY, headPosZ;
        public float headRotX, headRotY, headRotZ, headRotW;
        public float leftPosX, leftPosY, leftPosZ;
        public float leftRotX, leftRotY, leftRotZ, leftRotW;
        public float rightPosX, rightPosY, rightPosZ;
        public float rightRotX, rightRotY, rightRotZ, rightRotW;
        public bool hasLeft;
        public bool hasRight;

        // NGO 1.5.2 的 INetworkSerializable 是泛型签名（BufferSerializer<T>），
        // 不是 NetworkSerializer——写错会报 CS0535 + CS0246。
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref headPosX);
            serializer.SerializeValue(ref headPosY);
            serializer.SerializeValue(ref headPosZ);
            serializer.SerializeValue(ref headRotX);
            serializer.SerializeValue(ref headRotY);
            serializer.SerializeValue(ref headRotZ);
            serializer.SerializeValue(ref headRotW);
            serializer.SerializeValue(ref leftPosX);
            serializer.SerializeValue(ref leftPosY);
            serializer.SerializeValue(ref leftPosZ);
            serializer.SerializeValue(ref leftRotX);
            serializer.SerializeValue(ref leftRotY);
            serializer.SerializeValue(ref leftRotZ);
            serializer.SerializeValue(ref leftRotW);
            serializer.SerializeValue(ref rightPosX);
            serializer.SerializeValue(ref rightPosY);
            serializer.SerializeValue(ref rightPosZ);
            serializer.SerializeValue(ref rightRotX);
            serializer.SerializeValue(ref rightRotY);
            serializer.SerializeValue(ref rightRotZ);
            serializer.SerializeValue(ref rightRotW);
            serializer.SerializeValue(ref hasLeft);
            serializer.SerializeValue(ref hasRight);
        }
    }

    /// <summary>
    /// 校准点上报（各端 → Host）：本机空间下三个物理标记点的坐标。
    /// 一次上报三个点，避免多包乱序；Host 收集齐后再统一解算。
    /// </summary>
    public struct BpfCalibrationReportPayload : INetworkSerializable
    {
        public ulong clientId;
        public float p0x, p0y, p0z;
        public float p1x, p1y, p1z;
        public float p2x, p2y, p2z;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref p0x);
            serializer.SerializeValue(ref p0y);
            serializer.SerializeValue(ref p0z);
            serializer.SerializeValue(ref p1x);
            serializer.SerializeValue(ref p1y);
            serializer.SerializeValue(ref p1z);
            serializer.SerializeValue(ref p2x);
            serializer.SerializeValue(ref p2y);
            serializer.SerializeValue(ref p2z);
        }
    }

    /// <summary>
    /// 校准完成广播（Host → 全员）：某个客户端的「本机空间 → 会话空间」变换。
    /// 每个客户端一条消息，接收端只认自己那条。
    /// </summary>
    public struct BpfCalibrationResultPayload : INetworkSerializable
    {
        public ulong clientId;
        public float posX, posY, posZ;
        public float rotX, rotY, rotZ, rotW;
        public bool valid;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref posX);
            serializer.SerializeValue(ref posY);
            serializer.SerializeValue(ref posZ);
            serializer.SerializeValue(ref rotX);
            serializer.SerializeValue(ref rotY);
            serializer.SerializeValue(ref rotZ);
            serializer.SerializeValue(ref rotW);
            serializer.SerializeValue(ref valid);
        }
    }

    /// <summary>
    /// 交互请求（Client → Host）：Client 只发请求，是否执行由 Host 裁决。
    /// </summary>
    public struct BpfInteractionRequestPayload : INetworkSerializable
    {
        public ulong clientId;
        public BpfObjectId id;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref id);
        }
    }

    /// <summary>
    /// 题目面板状态镜像（Host → 全员，0.5s 一次）：stepIndex / haveScore /
    /// errorNum / isSubmit + 当前题目名。epoch 用于丢弃乱序的旧包。
    /// </summary>
    public struct BpfTopicStatePayload : INetworkSerializable
    {
        public int epoch;
        public int stepIndex;
        public int haveScore;
        public int errorNum;
        public bool isSubmit;
        public FixedString64Bytes topicName;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref epoch);
            serializer.SerializeValue(ref stepIndex);
            serializer.SerializeValue(ref haveScore);
            serializer.SerializeValue(ref errorNum);
            serializer.SerializeValue(ref isSubmit);
            serializer.SerializeValue(ref topicName);
        }
    }

    /// <summary>
    /// 对象状态快照（Host → 新加入的 Client）：一次性把已占用的业务对象补齐。
    /// </summary>
    public struct BpfObjectSnapshotPayload : INetworkSerializable
    {
        public int count;
        public int epoch;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref count);
            serializer.SerializeValue(ref epoch);
        }
    }

    /// <summary>
    /// 抓取/释放请求（Client -> Host）。Host 自己抓着时本地直接仲裁，不走消息。
    /// </summary>
    public struct BpfInteractiveRequestPayload : INetworkSerializable
    {
        public ulong clientId;
        public BpfObjectId id;
        public bool grab; // true=抓取 false=释放

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref id);
            serializer.SerializeValue(ref grab);
        }
    }

    /// <summary>
    /// 占用变更广播（Host -> 全员）：granted=请求是否批准，
    /// held=当前是否被占用，ownerClientId=占用者（held=false 时为 0）。
    /// </summary>
    public struct BpfInteractiveResultPayload : INetworkSerializable
    {
        public BpfObjectId id;
        public bool granted;
        public bool held;
        public ulong ownerClientId;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref id);
            serializer.SerializeValue(ref granted);
            serializer.SerializeValue(ref held);
            serializer.SerializeValue(ref ownerClientId);
        }
    }

    /// <summary>
    /// 抓取物位姿流（拥有者 -> Host -> 全员）：低频权威写入。
    /// held=false 的最后一帧是释放落点，接收端硬对齐。
    /// </summary>
    public struct BpfInteractivePosePayload : INetworkSerializable
    {
        public BpfObjectId id;
        public float posX, posY, posZ;
        public float rotX, rotY, rotZ, rotW;
        public ulong ownerClientId;
        public bool held;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref id);
            serializer.SerializeValue(ref posX);
            serializer.SerializeValue(ref posY);
            serializer.SerializeValue(ref posZ);
            serializer.SerializeValue(ref rotX);
            serializer.SerializeValue(ref rotY);
            serializer.SerializeValue(ref rotZ);
            serializer.SerializeValue(ref rotW);
            serializer.SerializeValue(ref ownerClientId);
            serializer.SerializeValue(ref held);
        }
    }

    /// <summary>
    /// 晚加入补齐用的一条抓取物状态（Host -> 新 Client）。
    /// </summary>
    public struct BpfInteractiveSnapshotItem : INetworkSerializable
    {
        public BpfObjectId id;
        public float posX, posY, posZ;
        public float rotX, rotY, rotZ, rotW;
        public bool held;
        public ulong ownerClientId;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref id);
            serializer.SerializeValue(ref posX);
            serializer.SerializeValue(ref posY);
            serializer.SerializeValue(ref posZ);
            serializer.SerializeValue(ref rotX);
            serializer.SerializeValue(ref rotY);
            serializer.SerializeValue(ref rotZ);
            serializer.SerializeValue(ref rotW);
            serializer.SerializeValue(ref held);
            serializer.SerializeValue(ref ownerClientId);
        }
    }

    /// <summary>
    /// 音视频事件载荷：事件 ID + 网络起始时间 + 状态 + 类别 + 资产名。
    /// 接收端按 `startServerTime - ServerTime` 计算本地起播时刻，
    /// 到点在本地执行播放，双端误差由 leadTime（默认 150ms）吸收。
    ///
    /// kind: 0=audio(PlayAudio) 1=bgm 2=stepvoice 3=stepvoicestop
    ///       4=video 5=videopause 6=videoresume 7=videostop
    /// state: 0=play 1=pause 2=stop 3=seek
    /// argument: 步骤语音时=stepIndex（本地按索引解析自己的 clip）；其余 0
    /// </summary>
    public struct BpfAvEventPayload : INetworkSerializable
    {
        public int eventId;
        public double startServerTime;
        public byte state;
        public byte kind;
        public double argument;
        public FixedString64Bytes assetName;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref eventId);
            serializer.SerializeValue(ref startServerTime);
            serializer.SerializeValue(ref state);
            serializer.SerializeValue(ref kind);
            serializer.SerializeValue(ref argument);
            serializer.SerializeValue(ref assetName);
        }
    }
}
