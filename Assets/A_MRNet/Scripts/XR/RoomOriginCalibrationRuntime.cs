using System.Collections;
using MixedReality.Toolkit.SpatialManipulation;
using MixedReality.Toolkit.UX;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Bpf.Diagnostics;

namespace App.Runtime.RoomCalibration
{
    /// <summary>
    /// Runtime-only calibration that maps the agreed room marker position to
    /// the app origin. It does not call PICO-specific APIs or persist across
    /// cold app starts.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-900)]
    public sealed class RoomOriginCalibrationManager : MonoBehaviour
    {
        private const string RuntimeObjectName = "Room Origin Calibration Runtime";
        private const string PanelObjectName = "Room Origin Calibration Panel";
        private const string RecalibrationButtonObjectName = "List Item (6)";
        private const string RuntimeCanvasObjectName = "Room Origin Calibration Canvas";
        private const string ButtonsCanvasObjectName = "Buttons Canvas";
        private const string ButtonsPanelPath = "ListItems/ParentObject";

        public static RoomOriginCalibrationManager Instance { get; private set; }

        [SerializeField] private float stableSecondsRequired = 1.5f;
        [SerializeField] private float stablePositionTolerance = 0.03f;
        [SerializeField] private float stableYawToleranceDegrees = 5f;
        [SerializeField] private float startupCalibrationDelaySeconds = 2.5f;
        [SerializeField] private bool allowEditorCameraFallback = true;
        [SerializeField] private bool debugLog;

        private bool isCalibrating;
        private bool hasLastPose;
        private float stableSeconds;
        private Vector3 lastHeadPosition;
        private Vector3 lastHeadForward;

        private Canvas targetCanvas;
        private GameObject panelRoot;
        private Coroutine sceneHandlingCoroutine;
        private Text titleText;
        private Text statusText;
        private Text detailText;
        private Button confirmButton;
        private Button cancelButton;
        private PressableButton recalibrationButton;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            RoomOriginCalibrationSession.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureRuntime()
        {
            EnsureRuntimeManager();
        }

        /// <summary>
        /// Creates the calibration runtime when an already-initialized business UI
        /// needs its re-calibration action. This is used by the 03 Buttons Canvas
        /// entry point, which is initialized after the scene has loaded.
        /// </summary>
        public static RoomOriginCalibrationManager EnsureRuntimeForActiveScene()
        {
            RoomOriginCalibrationManager manager = EnsureRuntimeManager();
            if (manager != null)
            {
                manager.HandleActiveSceneChanged();
            }

            return manager;
        }

        private static RoomOriginCalibrationManager EnsureRuntimeManager()
        {
            if (Instance != null)
            {
                return Instance;
            }

            GameObject runtimeObject = new GameObject(RuntimeObjectName);
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(runtimeObject);
            }

            return runtimeObject.AddComponent<RoomOriginCalibrationManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Application.onBeforeRender += OnBeforeRender;
            RestartSceneHandling();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Application.onBeforeRender -= OnBeforeRender;
            if (sceneHandlingCoroutine != null)
            {
                StopCoroutine(sceneHandlingCoroutine);
                sceneHandlingCoroutine = null;
            }
        }

        private void Update()
        {
            if (!isCalibrating)
            {
                return;
            }

            UpdateCalibrationReadiness();
            UpdateUiText();
        }

        private void LateUpdate()
        {
            UpdateCalibrationCanvasPose();
        }

        private void OnBeforeRender()
        {
            UpdateCalibrationCanvasPose();
        }

        public void BeginCalibration()
        {
            isCalibrating = true;
            stableSeconds = 0f;
            hasLastPose = false;
            EnsureCalibrationUi();
            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(RoomOriginCalibrationSession.IsCalibrated);
            }
            SetPanelVisible(true);
            UpdateUiText();
            Log("Calibration started.");
        }

        public bool TryConfirmCalibration()
        {
            if (!isCalibrating || stableSeconds < stableSecondsRequired)
            {
                UpdateUiText();
                return false;
            }

            if (!TryGetHeadPose(out Vector3 headPosition, out Quaternion headRotation, out string reason))
            {
                SetStatus("等待头显追踪", reason);
                return false;
            }

            XROrigin origin = FindActiveOrigin();
            if (origin == null)
            {
                SetStatus("未找到 XR Origin", "当前场景没有可校准的 XROrigin。");
                return false;
            }

            if (!TryBuildCalibration(origin, headPosition, headRotation, out Vector3 originPosition, out float originYaw))
            {
                SetStatus("无法确认朝向", "请面向地面箭头后再次确认。");
                return false;
            }

            RoomOriginCalibrationSession.Store(originPosition, originYaw);
            ApplyToActiveOrigin();
            isCalibrating = false;
            SetPanelVisible(false);
            Log("Calibration confirmed.");
            return true;
        }

        public void CancelCalibration()
        {
            if (!isCalibrating)
            {
                return;
            }

            isCalibrating = false;
            stableSeconds = 0f;
            hasLastPose = false;
            SetPanelVisible(false);
            ApplyToActiveOrigin();
            Log("Calibration cancelled.");
        }

        public bool ApplyToActiveOrigin()
        {
            if (!RoomOriginCalibrationSession.IsCalibrated)
            {
                return false;
            }

            XROrigin origin = FindActiveOrigin();
            if (origin == null)
            {
                return false;
            }

            Transform originTransform = origin.transform;
            Vector3 appliedPosition = RoomOriginCalibrationSession.OriginPosition;
            appliedPosition.y = originTransform.position.y;

            originTransform.SetPositionAndRotation(
                appliedPosition,
                Quaternion.Euler(0f, RoomOriginCalibrationSession.OriginYawDegrees, 0f));

            if (debugLog)
            {
                BpfLog.Info(BpfLogCategory.XR, "room_origin_calibration_runtime.apply_calibration", "[RoomOriginCalibration] Applied calibration to " + SceneManager.GetActiveScene().name + ".", this);
            }
            return true;
        }

        public void ClearCalibration()
        {
            RoomOriginCalibrationSession.Clear();
            isCalibrating = false;
            stableSeconds = 0f;
            hasLastPose = false;
            SetPanelVisible(false);
            Log("Calibration cleared.");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RestartSceneHandling();
        }

        private void RestartSceneHandling()
        {
            if (sceneHandlingCoroutine != null)
            {
                StopCoroutine(sceneHandlingCoroutine);
            }

            sceneHandlingCoroutine = StartCoroutine(HandleActiveSceneChangedAfterFrame());
        }

        private IEnumerator HandleActiveSceneChangedAfterFrame()
        {
            yield return null;

            if (Instance == null)
            {
                Instance = this;
            }

            if (Instance == this)
            {
                Scene sceneAtStart = SceneManager.GetActiveScene();
                if (IsStartupScene(sceneAtStart) && !RoomOriginCalibrationSession.IsCalibrated)
                {
                    yield return new WaitForSecondsRealtime(startupCalibrationDelaySeconds);

                    if (Instance != this || SceneManager.GetActiveScene() != sceneAtStart)
                    {
                        sceneHandlingCoroutine = null;
                        yield break;
                    }
                }

                HandleActiveSceneChanged();
            }

            sceneHandlingCoroutine = null;
        }

        private void HandleActiveSceneChanged()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (IsStartupScene(activeScene))
            {
                SetRecalibrationButtonVisible(false);
                if (!RoomOriginCalibrationSession.IsCalibrated)
                {
                    BeginCalibration();
                }
                else
                {
                    EnsureCalibrationUi();
                    SetPanelVisible(false);
                    ApplyToActiveOrigin();
                }

                return;
            }

            SetPanelVisible(false);
            ApplyToActiveOrigin();
            EnsureRecalibrationButton();
            SetRecalibrationButtonVisible(true);
        }

        private void UpdateCalibrationReadiness()
        {
            if (!TryGetHeadPose(out Vector3 headPosition, out Quaternion headRotation, out _))
            {
                stableSeconds = 0f;
                hasLastPose = false;
                return;
            }

            Vector3 headForward = FlattenForward(headRotation * Vector3.forward);
            if (headForward.sqrMagnitude < 0.0001f)
            {
                stableSeconds = 0f;
                hasLastPose = false;
                return;
            }

            bool isStable = true;
            if (hasLastPose)
            {
                float positionDelta = Vector3.Distance(FlattenPosition(headPosition), FlattenPosition(lastHeadPosition));
                float yawDelta = Vector3.Angle(lastHeadForward, headForward);
                isStable = positionDelta <= stablePositionTolerance && yawDelta <= stableYawToleranceDegrees;
            }

            stableSeconds = isStable ? Mathf.Min(stableSeconds + Time.unscaledDeltaTime, stableSecondsRequired) : 0f;
            lastHeadPosition = headPosition;
            lastHeadForward = headForward;
            hasLastPose = true;
        }

        private bool TryBuildCalibration(
            XROrigin origin,
            Vector3 headPosition,
            Quaternion headRotation,
            out Vector3 originPosition,
            out float originYawDegrees)
        {
            originPosition = Vector3.zero;
            originYawDegrees = 0f;

            Transform originTransform = origin.transform;
            Vector3 headForward = FlattenForward(headRotation * Vector3.forward);
            if (headForward.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            float yawDelta = Vector3.SignedAngle(headForward, Vector3.forward, Vector3.up);
            Quaternion targetRotation = Quaternion.AngleAxis(yawDelta, Vector3.up) * originTransform.rotation;
            originYawDegrees = targetRotation.eulerAngles.y;

            Vector3 headLocalPosition = originTransform.InverseTransformPoint(headPosition);
            Vector3 rotatedHeadOffset = Quaternion.Euler(0f, originYawDegrees, 0f) * headLocalPosition;

            originPosition = originTransform.position;
            originPosition.x = -rotatedHeadOffset.x;
            originPosition.z = -rotatedHeadOffset.z;
            return true;
        }

        private bool TryGetHeadPose(out Vector3 position, out Quaternion rotation, out string reason)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            reason = string.Empty;

            Camera camera = FindActiveCamera();
            if (camera == null || !camera.isActiveAndEnabled)
            {
                reason = "当前场景未找到可用 XR Camera。";
                return false;
            }

            InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
            if (headDevice.isValid &&
                headDevice.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked) &&
                isTracked)
            {
                position = camera.transform.position;
                rotation = camera.transform.rotation;
                return true;
            }

#if UNITY_EDITOR
            if (allowEditorCameraFallback)
            {
                position = camera.transform.position;
                rotation = camera.transform.rotation;
                return true;
            }
#endif

            reason = "头显未返回完整位置和旋转追踪。";
            return false;
        }

        private XROrigin FindActiveOrigin()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            XROrigin[] origins = FindObjectsOfType<XROrigin>(true);
            for (int i = 0; i < origins.Length; i++)
            {
                XROrigin origin = origins[i];
                if (origin != null && origin.gameObject.scene == activeScene && origin.gameObject.activeInHierarchy)
                {
                    return origin;
                }
            }

            for (int i = 0; i < origins.Length; i++)
            {
                XROrigin origin = origins[i];
                if (origin != null && origin.gameObject.scene == activeScene)
                {
                    return origin;
                }
            }

            return origins.Length > 0 ? origins[0] : null;
        }

        private Camera FindActiveCamera()
        {
            XROrigin origin = FindActiveOrigin();
            if (origin != null)
            {
                Camera originCamera = origin.Camera != null
                    ? origin.Camera
                    : origin.GetComponentInChildren<Camera>(true);
                if (originCamera != null)
                {
                    return originCamera;
                }
            }

            return Camera.main;
        }

        private void EnsureCalibrationUi()
        {
            if (panelRoot != null)
            {
                return;
            }

            targetCanvas = GetOrCreateActiveCanvas();

            Font font = FindSceneFont();
            panelRoot = CreatePanel(targetCanvas.transform, font);
            panelRoot.transform.SetAsLastSibling();
        }

        private Canvas GetOrCreateActiveCanvas()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            Canvas[] canvases = FindObjectsOfType<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas existingCanvas = canvases[i];
                if (existingCanvas != null &&
                    existingCanvas.name == RuntimeCanvasObjectName &&
                    existingCanvas.gameObject.scene == activeScene)
                {
                    ConfigureTrackingCanvas(existingCanvas);
                    return existingCanvas;
                }
            }

            GameObject canvasObject = new GameObject(RuntimeCanvasObjectName, typeof(RectTransform));
            canvasObject.layer = 5;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1000f, 620f);
            canvasRect.localScale = Vector3.one * 0.001f;

            Canvas runtimeCanvas = canvasObject.AddComponent<Canvas>();
            ConfigureTrackingCanvas(runtimeCanvas);
            return runtimeCanvas;
        }

        private void ConfigureTrackingCanvas(Canvas canvas)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 32000;
            canvas.worldCamera = FindActiveCamera();

            TrackedDeviceGraphicRaycaster raycaster = canvas.GetComponent<TrackedDeviceGraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            }

            SolverHandler solverHandler = canvas.GetComponent<SolverHandler>();
            if (solverHandler == null)
            {
                solverHandler = canvas.gameObject.AddComponent<SolverHandler>();
            }

            solverHandler.TrackedTargetType = TrackedObjectType.Head;
            solverHandler.enabled = false;

            Follow follow = canvas.GetComponent<Follow>();
            if (follow == null)
            {
                follow = canvas.gameObject.AddComponent<Follow>();
            }

            follow.MinDistance = 0.9f;
            follow.MaxDistance = 1.4f;
            follow.DefaultDistance = 1.1f;
            follow.IgnoreReferencePitchAndRoll = true;
            follow.ReorientWhenOutsideParameters = false;
            follow.enabled = false;

            UpdateCalibrationCanvasPose(canvas);
        }

        private GameObject CreatePanel(Transform parent, Font font)
        {
            GameObject root = new GameObject(PanelObjectName);
            root.transform.SetParent(parent, false);

            RectTransform rootRect = root.AddComponent<RectTransform>();
            Stretch(rootRect);

            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            titleText = CreateText(root.transform, "请先校准房间原点", font, 32, TextAnchor.MiddleCenter);
            SetAnchored(titleText.rectTransform, 0f, 180f, 880f, 54f);

            detailText = CreateText(root.transform, "站到地面标记位置，并面向箭头方向。", font, 24, TextAnchor.MiddleCenter);
            SetAnchored(detailText.rectTransform, 0f, 124f, 920f, 48f);

            RectTransform reticle = CreateReticle(root.transform);
            SetAnchored(reticle, 0f, 0f, 72f, 72f);

            statusText = CreateText(root.transform, "等待头显追踪...", font, 22, TextAnchor.MiddleCenter);
            SetAnchored(statusText.rectTransform, 0f, -90f, 920f, 54f);

            confirmButton = CreateButton(
                root.transform,
                font,
                "Confirm Calibration Button",
                "确认校准",
                new Color(0.1f, 0.45f, 0.85f, 1f));
            SetAnchored(confirmButton.GetComponent<RectTransform>(), 0f, -180f, 420f, 84f);
            confirmButton.onClick.AddListener(HandleConfirmClicked);

            cancelButton = CreateButton(
                root.transform,
                font,
                "Cancel Calibration Button",
                "取消",
                new Color(0.35f, 0.35f, 0.35f, 1f));
            SetAnchored(cancelButton.GetComponent<RectTransform>(), 0f, -270f, 360f, 64f);
            cancelButton.onClick.AddListener(CancelCalibration);

            return root;
        }

        private RectTransform CreateReticle(Transform parent)
        {
            GameObject reticleObject = new GameObject("Calibration Reticle");
            reticleObject.transform.SetParent(parent, false);

            RectTransform reticleRect = reticleObject.AddComponent<RectTransform>();
            Image horizontal = CreateReticlePart(reticleRect, "Horizontal", new Vector2(64f, 4f));
            Image vertical = CreateReticlePart(reticleRect, "Vertical", new Vector2(4f, 64f));
            Color reticleColor = new Color(0.25f, 0.95f, 1f, 0.95f);
            horizontal.color = reticleColor;
            vertical.color = reticleColor;
            return reticleRect;
        }

        private Image CreateReticlePart(RectTransform parent, string objectName, Vector2 size)
        {
            GameObject partObject = new GameObject(objectName);
            partObject.transform.SetParent(parent, false);

            Image image = partObject.AddComponent<Image>();
            image.raycastTarget = false;
            SetAnchored(image.rectTransform, 0f, 0f, size.x, size.y);
            return image;
        }

        private Text CreateText(Transform parent, string content, Font font, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(content);
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private Button CreateButton(
            Transform parent,
            Font font,
            string objectName,
            string labelText,
            Color normalColor)
        {
            GameObject buttonObject = new GameObject(objectName);
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.AddComponent<Image>();
            image.color = normalColor;

            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.disabledColor = new Color(0.22f, 0.22f, 0.22f, 0.85f);
            colors.highlightedColor = new Color(0.18f, 0.58f, 1f, 1f);
            colors.pressedColor = new Color(0.05f, 0.28f, 0.62f, 1f);
            button.colors = colors;

            Text label = CreateText(buttonObject.transform, labelText, font, 24, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            label.raycastTarget = false;
            return button;
        }

        private void EnsureRecalibrationButton()
        {
            Transform buttonPanel = FindButtonsPanel();
            if (buttonPanel == null)
            {
                Log("Buttons Canvas/ListItems/ParentObject was not found.");
                return;
            }

            Transform targetButton = buttonPanel.Find(RecalibrationButtonObjectName);
            if (targetButton == null)
            {
                Log("Buttons Canvas/ListItems/ParentObject/List Item (6) was not found.");
                return;
            }

            recalibrationButton = targetButton.GetComponent<PressableButton>();
            if (recalibrationButton == null)
            {
                Log("Buttons Canvas/ListItems/ParentObject/List Item (6) has no PressableButton.");
                return;
            }

            ConfigureRecalibrationButton(recalibrationButton);
        }

        private void ConfigureRecalibrationButton(PressableButton button)
        {
            button.OnClicked.RemoveAllListeners();
            button.OnClicked.AddListener(BeginCalibration);

            TMP_Text label = FindButtonLabel(button.gameObject);
            if (label != null)
            {
                label.text = "<size=8>重新校准原点</size>\n<size=6><alpha=#88>站到标记位置</size>";

                Transform icon = label.transform.parent.Find("Icon");
                if (icon != null)
                {
                    icon.gameObject.SetActive(false);
                }
            }
        }

        private Transform FindButtonsPanel()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            Canvas[] canvases = FindObjectsOfType<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null ||
                    canvas.name != ButtonsCanvasObjectName ||
                    canvas.gameObject.scene != activeScene)
                {
                    continue;
                }

                Transform buttonPanel = canvas.transform.Find(ButtonsPanelPath);
                if (buttonPanel != null)
                {
                    return buttonPanel;
                }
            }

            return null;
        }

        private TMP_Text FindButtonLabel(GameObject buttonObject)
        {
            TMP_Text[] labels = buttonObject.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] != null && labels[i].gameObject.name == "Text")
                {
                    return labels[i];
                }
            }

            return null;
        }

        private Font FindSceneFont()
        {
            Text[] texts = FindObjectsOfType<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].font != null)
                {
                    return texts[i].font;
                }
            }

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private void HandleConfirmClicked()
        {
            TryConfirmCalibration();
        }

        private void UpdateUiText()
        {
            if (panelRoot == null || !isCalibrating)
            {
                return;
            }

            bool hasPose = TryGetHeadPose(out _, out _, out string reason);
            if (!hasPose)
            {
                SetStatus("等待头显追踪", reason);
                return;
            }

            float remaining = Mathf.Max(0f, stableSecondsRequired - stableSeconds);
            if (remaining > 0.01f)
            {
                SetStatus("请保持站立并面向箭头", "保持稳定，倒计时完成后即可确认。");
                return;
            }

            SetStatus("可以确认校准", "点击确认后，当前位置将作为应用坐标原点，面向方向作为 +Z。");
        }

        private void SetStatus(string title, string detail)
        {
            if (statusText != null)
            {
                statusText.text = title;
            }

            if (detailText != null)
            {
                detailText.text = detail;
            }

            if (confirmButton != null)
            {
                confirmButton.interactable = isCalibrating && stableSeconds >= stableSecondsRequired;
            }
        }

        private void SetPanelVisible(bool visible)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(visible);
                panelRoot.transform.SetAsLastSibling();
            }
        }

        private void SetRecalibrationButtonVisible(bool visible)
        {
            if (recalibrationButton != null)
            {
                recalibrationButton.gameObject.SetActive(visible);
            }
        }

        private void UpdateCalibrationCanvasPose()
        {
            if (!isCalibrating || targetCanvas == null)
            {
                return;
            }

            UpdateCalibrationCanvasPose(targetCanvas);
        }

        private void UpdateCalibrationCanvasPose(Canvas canvas)
        {
            if (canvas == null)
            {
                return;
            }

            Camera camera = FindActiveCamera();
            if (camera == null || !camera.isActiveAndEnabled)
            {
                return;
            }

            canvas.worldCamera = camera;
            Transform cameraTransform = camera.transform;
            canvas.transform.SetPositionAndRotation(
                cameraTransform.position + cameraTransform.forward * 1.1f,
                cameraTransform.rotation);
        }

        private bool IsStartupScene(Scene scene)
        {
            return scene.IsValid() && scene.name == TexCore_LoadScene.StartupSceneName;
        }

        private static Vector3 FlattenPosition(Vector3 value)
        {
            value.y = 0f;
            return value;
        }

        private static Vector3 FlattenForward(Vector3 value)
        {
            value.y = 0f;
            return value.normalized;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void SetAnchored(RectTransform rectTransform, float x, float y, float width, float height)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(width, height);
            rectTransform.anchoredPosition = new Vector2(x, y);
        }

        private void Log(string message)
        {
            if (debugLog)
            {
                BpfLog.Info(BpfLogCategory.XR, "room_origin_calibration_runtime.log", "[RoomOriginCalibration] " + message, this);
            }
        }

    }

    public static class RoomOriginCalibrationSession
    {
        public static bool IsCalibrated { get; private set; }
        public static Vector3 OriginPosition { get; private set; }
        public static float OriginYawDegrees { get; private set; }

        public static void Store(Vector3 originPosition, float originYawDegrees)
        {
            OriginPosition = originPosition;
            OriginYawDegrees = originYawDegrees;
            IsCalibrated = true;
        }

        public static void Clear()
        {
            IsCalibrated = false;
            OriginPosition = Vector3.zero;
            OriginYawDegrees = 0f;
        }
    }
}
