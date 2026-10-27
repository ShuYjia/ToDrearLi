using MixedReality.Toolkit.Input;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using Bpf.Diagnostics;

namespace App.Runtime.HandCalibration
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-850)]
    public sealed class HandAnchorCalibrationController : MonoBehaviour
    {
        [SerializeField] private Transform leftAnchor;
        [SerializeField] private Transform rightAnchor;
        [SerializeField] private Transform leftControllerRoot;
        [SerializeField] private Transform rightControllerRoot;
        [SerializeField] private float positionTolerance = 0.03f;
        [SerializeField] private float rotationToleranceDegrees = 8f;
        [SerializeField] private float holdSeconds = 0.35f;
        [SerializeField] private bool autoHideAnchorsOnComplete = true;
        [SerializeField] private bool debugLog;

        private ArticulatedHandController leftController;
        private ArticulatedHandController rightController;
        private Transform leftOffsetRoot;
        private Transform rightOffsetRoot;
        private float leftAlignedTime;
        private float rightAlignedTime;
        private bool leftLocked;
        private bool rightLocked;
        private Vector3 leftLockedLocalPosition;
        private Quaternion leftLockedLocalRotation = Quaternion.identity;
        private Vector3 rightLockedLocalPosition;
        private Quaternion rightLockedLocalRotation = Quaternion.identity;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            ResetSceneBindings();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update()
        {
            if (HandAnchorCalibrationSession.IsCalibrated || !IsStartupScene())
            {
                return;
            }

            if (!TryResolveSceneBindings())
            {
                return;
            }

            TryLockHand(
                leftController,
                leftAnchor,
                leftOffsetRoot,
                ref leftAlignedTime,
                ref leftLocked,
                ref leftLockedLocalPosition,
                ref leftLockedLocalRotation,
                "Left");

            TryLockHand(
                rightController,
                rightAnchor,
                rightOffsetRoot,
                ref rightAlignedTime,
                ref rightLocked,
                ref rightLockedLocalPosition,
                ref rightLockedLocalRotation,
                "Right");

            if (leftLocked && rightLocked)
            {
                HandAnchorCalibrationSession.Store(
                    leftLockedLocalPosition,
                    leftLockedLocalRotation,
                    rightLockedLocalPosition,
                    rightLockedLocalRotation);

                if (autoHideAnchorsOnComplete)
                {
                    SetAnchorVisibility(false);
                }

                Log("Calibration completed and stored for this runtime session.");
                enabled = false;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ResetSceneBindings();
            if (HandAnchorCalibrationSession.IsCalibrated || !IsStartupScene(scene))
            {
                return;
            }

            SetAnchorVisibility(true);
        }

        private void ResetSceneBindings()
        {
            leftController = null;
            rightController = null;
            leftOffsetRoot = null;
            rightOffsetRoot = null;
            leftAlignedTime = 0f;
            rightAlignedTime = 0f;
            leftLocked = false;
            rightLocked = false;
            leftLockedLocalPosition = Vector3.zero;
            leftLockedLocalRotation = Quaternion.identity;
            rightLockedLocalPosition = Vector3.zero;
            rightLockedLocalRotation = Quaternion.identity;
        }

        private bool TryResolveSceneBindings()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (leftAnchor == null || leftAnchor.gameObject.scene != activeScene)
            {
                leftAnchor = HandAnchorCalibrationUtility.FindAnchor(isLeft: true, scene: activeScene);
            }

            if (rightAnchor == null || rightAnchor.gameObject.scene != activeScene)
            {
                rightAnchor = HandAnchorCalibrationUtility.FindAnchor(isLeft: false, scene: activeScene);
            }

            if (leftController == null || leftController.gameObject.scene != activeScene)
            {
                leftController = HandAnchorCalibrationUtility.FindController(XRNode.LeftHand, activeScene);
            }

            if (rightController == null || rightController.gameObject.scene != activeScene)
            {
                rightController = HandAnchorCalibrationUtility.FindController(XRNode.RightHand, activeScene);
            }

            if (leftControllerRoot == null || leftControllerRoot.gameObject.scene != activeScene)
            {
                leftControllerRoot = leftController != null ? leftController.transform : null;
            }

            if (rightControllerRoot == null || rightControllerRoot.gameObject.scene != activeScene)
            {
                rightControllerRoot = rightController != null ? rightController.transform : null;
            }

            if (leftControllerRoot != null)
            {
                leftOffsetRoot = HandAnchorCalibrationUtility.EnsureOffsetRoot(leftControllerRoot);
            }

            if (rightControllerRoot != null)
            {
                rightOffsetRoot = HandAnchorCalibrationUtility.EnsureOffsetRoot(rightControllerRoot);
            }

            return leftAnchor != null &&
                   rightAnchor != null &&
                   leftController != null &&
                   rightController != null &&
                   leftOffsetRoot != null &&
                   rightOffsetRoot != null;
        }

        private void TryLockHand(
            ArticulatedHandController controller,
            Transform anchor,
            Transform offsetRoot,
            ref float alignedTime,
            ref bool locked,
            ref Vector3 lockedLocalPosition,
            ref Quaternion lockedLocalRotation,
            string handLabel)
        {
            if (locked || controller == null || anchor == null || offsetRoot == null)
            {
                return;
            }

            if (!HandAnchorCalibrationUtility.HasValidTracking(controller))
            {
                alignedTime = 0f;
                return;
            }

            float positionError = Vector3.Distance(offsetRoot.position, anchor.position);
            float rotationError = Quaternion.Angle(offsetRoot.rotation, anchor.rotation);
            if (positionError > positionTolerance || rotationError > rotationToleranceDegrees)
            {
                alignedTime = 0f;
                return;
            }

            alignedTime += Time.unscaledDeltaTime;
            if (alignedTime < holdSeconds)
            {
                return;
            }

            Transform controllerRoot = controller.transform;
            lockedLocalPosition = controllerRoot.InverseTransformPoint(anchor.position);
            lockedLocalRotation = Quaternion.Inverse(controllerRoot.rotation) * anchor.rotation;

            offsetRoot.localPosition = lockedLocalPosition;
            offsetRoot.localRotation = lockedLocalRotation;
            offsetRoot.localScale = Vector3.one;

            locked = true;
            alignedTime = 0f;
            Log(handLabel + " hand locked to anchor.");
        }

        private void SetAnchorVisibility(bool isVisible)
        {
            if (leftAnchor != null)
            {
                leftAnchor.gameObject.SetActive(isVisible);
            }

            if (rightAnchor != null)
            {
                rightAnchor.gameObject.SetActive(isVisible);
            }
        }

        private bool IsStartupScene()
        {
            return IsStartupScene(SceneManager.GetActiveScene());
        }

        private bool IsStartupScene(Scene scene)
        {
            return scene.IsValid() && scene.name == TexCore_LoadScene.StartupSceneName;
        }

        private void Log(string message)
        {
            if (debugLog)
            {
                BpfLog.Info(BpfLogCategory.XR, "hand_anchor_calibration_controller.log", "[HandAnchorCalibrationController] " + message, this);
            }
        }
    }
}
