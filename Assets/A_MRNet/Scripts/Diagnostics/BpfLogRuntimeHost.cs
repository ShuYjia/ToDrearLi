using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bpf.Diagnostics
{
    /// <summary>
    /// Persistent runtime object that owns the BPF diagnostics session: it
    /// drains the log queue, tracks scene changes and flushes on pause, focus
    /// loss and quit. It is created before the first scene of every play
    /// session.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BpfLogRuntimeHost : MonoBehaviour
    {
        private const string RuntimeObjectName = "Bpf Diagnostics Runtime";

        private static BpfLogRuntimeHost instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            BpfLog.ResetStatics();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureRuntime()
        {
            BpfLog.EnsureInitialized();
            if (instance != null)
            {
                return;
            }

            GameObject runtimeObject = new GameObject(RuntimeObjectName);
            DontDestroyOnLoad(runtimeObject);
            runtimeObject.AddComponent<BpfLogRuntimeHost>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            runtimeObject.AddComponent<BpfDiagnosticsOverlay>();
#endif
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update()
        {
            BpfLog.Tick(Time.unscaledDeltaTime);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            BpfLog.Info(
                BpfLogCategory.Diagnostics,
                pauseStatus ? "app.paused" : "app.resumed",
                pauseStatus ? "application paused" : "application resumed");
            BpfLog.Flush();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                BpfLog.Flush();
            }
        }

        private void OnApplicationQuit()
        {
            BpfLog.Info(BpfLogCategory.Diagnostics, "app.quit", "application quitting");
            BpfLog.Flush();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BpfLog.NotifySceneLoaded(scene, mode);
        }
    }
}
