using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MixedReality.Toolkit.Input;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR;

namespace App.Runtime.HandCalibration
{
    public static class HandAnchorCalibrationSession
    {
        public static bool IsCalibrated { get; private set; }
        public static Vector3 LeftLocalPositionOffset { get; private set; }
        public static Quaternion LeftLocalRotationOffset { get; private set; } = Quaternion.identity;
        public static Vector3 RightLocalPositionOffset { get; private set; }
        public static Quaternion RightLocalRotationOffset { get; private set; } = Quaternion.identity;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            Clear();
        }

        public static void Store(
            Vector3 leftLocalPositionOffset,
            Quaternion leftLocalRotationOffset,
            Vector3 rightLocalPositionOffset,
            Quaternion rightLocalRotationOffset)
        {
            LeftLocalPositionOffset = leftLocalPositionOffset;
            LeftLocalRotationOffset = leftLocalRotationOffset;
            RightLocalPositionOffset = rightLocalPositionOffset;
            RightLocalRotationOffset = rightLocalRotationOffset;
            IsCalibrated = true;
        }

        public static void Clear()
        {
            IsCalibrated = false;
            LeftLocalPositionOffset = Vector3.zero;
            LeftLocalRotationOffset = Quaternion.identity;
            RightLocalPositionOffset = Vector3.zero;
            RightLocalRotationOffset = Quaternion.identity;
        }
    }

    internal static class HandAnchorCalibrationUtility
    {
        internal const string OffsetNodeName = "HandCalibrationOffset";
        private const string LeftAnchorName = "HandAnchor L";
        private const string RightAnchorName = "HandAnchor R";

        internal static Transform FindAnchor(bool isLeft, Scene scene)
        {
            string targetName = isLeft ? LeftAnchorName : RightAnchorName;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == targetName)
                {
                    return rootObject.transform;
                }

                Transform nested = FindChildRecursive(rootObject.transform, targetName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        internal static ArticulatedHandController FindController(XRNode handNode, Scene scene)
        {
            return Object.FindObjectsOfType<ArticulatedHandController>(true)
                .FirstOrDefault(controller =>
                    controller != null &&
                    controller.HandNode == handNode &&
                    controller.gameObject.scene == scene);
        }

        internal static Transform EnsureOffsetRoot(Transform controllerRoot)
        {
            if (controllerRoot == null)
            {
                return null;
            }

            Transform offsetRoot = controllerRoot.Find(OffsetNodeName);
            if (offsetRoot == null)
            {
                offsetRoot = new GameObject(OffsetNodeName).transform;
                offsetRoot.SetParent(controllerRoot, false);
                offsetRoot.localPosition = Vector3.zero;
                offsetRoot.localRotation = Quaternion.identity;
                offsetRoot.localScale = Vector3.one;
            }

            List<Transform> childrenToMove = new List<Transform>();
            for (int i = 0; i < controllerRoot.childCount; i++)
            {
                Transform child = controllerRoot.GetChild(i);
                if (child != offsetRoot)
                {
                    childrenToMove.Add(child);
                }
            }

            foreach (Transform child in childrenToMove)
            {
                child.SetParent(offsetRoot, false);
            }

            XRBaseController xrController = controllerRoot.GetComponent<XRBaseController>();
            if (xrController != null && xrController.modelParent != offsetRoot)
            {
                xrController.modelParent = offsetRoot;
            }

            offsetRoot.SetSiblingIndex(0);
            return offsetRoot;
        }

        internal static bool HasValidTracking(ArticulatedHandController controller)
        {
            if (controller == null)
            {
                return false;
            }

            var controllerState = controller.currentControllerState;
            InputTrackingState trackingState = controllerState.inputTrackingState;
            bool hasPose = (trackingState & (InputTrackingState.Position | InputTrackingState.Rotation)) ==
                           (InputTrackingState.Position | InputTrackingState.Rotation);
            return controllerState.isTracked && hasPose;
        }

        private static Transform FindChildRecursive(Transform root, string targetName)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == targetName)
                {
                    return child;
                }

                Transform nested = FindChildRecursive(child, targetName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}
