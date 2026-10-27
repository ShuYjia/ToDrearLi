using System;
using System.Text;

namespace Bpf.Networking
{
    /// <summary>
    /// 会话阶段（Host 权威）。业务判定（交互、校验）统一使用本枚举，
    /// 与网络根自身的生命周期阶段（BpfNetworkBootstrap.Phase）是两个来源。
    /// </summary>
    public enum BpfSessionPhase
    {
        /// <summary>未加入任何会话（单机状态）。</summary>
        Offline = 0,

        /// <summary>已建立连接，停留在大厅/房间内，尚未进入业务场景。</summary>
        Lobby = 1,

        /// <summary>已进入业务场景，正在做三点校准；校准完成前拒绝交互请求。</summary>
        Calibrating = 2,

        /// <summary>校准完成，协作进行中。</summary>
        Playing = 3,
    }

    /// <summary>
    /// 大厅/房间服务向上层汇报的事件。isError=true 时 message 面向用户展示。
    /// </summary>
    public struct BpfSessionEvent
    {
        public string code;
        public string message;
        public bool isError;

        public BpfSessionEvent(string code, string message, bool isError)
        {
            this.code = code;
            this.message = message;
            this.isError = isError;
        }

        public static BpfSessionEvent Ok(string code, string message)
        {
            return new BpfSessionEvent(code, message, false);
        }

        public static BpfSessionEvent Fail(string code, string message)
        {
            return new BpfSessionEvent(code, message, true);
        }
    }

    /// <summary>
    /// 一个可加入的房间（UDP 发现结果 + 连接信息）。
    /// </summary>
    [Serializable]
    public struct BpfDiscoveredRoom
    {
        public string roomName;
        public string hostName;
        public string hostAddress;
        public ushort gameplayPort;
        public int playerCount;
        public int maxPlayers;
        public int protocolVersion;
        public string appVersion;
        public string contentVersion;
        public float discoveredAt;

        public bool IsCompatible(BpfLanNetworkSettings settings)
        {
            return protocolVersion == settings.protocolVersion
                && appVersion == settings.appVersion
                && contentVersion == settings.contentVersion;
        }

        public string Describe()
        {
            string compat = IsCompatible(BpfLanNetworkSettings.Active) ? "兼容" : "版本不兼容";
            return roomName + "（" + hostName + "） " + playerCount + "/" + maxPlayers
                + " 人 [" + hostAddress + ":" + gameplayPort + "] " + compat;
        }
    }

    /// <summary>
    /// UDP 广播/扫描使用的房间报文（定长 ASCII 行式协议，字段用 '\n' 分隔）。
    /// 报文极简：不做加密与压缩，仅满足同一局域网内的找房需求。
    /// </summary>
    public static class BpfRoomPacket
    {
        public const string Probe = "BPF_DISCOVER_PROBE_V1";
        public const string Advertisement = "BPF_DISCOVER_ROOM_V1";

        private const char Separator = '\n';

        public static byte[] BuildAdvertisement(
            string roomName, string hostName, ushort gameplayPort,
            int playerCount, int maxPlayers,
            int protocolVersion, string appVersion, string contentVersion)
        {
            string text = Advertisement + Separator
                + Sanitize(roomName) + Separator
                + Sanitize(hostName) + Separator
                + gameplayPort + Separator
                + playerCount + Separator
                + maxPlayers + Separator
                + protocolVersion + Separator
                + Sanitize(appVersion) + Separator
                + Sanitize(contentVersion);
            return Encoding.ASCII.GetBytes(text);
        }

        public static byte[] BuildProbe()
        {
            return Encoding.ASCII.GetBytes(Probe);
        }

        /// <summary>
        /// 解析收到的 UDP 数据。返回 false 表示不是本协议的报文或字段缺失。
        /// </summary>
        public static bool TryParseAdvertisement(byte[] data, int length, out BpfDiscoveredRoom room)
        {
            room = default;
            if (data == null || length <= 0)
            {
                return false;
            }

            string[] parts;
            try
            {
                parts = Encoding.ASCII.GetString(data, 0, length).Split(Separator);
            }
            catch (Exception)
            {
                return false;
            }

            if (parts.Length < 9 || parts[0] != Advertisement)
            {
                return false;
            }

            room = new BpfDiscoveredRoom
            {
                roomName = parts[1],
                hostName = parts[2],
                gameplayPort = ushort.TryParse(parts[3], out ushort port) ? port : (ushort)7777,
                playerCount = int.TryParse(parts[4], out int count) ? count : 0,
                maxPlayers = int.TryParse(parts[5], out int max) ? max : 0,
                protocolVersion = int.TryParse(parts[6], out int version) ? version : 0,
                appVersion = parts[7],
                contentVersion = parts[8],
            };
            return true;
        }

        public static bool IsProbe(byte[] data, int length)
        {
            if (data == null || length != Probe.Length)
            {
                return false;
            }

            for (int i = 0; i < Probe.Length; i++)
            {
                if (data[i] != (byte)Probe[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>字段内不允许出现分隔符与空字节，统一替换为下划线。</summary>
        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "?";
            }

            char[] buffer = value.ToCharArray();
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] == Separator || buffer[i] == '\0' || buffer[i] == '\r')
                {
                    buffer[i] = '_';
                }
            }

            return new string(buffer);
        }
    }
}
