using System.Collections;
using MixedReality.Toolkit;
using MixedReality.Toolkit.Input;
using MixedReality.Toolkit.SpatialManipulation;
using MixedReality.Toolkit.Subsystems;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace App.Runtime.HandMenu
{
    /// <summary>
    /// Leaves Dash Holster at its scene-authored pose by default. A short left
    /// X press toggles its visibility, while a hold temporarily follows left hand.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-840)]
    public sealed class DashHolsterHandMenuRuntime : MonoBehaviour
    {
        private const string RuntimeObjectName = "Dash Holster Hand Menu Runtime";
        private const string DashHolsterObjectName = "Dash Holster";
        private const string StartupSceneName = "01StartUp";
        private const string LeftPrimaryBinding = "<XRController>{LeftHand}/primaryButton";
        private const float LongPressThreshold = 0.35f;
        private const float GestureMaxDistance = 0.8f;
        private const float GestureMinDistance = 0.12f;
        private const float GesturePalmAngle = 35f;
        private const float ControllerVerticalOffset = 0.18f;

        private static DashHolsterHandMenuRuntime instance;
        private InputAction leftPrimaryAction;
        private GameObject activeHolster;
        private bool controllerMenuVisible = true;
        private bool isConfigured;
        private bool isLeftPrimaryPressed;
        private bool isFollowingLeftHand;
        private float leftPrimaryPressedAt;
        private bool hasScenePose;
        private Vector3 sceneLocalPosition;
        private Quaternion sceneLocalRotation;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureRuntime()
        {
            if (instance != null) return;
            GameObject runtimeObject = new GameObject(RuntimeObjectName);
            DontDestroyOnLoad(runtimeObject);
            runtimeObject.AddComponent<DashHolsterHandMenuRuntime>();
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
            if (!IsDashHolsterScene(SceneManager.GetActiveScene().name)) return;
            if (!isConfigured) TryConfigureActiveScene();
            if (activeHolster == null || !isConfigured) return;

            bool shouldFollowLeftHand = ShouldFollowLeftHand(
                isLeftPrimaryPressed,
                Time.unscaledTime - leftPrimaryPressedAt);
            bool shouldBeVisible = controllerMenuVisible || shouldFollowLeftHand;
            if (isFollowingLeftHand != shouldFollowLeftHand ||
                activeHolster.activeSelf != shouldBeVisible)
            {
                isFollowingLeftHand = shouldFollowLeftHand;
                SetHolsterVisible(shouldBeVisible, shouldFollowLeftHand);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            activeHolster = null;
            isConfigured = false;
            controllerMenuVisible = true;
            isLeftPrimaryPressed = false;
            isFollowingLeftHand = false;
            hasScenePose = false;
            if (!IsDashHolsterScene(scene.name))
            {
                DisposeLeftPrimaryAction();
                return;
            }

            StartCoroutine(ConfigureActiveSceneAfterFrame());
        }

        private IEnumerator ConfigureActiveSceneAfterFrame()
        {
            yield return null;
            if (!IsDashHolsterScene(SceneManager.GetActiveScene().name)) yield break;
            EnsureLeftPrimaryAction();
            TryConfigureActiveScene();
        }

        public static bool Configure(
            GameObject dashHolster,
            XRBaseInteractor leftInteractor,
            XRBaseInteractor rightInteractor)
        {
            // A right-only controller must never be allowed to activate it.
            if (dashHolster == null || (leftInteractor == null && rightInteractor != null)) return false;

            Follow follow = dashHolster.GetComponent<Follow>();
            if (follow != null) follow.enabled = false;

            SolverHandler solverHandler = dashHolster.GetComponent<SolverHandler>();
            if (solverHandler == null) solverHandler = dashHolster.AddComponent<SolverHandler>();
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

            HandConstraint handConstraint = dashHolster.GetComponent<HandConstraint>();
            if (handConstraint == null) handConstraint = dashHolster.AddComponent<HandConstraint>();
            handConstraint.SafeZone = HandConstraint.SolverSafeZone.UlnarSide;
            handConstraint.SafeZoneBuffer = 0.16f;
            handConstraint.RotationBehavior = HandConstraint.SolverRotationBehavior.LookAtMainCamera;
            handConstraint.OffsetBehavior = HandConstraint.SolverOffsetBehavior.LookAtCameraRotation;
            handConstraint.HideHandCursorsOnActivate = false;

            solverHandler.RefreshTrackedObject();
            ConfigureReticle(leftInteractor);
            return true;
        }

        public static int ConfigureReticle(XRBaseInteractor interactor)
        {
            if (interactor == null) return 0;
            XRBaseController controller = interactor.GetComponentInParent<XRBaseController>();
            Transform searchRoot = controller != null ? controller.transform : interactor.transform;
            MRTKRayReticleVisual[] reticles = searchRoot.GetComponentsInChildren<MRTKRayReticleVisual>(true);
            for (int i = 0; i < reticles.Length; i++)
            {
                reticles[i].VisibilitySettings = MRTKRayReticleVisual.ReticleVisibilitySettings.AllValidSurfaces;
                reticles[i].enabled = true;
            }
            return reticles.Length;
        }

        public static bool IsGestureActive(HandJointPose palm, Transform cameraTransform)
        {
            if (cameraTransform == null) return false;
            float distance = Vector3.Distance(cameraTransform.position, palm.Position);
            if (distance < GestureMinDistance || distance > GestureMaxDistance) return false;
            Vector3 palmNormalTowardUser = palm.Rotation * -Vector3.up;
            return Vector3.Angle(palmNormalTowardUser, -cameraTransform.forward) <= GesturePalmAngle;
        }

        public static bool ToggleControllerVisibility(bool isVisible) => !isVisible;

        public static bool IsLongPress(float heldSeconds) =>
            heldSeconds >= LongPressThreshold;

        public static bool ShouldFollowLeftHand(bool isPrimaryPressed, float heldSeconds) =>
            isPrimaryPressed && IsLongPress(heldSeconds);

        public static bool IsDashHolsterScene(string sceneName) =>
            sceneName == StartupSceneName;

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
                controllerMenuVisible = ToggleControllerVisibility(controllerMenuVisible);
            }
        }

        private void EnsureLeftPrimaryAction()
        {
            if (leftPrimaryAction != null) return;
            leftPrimaryAction = new InputAction("Show Dash Holster", InputActionType.Button, LeftPrimaryBinding);
            leftPrimaryAction.started += OnLeftPrimaryStarted;
            leftPrimaryAction.canceled += OnLeftPrimaryCanceled;
            leftPrimaryAction.Enable();
        }

        private void DisposeLeftPrimaryAction()
        {
            if (leftPrimaryAction == null) return;
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
            return hands != null && cameraTransform != null &&
                hands.TryGetJoint(TrackedHandJoint.Palm, XRNode.LeftHand, out HandJointPose palm) &&
                IsGestureActive(palm, cameraTransform);
        }

        private void TryConfigureActiveScene()
        {
            if (!IsDashHolsterScene(SceneManager.GetActiveScene().name)) return;
            activeHolster = FindDashHolster();
            if (activeHolster == null) return;
            ControllerLookup controllerLookup = FindObjectOfType<ControllerLookup>(true);
            if (controllerLookup == null) return;
            isConfigured = Configure(activeHolster, FindRayInteractor(controllerLookup.LeftHandController), null);
            if (isConfigured)
            {
                CaptureScenePose();
                SetHolsterVisible(controllerMenuVisible, false);
            }
        }

        private void SetHolsterVisible(bool visible, bool followLeftHand)
        {
            if (activeHolster == null) return;
            SolverHandler solverHandler = activeHolster.GetComponent<SolverHandler>();
            if (solverHandler != null)
            {
                solverHandler.UpdateSolvers = false;
                solverHandler.AdditionalOffset = followLeftHand
                    ? GetControllerMenuOffset()
                    : Vector3.zero;
                if (followLeftHand)
                {
                    solverHandler.TrackedTargetType = solverHandler.LeftInteractor != null
                        ? TrackedObjectType.Interactor
                        : TrackedObjectType.HandJoint;
                }
            }

            if (!visible)
            {
                activeHolster.SetActive(false);
                return;
            }

            if (!activeHolster.activeSelf) activeHolster.SetActive(true);
            if (followLeftHand && solverHandler != null)
            {
                solverHandler.UpdateSolvers = true;
                solverHandler.RefreshTrackedObject();
            }
            else
            {
                RestoreScenePose();
            }
        }

        private void CaptureScenePose()
        {
            Transform holsterTransform = activeHolster.transform;
            sceneLocalPosition = holsterTransform.localPosition;
            sceneLocalRotation = holsterTransform.localRotation;
            hasScenePose = true;
        }

        private void RestoreScenePose()
        {
            if (!hasScenePose || activeHolster == null) return;
            Transform holsterTransform = activeHolster.transform;
            holsterTransform.localPosition = sceneLocalPosition;
            holsterTransform.localRotation = sceneLocalRotation;
        }

        private static GameObject FindDashHolster()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            Transform[] transforms = FindObjectsOfType<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate != null && candidate.name == DashHolsterObjectName && candidate.gameObject.scene == activeScene)
                {
                    return candidate.gameObject;
                }
            }
            return null;
        }

        private static XRBaseInteractor FindRayInteractor(XRBaseController controller)
        {
            return controller != null ? controller.GetComponentInChildren<MRTKRayInteractor>(true) : null;
        }

        private static Transform FindActiveCameraTransform()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null && mainCamera.isActiveAndEnabled) return mainCamera.transform;
            Camera[] cameras = FindObjectsOfType<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null && cameras[i].isActiveAndEnabled) return cameras[i].transform;
            }
            return null;
        }
    }
}
