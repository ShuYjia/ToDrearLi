using UnityEngine;

#if UNITY_EDITOR
using UnityEngine.XR.Management;
#endif

/// <summary>
/// Keeps MRTK mouse navigation and rays aligned with the Game view in the
/// editor. The PICO/Android runtime path is intentionally left unchanged.
/// </summary>
[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
public sealed class EditorMouseCoordinateAdapter : MonoBehaviour
{
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        DisableEditorXR();

        GameObject adapterObject = new GameObject(nameof(EditorMouseCoordinateAdapter));
        adapterObject.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(adapterObject);
        adapterObject.AddComponent<EditorMouseCoordinateAdapter>();
    }

    private static void DisableEditorXR()
    {
        XRManagerSettings xrManager = XRGeneralSettings.Instance?.Manager;
        if (xrManager == null || xrManager.activeLoader == null)
        {
            return;
        }

        // MRTK checks whether an XR display exists, not whether it is running.
        // Deinitialize the loader so editor simulation and mouse raycasters stay enabled.
        xrManager.StopSubsystems();
        xrManager.DeinitializeLoader();
    }

    private Camera targetCamera;

    private void Update()
    {
        ApplyCameraSettings();
    }

    private void LateUpdate()
    {
        ApplyCameraSettings();
    }

    private void ApplyCameraSettings()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            targetCamera = null;
            return;
        }

        if (targetCamera != mainCamera)
        {
            targetCamera = mainCamera;
        }

        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }

        targetCamera.stereoTargetEye = StereoTargetEyeMask.None;
        targetCamera.rect = new Rect(0f, 0f, 1f, 1f);
        targetCamera.pixelRect = new Rect(0f, 0f, Screen.width, Screen.height);
    }
#endif
}
