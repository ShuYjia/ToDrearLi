using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace App.Runtime.Locomotion
{
    /// <summary>
    /// Enables head-relative, planar locomotion in the main single-player
    /// scene with the PICO left controller thumbstick.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-800)]
    public sealed class PicoLeftJoystickLocomotionRuntime : MonoBehaviour
    {
        private const string RuntimeObjectName = "PICO Left Joystick Locomotion Runtime";
        private const string LeftMoveBinding = "<XRController>{LeftHand}/Primary2DAxis";
        private const float MoveSpeedMetersPerSecond = 1.5f;
        private const float DeadZone = 0.2f;

        private static PicoLeftJoystickLocomotionRuntime instance;

        private InputAction leftMoveAction;
        private XROrigin activeOrigin;

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
            runtimeObject.AddComponent<PicoLeftJoystickLocomotionRuntime>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            EnsureLeftMoveAction();
        }

        private void Update()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!IsSingleplayerScene(activeScene.name))
            {
                return;
            }

            if (activeOrigin == null || activeOrigin.gameObject.scene != activeScene ||
                !activeOrigin.gameObject.activeInHierarchy)
            {
                activeOrigin = FindActiveOrigin(activeScene);
                return;
            }

            EnsureLeftMoveAction();

            Vector2 moveInput = leftMoveAction.ReadValue<Vector2>();
            if (moveInput.sqrMagnitude < DeadZone * DeadZone)
            {
                return;
            }

            Camera originCamera = activeOrigin.Camera != null
                ? activeOrigin.Camera
                : activeOrigin.GetComponentInChildren<Camera>(true);
            if (originCamera == null)
            {
                return;
            }

            Vector3 moveDirection = GetPlanarMoveDirection(moveInput, originCamera.transform.forward);
            activeOrigin.transform.position += moveDirection * MoveSpeedMetersPerSecond * Time.deltaTime;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }

            DisposeLeftMoveAction();
        }

        public static Vector3 GetPlanarMoveDirection(Vector2 moveInput, Vector3 headForward)
        {
            Vector3 forward = Vector3.ProjectOnPlane(headForward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
            {
                return Vector3.zero;
            }

            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 moveDirection = right * moveInput.x + forward * moveInput.y;
            return moveDirection.sqrMagnitude > 1f ? moveDirection.normalized : moveDirection;
        }

        private static XROrigin FindActiveOrigin(Scene activeScene)
        {
            XROrigin[] origins = FindObjectsOfType<XROrigin>(true);
            for (int i = 0; i < origins.Length; i++)
            {
                XROrigin origin = origins[i];
                if (origin != null && origin.gameObject.scene == activeScene && origin.gameObject.activeInHierarchy)
                {
                    return origin;
                }
            }

            return null;
        }

        private void EnsureLeftMoveAction()
        {
            if (leftMoveAction != null)
            {
                return;
            }

            leftMoveAction = new InputAction(
                "Move XR Rig With Left Joystick",
                InputActionType.Value,
                LeftMoveBinding,
                processors: "StickDeadzone(min=0.2,max=0.95)",
                expectedControlType: "Vector2");
            leftMoveAction.Enable();
        }

        private void DisposeLeftMoveAction()
        {
            if (leftMoveAction == null)
            {
                return;
            }

            leftMoveAction.Disable();
            leftMoveAction.Dispose();
            leftMoveAction = null;
        }

        public static bool IsSingleplayerScene(string sceneName) =>
            TexCore_LoadScene.IsBusinessScene(sceneName);
    }
}
