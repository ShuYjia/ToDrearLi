using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Bpf.Networking
{
    /// <summary>
    /// 音视频同步（阶段 4）。
    ///
    /// 挂接方式：自装配到网络根（与 BpfTexCoreNetworkBridge 同一模式），
    /// Awake 时给 TexCore_AudioManager / TexCore_VideoManager 安装静态
    /// 观察者拦截器（Action，不影响本地播放，仅决定是否广播）。
    ///
    /// 同步模型：
    ///   - 只有 Host 广播（NGO 客户端不能群发，见 BpfNetSend）；
    ///   - 事件携带 startServerTime = ServerTime + leadTime（默认 150ms），
    ///     吸收网络抖动，双端播放误差目标 &lt; 100ms；
    ///   - Client 收到后换算成本地时刻排程，Update 到点执行，
    ///     过期事件（delay &lt;= 0）立即播放；
    ///   - Host 自己的客户端会收到回声，按 senderClientId == LocalClientId 去重；
    ///   - Client 本地执行播放时不会再广播（拦截器只认 IsServer），无回环。
    ///
    /// 步骤语音（ narration ）：广播 stepIndex，各端用自己题目面板上的
    /// TexCore_StepVoiceProfile 按索引解析本地 clip——clip 引用不可跨机
    /// 序列化，但两端题目相同、索引相同，解析结果一致。
    /// </summary>
    [DisallowMultipleComponent]
    public class BpfSessionAVSync : MonoBehaviour
    {
        private const string LogTag = "Bpf|AVSync";

        // 与 BpfAvEventPayload.kind 对应的字符串标签（管理器侧用，免引用 Bpf 类型）
        private const string KindAudio = "audio";
        private const string KindBgm = "bgm";
        private const string KindStepVoice = "stepvoice";
        private const string KindStepVoiceStop = "stepvoicestop";
        private const string KindVideo = "video";
        private const string KindVideoPause = "videopause";
        private const string KindVideoResume = "videoresume";
        private const string KindVideoStop = "videostop";

        [Tooltip("起播提前量（秒）：Host 广播时刻相对本地播放提前多久，吸收网络抖动。")]
        [SerializeField] private float leadTime = 0.15f;

        [Tooltip("单端最多缓存的待播事件数，超出丢弃最旧的。")]
        [SerializeField] private int maxPending = 32;

        public static BpfSessionAVSync Instance { get; private set; }

        /// <summary>诊断用：Host 已广播的音视频事件数。</summary>
        public static int EventsBroadcast { get; private set; }

        /// <summary>诊断用：本机已执行的排程播放数（Client 路径）。</summary>
        public static int EventsExecuted { get; private set; }

        private int eventCounter;
        private readonly List<PendingAvEvent> pending = new List<PendingAvEvent>();

        private struct PendingAvEvent
        {
            public int EventId;
            public byte Kind;
            public byte State;
            public string AssetName;
            public double Argument;
            public double FireLocalTime;
        }

        private void Awake()
        {
            Instance = this;
            TexCore_AudioManager.AvPlayInterceptor = OnAudioPlay;
            TexCore_VideoManager.AvPlayInterceptor = OnVideoPlay;
            BpfNetLog.Info(LogTag, "interceptors_installed",
                "音视频播放拦截器已安装（lead=" + leadTime.ToString("F2") + "s）");
        }

        private void OnDestroy()
        {
            if (TexCore_AudioManager.AvPlayInterceptor != null
                && TexCore_AudioManager.AvPlayInterceptor.Equals(
                    (System.Action<string, string, double>)OnAudioPlay))
            {
                TexCore_AudioManager.AvPlayInterceptor = null;
            }

            if (TexCore_VideoManager.AvPlayInterceptor != null
                && TexCore_VideoManager.AvPlayInterceptor.Equals(
                    (System.Action<string, string, double>)OnVideoPlay))
            {
                TexCore_VideoManager.AvPlayInterceptor = null;
            }

            pending.Clear();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (pending.Count == 0)
            {
                return;
            }

            NetworkManager manager = BpfNetworkBootstrap.Instance != null
                ? BpfNetworkBootstrap.Instance.NetworkManager
                : null;
            if (manager == null || !manager.IsListening)
            {
                return;
            }

            double now = manager.LocalTime.Time;
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (now >= pending[i].FireLocalTime)
                {
                    PendingAvEvent e = pending[i];
                    pending.RemoveAt(i);
                    ExecuteLocal(e);
                }
            }
        }

        // ------------------------------------------------------------ 广播（Host）

        private void OnAudioPlay(string kind, string assetName, double argument)
        {
            Broadcast(kind, assetName, argument);
        }

        private void OnVideoPlay(string kind, string assetName, double argument)
        {
            Broadcast(kind, assetName, argument);
        }

        private void Broadcast(string kind, string assetName, double argument)
        {
            NetworkManager manager = BpfNetworkBootstrap.Instance != null
                ? BpfNetworkBootstrap.Instance.NetworkManager
                : null;

            // 只有 Host 广播；Client 的本地播放（含执行远端事件）不透传。
            // 单机（未建房）时同样不广播，行为与改造前一致。
            if (manager == null || !manager.IsListening || !manager.IsServer)
            {
                return;
            }

            byte kindCode;
            byte state;
            MapKind(kind, out kindCode, out state);

            BpfAvEventPayload payload = new BpfAvEventPayload
            {
                eventId = ++eventCounter,
                startServerTime = manager.ServerTime.Time + leadTime,
                state = state,
                kind = kindCode,
                argument = argument,
                assetName = new FixedString64Bytes(assetName ?? string.Empty),
            };

            try
            {
                using (FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp))
                {
                    writer.WriteNetworkSerializable(payload);
                    manager.CustomMessagingManager.SendNamedMessageToAll(
                        BpfNetMessage.AvEvent, writer, NetworkDelivery.Reliable);
                }

                BpfNetLog.Info(LogTag, "event_broadcast",
                    kind + "/" + (assetName ?? string.Empty) + " @" + payload.startServerTime.ToString("F2"));
                EventsBroadcast++;
            }
            catch (System.Exception e)
            {
                BpfNetLog.Warning(LogTag, "broadcast_failed", e.Message);
            }
        }

        private static void MapKind(string kind, out byte kindCode, out byte state)
        {
            state = 0; // play
            switch (kind)
            {
                case KindAudio: kindCode = 0; break;
                case KindBgm: kindCode = 1; break;
                case KindStepVoice: kindCode = 2; break;
                case KindStepVoiceStop: kindCode = 3; state = 2; break;
                case KindVideo: kindCode = 4; break;
                case KindVideoPause: kindCode = 5; state = 1; break;
                case KindVideoResume: kindCode = 6; break;
                case KindVideoStop: kindCode = 7; state = 2; break;
                default: kindCode = 0; break;
            }
        }

        // ------------------------------------------------------------ 接收（Client）

        /// <summary>Client 收到音视频事件：换算本地起播时刻后排程。</summary>
        public static void HandleAvEvent(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadNetworkSerializable(out BpfAvEventPayload payload);

            if (Instance == null)
            {
                return;
            }

            NetworkManager manager = BpfNetworkBootstrap.Instance != null
                ? BpfNetworkBootstrap.Instance.NetworkManager
                : null;
            if (manager == null || !manager.IsListening)
            {
                return;
            }

            // Host 自己的回声（SendNamedMessageToAll 含本机客户端）
            if (senderClientId == manager.LocalClientId)
            {
                return;
            }

            double delay = payload.startServerTime - manager.ServerTime.Time;
            double fireLocalTime = manager.LocalTime.Time + (delay > 0 ? delay : 0);

            PendingAvEvent e = new PendingAvEvent
            {
                EventId = payload.eventId,
                Kind = payload.kind,
                State = payload.state,
                AssetName = payload.assetName.ToString(),
                Argument = payload.argument,
                FireLocalTime = fireLocalTime,
            };

            Instance.Enqueue(e);
        }

        private void Enqueue(PendingAvEvent e)
        {
            while (pending.Count >= maxPending)
            {
                pending.RemoveAt(0); // 丢最旧，防时钟异常把队列撑爆
            }

            pending.Add(e);
        }

        /// <summary>到点在本地执行播放（不触发广播：拦截器只认 IsServer）。</summary>
        private void ExecuteLocal(PendingAvEvent e)
        {
            TexCore_AudioManager audio = TexCore_AudioManager.self;
            TexCore_VideoManager video = TexCore_VideoManager.self;

            switch (e.Kind)
            {
                case 0:
                    if (audio != null) audio.PlayAudio(e.AssetName);
                    break;
                case 1:
                    if (audio != null) audio.PlayBGM(e.AssetName);
                    break;
                case 2:
                    PlayStepVoiceLocal(audio, e);
                    break;
                case 3:
                    if (audio != null) audio.StopStepVoice();
                    break;
                case 4:
                    if (video != null) video.Play(e.AssetName);
                    break;
                case 5:
                    if (video != null) video.PauseDirect();
                    break;
                case 6:
                    if (video != null) video.ResumeDirect();
                    break;
                case 7:
                    if (video != null) video.Exit();
                    break;
                default:
                    break;
            }

            BpfNetLog.Info(LogTag, "event_executed",
                "kind=" + e.Kind + " name=" + e.AssetName + " delay=" + e.Argument.ToString("F2"));
            EventsExecuted++;
        }

        /// <summary>
        /// 步骤语音本地解析：按 stepIndex 从本题目板的 StepVoiceProfile 取 clip，
        /// 音量也用本地配置（各端题目数据一致，解析结果一致）。
        /// </summary>
        private static void PlayStepVoiceLocal(TexCore_AudioManager audio, PendingAvEvent e)
        {
            if (audio == null)
            {
                return;
            }

            int stepIndex = (int)e.Argument;
            TexCore_StepVoiceProfile profile = Object.FindObjectOfType<TexCore_StepVoiceProfile>();
            AudioClip clip = profile != null ? profile.GetClip(stepIndex) : null;
            float volume = profile != null ? profile.volume : 1f;

            audio.PlayStepVoice(clip, volume, default);
        }
    }
}
