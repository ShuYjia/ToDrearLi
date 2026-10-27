using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Bpf.Networking
{
    /// <summary>
    /// 局域网大厅面板（运行期 uGUI + TMP 搭建，自带世界空间画布）。
    ///
    /// 阶段 5 变更（设备可操作性，原 runbook 已知限制第 3 条）：
    ///   - 不再寄宿在 MainPage.coopRoot 下（那个画布没有 GraphicRaycaster，
    ///     uGUI 收不到 XR 指针事件）；改为自建 DontDestroyOnLoad 世界空间
    ///     画布 + TrackedDeviceGraphicRaycaster，直接吃场景里已有的
    ///     XRUIInputModule 事件链；
    ///   - 画布常驻：开始业务切到 03 场景后面板不销毁，切换为紧凑模式
    ///     （收起建房/扫描/房间列表，保留状态/校准/离开），校准因此能在
    ///     业务场景里进行；
    ///   - 场景缺 EventSystem 时自补一个（仅当不存在，避免和场景里的打架）。
    ///
    /// 结构（自上而下）：
    ///   标题 / 大厅操作区（昵称·房间名·IP 输入 + 建房·扫描·加入 + 房间列表）
    ///   常驻区（开始业务·离开 / 状态栏 / 校准区）
    /// </summary>
    [DisallowMultipleComponent]
    public class BpfLanLobbyPanel : MonoBehaviour
    {
        private const string LogTag = "Bpf|Lobby";
        private const string NicknamePrefsKey = "bpf.net.player_name";
        private const float RowHeight = 36f;
        private const float ListRefreshInterval = 0.5f;

        // 画布设计尺寸（uGUI 像素单位，世界空间经 Canvas 缩放换算）
        private const float CanvasWidth = 900f;
        private const float CanvasHeightLobby = 640f;
        private const float CanvasHeightCompact = 340f;
        private const float CanvasWorldScale = 0.0012f;   // 900px ≈ 1.08m

        private const float FollowDistance = 1.15f;
        private const float FollowHeight = -0.18f;
        private const float FollowSpeed = 4f;

        private static BpfLanLobbyPanel activePanel;

        private BpfNetworkBootstrap bootstrap;
        private BpfRoomService roomService;
        private BpfLanDiscovery discovery;

        private TMP_InputField nicknameInput;
        private TMP_InputField roomNameInput;
        private TMP_InputField hostAddressInput;
        private TextMeshProUGUI statusText;
        private TextMeshProUGUI titleText;
        private RectTransform roomListContent;
        private GameObject roomRowPrefab;
        private GameObject calibrationSection;
        private GameObject lobbySection;
        private Button leaveButton;
        private Button createButton;
        private Button startButton;
        private Button calibrateButton;
        private TextMeshProUGUI calibrationHint;

        private GameObject canvasRoot;
        private RectTransform canvasRect;
        private bool compact;

        private readonly List<GameObject> roomRows = new List<GameObject>();
        private BpfDiscoveredRoom selectedRoom;
        private float nextListRefreshTime;

        /// <summary>当前面板实例（MainPage 显隐用）。</summary>
        public static BpfLanLobbyPanel ActivePanel => activePanel;

        [Tooltip("大厅场景名：处于该场景时显示完整面板，其余场景用紧凑模式。")]
        [SerializeField] private string lobbySceneName = "01StartUp";

        // ------------------------------------------------------------ 入口

        /// <summary>
        /// 确保大厅面板存在（幂等）。自带 DontDestroyOnLoad 世界空间画布，
        /// 不依赖调用方的 Canvas——container 参数保留仅为兼容旧调用。
        /// </summary>
        public static BpfLanLobbyPanel EnsureFor(Transform container = null)
        {
            if (activePanel != null)
            {
                return activePanel;
            }

            GameObject host = new GameObject("BpfLobbyCanvas", typeof(RectTransform));
            DontDestroyOnLoad(host);
            BpfLanLobbyPanel panel = host.AddComponent<BpfLanLobbyPanel>();
            return panel;
        }

        /// <summary>显示/隐藏整个面板（MainPage 协同子页切换用）。</summary>
        public void SetVisible(bool visible)
        {
            if (canvasRoot != null)
            {
                canvasRoot.SetActive(visible);
                if (visible)
                {
                    PlaceInFrontOfUser(immediate: true);
                }
            }
        }

        // ------------------------------------------------------------ 生命周期

        private void Awake()
        {
            activePanel = this;
            bootstrap = BpfNetworkBootstrap.Instance;
            if (bootstrap != null)
            {
                roomService = bootstrap.RoomService;
                discovery = bootstrap.Discovery;
            }

            BuildCanvas();
            BuildUi();
            EnsureEventSystem();
            ApplySceneMode(SceneManager.GetActiveScene().name);
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;

            // 退订事件不需要空值守卫（未订阅时 -= 是无害 no-op）
            BpfSessionCalibration.PhaseChanged -= HandleCalibrationPhase;
            BpfSessionCalibration.PointHint -= HandleCalibrationHint;

            if (roomService != null)
            {
                roomService.StatusChanged -= HandleStatusChanged;
                roomService.Event -= HandleSessionEvent;
            }

            if (discovery != null)
            {
                discovery.RoomDiscovered -= HandleRoomDiscovered;
            }

            if (bootstrap != null)
            {
                bootstrap.SessionPhaseChanged -= HandleSessionPhaseChanged;
            }

            if (activePanel == this)
            {
                activePanel = null;
            }
        }

        private void OnEnable()
        {
            if (roomService != null)
            {
                roomService.StatusChanged += HandleStatusChanged;
                roomService.Event += HandleSessionEvent;
            }

            if (discovery != null)
            {
                discovery.RoomDiscovered += HandleRoomDiscovered;
            }

            if (bootstrap != null)
            {
                bootstrap.SessionPhaseChanged += HandleSessionPhaseChanged;
            }

            BpfSessionCalibration.PhaseChanged += HandleCalibrationPhase;
            BpfSessionCalibration.PointHint += HandleCalibrationHint;
        }

        private void OnDisable()
        {
            BpfSessionCalibration.PhaseChanged -= HandleCalibrationPhase;
            BpfSessionCalibration.PointHint -= HandleCalibrationHint;

            if (roomService != null)
            {
                roomService.StatusChanged -= HandleStatusChanged;
                roomService.Event -= HandleSessionEvent;
            }

            if (discovery != null)
            {
                discovery.RoomDiscovered -= HandleRoomDiscovered;
            }

            if (bootstrap != null)
            {
                bootstrap.SessionPhaseChanged -= HandleSessionPhaseChanged;
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplySceneMode(scene.name);
        }

        /// <summary>
        /// 大厅场景 → 完整面板；业务场景 → 紧凑面板（收起建房/扫描/房间列表，
        /// 保留状态/校准/离开）。画布常驻所以校准能在业务场景里做。
        /// </summary>
        private void ApplySceneMode(string sceneName)
        {
            bool nextCompact = !string.Equals(sceneName, lobbySceneName, StringComparison.Ordinal);
            if (nextCompact == compact && canvasRect != null)
            {
                return;
            }

            compact = nextCompact;
            if (lobbySection != null)
            {
                lobbySection.SetActive(!compact);
            }

            if (canvasRect != null)
            {
                canvasRect.sizeDelta = new Vector2(
                    CanvasWidth, compact ? CanvasHeightCompact : CanvasHeightLobby);
            }

            if (titleText != null)
            {
                titleText.text = compact ? "多人协作（进行中）" : "局域网多人协作";
            }

            PlaceInFrontOfUser(immediate: true);
            BpfNetLog.Info(LogTag, compact ? "compact_mode" : "lobby_mode",
                "面板模式切换：" + sceneName);

            // 模式切换后按当前阶段刷新校准区可见性（切场景不会重放阶段事件）
            BpfSessionPhase current = bootstrap != null
                ? bootstrap.SessionPhase
                : BpfSessionPhase.Offline;
            if (calibrationSection != null)
            {
                calibrationSection.SetActive(current == BpfSessionPhase.Lobby
                    || current == BpfSessionPhase.Calibrating);
            }
        }

        // ------------------------------------------------------------ 画布

        private void BuildCanvas()
        {
            canvasRoot = gameObject;
            RectTransform root = (RectTransform)transform;
            root.localScale = Vector3.one * CanvasWorldScale;

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            // XR 指针事件链：TrackedDeviceGraphicRaycaster + 场景里的
            // XRUIInputModule。普通 GraphicRaycaster 收不到 XR 手柄射线。
            TrackedDeviceGraphicRaycaster raycaster = gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();

            canvasRect = root;
            canvasRect.sizeDelta = new Vector2(CanvasWidth, CanvasHeightLobby);

            Image background = gameObject.AddComponent<Image>();
            background.color = new Color(0.06f, 0.07f, 0.10f, 0.92f);

            PlaceInFrontOfUser(immediate: true);
        }

        /// <summary>场景没有 EventSystem 时自补一个（存在则不动）。</summary>
        private void EnsureEventSystem()
        {
            EventSystem existing = FindObjectOfType<EventSystem>();
            if (existing != null)
            {
                BpfNetLog.Info(LogTag, "event_system_reused",
                    "复用场景 EventSystem，module="
                    + (existing.currentInputModule != null ? existing.currentInputModule.GetType().Name : "null"));
                return;
            }

            GameObject go = new GameObject("BpfLobbyEventSystem");
            DontDestroyOnLoad(go);
            go.AddComponent<EventSystem>();
            go.AddComponent<XRUIInputModule>();
            BpfNetLog.Info(LogTag, "event_system_created", "场景无 EventSystem，已自建 XRUIInputModule");
        }

        // ------------------------------------------------------------ 构建

        private void BuildUi()
        {
            VerticalLayoutGroup layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            titleText = CreateLabel("标题", transform, "局域网多人协作", 30, TextAlignmentOptions.Center, Color.white);
            LayoutElement titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
            titleLayout.minHeight = 40f;

            // 大厅操作区：紧凑模式下整体收起
            lobbySection = new GameObject("大厅操作区", typeof(RectTransform));
            lobbySection.transform.SetParent(transform, false);
            VerticalLayoutGroup lobbyLayout = lobbySection.AddComponent<VerticalLayoutGroup>();
            lobbyLayout.spacing = 8f;
            lobbyLayout.childControlWidth = true;
            lobbyLayout.childForceExpandWidth = true;

            nicknameInput = CreateInputRow(lobbySection.transform, "昵称", LoadNickname());
            roomNameInput = CreateInputRow(lobbySection.transform, "房间名", "BPF协作房间");
            hostAddressInput = CreateInputRow(lobbySection.transform, "房主IP（手填选填）", string.Empty);

            GameObject buttonRow = CreateButtonRow(out createButton, out Button scanButton,
                out Button joinButton);
            LayoutElement buttonRowLayout = buttonRow.AddComponent<LayoutElement>();
            buttonRowLayout.minHeight = RowHeight;

            CreateRoomList(out roomListContent, out roomRowPrefab);

            // 常驻区：两种模式都可见
            GameObject actionRow = new GameObject("动作行", typeof(RectTransform));
            actionRow.transform.SetParent(transform, false);
            HorizontalLayoutGroup actionHorizontal = actionRow.AddComponent<HorizontalLayoutGroup>();
            actionHorizontal.spacing = 8f;
            actionHorizontal.childControlWidth = true;
            actionHorizontal.childForceExpandWidth = true;
            LayoutElement actionRowLayout = actionRow.AddComponent<LayoutElement>();
            actionRowLayout.minHeight = RowHeight;

            startButton = CreateButton(actionRow.transform, "开始业务");
            leaveButton = CreateButton(actionRow.transform, "离开房间");

            statusText = CreateLabel("状态", transform, "进入多人页即就绪：建房或扫描加入", 22,
                TextAlignmentOptions.Left, new Color(0.75f, 0.85f, 1f));
            LayoutElement statusLayout = statusText.gameObject.AddComponent<LayoutElement>();
            statusLayout.minHeight = 30f;

            calibrationSection = CreateCalibrationSection(transform);
            calibrationSection.SetActive(false);

            createButton.onClick.AddListener(HandleCreateClicked);
            scanButton.onClick.AddListener(HandleScanClicked);
            joinButton.onClick.AddListener(HandleJoinClicked);
            leaveButton.onClick.AddListener(HandleLeaveClicked);
            startButton.onClick.AddListener(HandleStartBusinessClicked);
            calibrateButton.onClick.AddListener(HandleCalibrateClicked);
            startButton.interactable = false;
        }

        // ------------------------------------------------------------ 交互

        private void HandleCreateClicked()
        {
            if (roomService == null)
            {
                return;
            }

            SaveNickname(nicknameInput.text);
            roomService.LocalPlayerName = nicknameInput.text;
            roomService.CreateRoom(roomNameInput.text);
        }

        private void HandleScanClicked()
        {
            if (discovery == null)
            {
                return;
            }

            selectedRoom = default;
            discovery.StartScan();
            SetStatus("正在扫描局域网房间…", false);
        }

        private void HandleJoinClicked()
        {
            if (roomService == null)
            {
                return;
            }

            SaveNickname(nicknameInput.text);
            roomService.LocalPlayerName = nicknameInput.text;

            string manual = hostAddressInput.text != null ? hostAddressInput.text.Trim() : string.Empty;
            if (!string.IsNullOrEmpty(manual))
            {
                roomService.JoinRoom(manual, "手动加入");
                return;
            }

            if (string.IsNullOrEmpty(selectedRoom.hostAddress))
            {
                SetStatus("请先扫描并选中一个房间，或手动填写房主 IP", true);
                return;
            }

            roomService.JoinRoom(selectedRoom.hostAddress, selectedRoom.roomName);
        }

        private void HandleLeaveClicked()
        {
            roomService?.LeaveRoom();
        }

        /// <summary>房主点击"开始业务"：经场景路由走 NGO 场景管理，全员跟进。</summary>
        private void HandleStartBusinessClicked()
        {
            if (bootstrap == null)
            {
                return;
            }

            if (bootstrap.SceneRouter == null)
            {
                SetStatus("场景路由未就绪", true);
                return;
            }

            bootstrap.SceneRouter.RequestStartBusinessScene();
        }

        /// <summary>房主点击"开始校准"：交 BpfSessionCalibration 推进。</summary>
        private void HandleCalibrateClicked()
        {
            BpfSessionState state = BpfSessionState.Instance;
            if (state == null || state.Calibration == null)
            {
                SetStatus("会话状态尚未就绪（需要在房间内）", true);
                return;
            }

            state.Calibration.BeginCalibration();
            SetStatus("已进入校准：请依次扣扳机记录三个物理标记点", false);
        }

        private void HandleCalibrationPhase(BpfSessionPhase phase)
        {
            // 校准区在大堂（等房主点开始）和校准中（提示采点）都可见；
            // 此前方块只在 Calibrating 显示，「开始校准」按钮根本点不到。
            if (calibrationSection != null)
            {
                calibrationSection.SetActive(phase == BpfSessionPhase.Lobby
                    || phase == BpfSessionPhase.Calibrating);
            }

            SetStatus(phase == BpfSessionPhase.Playing
                ? "校准完成，协作进行中"
                : "校准阶段：" + phase, false);
        }

        private void HandleCalibrationHint(int index)
        {
            if (calibrationHint == null)
            {
                return;
            }

            calibrationHint.text = index <= 0
                ? "校准已完成"
                : "请用任一只手触摸第 " + index + " 个物理标记点并扣扳机";
        }

        // ------------------------------------------------------------ 事件

        private void HandleStatusChanged(string status)
        {
            SetStatus(status, false);
        }

        private void HandleSessionEvent(BpfSessionEvent evt)
        {
            SetStatus(evt.message, evt.isError);
        }

        private void HandleRoomDiscovered(BpfDiscoveredRoom room)
        {
            RefreshRoomList();
        }

        private void HandleSessionPhaseChanged(BpfSessionPhase phase)
        {
            bool inRoom = phase != BpfSessionPhase.Offline;
            bool isHost = bootstrap != null && bootstrap.NetworkManager != null && bootstrap.NetworkManager.IsHost;
            createButton.interactable = !inRoom;
            leaveButton.interactable = inRoom;
            // 开始业务只在大堂模式有意义（紧凑模式下人已在业务场景里）
            startButton.interactable = inRoom && isHost && !compact;
            calibrateButton.interactable = inRoom && isHost;

            // 校准界面在大堂/校准中显示（数据由 BpfSessionCalibration 驱动）。
            calibrationSection.SetActive(phase == BpfSessionPhase.Lobby
                || phase == BpfSessionPhase.Calibrating);
        }

        private void Update()
        {
            if (discovery == null || roomService == null)
            {
                return;
            }

            if (discovery.IsScanning && Time.time >= nextListRefreshTime)
            {
                nextListRefreshTime = Time.time + ListRefreshInterval;
                RefreshRoomList();
            }

            UpdatePlacement();
        }

        // ------------------------------------------------------------ 房间列表

        private void RefreshRoomList()
        {
            if (roomListContent == null)
            {
                return;
            }

            IReadOnlyList<BpfDiscoveredRoom> rooms = discovery.Rooms;
            HashSet<string> seen = new HashSet<string>();

            foreach (BpfDiscoveredRoom room in rooms)
            {
                string key = room.hostAddress + ":" + room.gameplayPort;
                seen.Add(key);
                GameObject row = FindOrCreateRow(key);
                ConfigureRow(row, room);
            }

            // 移除已不存在的房间行
            for (int i = roomRows.Count - 1; i >= 0; i--)
            {
                GameObject row = roomRows[i];
                string key = row.name;
                if (!seen.Contains(key))
                {
                    roomRows.RemoveAt(i);
                    Destroy(row);
                }
            }

            if (rooms.Count == 0)
            {
                SetStatus(discovery.IsScanning ? "扫描中…暂未发现房间" : "没有发现房间，确认同一局域网后重试扫描", false);
            }
        }

        private GameObject FindOrCreateRow(string key)
        {
            foreach (GameObject row in roomRows)
            {
                if (row.name == key)
                {
                    return row;
                }
            }

            GameObject created = Instantiate(roomRowPrefab, roomListContent);
            created.name = key;
            created.SetActive(true); // 模板默认隐藏，实例必须显式激活
            roomRows.Add(created);
            return created;
        }

        private void ConfigureRow(GameObject row, BpfDiscoveredRoom room)
        {
            Button button = row.GetComponent<Button>();
            TextMeshProUGUI label = row.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                bool compatible = room.IsCompatible(BpfLanNetworkSettings.Active);
                label.text = room.roomName + "  " + room.playerCount + "/" + room.maxPlayers
                    + "  " + room.hostAddress + (compatible ? string.Empty : "  （版本不兼容）");
                label.color = compatible ? Color.white : new Color(1f, 0.55f, 0.55f);
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    selectedRoom = room;
                    SetStatus("已选中房间「" + room.roomName + "」，点加入", false);
                });
            }
        }

        // ------------------------------------------------------------ 摆位

        /// <summary>把面板摆到主相机前方（大厅模式只摆一次，紧凑模式每帧缓跟）。</summary>
        private void PlaceInFrontOfUser(bool immediate)
        {
            if (canvasRoot == null)
            {
                return;
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            Vector3 forward = cam.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            Vector3 target = cam.transform.position + forward * FollowDistance + Vector3.up * FollowHeight;

            if (immediate)
            {
                canvasRoot.transform.position = target;
                Vector3 look = canvasRoot.transform.position - cam.transform.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.0001f)
                {
                    canvasRoot.transform.rotation = Quaternion.LookRotation(look);
                }
            }
        }

        private void UpdatePlacement()
        {
            if (!compact || canvasRoot == null || !canvasRoot.activeSelf)
            {
                return; // 大厅模式：创建时摆好就不动
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            Vector3 forward = cam.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
            {
                forward.Normalize();
                Vector3 target = cam.transform.position + forward * FollowDistance + Vector3.up * FollowHeight;
                canvasRoot.transform.position = Vector3.Lerp(
                    canvasRoot.transform.position, target, Time.deltaTime * FollowSpeed);

                Vector3 look = canvasRoot.transform.position - cam.transform.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.0001f)
                {
                    canvasRoot.transform.rotation = Quaternion.Slerp(
                        canvasRoot.transform.rotation, Quaternion.LookRotation(look),
                        Time.deltaTime * FollowSpeed);
                }
            }
        }

        // ------------------------------------------------------------ UI 构件

        private GameObject CreateButtonRow(out Button create, out Button scan, out Button join)
        {
            GameObject row = new GameObject("按钮行", typeof(RectTransform));
            row.transform.SetParent(lobbySection.transform, false);
            HorizontalLayoutGroup horizontal = row.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 8f;
            horizontal.childControlWidth = true;
            horizontal.childForceExpandWidth = true;

            create = CreateButton(row.transform, "创建房间");
            scan = CreateButton(row.transform, "扫描局域网");
            join = CreateButton(row.transform, "加入房间");
            return row;
        }

        private Button CreateButton(Transform parent, string text)
        {
            GameObject go = new GameObject(text, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = new Color(0.20f, 0.32f, 0.50f, 1f);

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.20f, 0.32f, 0.50f, 1f);
            colors.highlightedColor = new Color(0.30f, 0.45f, 0.68f, 1f);
            colors.pressedColor = new Color(0.14f, 0.22f, 0.36f, 1f);
            colors.selectedColor = new Color(0.20f, 0.32f, 0.50f, 1f);
            button.colors = colors;

            LayoutElement layout = go.AddComponent<LayoutElement>();
            layout.minHeight = RowHeight;

            TextMeshProUGUI label = CreateLabel(text, go.transform, text, 22, TextAlignmentOptions.Center, Color.white);
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.raycastTarget = false;

            return button;
        }

        private TMP_InputField CreateInputRow(Transform parent, string label, string initial)
        {
            GameObject row = new GameObject("输入_" + label, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            HorizontalLayoutGroup horizontal = row.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 8f;
            horizontal.childControlWidth = true;
            horizontal.childForceExpandWidth = false;

            TextMeshProUGUI labelText = CreateLabel("标签", row.transform, label, 22,
                TextAlignmentOptions.Left, new Color(0.8f, 0.85f, 0.9f));
            LayoutElement labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 150f;
            labelLayout.minHeight = RowHeight;

            TMP_InputField field = CreateInputField("输入框", row.transform, initial, label);
            LayoutElement fieldLayout = field.gameObject.AddComponent<LayoutElement>();
            fieldLayout.flexibleWidth = 1f;
            fieldLayout.minHeight = RowHeight;
            return field;
        }

        private TMP_InputField CreateInputField(string name, Transform parent, string initial, string placeholder)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = new Color(0.16f, 0.18f, 0.22f, 1f);

            TMP_InputField field = go.AddComponent<TMP_InputField>();
            field.targetGraphic = image;
            field.transition = Selectable.Transition.None;

            GameObject area = new GameObject("TextArea", typeof(RectTransform));
            area.transform.SetParent(go.transform, false);
            area.AddComponent<RectMask2D>();
            RectTransform areaRect = (RectTransform)area.transform;
            areaRect.anchorMin = Vector2.zero;
            areaRect.anchorMax = Vector2.one;
            areaRect.offsetMin = new Vector2(6f, 4f);
            areaRect.offsetMax = new Vector2(-6f, -4f);

            TextMeshProUGUI text = CreateLabel("Text", area.transform, initial, 22,
                TextAlignmentOptions.Left, Color.white);
            text.raycastTarget = false;
            RectTransform textRect = (RectTransform)text.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI hint = CreateLabel("Placeholder", area.transform, placeholder, 22,
                TextAlignmentOptions.Left, new Color(0.45f, 0.48f, 0.52f));
            hint.raycastTarget = false;
            RectTransform hintRect = (RectTransform)hint.transform;
            hintRect.anchorMin = Vector2.zero;
            hintRect.anchorMax = Vector2.one;
            hintRect.offsetMin = Vector2.zero;
            hintRect.offsetMax = Vector2.zero;

            field.textViewport = areaRect;
            field.textComponent = text;
            field.placeholder = hint;
            field.text = initial ?? string.Empty;
            field.lineType = TMP_InputField.LineType.SingleLine;
            return field;
        }

        private void CreateRoomList(out RectTransform content, out GameObject rowPrefab)
        {
            GameObject scrollGo = new GameObject("房间列表", typeof(RectTransform));
            scrollGo.transform.SetParent(lobbySection.transform, false);
            ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;

            Image scrollBg = scrollGo.AddComponent<Image>();
            scrollBg.color = new Color(0.10f, 0.12f, 0.16f, 1f);
            LayoutElement scrollLayout = scrollGo.AddComponent<LayoutElement>();
            scrollLayout.flexibleHeight = 1f;
            scrollLayout.minHeight = 120f;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollGo.transform, false);
            viewport.AddComponent<RectMask2D>();
            RectTransform viewportRect = (RectTransform)viewport.transform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            GameObject contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewport.transform, false);
            content = (RectTransform)contentGo.transform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            VerticalLayoutGroup contentLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 4f;
            contentLayout.padding = new RectOffset(6, 6, 6, 6);
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRect;
            scroll.content = content;

            rowPrefab = CreateRoomRowPrefab();
        }

        private GameObject CreateRoomRowPrefab()
        {
            GameObject go = new GameObject("RoomRow", typeof(RectTransform));
            go.transform.SetParent(roomListContent, false);
            go.SetActive(false); // 只作模板，Instantiate 时激活

            Image image = go.AddComponent<Image>();
            image.color = new Color(0.18f, 0.21f, 0.27f, 1f);

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            LayoutElement layout = go.AddComponent<LayoutElement>();
            layout.minHeight = RowHeight;

            TextMeshProUGUI label = CreateLabel("房间名", go.transform, string.Empty, 20,
                TextAlignmentOptions.Left, Color.white);
            label.raycastTarget = false;
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 0f);
            labelRect.offsetMax = new Vector2(-8f, 0f);

            return go;
        }

        private static TMP_FontAsset cjkFont;

        /// <summary>
        /// 找一个能画中文的字体：优先场景里已有 TMP 文本在用的字体
        /// （01StartUp 的 UI 用的是 SIMHEI），再退到 TMP 默认与回退列表。
        /// TMP_Settings.defaultFontAsset（LiberationSans）没有中文字形，
        /// 直接用会把面板文字全渲染成方框——预检日志里已见到。
        /// </summary>
        private static TMP_FontAsset ResolveCjkFont()
        {
            if (cjkFont != null)
            {
                return cjkFont;
            }

            List<TMP_FontAsset> candidates = new List<TMP_FontAsset>();
            foreach (TextMeshProUGUI text in UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>(true))
            {
                if (text.font != null && !candidates.Contains(text.font))
                {
                    candidates.Add(text.font);
                }
            }

            if (TMP_Settings.defaultFontAsset != null && !candidates.Contains(TMP_Settings.defaultFontAsset))
            {
                candidates.Add(TMP_Settings.defaultFontAsset);
            }

            if (TMP_Settings.fallbackFontAssets != null)
            {
                foreach (TMP_FontAsset fallback in TMP_Settings.fallbackFontAssets)
                {
                    if (fallback != null && !candidates.Contains(fallback))
                    {
                        candidates.Add(fallback);
                    }
                }
            }

            foreach (TMP_FontAsset candidate in candidates)
            {
                if (candidate.HasCharacter('协') && candidate.HasCharacter('校'))
                {
                    cjkFont = candidate;
                    BpfNetLog.Info(LogTag, "cjk_font_resolved", "中文字体：" + candidate.name);
                    return candidate;
                }
            }

            return null;
        }

        private TextMeshProUGUI CreateLabel(string name, Transform parent, string content,
            float fontSize, TextAlignmentOptions alignment, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.text = content ?? string.Empty;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Overflow;

            TMP_FontAsset font = ResolveCjkFont() ?? TMP_Settings.defaultFontAsset;
            if (font != null)
            {
                text.font = font;
            }

            return text;
        }

        private GameObject CreateCalibrationSection(Transform parent)
        {
            GameObject section = new GameObject("校准区", typeof(RectTransform));
            section.transform.SetParent(parent, false);
            VerticalLayoutGroup layout = section.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            CreateLabel("校准标题", section.transform, "三点校准", 24,
                TextAlignmentOptions.Center, new Color(0.9f, 0.85f, 0.6f));

            calibrateButton = CreateButton(section.transform, "开始校准");
            calibrateButton.interactable = false;

            calibrationHint = CreateLabel("校准提示", section.transform,
                "房主点「开始校准」，全员依次用任一只手触摸三个物理标记点并扣扳机",
                20, TextAlignmentOptions.Left, new Color(0.8f, 0.8f, 0.8f));
            LayoutElement hintLayout = calibrationHint.gameObject.AddComponent<LayoutElement>();
            hintLayout.minHeight = 34f;

            return section;
        }

        // ------------------------------------------------------------ 状态

        private void SetStatus(string message, bool isError)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = message ?? string.Empty;
            statusText.color = isError ? new Color(1f, 0.55f, 0.55f) : new Color(0.75f, 0.85f, 1f);
            BpfNetLog.Info(LogTag, isError ? "status_error" : "status", message);
        }

        private static string LoadNickname()
        {
            return PlayerPrefs.GetString(NicknamePrefsKey, "玩家");
        }

        private static void SaveNickname(string nickname)
        {
            if (string.IsNullOrEmpty(nickname))
            {
                return;
            }

            PlayerPrefs.SetString(NicknamePrefsKey, nickname);
            PlayerPrefs.Save();
        }
    }
}
