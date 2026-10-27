using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Bpf.Networking
{
    /// <summary>房间服务当前模式。</summary>
    public enum BpfRoomMode
    {
        None = 0,
        Hosting = 1,
        Connecting = 2,
        Joined = 3,
    }

    /// <summary>
    /// 房间生命周期服务：创建 / 加入 / 离开 / 超时 / Host 退出处理。
    ///
    /// 只负责"连上与断开"的编排，不持有业务状态；
    /// 会话阶段（Offline/Lobby/Calibrating/Playing）由 Bootstrap 统一持有。
    /// </summary>
    [DisallowMultipleComponent]
    public class BpfRoomService : MonoBehaviour
    {
        private const string LogTag = "Bpf|RoomService";

        [SerializeField] private BpfLanNetworkSettings settings;
        [SerializeField] private BpfNetworkBootstrap bootstrap;
        [SerializeField] private BpfLanDiscovery discovery;
        [SerializeField] private BpfConnectionApproval connectionApproval;

        private BpfRoomMode currentMode = BpfRoomMode.None;
        private float connectDeadline;
        private string pendingRoomName = string.Empty;

        /// <summary>本机玩家昵称（建房/加入时写入握手负载）。</summary>
        public string LocalPlayerName { get; set; } = "玩家";

        public BpfRoomMode CurrentMode => currentMode;

        public bool IsBusy => currentMode == BpfRoomMode.Hosting || currentMode == BpfRoomMode.Connecting;

        public bool IsInRoom => currentMode == BpfRoomMode.Hosting || currentMode == BpfRoomMode.Joined;

        public string LastError { get; private set; } = string.Empty;

        /// <summary>主动离开中：用于区分"Host 关闭房间"与"本机主动断开"。</summary>
        public bool IsLeavingIntentionally { get; private set; }

        /// <summary>面向用户的状态行变化（大厅状态栏订阅）。</summary>
        public event Action<string> StatusChanged;

        /// <summary>协议级事件（错误也走这里，isError=true）。</summary>
        public event Action<BpfSessionEvent> Event;

        public void Initialize(
            BpfLanNetworkSettings networkSettings,
            BpfNetworkBootstrap owner,
            BpfLanDiscovery lanDiscovery,
            BpfConnectionApproval approval)
        {
            settings = networkSettings;
            bootstrap = owner;
            discovery = lanDiscovery;
            connectionApproval = approval;
        }

        // ------------------------------------------------------------ 建房 / 加入

        /// <summary>创建房间（本机成为 Host）。</summary>
        public void CreateRoom(string roomName)
        {
            if (IsBusy)
            {
                return;
            }

            NetworkManager networkManager = bootstrap != null ? bootstrap.NetworkManager : null;
            UnityTransport transport = bootstrap != null ? bootstrap.Transport : null;
            if (networkManager == null || transport == null)
            {
                Fail("网络根尚未就绪，请重新进入多人页");
                return;
            }

            string room = string.IsNullOrEmpty(roomName) ? LocalPlayerName + "的房间" : roomName;
            pendingRoomName = room;

            // Host：监听任意网卡的 gameplayPort，客户端回连地址同为 0.0.0.0。
            transport.SetConnectionData("0.0.0.0", settings.gameplayPort, "0.0.0.0");
            // Host 自己的本地客户端（clientId 0）也走一遍审批， payload 缺失会被拒。
            BpfConnectionApproval.WriteClientConnectionData(
                networkManager, settings, LocalPlayerName, BpfConnectionRole.Host);
            connectionApproval.Initialize(settings, bootstrap);

            if (!networkManager.StartHost())
            {
                Fail("建房失败：端口 " + settings.gameplayPort + " 可能被占用");
                return;
            }

            currentMode = BpfRoomMode.Hosting;
            LastError = string.Empty;

            if (discovery != null)
            {
                discovery.HostRoomName = room;
                discovery.HostPlayerName = LocalPlayerName;
                discovery.StartHosting(room, LocalPlayerName);
                discovery.UpdateAdvertisedPlayerCount(1);
            }

            bootstrap.SetSessionPhase(BpfSessionPhase.Lobby);
            RaiseStatus("已创建房间「" + room + "」，等待其他玩家加入…");
            Raise(BpfSessionEvent.Ok("host_started", "房间已创建"));
        }

        /// <summary>加入房间（address 为 Host 的 IPv4）。</summary>
        public void JoinRoom(string address, string roomName)
        {
            if (IsBusy)
            {
                return;
            }

            NetworkManager networkManager = bootstrap != null ? bootstrap.NetworkManager : null;
            UnityTransport transport = bootstrap != null ? bootstrap.Transport : null;
            if (networkManager == null || transport == null)
            {
                Fail("网络根尚未就绪，请重新进入多人页");
                return;
            }

            if (string.IsNullOrEmpty(address))
            {
                Fail("请先选择房间或手动填写房主 IP");
                return;
            }

            pendingRoomName = string.IsNullOrEmpty(roomName) ? address : roomName;
            IsLeavingIntentionally = false;

            transport.SetConnectionData(address, settings.gameplayPort, "0.0.0.0");
            BpfConnectionApproval.WriteClientConnectionData(
                networkManager, settings, LocalPlayerName, BpfConnectionRole.Client);
            connectionApproval.Initialize(settings, bootstrap);

            if (!networkManager.StartClient())
            {
                Fail("加入失败：无法连接到 " + address);
                return;
            }

            currentMode = BpfRoomMode.Connecting;
            connectDeadline = Time.time + settings.joinTimeoutSeconds;
            RaiseStatus("正在加入 " + address + ":" + settings.gameplayPort + " …");
            Raise(BpfSessionEvent.Ok("join_started", "正在连接 Host"));
        }

        /// <summary>主动离开房间（Host 与 Client 共用）。</summary>
        public void LeaveRoom()
        {
            IsLeavingIntentionally = true;

            NetworkManager networkManager = bootstrap != null ? bootstrap.NetworkManager : null;
            if (networkManager != null && networkManager.IsListening)
            {
                networkManager.Shutdown();
            }

            if (discovery != null)
            {
                discovery.StopHosting();
                discovery.StopScan();
            }

            currentMode = BpfRoomMode.None;
            pendingRoomName = string.Empty;
            bootstrap?.SetSessionPhase(BpfSessionPhase.Offline);
            RaiseStatus("已离开房间");
            Raise(BpfSessionEvent.Ok("room_left", "已离开房间"));
        }

        /// <summary>
        /// Host 退出/掉线（仅 Client 侧调用）。
        /// 设计边界：第一阶段不做主机迁移，Host 退出即房间结束，全员回大厅。
        /// </summary>
        public void HandleHostLeft(string reason)
        {
            if (currentMode != BpfRoomMode.Joined)
            {
                return;
            }

            currentMode = BpfRoomMode.None;
            bootstrap?.SetSessionPhase(BpfSessionPhase.Offline);
            Fail("房主已离开房间" + (string.IsNullOrEmpty(reason) ? string.Empty : "（" + reason + "）"));
            RaiseStatus("房主已离开，房间结束");
        }

        /// <summary>连接成功（Bootstrap 的 OnClientConnected 回调里调用）。</summary>
        internal void NotifyConnected()
        {
            if (currentMode != BpfRoomMode.Connecting)
            {
                return;
            }

            currentMode = BpfRoomMode.Joined;
            bootstrap.SetSessionPhase(BpfSessionPhase.Lobby);
            RaiseStatus("已加入「" + pendingRoomName + "」，等待房主开始…");
            Raise(BpfSessionEvent.Ok("joined", "已加入房间"));
        }

        /// <summary>
        /// 连接失败（审批拒绝/握手中断，Bootstrap 的 OnClientDisconnect 回调里调用）。
        /// 与 <see cref="HandleHostLeft"/> 区分：从未加入过就不算"房主离开"。
        /// </summary>
        internal void NotifyConnectFailed(string reason)
        {
            if (currentMode != BpfRoomMode.Connecting)
            {
                return;
            }

            Fail(string.IsNullOrEmpty(reason) ? "连接失败：Host 无响应" : "连接失败：" + reason);
        }

        // ------------------------------------------------------------ 主线程

        private void Update()
        {
            if (currentMode == BpfRoomMode.Connecting && Time.time >= connectDeadline)
            {
                Fail("加入超时（" + settings.joinTimeoutSeconds + "s）：Host 无响应");
                LeaveRoom();
            }
        }

        // ------------------------------------------------------------ 工具

        private void RaiseStatus(string status)
        {
            StatusChanged?.Invoke(status);
        }

        private void Raise(BpfSessionEvent evt)
        {
            BpfNetLog.Info(LogTag, evt.code, evt.message);
            Event?.Invoke(evt);
        }

        private void Fail(string message)
        {
            LastError = message;
            currentMode = BpfRoomMode.None;
            Raise(BpfSessionEvent.Fail("room_error", message));
        }
    }
}
