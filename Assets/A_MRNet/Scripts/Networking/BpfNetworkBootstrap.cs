using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bpf.Networking
{
    /// <summary>
    /// 唯一的运行时网络根。整个工程只允许存在一个。
    ///
    /// 生成方式（阶段 0 起）：
    ///   由 <see cref="MainPage"/> 首次打开 Coop 子页时调用
    ///   <see cref="EnsureRuntimeRoot"/>：优先实例化
    ///   `Resources/Networking/BpfNetworkRoot` 预制体；找不到则创建
    ///   裸对象并自装配。两种路径都在 Awake 里补齐全部组件，
    ///   场景文件（01StartUp.unity）因此可以零改动。
    ///
    /// 约定：
    ///   - DontDestroyOnLoad 跨场景常驻；
    ///   - 不包含 Camera / XR Origin / 业务 Canvas；
    ///   - 业务场景不得再创建第二个 NetworkManager（Awake 强制去重）；
    ///   - 会话阶段 <see cref="SessionPhase"/> 是业务判定权威；
    ///     阶段 2 起由 BpfSessionState 接管，本属性改为转发。
    /// </summary>
    [DisallowMultipleComponent]
    public class BpfNetworkBootstrap : MonoBehaviour
    {
        public const string RootObjectName = "BpfNetworkRoot";

        private const string RootResourcePath = "Networking/BpfNetworkRoot";
        private const string SessionRootResourcePath = "Networking/BpfSessionRoot";
        private const string PlayerAvatarResourcePath = "Networking/BpfPlayerAvatar";
        private const string LogTag = "Bpf|Bootstrap";

        private static BpfNetworkBootstrap instance;

        public static BpfNetworkBootstrap Instance => instance;

        public static bool Exists => instance != null;

        [SerializeField] private BpfLanNetworkSettings settings;
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private UnityTransport transport;
        [SerializeField] private BpfLanDiscovery discovery;
        [SerializeField] private BpfRoomService roomService;
        [SerializeField] private BpfConnectionApproval connectionApproval;
        [SerializeField] private BpfNetworkSceneRouter sceneRouter;
        [SerializeField] private BpfLocalRigPoseSource localPoseSource;
        [SerializeField] private BpfNetMessageRouter messageRouter;
        [SerializeField] private BpfTexCoreNetworkBridge texCoreBridge;
        [SerializeField] private BpfSessionAVSync avSync;
        [SerializeField] private BpfCalibrationCapture calibrationCapture;

        private BpfSessionPhase sessionPhase = BpfSessionPhase.Offline;
        private GameObject sessionRootPrefab;
        private GameObject playerAvatarPrefab;

        public BpfLanNetworkSettings Settings => settings;
        public NetworkManager NetworkManager => networkManager;
        public UnityTransport Transport => transport;
        public BpfLanDiscovery Discovery => discovery;
        public BpfRoomService RoomService => roomService;
        public BpfConnectionApproval ConnectionApproval => connectionApproval;
        public BpfNetworkSceneRouter SceneRouter => sceneRouter;
        public BpfLocalRigPoseSource LocalPoseSource => localPoseSource;
        public BpfTexCoreNetworkBridge TexCoreBridge => texCoreBridge;
        public BpfSessionAVSync AvSync => avSync;
        public BpfCalibrationCapture CalibrationCapture => calibrationCapture;

        /// <summary>会话阶段（Host 权威；业务判定统一用本值）。</summary>
        public BpfSessionPhase SessionPhase => sessionPhase;

        public event Action<BpfSessionPhase> SessionPhaseChanged;

        // ------------------------------------------------------------ 生成入口

        /// <summary>
        /// 确保网络根存在（幂等）。已存在则直接返回现有实例。
        /// </summary>
        public static BpfNetworkBootstrap EnsureRuntimeRoot()
        {
            if (instance != null)
            {
                return instance;
            }

            GameObject prefab = Resources.Load<GameObject>(RootResourcePath);
            GameObject root = prefab != null
                ? Instantiate(prefab)
                : new GameObject(RootObjectName + "(runtime)");
            root.name = RootObjectName;

            BpfNetworkBootstrap bootstrap = root.GetComponent<BpfNetworkBootstrap>();
            if (bootstrap == null)
            {
                bootstrap = root.AddComponent<BpfNetworkBootstrap>();
            }

            return bootstrap;
        }

        // ------------------------------------------------------------ 生命周期

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                BpfNetLog.Error(LogTag, "duplicate_root",
                    "检测到重复的 " + RootObjectName + "，已销毁 " + name);
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            if (transform.parent != null)
            {
                transform.SetParent(null, true);
            }

            settings = BpfLanNetworkSettings.Resolve(settings);
            ResolveReferences();
            EnforceSingleNetworkManager();
            RegisterNetworkCallbacks();

            BpfNetLog.Info(LogTag, "root_ready", "网络根就绪：" + settings.Describe());
        }

        private void OnDisable()
        {
            if (networkManager != null)
            {
                networkManager.OnClientConnectedCallback -= HandleClientConnected;
                networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
                networkManager.OnServerStarted -= HandleServerStarted;
                networkManager.OnClientStopped -= HandleClientStopped;
            }

            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        /// <summary>
        /// 自装配：允许预制体只挂最小组件（甚至不挂），运行期自动接线。
        /// </summary>
        private void ResolveReferences()
        {
            if (networkManager == null)
            {
                networkManager = GetComponent<NetworkManager>();
            }

            if (networkManager == null)
            {
                networkManager = gameObject.AddComponent<NetworkManager>();
            }

            if (transport == null)
            {
                transport = GetComponent<UnityTransport>();
            }

            if (transport == null)
            {
                transport = gameObject.AddComponent<UnityTransport>();
            }

            if (networkManager.NetworkConfig == null)
            {
                networkManager.NetworkConfig = new NetworkConfig();
            }

            networkManager.NetworkConfig.NetworkTransport = transport;
            networkManager.NetworkConfig.EnableSceneManagement = true;
            networkManager.NetworkConfig.ConnectionApproval = true;

            // NGO 1.5.2 的 NetworkConfig 无人数字段，4 人上限在 BpfConnectionApproval 校验。

            if (discovery == null)
            {
                discovery = GetComponent<BpfLanDiscovery>();
            }

            if (discovery == null)
            {
                discovery = gameObject.AddComponent<BpfLanDiscovery>();
            }

            discovery.Settings = settings;

            if (connectionApproval == null)
            {
                connectionApproval = GetComponent<BpfConnectionApproval>();
            }

            if (connectionApproval == null)
            {
                connectionApproval = gameObject.AddComponent<BpfConnectionApproval>();
            }

            connectionApproval.Initialize(settings, this);

            if (roomService == null)
            {
                roomService = GetComponent<BpfRoomService>();
            }

            if (roomService == null)
            {
                roomService = gameObject.AddComponent<BpfRoomService>();
            }

            roomService.Initialize(settings, this, discovery, connectionApproval);

            if (sceneRouter == null)
            {
                sceneRouter = GetComponent<BpfNetworkSceneRouter>();
            }

            if (sceneRouter == null)
            {
                sceneRouter = gameObject.AddComponent<BpfNetworkSceneRouter>();
            }

            sceneRouter.Initialize(settings, this);

            if (localPoseSource == null)
            {
                localPoseSource = GetComponent<BpfLocalRigPoseSource>();
            }

            if (localPoseSource == null)
            {
                localPoseSource = gameObject.AddComponent<BpfLocalRigPoseSource>();
            }

            if (messageRouter == null)
            {
                messageRouter = GetComponent<BpfNetMessageRouter>();
            }

            if (messageRouter == null)
            {
                messageRouter = gameObject.AddComponent<BpfNetMessageRouter>();
            }

            if (texCoreBridge == null)
            {
                texCoreBridge = GetComponent<BpfTexCoreNetworkBridge>();
            }

            if (texCoreBridge == null)
            {
                texCoreBridge = gameObject.AddComponent<BpfTexCoreNetworkBridge>();
            }

            if (avSync == null)
            {
                avSync = GetComponent<BpfSessionAVSync>();
            }

            if (avSync == null)
            {
                avSync = gameObject.AddComponent<BpfSessionAVSync>();
            }

            if (calibrationCapture == null)
            {
                calibrationCapture = GetComponent<BpfCalibrationCapture>();
            }

            if (calibrationCapture == null)
            {
                calibrationCapture = gameObject.AddComponent<BpfCalibrationCapture>();
            }

            // 网络预制体：运行期注册，避开 NetworkConfig.Prefabs 的内嵌序列化结构。
            sessionRootPrefab = Resources.Load<GameObject>(SessionRootResourcePath);
            playerAvatarPrefab = Resources.Load<GameObject>(PlayerAvatarResourcePath);
            RegisterNetworkPrefabs();
        }

        private void RegisterNetworkPrefabs()
        {
            if (networkManager == null || networkManager.IsListening)
            {
                return;
            }

            AddNetworkPrefabSafe(sessionRootPrefab);
            AddNetworkPrefabSafe(playerAvatarPrefab);
        }

        private void AddNetworkPrefabSafe(GameObject prefab)
        {
            if (prefab == null)
            {
                return;
            }

            if (prefab.GetComponent<NetworkObject>() == null)
            {
                BpfNetLog.Error(LogTag, "prefab_invalid",
                    "预制体 " + prefab.name + " 缺少 NetworkObject，未注册");
                return;
            }

            try
            {
                networkManager.AddNetworkPrefab(prefab);
                BpfNetLog.Info(LogTag, "prefab_registered", "网络预制体已注册：" + prefab.name);
            }
            catch (Exception e)
            {
                BpfNetLog.Error(LogTag, "prefab_register_failed",
                    "注册 " + prefab.name + " 失败：" + e.Message);
            }
        }

        /// <summary>Gate A 检查项：运行期只允许一个 NetworkManager。</summary>
        private void EnforceSingleNetworkManager()
        {
            NetworkManager[] managers = FindObjectsOfType<NetworkManager>(true);
            if (managers == null || managers.Length <= 1)
            {
                return;
            }

            foreach (NetworkManager candidate in managers)
            {
                if (candidate == null || candidate == networkManager)
                {
                    continue;
                }

                BpfNetLog.Error(LogTag, "duplicate_network_manager",
                    "发现重复 NetworkManager（" + candidate.gameObject.name + "），已销毁");
                Destroy(candidate.gameObject);
            }
        }

        private void RegisterNetworkCallbacks()
        {
            networkManager.OnClientConnectedCallback += HandleClientConnected;
            networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            networkManager.OnServerStarted += HandleServerStarted;
            networkManager.OnClientStopped += HandleClientStopped;

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        // ------------------------------------------------------------ 阶段管理

        /// <summary>
        /// 设置会话阶段。阶段 2 起 BpfSessionState 成为权威源后会改由它驱动，
        /// 本方法保留给房间服务与场景路由使用。
        /// </summary>
        public void SetSessionPhase(BpfSessionPhase next)
        {
            if (sessionPhase == next)
            {
                return;
            }

            BpfSessionPhase previous = sessionPhase;
            sessionPhase = next;
            BpfNetLog.Info(LogTag, "phase_changed", previous + " -> " + next);
            SessionPhaseChanged?.Invoke(next);
        }

        // ------------------------------------------------------------ 网络对象生成

        /// <summary>
        /// 会话状态对象（Host 侧生成一次，Client 由 NGO 同步）。
        /// 预制体未配置时返回 null 并记 Info 日志（阶段 0 允许缺省）。
        /// </summary>
        public BpfSessionState EnsureSessionRootSpawned()
        {
            if (networkManager == null || !networkManager.IsServer)
            {
                return null;
            }

            if (sessionRootPrefab == null)
            {
                BpfNetLog.Info(LogTag, "session_prefab_not_configured",
                    "未配置会话状态预制体（Resources/Networking/BpfSessionRoot），跳过");
                return null;
            }

            GameObject spawned = Instantiate(sessionRootPrefab);
            spawned.name = "BpfSessionRoot";
            DontDestroyOnLoad(spawned);

            NetworkObject networkObject = spawned.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                BpfNetLog.Error(LogTag, "session_prefab_invalid", "会话预制体缺少 NetworkObject");
                Destroy(spawned);
                return null;
            }

            // destroyWithScene = false：跨场景常驻，切换业务场景不丢状态。
            networkObject.Spawn(false);

            BpfSessionState state = spawned.GetComponent<BpfSessionState>();
            BpfNetLog.Info(LogTag, "session_root_spawned", "会话状态对象已生成");
            return state;
        }

        /// <summary>
        /// Host 为某个客户端生成玩家头像，所有权交给该客户端。
        /// 预制体未配置时返回 null（阶段 0 允许缺省）。
        /// </summary>
        public BpfPlayerAvatar SpawnPlayerAvatar(ulong clientId)
        {
            if (networkManager == null || !networkManager.IsServer)
            {
                return null;
            }

            if (playerAvatarPrefab == null)
            {
                BpfNetLog.Info(LogTag, "avatar_prefab_not_configured",
                    "未配置玩家预制体（Resources/Networking/BpfPlayerAvatar），跳过");
                return null;
            }

            GameObject spawned = Instantiate(playerAvatarPrefab);
            spawned.name = "BpfPlayer_" + clientId;
            DontDestroyOnLoad(spawned);

            NetworkObject networkObject = spawned.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                BpfNetLog.Error(LogTag, "avatar_prefab_invalid", "玩家预制体缺少 NetworkObject");
                Destroy(spawned);
                return null;
            }

            networkObject.SpawnWithOwnership(clientId, false);

            BpfPlayerAvatar avatar = spawned.GetComponent<BpfPlayerAvatar>();
            if (avatar != null && BpfConnectionApproval.TryTakeProfile(clientId, out BpfConnectionProfile profile))
            {
                avatar.ApplyProfile(profile);
            }

            BpfNetLog.Info(LogTag, "avatar_spawned", "已为客户端 " + clientId + " 生成玩家头像");
            return avatar;
        }

        // ------------------------------------------------------------ NGO 回调

        private void HandleServerStarted()
        {
            BpfNetLog.Info(LogTag, "server_started",
                "Host 已启动，LocalClientId=" + networkManager.LocalClientId);
            EnsureSessionRootSpawned();
        }

        private void HandleClientConnected(ulong clientId)
        {
            BpfNetLog.Info(LogTag, "client_connected",
                "客户端 " + clientId + " 已连接，当前在线 " + networkManager.ConnectedClientsIds.Count);

            if (networkManager.IsServer)
            {
                if (discovery != null)
                {
                    discovery.UpdateAdvertisedPlayerCount(networkManager.ConnectedClientsIds.Count);
                }

                SpawnPlayerAvatar(clientId);

                // 晚加入：把 Host 侧已占用的业务对象状态补给它，避免重复点击。
                if (texCoreBridge != null)
                {
                    texCoreBridge.SendSnapshotTo(clientId);
                }

                // 晚加入：抓取物位姿/占用补齐（场景 Transform 不随 NGO 快照恢复）。
                BpfInteractiveRegistry.SendSnapshotTo(clientId);

                return;
            }

            // Client 侧：本机连接完成
            if (roomService != null && roomService.CurrentMode == BpfRoomMode.Connecting)
            {
                roomService.NotifyConnected();
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            BpfNetLog.Info(LogTag, "client_disconnected", "客户端 " + clientId + " 已断开");

            if (networkManager.IsServer)
            {
                if (discovery != null)
                {
                    discovery.UpdateAdvertisedPlayerCount(networkManager.ConnectedClientsIds.Count);
                }

                // 断线释放：玩家对象由 NGO 自动清理，注册表与占用关系同步清。
                BpfPlayerRegistry.NotifyDisconnected(clientId);

                // 断线释放抓取物：它占用的物体归还给大家，并广播。
                BpfInteractiveRegistry.ReleaseByOwner(clientId);
                return;
            }

            // Client 侧：区分"连接被拒/握手失败"与"Host 关闭房间"。
            if (roomService != null)
            {
                if (roomService.CurrentMode == BpfRoomMode.Connecting)
                {
                    roomService.NotifyConnectFailed(networkManager.DisconnectReason);
                    return;
                }

                if (!roomService.IsLeavingIntentionally && networkManager.DisconnectReason != null)
                {
                    roomService.HandleHostLeft(networkManager.DisconnectReason);
                }
            }
        }

        private void HandleClientStopped(bool wasHost)
        {
            // 会话结束：清掉业务对象占用表，避免下次建房时残留旧状态。
            BpfObjectStateRegistry.Clear();
            BpfNetLog.Info(LogTag, "client_stopped", "网络会话已停止（wasHost=" + wasHost + "）");
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BpfNetLog.Info(LogTag, "scene_loaded", scene.name + " (" + mode + ")");

            // 新场景里的抓取物自动挂载联机组件（场景文件零改动）。
            BpfInteractiveRegistry.ScanActiveScene();
        }
    }
}
