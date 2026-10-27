using System.Collections;
using MixedReality.Toolkit.Input;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using Bpf.Diagnostics;

namespace App.Runtime.HandCalibration
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-860)]
    public sealed class HandAnchorCalibrationApplier : MonoBehaviour
    {
        [SerializeField] private float resolveTimeoutSeconds = 5f;
        [SerializeField] private float retryIntervalSeconds = 0.2f;
        [SerializeField] private bool debugLog;

        private Coroutine applyRoutine;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            StartApplyRoutine();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            StopApplyRoutine();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartApplyRoutine();
        }

        private void StartApplyRoutine()
        {
            if (!HandAnchorCalibrationSession.IsCalibrated)
            {
                return;
            }

            StopApplyRoutine();
            applyRoutine = StartCoroutine(ApplyWhenReady());
        }

        private void StopApplyRoutine()
        {
            if (applyRoutine != null)
            {
                StopCoroutine(applyRoutine);
                applyRoutine = null;
            }
        }

        private IEnumerator ApplyWhenReady()
        {
            yield return null;
            yield return new WaitForEndOfFrame();

            float deadline = Time.unscaledTime + resolveTimeoutSeconds;
            while (Time.unscaledTime < deadline)
            {
                if (TryApplyToActiveScene())
                {
                    applyRoutine = null;
                    yield break;
                }

                yield return new WaitForSecondsRealtime(retryIntervalSeconds);
            }

            Log("Timed out while waiting for left/right hand controllers in active scene.");
            applyRoutine = null;
        }

        private bool TryApplyToActiveScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            ArticulatedHandController leftController = HandAnchorCalibrationUtility.FindController(XRNode.LeftHand, activeScene);
            ArticulatedHandController rightController = HandAnchorCalibrationUtility.FindController(XRNode.RightHand, activeScene);
            if (leftController == null || rightController == null)
            {
                return false;
            }

            ApplyOffset(leftController.transform, HandAnchorCalibrationSession.LeftLocalPositionOffset, HandAnchorCalibrationSession.LeftLocalRotationOffset);
            ApplyOffset(rightController.transform, HandAnchorCalibrationSession.RightLocalPositionOffset, HandAnchorCalibrationSession.RightLocalRotationOffset);
            Log("Applied stored hand calibration offsets to active scene.");
            return true;
        }

        private void ApplyOffset(Transform controllerRoot, Vector3 localPosition, Quaternion localRotation)
        {
            Transform offsetRoot = HandAnchorCalibrationUtility.EnsureOffsetRoot(controllerRoot);
            if (offsetRoot == null)
            {
                return;
            }

            offsetRoot.localPosition = localPosition;
            offsetRoot.localRotation = localRotation;
            offsetRoot.localScale = Vector3.one;
        }

        private void Log(string message)
        {
            if (debugLog)
            {
                BpfLog.Info(BpfLogCategory.XR, "hand_anchor_calibration_applier.log", "[HandAnchorCalibrationApplier] " + message, this);
            }
        }
    }
}
