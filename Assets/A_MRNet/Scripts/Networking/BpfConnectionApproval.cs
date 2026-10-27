using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace Bpf.Networking
{
    /// <summary>连接角色：Host 自己也走一遍审批（作为 clientId 0）。</summary>
    public enum BpfConnectionRole
    {
        Host = 0,
        Client = 1,
    }

    /// <summary>连接握手负载（版本 + 昵称 + 角色），BinaryWriter 定序。</summary>
    public struct BpfConnectionPayload
    {
        public int protocolVersion;
        public string applicationVersion;
        public string contentVersion;
        public string playerName;
        public byte requestedRole;
    }

    /// <summary>审批阶段登记的玩家档案，玩家对象生成时取走。</summary>
    public struct BpfConnectionProfile
    {
        public string playerName;
        public Color32 color;

        public static BpfConnectionProfile CreateDefault(ulong clientId)
        {
            return new BpfConnectionProfile
            {
                playerName = "P" + clientId,
                color = BpfPlayerPalette.GetColor((int)clientId),
            };
        }
    }

    /// <summary>玩家配色：同一 ClientId 在任何设备上解析同色。</summary>
    public static class BpfPlayerPalette
    {
        private static readonly Color32[] Colors =
        {
            new Color32(0x4F, 0xC3, 0xF7, 0xFF),
            new Color32(0xFF, 0xB7, 0x4D, 0xFF),
            new Color32(0x81, 0xC7, 0x84, 0xFF),
            new Color32(0xE5, 0x73, 0x73, 0xFF),
        };

        public static Color32 GetColor(int index)
        {
            if (index < 0)
            {
                index = 0;
            }

            return Colors[index % Colors.Length];
        }
    }

    /// <summary>
    /// NGO 连接审批（ConnectionApprovalCallback）。
    ///
    /// 规则：协议/应用/内容版本必须一致、房间未满、房间未开局。
    /// 拒绝原因原样回给客户端，RoomService 把它展示在大厅状态栏。
    /// </summary>
    [DisallowMultipleComponent]
    public class BpfConnectionApproval : MonoBehaviour
    {
        private const string LogTag = "Bpf|Approval";

        public const string RejectProtocol = "协议版本不一致";
        public const string RejectApplication = "应用版本不一致";
        public const string RejectContent = "内容版本不一致";
        public const string RejectRoomFull = "房间已满";
        public const string RejectSessionStarted = "房间已经开始";

        private static readonly ConcurrentDictionary<ulong, BpfConnectionProfile> PendingProfiles =
            new ConcurrentDictionary<ulong, BpfConnectionProfile>();

        private BpfLanNetworkSettings settings;
        private BpfNetworkBootstrap bootstrap;
        private NetworkManager networkManager;

        [Tooltip("房间进入业务场景后是否拒绝新玩家加入（默认开）。")]
        [SerializeField] private bool rejectAfterSceneStart = true;

        public int RejectedConnectionCount { get; private set; }

        public string LastRejectReason { get; private set; }

        public void Initialize(BpfLanNetworkSettings networkSettings, BpfNetworkBootstrap owner)
        {
            settings = networkSettings;
            bootstrap = owner;
            networkManager = owner != null ? owner.NetworkManager : GetComponent<NetworkManager>();

            if (networkManager != null)
            {
                networkManager.ConnectionApprovalCallback -= HandleConnectionApproval;
                networkManager.ConnectionApprovalCallback += HandleConnectionApproval;
            }
        }

        private void OnDestroy()
        {
            if (networkManager != null)
            {
                networkManager.ConnectionApprovalCallback -= HandleConnectionApproval;
            }
        }

        /// <summary>Client 侧：StartClient 之前写入握手负载。</summary>
        public static void WriteClientConnectionData(
            NetworkManager manager, BpfLanNetworkSettings networkSettings, string playerName, BpfConnectionRole role)
        {
            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager));
            }

            if (manager.NetworkConfig == null)
            {
                manager.NetworkConfig = new NetworkConfig();
            }

            manager.NetworkConfig.ConnectionData = Serialize(networkSettings, playerName, role);
        }

        /// <summary>取走审批阶段登记的档案（生成玩家对象时调用，取走即移除）。</summary>
        public static bool TryTakeProfile(ulong clientId, out BpfConnectionProfile profile)
        {
            return PendingProfiles.TryRemove(clientId, out profile);
        }

        public static void ClearProfiles()
        {
            PendingProfiles.Clear();
        }

        // ------------------------------------------------------------ Host 侧

        private void HandleConnectionApproval(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            response.CreatePlayerObject = false; // 玩家对象由本模块显式生成
            response.Pending = false;
            response.Position = null;
            response.Rotation = null;

            string rejectReason = Validate(request, out BpfConnectionProfile profile);
            if (rejectReason != null)
            {
                RejectedConnectionCount++;
                LastRejectReason = rejectReason;
                response.Approved = false;
                response.Reason = rejectReason;
                BpfNetLog.Warning(LogTag, "connection_rejected",
                    "拒绝客户端 " + request.ClientNetworkId + "：" + rejectReason);
                return;
            }

            PendingProfiles[request.ClientNetworkId] = profile;
            response.Approved = true;
            response.Reason = string.Empty;
            BpfNetLog.Info(LogTag, "connection_approved",
                "允许客户端 " + request.ClientNetworkId + " 加入（" + profile.playerName + "）");
        }

        /// <summary>返回 null 表示通过，否则为拒绝消息。</summary>
        private string Validate(NetworkManager.ConnectionApprovalRequest request, out BpfConnectionProfile profile)
        {
            profile = BpfConnectionProfile.CreateDefault(request.ClientNetworkId);

            if (settings == null)
            {
                settings = BpfLanNetworkSettings.Resolve(null);
            }

            if (request.Payload == null || request.Payload.Length == 0)
            {
                return RejectProtocol; // 裸连接按版本不符处理，防旧客户端混入
            }

            BpfConnectionPayload payload;
            try
            {
                payload = Deserialize(request.Payload);
            }
            catch (Exception e)
            {
                BpfNetLog.Error(LogTag, "payload_parse_failed", e.Message);
                return RejectProtocol;
            }

            if (payload.protocolVersion != settings.protocolVersion)
            {
                return RejectProtocol;
            }

            if (payload.applicationVersion != settings.appVersion)
            {
                return RejectApplication;
            }

            if (payload.contentVersion != settings.contentVersion)
            {
                return RejectContent;
            }

            profile = new BpfConnectionProfile
            {
                playerName = string.IsNullOrEmpty(payload.playerName) ? "P" + request.ClientNetworkId : payload.playerName,
                color = BpfPlayerPalette.GetColor((int)request.ClientNetworkId),
            };

            if (networkManager != null)
            {
                // ConnectedClientsIds 不含正在审批的客户端，满员判断用 +1。
                if (networkManager.ConnectedClientsIds.Count + 1 > settings.maxPlayers)
                {
                    return RejectRoomFull;
                }

                if (rejectAfterSceneStart && bootstrap != null
                    && bootstrap.SessionPhase != BpfSessionPhase.Offline
                    && bootstrap.SessionPhase != BpfSessionPhase.Lobby)
                {
                    return RejectSessionStarted;
                }
            }

            return null;
        }

        // ------------------------------------------------------------ 序列化

        private static byte[] Serialize(
            BpfLanNetworkSettings networkSettings, string playerName, BpfConnectionRole role)
        {
            using (MemoryStream stream = new MemoryStream(128))
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(networkSettings != null ? networkSettings.protocolVersion : 1);
                writer.Write(networkSettings != null ? networkSettings.appVersion : "0");
                writer.Write(networkSettings != null ? networkSettings.contentVersion : "0");
                writer.Write(string.IsNullOrEmpty(playerName) ? "Player" : playerName);
                writer.Write((byte)role);
                writer.Flush();
                return stream.ToArray();
            }
        }

        private static BpfConnectionPayload Deserialize(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
            {
                return new BpfConnectionPayload
                {
                    protocolVersion = reader.ReadInt32(),
                    applicationVersion = reader.ReadString(),
                    contentVersion = reader.ReadString(),
                    playerName = reader.ReadString(),
                    requestedRole = reader.ReadByte(),
                };
            }
        }
    }
}
