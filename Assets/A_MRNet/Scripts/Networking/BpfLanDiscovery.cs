using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Bpf.Networking
{
    /// <summary>
    /// UDP 局域网房间发现（探针-应答模型，主线程轮询架构）。
    ///
    /// 线程模型：所有 socket IO 都在 Unity 主线程的 Update 里完成，
    /// 用 `Available > 0` 守卫的非阻塞 Receive——绝不阻塞主线程，
    /// 也不依赖 Task/线程池（实测本环境后台任务的 UdpClient 异步接收不可靠）。
    ///
    /// 端口模型（避免多客户端同机抢 47777）：
    ///   Host   绑定 discoveryPort，应答探针；
    ///   Client 用临时端口发送探针到广播地址，同一 socket 收应答。
    ///
    /// VPN/虚拟网卡过滤：本机跑着 Clash Verge 等工具时，虚拟网卡会伪造
    /// "同子网广播地址"，把探针打进黑洞。这里按网卡名/描述关键词剔除。
    /// </summary>
    [DisallowMultipleComponent]
    public class BpfLanDiscovery : MonoBehaviour
    {
        private const string LogTag = "Bpf|Discovery";

        private static readonly string[] VpnInterfaceKeywords =
        {
            "vpn", "clash", "verge", "tailscale", "zerotier", "wireguard", "openvpn",
            "hamachi", "radmin", "wintun", "tap-", "tun", "proxy", "virtualbox",
            "vmware", "hyper-v", "loopback", "npcap", "wireshark", "microsoft wi-fi direct",
            "bluetooth", "pseudo"
        };

        [Tooltip("扫描时是否把 255.255.255.255 也作为目标（部分路由器只转发其中一种）。")]
        [SerializeField] private bool useGlobalBroadcast = true;

        [Tooltip("扫描窗口内每隔多久重发一次探针（秒）。")]
        [SerializeField] private float probeInterval = 1f;

        [SerializeField] private BpfLanNetworkSettings settings;

        // 主线程消费后的房间表（hostAddress:port 为键去重）
        private readonly Dictionary<string, BpfDiscoveredRoom> rooms = new Dictionary<string, BpfDiscoveredRoom>();

        private UdpClient hostSocket;
        private UdpClient scanSocket;

        private volatile int advertisedPlayerCount;
        private volatile bool advertising;
        private float nextProbeTime;
        private float scanStartTime = float.NegativeInfinity;

        /// <summary>Host 广播内容里的房间名（由 RoomService 在建房时写入）。</summary>
        public string HostRoomName { get; set; } = "BPF房间";

        /// <summary>Host 广播内容里的房主昵称。</summary>
        public string HostPlayerName { get; set; } = "Host";

        public BpfLanNetworkSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        /// <summary>主线程刷新：新发现的房间（去重后首次出现或信息变化时触发）。</summary>
        public event Action<BpfDiscoveredRoom> RoomDiscovered;

        /// <summary>当前房间表快照（主线程读取）。</summary>
        public IReadOnlyList<BpfDiscoveredRoom> Rooms
        {
            get
            {
                List<BpfDiscoveredRoom> snapshot = new List<BpfDiscoveredRoom>();
                foreach (KeyValuePair<string, BpfDiscoveredRoom> pair in rooms)
                {
                    snapshot.Add(pair.Value);
                }
                return snapshot;
            }
        }

        public bool IsHosting => advertising;

        public bool IsScanning => scanStartTime > 0f && Time.time - scanStartTime < settings.discoveryScanSeconds;

        // ------------------------------------------------------------ Host 侧

        /// <summary>
        /// 以 Host 身份开始应答探针（幂等，重复调用只更新广播内容）。
        /// </summary>
        public void StartHosting(string roomName, string hostName)
        {
            if (settings == null || advertising)
            {
                return;
            }

            HostRoomName = roomName;
            HostPlayerName = hostName;

            try
            {
                hostSocket = CreateBoundSocket(settings.discoveryPort);
                advertising = true;
                BpfNetLog.Info(LogTag, "hosting_started",
                    "UDP 发现已监听 " + settings.discoveryPort + "（房间 " + roomName + "）");
            }
            catch (Exception e)
            {
                BpfNetLog.Error(LogTag, "advertise_bind_failed",
                    "UDP 监听 " + settings.discoveryPort + " 失败：" + e.Message);
                advertising = false;
            }
        }

        public void StopHosting()
        {
            if (!advertising)
            {
                return;
            }

            advertising = false;
            CloseSocket(ref hostSocket);
            BpfNetLog.Info(LogTag, "hosting_stopped", "UDP 发现已停止应答");
        }

        /// <summary>线程安全：Host 在人数变化时调用，下一次应答即带上新人数。</summary>
        public void UpdateAdvertisedPlayerCount(int count)
        {
            advertisedPlayerCount = count;
        }

        // ------------------------------------------------------------ Client 侧

        /// <summary>开始一次扫描（立即发第一发探针，窗口内按间隔补发）。</summary>
        public void StartScan()
        {
            if (settings == null)
            {
                return;
            }

            StopScan();

            try
            {
                // 扫描侧绑定 0 端口（临时端口），避免与 Host 的发现端口冲突。
                scanSocket = CreateBoundSocket(0);
                scanSocket.EnableBroadcast = true;
            }
            catch (Exception e)
            {
                BpfNetLog.Error(LogTag, "scan_bind_failed", "UDP 扫描启动失败：" + e.Message);
                StopScan();
                return;
            }

            rooms.Clear();
            scanStartTime = Time.time;
            nextProbeTime = 0f;
            SendProbe();

            BpfNetLog.Info(LogTag, "scan_started",
                "开始扫描局域网房间（窗口 " + settings.discoveryScanSeconds + "s，目标 "
                + string.Join(", ", EnumerateBroadcastTargets()) + "）");
        }

        public void StopScan()
        {
            scanStartTime = float.NegativeInfinity;
            CloseSocket(ref scanSocket);
        }

        // ------------------------------------------------------------ 主线程轮询

        private void Update()
        {
            if (settings == null)
            {
                return;
            }

            // ---- 扫描窗口与探针节奏 ----
            if (scanStartTime > 0f)
            {
                if (Time.time - scanStartTime >= settings.discoveryScanSeconds)
                {
                    StopScan();
                }
                else if (Time.time >= nextProbeTime)
                {
                    SendProbe();
                }
            }

            // ---- Host：应答探针 ----
            DrainHostSocket();

            // ---- Client：收房间广播 ----
            DrainScanSocket();
        }

        /// <summary>主线程非阻塞排空 Host socket：收到探针就回复房间广告。</summary>
        private void DrainHostSocket()
        {
            if (!advertising || hostSocket == null)
            {
                return;
            }

            try
            {
                while (hostSocket.Available > 0)
                {
                    IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = hostSocket.Receive(ref remote);

                    if (!BpfRoomPacket.IsProbe(data, data.Length))
                    {
                        continue;
                    }

                    byte[] payload = BpfRoomPacket.BuildAdvertisement(
                        HostRoomName, HostPlayerName, settings.gameplayPort,
                        advertisedPlayerCount, settings.maxPlayers,
                        settings.protocolVersion, settings.appVersion, settings.contentVersion);

                    hostSocket.Send(payload, payload.Length, remote);
                    BpfNetLog.Info(LogTag, "probe_answered",
                        "应答探针 " + remote + " -> " + HostRoomName);
                }
            }
            catch (ObjectDisposedException)
            {
                CloseSocket(ref hostSocket);
                advertising = false;
            }
            catch (Exception e)
            {
                BpfNetLog.Error(LogTag, "host_io_error", "Host 发现 IO 异常：" + e.Message);
            }
        }

        /// <summary>主线程非阻塞排空扫描 socket：收到广告就进房间表。</summary>
        private void DrainScanSocket()
        {
            if (scanSocket == null)
            {
                return;
            }

            try
            {
                while (scanSocket.Available > 0)
                {
                    IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = scanSocket.Receive(ref remote);

                    if (!BpfRoomPacket.TryParseAdvertisement(data, data.Length, out BpfDiscoveredRoom room))
                    {
                        continue;
                    }

                    room.hostAddress = remote.Address.ToString();
                    room.discoveredAt = Time.time;
                    AcceptRoom(room);
                }
            }
            catch (ObjectDisposedException)
            {
                CloseSocket(ref scanSocket);
            }
            catch (Exception e)
            {
                BpfNetLog.Error(LogTag, "scan_io_error", "扫描 IO 异常：" + e.Message);
            }
        }

        private void OnDestroy()
        {
            StopHosting();
            StopScan();
        }

        // ------------------------------------------------------------ 房间表

        private void AcceptRoom(BpfDiscoveredRoom room)
        {
            string key = room.hostAddress + ":" + room.gameplayPort;
            bool isNew = !rooms.TryGetValue(key, out BpfDiscoveredRoom existing);
            if (isNew || !RoomEquals(existing, room))
            {
                rooms[key] = room;
                if (isNew)
                {
                    BpfNetLog.Info(LogTag, "room_found", room.Describe());
                }

                RoomDiscovered?.Invoke(room);
            }
            else
            {
                // 已知房间：只刷新时间戳与人数，保持列表不跳动
                existing.discoveredAt = room.discoveredAt;
                existing.playerCount = room.playerCount;
                rooms[key] = existing;
            }
        }

        // ------------------------------------------------------------ 工具

        private void SendProbe()
        {
            if (settings == null || scanSocket == null)
            {
                return;
            }

            nextProbeTime = Time.time + probeInterval;

            try
            {
                byte[] probe = BpfRoomPacket.BuildProbe();
                foreach (IPEndPoint target in EnumerateBroadcastTargets())
                {
                    scanSocket.Send(probe, probe.Length, target);
                }
            }
            catch (Exception e)
            {
                BpfNetLog.Error(LogTag, "probe_send_failed", "发送探针失败：" + e.Message);
            }
        }

        /// <summary>
        /// 枚举本机可用的 IPv4 广播目标：每个合格网卡的子网广播地址 +
        /// （可选）255.255.255.255 + 127.0.0.1 回环。剔除回环、VPN 与虚拟网卡。
        /// 回环目标保证"编辑器 Host + 本机 Client"同机联调必定可用
        ///（部分 Windows 网络栈不把子网广播回环给本机 socket）。
        /// </summary>
        private List<IPEndPoint> EnumerateBroadcastTargets()
        {
            List<IPEndPoint> targets = new List<IPEndPoint>();
            HashSet<string> seen = new HashSet<string>();

            try
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up)
                    {
                        continue;
                    }

                    if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    {
                        continue;
                    }

                    string nicName = (nic.Name + " " + nic.Description).ToLowerInvariant();
                    bool isVirtual = false;
                    foreach (string keyword in VpnInterfaceKeywords)
                    {
                        if (nicName.Contains(keyword))
                        {
                            isVirtual = true;
                            break;
                        }
                    }

                    if (isVirtual)
                    {
                        continue;
                    }

                    foreach (UnicastIPAddressInformation ip in nic.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily != AddressFamily.InterNetwork)
                        {
                            continue;
                        }

                        byte[] addressBytes = ip.Address.GetAddressBytes();
                        byte[] maskBytes = ip.IPv4Mask?.GetAddressBytes();
                        if (maskBytes == null || maskBytes.Length != 4)
                        {
                            continue;
                        }

                        byte[] broadcast = new byte[4];
                        for (int i = 0; i < 4; i++)
                        {
                            broadcast[i] = (byte)(addressBytes[i] | ~maskBytes[i]);
                        }

                        string text = new IPAddress(broadcast).ToString();
                        if (seen.Add(text))
                        {
                            targets.Add(new IPEndPoint(IPAddress.Parse(text), settings.discoveryPort));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                BpfNetLog.Warning(LogTag, "interface_scan_failed", "枚举网卡失败：" + e.Message);
            }

            if (useGlobalBroadcast)
            {
                targets.Add(new IPEndPoint(IPAddress.Broadcast, settings.discoveryPort));
            }

            targets.Add(new IPEndPoint(IPAddress.Loopback, settings.discoveryPort));

            return targets;
        }

        /// <summary>
        /// 创建并绑定 UDP socket。Host 侧传 discoveryPort，扫描侧传 0（临时端口）。
        /// ReuseAddress 允许同机多实例（编辑器 Host + 独立 Client）各自绑定发现端口。
        /// </summary>
        private static UdpClient CreateBoundSocket(int port)
        {
            UdpClient client = new UdpClient(AddressFamily.InterNetwork);
            client.EnableBroadcast = true;
            client.MulticastLoopback = false;
            client.Client.SetSocketOption(
                SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Client.Bind(new IPEndPoint(IPAddress.Any, port));

            // Windows 下向无监听目标发包会收到 ICMP Port Unreachable，
            // 默认让后续 Receive 抛 ECONNRESET。这个 ioctl 关掉该行为；
            // 部分运行时（旧 Mono）不支持时会抛 WSAEOPNOTSUPP，忽略即可。
            const int SioUdpConnreset = -1744830460; // 0x9800000C
            try
            {
                client.Client.IOControl(SioUdpConnreset, new byte[] { 0, 0, 0, 0 }, null);
            }
            catch (Exception)
            {
                // 非 Windows 或不支持时静默跳过（主线程轮询 + Available 守卫
                // 已经不会被单次 ICMP 毒化，这只是双保险）。
            }

            return client;
        }

        private static void CloseSocket(ref UdpClient socket)
        {
            if (socket == null)
            {
                return;
            }

            try
            {
                socket.Close();
            }
            catch (Exception)
            {
                // 关闭失败不致命
            }

            socket = null;
        }

        private static bool RoomEquals(BpfDiscoveredRoom a, BpfDiscoveredRoom b)
        {
            return a.roomName == b.roomName
                && a.hostName == b.hostName
                && a.playerCount == b.playerCount
                && a.maxPlayers == b.maxPlayers
                && a.protocolVersion == b.protocolVersion
                && a.appVersion == b.appVersion
                && a.contentVersion == b.contentVersion;
        }
    }
}
