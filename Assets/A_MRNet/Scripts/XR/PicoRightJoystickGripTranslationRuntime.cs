using System.Collections.Generic;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace App.Runtime.GripTranslation
{
    /// <summary>
    /// Moves a right-hand grabbed object along its grab direction with the PICO right thumbstick.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-29980)]
    public sealed class PicoRightJoystickGripTranslationRuntime : MonoBehaviour
    {
        private const string RuntimeObjectName = "PICO Right Joystick Grip Translation Runtime";
        private const string RightTranslationBinding = "<XRController>{RightHand}/Primary2DAxis";
        private const float TranslationSpeedMetersPerSecond = 1.5f;
        private const float DeadZone = 0.2f;

        private static PicoRightJoystickGripTranslationRuntime instance;

        private readonly Dictionary<ActionBasedController, InputActionProperty> originalTranslateActions =
            new Dictionary<ActionBasedController, InputActionProperty>();
        private readonly Dictionary<ObjectManipulator, GripOffsetState> gripOffsets =
            new Dictionary<ObjectManipulator, GripOffsetState>();
        private readonly List<ObjectManipulator> staleManipulators = new List<ObjectManipulator>();

        private InputAction rightTranslationAction;
        private ControllerLookup controllerLookup;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureRuntime()
        {
            if (instance != null)
            {
                return;
            }

            GameObject runtimeObject = new GameObject(RuntimeObjectName);
            DontDestroyOnLoad(runtimeObject);
            runtimeObject.AddComponent<PicoRightJoystickGripTranslationRuntime>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            EnsureRightTranslationAction();
        }

        private void Update()
        {
            if (!IsSingleplayerScene(SceneManager.GetActiveScene().name))
            {
                ClearGripOffsets();
                RestoreBuiltInTranslationActions();
                return;
            }

            EnsureRightTranslationAction();
            DisableBuiltInTranslation(GetRightHandController() as ActionBasedController);
        }

        private void LateUpdate()
        {
            if (!IsSingleplayerScene(SceneManager.GetActiveScene().name))
            {
                return;
            }

            float verticalInput = rightTranslationAction.ReadValue<Vector2>().y;
            XRBaseController rightHandController = GetRightHandController();
            if (rightHandController == null)
            {
                ClearGripOffsets();
                return;
            }

            XRBaseInteractor[] interactors = rightHandController.GetComponentsInChildren<XRBaseInteractor>(true);
            for (int i = 0; i < interactors.Length; i++)
            {
                ProcessRightHandSelection(interactors[i], verticalInput);
            }

            RemoveStaleGripOffsets(interactors);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }

            ClearGripOffsets();
            RestoreBuiltInTranslationActions();
            DisposeRightTranslationAction();
        }

        public static Vector3 GetTranslationDelta(
            float verticalInput,
            Vector3 movementForward,
            float speedMetersPerSecond,
            float deltaTime)
        {
            if (Mathf.Approximately(verticalInput, 0f) || movementForward.sqrMagnitude < 0.0001f)
            {
                return Vector3.zero;
            }

            return movementForward.normalized * (verticalInput * speedMetersPerSecond * deltaTime);
        }

        public static bool IsSingleplayerScene(string sceneName) =>
            TexCore_LoadScene.IsBusinessScene(sceneName);

        private void ProcessRightHandSelection(XRBaseInteractor interactor, float verticalInput)
        {
            if (interactor == null || !interactor.isActiveAndEnabled)
            {
                return;
            }

            for (int i = 0; i < interactor.interactablesSelected.Count; i++)
            {
                ObjectManipulator manipulator = FindObjectManipulator(interactor.interactablesSelected[i]);
                if (manipulator == null)
                {
                    continue;
                }

                if (!gripOffsets.TryGetValue(manipulator, out GripOffsetState state) ||
                    state.interactor != interactor)
                {
                    state = new GripOffsetState(interactor);
                    gripOffsets[manipulator] = state;
                }

                if (Mathf.Abs(verticalInput) >= DeadZone)
                {
                    state.offset += GetTranslationDelta(
                        verticalInput,
                        interactor.transform.forward,
                        TranslationSpeedMetersPerSecond,
                        Time.deltaTime);
                }

                manipulator.HostTransform.position += state.offset;
            }
        }

        private XRBaseController GetRightHandController()
        {
            if (controllerLookup == null)
            {
                controllerLookup = FindObjectOfType<ControllerLookup>(true);
            }

            return controllerLookup != null ? controllerLookup.RightHandController : null;
        }

        private static ObjectManipulator FindObjectManipulator(IXRSelectInteractable interactable)
        {
            Component interactableComponent = interactable as Component;
            return interactableComponent != null
                ? interactableComponent.GetComponentInParent<ObjectManipulator>()
                : null;
        }

        private void DisableBuiltInTranslation(ActionBasedController controller)
        {
            if (controller == null || originalTranslateActions.ContainsKey(controller))
            {
                return;
            }

            originalTranslateActions.Add(controller, controller.translateAnchorAction);
            controller.translateAnchorAction = new InputActionProperty();
        }

        private void RestoreBuiltInTranslationActions()
        {
            foreach (KeyValuePair<ActionBasedController, InputActionProperty> entry in originalTranslateActions)
            {
                if (entry.Key != null)
                {
                    entry.Key.translateAnchorAction = entry.Value;
                }
            }

            originalTranslateActions.Clear();
        }

        private void RemoveStaleGripOffsets(XRBaseInteractor[] rightHandInteractors)
        {
            staleManipulators.Clear();
            foreach (KeyValuePair<ObjectManipulator, GripOffsetState> entry in gripOffsets)
            {
                if (entry.Key == null || !IsStillSelected(entry.Key, entry.Value.interactor, rightHandInteractors))
                {
                    staleManipulators.Add(entry.Key);
                }
            }

            for (int i = 0; i < staleManipulators.Count; i++)
            {
                gripOffsets.Remove(staleManipulators[i]);
            }
        }

        private static bool IsStillSelected(
            ObjectManipulator manipulator,
            XRBaseInteractor interactor,
            XRBaseInteractor[] rightHandInteractors)
        {
            if (manipulator == null || interactor == null)
            {
                return false;
            }

            for (int i = 0; i < rightHandInteractors.Length; i++)
            {
                if (rightHandInteractors[i] != interactor)
                {
                    continue;
                }

                for (int j = 0; j < interactor.interactablesSelected.Count; j++)
                {
                    if (FindObjectManipulator(interactor.interactablesSelected[j]) == manipulator)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void ClearGripOffsets()
        {
            gripOffsets.Clear();
            staleManipulators.Clear();
        }

        private void EnsureRightTranslationAction()
        {
            if (rightTranslationAction != null)
            {
                return;
            }

            rightTranslationAction = new InputAction(
                "Move Grabbed Object With Right Joystick",
                InputActionType.Value,
                RightTranslationBinding,
                processors: "StickDeadzone(min=0.2,max=0.95)",
                expectedControlType: "Vector2");
            rightTranslationAction.Enable();
        }

        private void DisposeRightTranslationAction()
        {
            if (rightTranslationAction == null)
            {
                return;
            }

            rightTranslationAction.Disable();
            rightTranslationAction.Dispose();
            rightTranslationAction = null;
        }

        private sealed class GripOffsetState
        {
            public GripOffsetState(XRBaseInteractor interactor)
            {
                this.interactor = interactor;
            }

            public XRBaseInteractor interactor;
            public Vector3 offset;
        }
    }
}
