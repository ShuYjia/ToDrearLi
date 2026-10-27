using System.Collections;
using System.Collections.Generic;
using MixedReality.Toolkit;
using MixedReality.Toolkit.Input;
using MixedReality.Toolkit.SpatialManipulation;
using MixedReality.Toolkit.Subsystems;
using MixedReality.Toolkit.UX;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace App.Runtime.HandMenu
{
    /// <summary>
    /// Leaves the 03 Buttons Canvas at its scene-authored pose by default. A
    /// short left X press toggles it, while a hold follows the left hand and
    /// preserves the last followed pose when released.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-850)]
    public sealed class ButtonsCanvasHandMenuRuntime : MonoBehaviour
    {
        private const string RuntimeObjectName = "Buttons Canvas Hand Menu Runtime";
        private const string ButtonsCanvasObjectName = "Buttons Canvas";
        private const string ListItemsParentPath = "ListItems/ParentObject";
        private const string SingleMenuButtonName = "List Item";
        private const string BackpackButtonName = "List Item (3)";
        private const string SingleMenuCanvasName = "Single Menu Canvas";
        private const string BackpackUiName = "背包UI";
        private const string LeftPrimaryBinding = "<XRController>{LeftHand}/primaryButton";
        private const string KeyboardXBinding = "<Keyboard>/x";
        private const float LongPressThreshold = 0.35f;
        private const float ControllerVerticalOffset = 0.18f;
        private const float GestureMaxDistance = 0.8f;
        private const float GestureMinDistance = 0.12f;
        private const float GesturePalmAngle = 35f;
        // The three Canvas roots share one origin while linked. Any visual
        // spacing must be authored inside each Canvas, not by moving roots.
        private static readonly Vector3 SingleMenuFollowOffset = Vector3.zero;
        private static readonly Vector3 BackpackFollowOffset = Vector3.zero;

        private enum VisibleUi
        {
            Buttons,
            SingleMenu,
            Backpack
        }

        private static ButtonsCanvasHandMenuRuntime instance;
        private static readonly HashSet<int> SceneButtonBindingIds = new HashSet<int>();

        private InputAction leftPrimaryAction;
        private Canvas activeButtonsCanvas;
        private bool controllerMenuVisible = true;
        private VisibleUi visibleUi = VisibleUi.Buttons;
        private bool isConfigured;
        private bool isLeftPrimaryPressed;
        private bool isFollowingLeftHand;
        private float leftPrimaryPressedAt;
        private GameObject linkedSingleMenuCanvas;
        private GameObject linkedBackpackUi;
        private Vector3 singleMenuRelativePosition;
        private Quaternion singleMenuRelativeRotation;
        private Vector3 backpackRelativePosition;
        private Quaternion backpackRelativeRotation;
        private bool hasLinkedUiPoses;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            SceneButtonBindingIds.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureRuntime()
        {
            if (instance != null)
            {
                return;
            }

            GameObject runtimeObject = new GameObject(RuntimeObjectName);
            DontDestroyOnLoad(runtimeObject);
            runtimeObject.AddComponent<ButtonsCanvasHandMenuRuntime>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            StartCoroutine(ConfigureActiveSceneAfterFrame());
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            DisposeLeftPrimaryAction();
        }

        private void Update()
        {
            if (!IsButtonsCanvasScene(SceneManager.GetActiveScene().name))
            {
                return;
            }

            if (!isConfigured)
            {
                TryConfigureActiveScene();
            }

            if (activeButtonsCanvas == null || !isConfigured)
            {
                return;
            }

            bool shouldFollowLeftHand = ShouldFollowLeftHand(
                isLeftPrimaryPressed,
                Time.unscaledTime - leftPrimaryPressedAt);
            if (isFollowingLeftHand != shouldFollowLeftHand)
            {
                isFollowingLeftHand = shouldFollowLeftHand;
                SetButtonsCanvasFollowing(shouldFollowLeftHand);
            }

            if (visibleUi == VisibleUi.Buttons)
            {
                bool shouldBeVisible = controllerMenuVisible || shouldFollowLeftHand;
                if (IsPanelVisible(activeButtonsCanvas.gameObject) != shouldBeVisible)
                {
                    SetButtonsCanvasVisible(shouldBeVisible, shouldFollowLeftHand);
                }
            }
            else
            {
                SetVisibleUi(
                    visibleUi,
                    activeButtonsCanvas.gameObject,
                    linkedSingleMenuCanvas,
                    linkedBackpackUi);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneButtonBindingIds.Clear();
            activeButtonsCanvas = null;
            controllerMenuVisible = true;
            visibleUi = VisibleUi.Buttons;
            isConfigured = false;
            isLeftPrimaryPressed = false;
            isFollowingLeftHand = false;
            linkedSingleMenuCanvas = null;
            linkedBackpackUi = null;
            hasLinkedUiPoses = false;
            if (!IsButtonsCanvasScene(scene.name))
            {
                DisposeLeftPrimaryAction();
                return;
            }

            StartCoroutine(ConfigureActiveSceneAfterFrame());
        }

        private IEnumerator ConfigureActiveSceneAfterFrame()
        {
            yield return null;
            if (!IsButtonsCanvasScene(SceneManager.GetActiveScene().name))
            {
                yield break;
            }

            EnsureLeftPrimaryAction();
            TryConfigureActiveScene();
        }

        public static bool Configure(
            GameObject buttonsCanvas,
            XRBaseInteractor leftInteractor,
            XRBaseInteractor rightInteractor)
        {
            if (buttonsCanvas == null ||
                buttonsCanvas.GetComponent<Canvas>() == null ||
                (leftInteractor == null && rightInteractor != null))
            {
                return false;
            }

            SolverHandler solverHandler = buttonsCanvas.GetComponent<SolverHandler>();
            if (solverHandler == null)
            {
                solverHandler = buttonsCanvas.AddComponent<SolverHandler>();
            }

            solverHandler.LeftInteractor = leftInteractor;
            solverHandler.RightInteractor = null;
            solverHandler.TrackedHandedness = Handedness.Left;
            solverHandler.PreferredTrackedHandedness = Handedness.Left;
            solverHandler.TrackedTargetType = leftInteractor != null
                ? TrackedObjectType.Interactor
                : TrackedObjectType.HandJoint;
            solverHandler.TrackedHandJoint = TrackedHandJoint.Palm;
            solverHandler.AdditionalOffset = Vector3.zero;
            solverHandler.UpdateSolvers = false;

            HandConstraint handConstraint = buttonsCanvas.GetComponent<HandConstraint>();
            if (handConstraint == null)
            {
                handConstraint = buttonsCanvas.AddComponent<HandConstraint>();
            }

            handConstraint.SafeZone = HandConstraint.SolverSafeZone.UlnarSide;
            handConstraint.SafeZoneBuffer = 0.16f;
            handConstraint.RotationBehavior = HandConstraint.SolverRotationBehavior.LookAtMainCamera;
            handConstraint.OffsetBehavior = HandConstraint.SolverOffsetBehavior.LookAtCameraRotation;
            handConstraint.HideHandCursorsOnActivate = false;

            solverHandler.RefreshTrackedObject();
            return true;
        }

        public static bool IsButtonsCanvasScene(string sceneName) =>
            TexCore_LoadScene.IsBusinessScene(sceneName);

        public static bool ToggleControllerVisibility(bool isVisible) => !isVisible;

        public static bool IsLongPress(float heldSeconds) =>
            heldSeconds >= LongPressThreshold;

        public static bool ShouldFollowLeftHand(bool isPrimaryPressed, float heldSeconds) =>
            isPrimaryPressed && IsLongPress(heldSeconds);

        public static Vector3 GetControllerMenuOffset() =>
            Vector3.up * ControllerVerticalOffset;

        private void OnLeftPrimaryStarted(InputAction.CallbackContext context)
        {
            isLeftPrimaryPressed = true;
            leftPrimaryPressedAt = Time.unscaledTime;
        }

        private void OnLeftPrimaryCanceled(InputAction.CallbackContext context)
        {
            bool wasLongPress = ShouldFollowLeftHand(
                isLeftPrimaryPressed,
                Time.unscaledTime - leftPrimaryPressedAt);
            isLeftPrimaryPressed = false;
            if (!wasLongPress)
            {
                ShowButtonsCanvas(
                    activeButtonsCanvas != null ? activeButtonsCanvas.gameObject : null,
                    linkedSingleMenuCanvas,
                    linkedBackpackUi);
            }
        }

        private void EnsureLeftPrimaryAction()
        {
            if (leftPrimaryAction != null)
            {
                return;
            }

            leftPrimaryAction = new InputAction(
                "Show Buttons Canvas",
                InputActionType.Button,
                LeftPrimaryBinding);
            leftPrimaryAction.AddBinding(KeyboardXBinding);
            leftPrimaryAction.started += OnLeftPrimaryStarted;
            leftPrimaryAction.canceled += OnLeftPrimaryCanceled;
            leftPrimaryAction.Enable();
        }

        private void DisposeLeftPrimaryAction()
        {
            if (leftPrimaryAction == null)
            {
                return;
            }

            leftPrimaryAction.started -= OnLeftPrimaryStarted;
            leftPrimaryAction.canceled -= OnLeftPrimaryCanceled;
            leftPrimaryAction.Disable();
            leftPrimaryAction.Dispose();
            leftPrimaryAction = null;
            isLeftPrimaryPressed = false;
            isFollowingLeftHand = false;
        }

        private bool IsLeftHandGestureActive()
        {
            IHandsAggregatorSubsystem hands = XRSubsystemHelpers.HandsAggregator;
            Transform cameraTransform = FindActiveCameraTransform();
            if (hands == null ||
                cameraTransform == null ||
                !hands.TryGetJoint(TrackedHandJoint.Palm, XRNode.LeftHand, out HandJointPose palm))
            {
                return false;
            }

            float distance = Vector3.Distance(cameraTransform.position, palm.Position);
            if (distance < GestureMinDistance || distance > GestureMaxDistance)
            {
                return false;
            }

            Vector3 palmNormalTowardUser = palm.Rotation * -Vector3.up;
            return Vector3.Angle(palmNormalTowardUser, -cameraTransform.forward) <= GesturePalmAngle;
        }

        private void TryConfigureActiveScene()
        {
            if (!IsButtonsCanvasScene(SceneManager.GetActiveScene().name))
            {
                return;
            }

            activeButtonsCanvas = FindButtonsCanvas();
            if (activeButtonsCanvas == null)
            {
                return;
            }

            GameObject singleMenuCanvas = FindSceneObject(SingleMenuCanvasName);
            GameObject backpackUi = FindSceneObject(BackpackUiName);
            ConfigureSceneButtonActions(
                activeButtonsCanvas.gameObject,
                singleMenuCanvas,
                backpackUi);
            ConfigureLinkedSceneUi(singleMenuCanvas, backpackUi);

            ControllerLookup controllerLookup = FindObjectOfType<ControllerLookup>(true);
            if (controllerLookup == null)
            {
                return;
            }

            isConfigured = Configure(
                activeButtonsCanvas.gameObject,
                FindRayInteractor(controllerLookup.LeftHandController),
                null);
            if (isConfigured)
            {
                SetButtonsCanvasVisible(controllerMenuVisible, false);
            }
        }

        public static bool ConfigureSceneButtonActions(
            GameObject buttonsCanvas,
            GameObject singleMenuCanvas,
            GameObject backpackUi)
        {
            if (buttonsCanvas == null || singleMenuCanvas == null || backpackUi == null)
            {
                return false;
            }

            Transform listItemsParent = buttonsCanvas.transform.Find(ListItemsParentPath);
            if (listItemsParent == null)
            {
                return false;
            }

            PressableButton singleMenuButton = FindButton(listItemsParent, SingleMenuButtonName);
            PressableButton backpackButton = FindButton(listItemsParent, BackpackButtonName);
            if (singleMenuButton == null || backpackButton == null)
            {
                return false;
            }

            if (SceneButtonBindingIds.Add(singleMenuButton.GetInstanceID()))
            {
                singleMenuButton.OnClicked.AddListener(() => ShowSingleMenuCanvas(
                    buttonsCanvas,
                    singleMenuCanvas,
                    backpackUi));
            }

            if (SceneButtonBindingIds.Add(backpackButton.GetInstanceID()))
            {
                backpackButton.OnClicked.AddListener(() => ShowBackpackUi(
                    buttonsCanvas,
                    singleMenuCanvas,
                    backpackUi));
            }

            ShowButtonsCanvas(buttonsCanvas, singleMenuCanvas, backpackUi);
            return true;
        }

        public static void ShowButtonsCanvas(
            GameObject buttonsCanvas,
            GameObject singleMenuCanvas,
            GameObject backpackUi)
        {
            SetVisibleUi(
                VisibleUi.Buttons,
                buttonsCanvas,
                singleMenuCanvas,
                backpackUi);
            if (instance != null)
            {
                instance.visibleUi = VisibleUi.Buttons;
                instance.controllerMenuVisible = true;
            }
        }

        public static void ShowSingleMenuCanvas(
            GameObject buttonsCanvas,
            GameObject singleMenuCanvas,
            GameObject backpackUi)
        {
            SetVisibleUi(
                VisibleUi.SingleMenu,
                buttonsCanvas,
                singleMenuCanvas,
                backpackUi);
            if (instance != null)
            {
                instance.visibleUi = VisibleUi.SingleMenu;
                instance.controllerMenuVisible = true;
                instance.ApplyLinkedSceneUiPose();
            }
        }

        public static void ShowBackpackUi(
            GameObject buttonsCanvas,
            GameObject singleMenuCanvas,
            GameObject backpackUi)
        {
            SetVisibleUi(
                VisibleUi.Backpack,
                buttonsCanvas,
                singleMenuCanvas,
                backpackUi);
            if (instance != null)
            {
                instance.visibleUi = VisibleUi.Backpack;
                instance.controllerMenuVisible = true;
                instance.ApplyLinkedSceneUiPose();
            }
        }

        private void SetButtonsCanvasVisible(bool visible, bool followLeftHand)
        {
            if (activeButtonsCanvas == null)
            {
                return;
            }

            SetButtonsCanvasFollowing(followLeftHand);
            SetPanelVisible(activeButtonsCanvas.gameObject, visible);
        }

        private void SetButtonsCanvasFollowing(bool followLeftHand)
        {
            if (activeButtonsCanvas == null)
            {
                return;
            }

            SolverHandler solverHandler = activeButtonsCanvas.GetComponent<SolverHandler>();
            if (solverHandler == null)
            {
                return;
            }

            solverHandler.UpdateSolvers = false;
            solverHandler.AdditionalOffset = followLeftHand
                ? GetControllerMenuOffset()
                : Vector3.zero;
            if (followLeftHand)
            {
                solverHandler.TrackedTargetType = solverHandler.LeftInteractor != null
                    ? TrackedObjectType.Interactor
                    : TrackedObjectType.HandJoint;
                solverHandler.UpdateSolvers = true;
                solverHandler.RefreshTrackedObject();
            }
        }

        private void LateUpdate()
        {
            if (isFollowingLeftHand && activeButtonsCanvas != null && isConfigured)
            {
                ApplyLinkedSceneUiPose();
            }
        }

        private void ConfigureLinkedSceneUi(GameObject singleMenuCanvas, GameObject backpackUi)
        {
            if (hasLinkedUiPoses || activeButtonsCanvas == null)
            {
                return;
            }

            linkedSingleMenuCanvas = singleMenuCanvas;
            linkedBackpackUi = backpackUi;
            if (linkedSingleMenuCanvas == null || linkedBackpackUi == null)
            {
                return;
            }

            // Keep all three Canvas roots coincident in scene space. The
            // linked panels remain scene objects, so desktop testing still
            // works without attaching them to a hand solver.
            singleMenuRelativePosition = Vector3.zero;
            singleMenuRelativeRotation = Quaternion.identity;
            backpackRelativePosition = Vector3.zero;
            backpackRelativeRotation = Quaternion.identity;
            hasLinkedUiPoses = true;
            ApplyLinkedSceneUiPose();
        }

        private void ApplyLinkedSceneUiPose()
        {
            if (!hasLinkedUiPoses || activeButtonsCanvas == null)
            {
                return;
            }

            Transform buttonsTransform = activeButtonsCanvas.transform;
            ApplyLinkedPose(
                buttonsTransform,
                linkedSingleMenuCanvas != null ? linkedSingleMenuCanvas.transform : null,
                singleMenuRelativePosition,
                singleMenuRelativeRotation,
                SingleMenuFollowOffset);
            ApplyLinkedPose(
                buttonsTransform,
                linkedBackpackUi != null ? linkedBackpackUi.transform : null,
                backpackRelativePosition,
                backpackRelativeRotation,
                BackpackFollowOffset);
        }

        private static void ApplyLinkedPose(
            Transform source,
            Transform target,
            Vector3 relativePosition,
            Quaternion relativeRotation,
            Vector3 worldOffset)
        {
            if (source == null || target == null)
            {
                return;
            }

            target.position = source.TransformPoint(relativePosition) +
                source.rotation * worldOffset;
            target.rotation = source.rotation * relativeRotation;
        }

        private static Canvas FindButtonsCanvas()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            Canvas[] canvases = FindObjectsOfType<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas != null &&
                    canvas.name == ButtonsCanvasObjectName &&
                    canvas.gameObject.scene == activeScene)
                {
                    return canvas;
                }
            }

            return null;
        }

        private static PressableButton FindButton(Transform parent, string buttonName)
        {
            Transform buttonTransform = parent.Find(buttonName);
            return buttonTransform != null
                ? buttonTransform.GetComponentInChildren<PressableButton>(true)
                : null;
        }

        private static GameObject FindSceneObject(string objectName)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] objects = FindObjectsOfType<GameObject>(true);
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject candidate = objects[i];
                if (candidate != null &&
                    candidate.name == objectName &&
                    candidate.scene == activeScene)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void SetVisibleUi(
            VisibleUi selectedUi,
            GameObject buttonsCanvas,
            GameObject singleMenuCanvas,
            GameObject backpackUi)
        {
            SetPanelVisible(buttonsCanvas, selectedUi == VisibleUi.Buttons);
            SetPanelVisible(singleMenuCanvas, selectedUi == VisibleUi.SingleMenu);
            SetPanelVisible(backpackUi, selectedUi == VisibleUi.Backpack);
        }

        private static bool IsPanelVisible(GameObject panel)
        {
            if (panel == null || !panel.activeSelf)
            {
                return false;
            }

            Canvas canvas = panel.GetComponent<Canvas>();
            return canvas == null || canvas.enabled;
        }

        private static void SetPanelVisible(GameObject panel, bool visible)
        {
            if (panel == null)
            {
                return;
            }

            Canvas canvas = panel.GetComponent<Canvas>();
            if (canvas == null)
            {
                panel.SetActive(visible);
                return;
            }

            if (!panel.activeSelf)
            {
                panel.SetActive(true);
            }

            canvas.enabled = visible;
            PressableButton[] buttons = panel.GetComponentsInChildren<PressableButton>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].enabled = visible;
            }
        }

        private static XRBaseInteractor FindRayInteractor(XRBaseController controller)
        {
            return controller != null
                ? controller.GetComponentInChildren<MRTKRayInteractor>(true)
                : null;
        }

        private static Transform FindActiveCameraTransform()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null && mainCamera.isActiveAndEnabled)
            {
                return mainCamera.transform;
            }

            Camera[] cameras = FindObjectsOfType<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];
                if (camera != null && camera.isActiveAndEnabled)
                {
                    return camera.transform;
                }
            }

            return null;
        }
    }
}
