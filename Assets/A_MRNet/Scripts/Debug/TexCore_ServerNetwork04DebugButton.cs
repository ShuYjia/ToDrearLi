using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Bpf.Diagnostics;

/// <summary>
/// Development-only shortcuts for verifying the server power-on topic and the
/// first system-installation phase in 04SingleplayerScene.
/// </summary>
[DisallowMultipleComponent]
public sealed class TexCore_ServerNetwork04DebugButton : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private const string SceneName = "04SingleplayerScene";
    private const string ServerNetworkName = "01服务器网络搭建";
    private const string PowerOnTopicName = "05开启服务器";
    private const string PowerButtonName = "KaiGuanBtn";
    private const string ServerInstallName = "02服务器系统安装";
    private const string InstallPhaseTopicName = "04系统镜像安装_第一阶段";
    private const string ServerInstallScreenName = "BPF_ServerInstallScreen";
    private const string ServerInstallCanvasName = "ServerInstallCanvas";
    private const string SoftwareSelectionPageName = "Page_08_SoftwareSelection";
    private const string SelectGnomeButtonName = "SYS_07_SelectGNOME";
    private const string InstallationSummaryPageName = "Page_11_InstallationSummary_AfterGNOME";
    private const string ReviewInstallationSummaryButtonName = "SYS_08_ReviewInstallationSummary";
    private const string StartInstallationButtonName = "SYS_09_StartInstallation";
    private const int SelectGnomeStepIndex = 6;
    private const int StartInstallationStepIndex = 8;

    private static bool sceneCallbackRegistered;

    private string buttonText = "DEBUG: Complete 05 Power On";
    private string installPhaseButtonText = "DEBUG: Test 04 Install / GNOME";
    private string startInstallationButtonText = "DEBUG: Test 04 Start Installation";
    private bool isCompleting;
    private bool isCompleted;
    private bool isPreparingInstallPhase;
    private bool isInstallPhasePrepared;
    private bool isPreparingStartInstallation;
    private bool isStartInstallationPrepared;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        if (sceneCallbackRegistered)
        {
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        sceneCallbackRegistered = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AddToInitialScene()
    {
        CreateForScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CreateForScene(scene);
    }

    private static void CreateForScene(Scene scene)
    {
        if (!scene.IsValid() || scene.name != SceneName)
        {
            return;
        }

        TexCore_ServerNetwork04DebugButton[] existingButtons =
            Resources.FindObjectsOfTypeAll<TexCore_ServerNetwork04DebugButton>();
        foreach (TexCore_ServerNetwork04DebugButton button in existingButtons)
        {
            if (button.gameObject.scene.handle == scene.handle)
            {
                return;
            }
        }

        GameObject debugRoot = new GameObject("Debug_Complete05ServerPowerOn");
        SceneManager.MoveGameObjectToScene(debugRoot, scene);
        debugRoot.AddComponent<TexCore_ServerNetwork04DebugButton>();
    }

    private void OnGUI()
    {
        if (SceneManager.GetActiveScene().name != SceneName)
        {
            return;
        }

        GUI.enabled = !isCompleting && !isCompleted;
        if (GUI.Button(new Rect(Screen.width - 364f, 24f, 340f, 56f), buttonText))
        {
            StartCoroutine(CompletePowerOn());
        }

        GUI.enabled = !isPreparingInstallPhase && !isInstallPhasePrepared;
        if (GUI.Button(new Rect(Screen.width - 364f, 88f, 340f, 56f), installPhaseButtonText))
        {
            StartCoroutine(PrepareInstallPhaseAtGnome());
        }

        GUI.enabled = !isPreparingStartInstallation && !isStartInstallationPrepared;
        if (GUI.Button(new Rect(Screen.width - 364f, 152f, 340f, 56f), startInstallationButtonText))
        {
            StartCoroutine(PrepareInstallPhaseAtStartInstallation());
        }

        GUI.enabled = true;
    }

    private IEnumerator CompletePowerOn()
    {
        isCompleting = true;
        buttonText = "DEBUG: Completing 05...";

        yield return new WaitUntil(FrameworkReady);

        TexCore_DataClass root = TexCore_ConfigManager.self.datas.datas.FindRoot(TexCore_HeadMenu.self.node);
        TexCore_DataClass serverNetwork = root != null ? root.Find(ServerNetworkName) : null;
        if (serverNetwork == null)
        {
            Fail("01服务器网络搭建 was not found in Config.xml.");
            yield break;
        }

        TexCore_PlaneManager planeManager = TexCore_PlaneManager.self;
        if (planeManager.ShowPlane(serverNetwork.name, serverNetwork.GetUrl(), true) == null)
        {
            Fail("The server-network panel could not be loaded.");
            yield break;
        }

        yield return null;
        NodePanelBase nodePanel = planeManager.activePlane as NodePanelBase;
        if (nodePanel == null)
        {
            Fail("The server-network panel did not become active.");
            yield break;
        }

        yield return new WaitUntil(() => nodePanel.activeChild != null);
        nodePanel.LoadChild(PowerOnTopicName);

        yield return null;
        Panel_Operate powerOnTopic = nodePanel.activeChild as Panel_Operate;
        if (powerOnTopic == null)
        {
            Fail("05开启服务器 could not be loaded as a model-operation topic.");
            yield break;
        }

        yield return new WaitUntil(() =>
            powerOnTopic.topicData != null && powerOnTopic.gameObject.activeInHierarchy);

        GameObject powerButton = GameObject.Find(PowerButtonName);
        if (powerButton == null)
        {
            Fail("The active KaiGuanBtn step object could not be found.");
            yield break;
        }

        TexCore_BusinessManager.OnClick(powerButton);

        yield return new WaitUntil(() =>
            powerOnTopic.isSubmit && powerOnTopic.btn_Next != null && powerOnTopic.btn_Next.activeSelf);

        isCompleting = false;
        isCompleted = true;
        buttonText = "DEBUG: 05 Done - Next Shown";
        BpfLog.Info(BpfLogCategory.Diagnostics, "tex_core_server_network04_debug_button.complete_power_on", "Debug shortcut completed 05开启服务器 and displayed 下一步.", this);
    }

    private IEnumerator PrepareInstallPhaseAtGnome()
    {
        isPreparingInstallPhase = true;
        installPhaseButtonText = "DEBUG: Preparing 04 Install...";

        yield return new WaitUntil(FrameworkReady);

        TexCore_DataClass root = TexCore_ConfigManager.self.datas.datas.FindRoot(TexCore_HeadMenu.self.node);
        TexCore_DataClass serverInstall = root != null ? root.Find(ServerInstallName) : null;
        if (serverInstall == null)
        {
            FailInstallPhase("02服务器系统安装 was not found in Config.xml.");
            yield break;
        }

        TexCore_PlaneManager planeManager = TexCore_PlaneManager.self;
        if (planeManager.ShowPlane(serverInstall.name, serverInstall.GetUrl(), true) == null)
        {
            FailInstallPhase("The server-install panel could not be loaded.");
            yield break;
        }

        yield return null;
        NodePanelBase nodePanel = planeManager.activePlane as NodePanelBase;
        if (nodePanel == null)
        {
            FailInstallPhase("The server-install panel did not become active.");
            yield break;
        }

        yield return new WaitUntil(() => nodePanel.activeChild != null);
        nodePanel.LoadChild(InstallPhaseTopicName);

        yield return null;
        Panel_Operate installPhase = nodePanel.activeChild as Panel_Operate;
        if (installPhase == null)
        {
            FailInstallPhase("04系统镜像安装_第一阶段 could not be loaded as a model-operation topic.");
            yield break;
        }

        yield return new WaitUntil(() =>
            installPhase.topicData != null && installPhase.gameObject.activeInHierarchy);

        if (installPhase.topicData.tasks == null ||
            installPhase.topicData.tasks.Count <= SelectGnomeStepIndex)
        {
            FailInstallPhase("The GNOME step is missing from 04系统镜像安装_第一阶段.");
            yield break;
        }

        installPhase.stepIndex = SelectGnomeStepIndex;
        installPhase.NextStepState();

        if (!ShowOnlyServerInstallPage(SoftwareSelectionPageName))
        {
            FailInstallPhase("Page_08_SoftwareSelection could not be displayed.");
            yield break;
        }

        yield return null;
        TexCore_PressableButtonStep selectGnome =
            FindSceneComponent<TexCore_PressableButtonStep>(SelectGnomeButtonName);
        if (selectGnome == null || (int)selectGnome.state != 1)
        {
            FailInstallPhase("SYS_07_SelectGNOME did not enter the waiting state.");
            yield break;
        }

        isPreparingInstallPhase = false;
        isInstallPhasePrepared = true;
        installPhaseButtonText = "DEBUG: 04 Ready - Select GNOME";
        BpfLog.Info(BpfLogCategory.Diagnostics, "tex_core_server_network04_debug_button.prepare_install_phase_at_gnome", "Debug shortcut prepared 04系统镜像安装_第一阶段 at SYS_07_SelectGNOME.", this);
    }

    private IEnumerator PrepareInstallPhaseAtStartInstallation()
    {
        isPreparingStartInstallation = true;
        startInstallationButtonText = "DEBUG: Preparing Start Install...";

        yield return StartCoroutine(OpenInstallPhase(StartInstallationStepIndex));

        Panel_Operate installPhase = GetActiveInstallPhase();
        if (installPhase == null)
        {
            yield break;
        }

        if (!ShowOnlyServerInstallPage(InstallationSummaryPageName))
        {
            FailStartInstallation("Page_11_InstallationSummary_AfterGNOME could not be displayed.");
            yield break;
        }

        yield return null;
        TexCore_PressableButtonStep startInstallation =
            FindSceneComponent<TexCore_PressableButtonStep>(StartInstallationButtonName);
        if (startInstallation == null || (int)startInstallation.state != 1)
        {
            FailStartInstallation("SYS_09_StartInstallation did not enter the waiting state.");
            yield break;
        }

        isPreparingStartInstallation = false;
        isStartInstallationPrepared = true;
        startInstallationButtonText = "DEBUG: 04 Ready - Start Installation";
        BpfLog.Info(BpfLogCategory.Diagnostics, "tex_core_server_network04_debug_button.prepare_install_phase_at_start_installation", "Debug shortcut prepared 04系统镜像安装_第一阶段 at SYS_09_StartInstallation.", this);
    }

    private IEnumerator OpenInstallPhase(int stepIndex)
    {
        yield return new WaitUntil(FrameworkReady);

        TexCore_DataClass root = TexCore_ConfigManager.self.datas.datas.FindRoot(TexCore_HeadMenu.self.node);
        TexCore_DataClass serverInstall = root != null ? root.Find(ServerInstallName) : null;
        if (serverInstall == null)
        {
            FailInstallPhase("02服务器系统安装 was not found in Config.xml.");
            yield break;
        }

        TexCore_PlaneManager planeManager = TexCore_PlaneManager.self;
        if (planeManager.ShowPlane(serverInstall.name, serverInstall.GetUrl(), true) == null)
        {
            FailInstallPhase("The server-install panel could not be loaded.");
            yield break;
        }

        yield return null;
        NodePanelBase nodePanel = planeManager.activePlane as NodePanelBase;
        if (nodePanel == null)
        {
            FailInstallPhase("The server-install panel did not become active.");
            yield break;
        }

        yield return new WaitUntil(() => nodePanel.activeChild != null);
        nodePanel.LoadChild(InstallPhaseTopicName);

        yield return null;
        Panel_Operate installPhase = nodePanel.activeChild as Panel_Operate;
        if (installPhase == null)
        {
            FailInstallPhase("04系统镜像安装_第一阶段 could not be loaded as a model-operation topic.");
            yield break;
        }

        yield return new WaitUntil(() =>
            installPhase.topicData != null && installPhase.gameObject.activeInHierarchy);

        if (installPhase.topicData.tasks == null ||
            installPhase.topicData.tasks.Count <= stepIndex)
        {
            FailInstallPhase("The requested step is missing from 04系统镜像安装_第一阶段.");
            yield break;
        }

        installPhase.stepIndex = stepIndex;
        installPhase.NextStepState();
    }

    private static Panel_Operate GetActiveInstallPhase()
    {
        NodePanelBase nodePanel = TexCore_PlaneManager.self != null
            ? TexCore_PlaneManager.self.activePlane as NodePanelBase
            : null;
        return nodePanel != null ? nodePanel.activeChild as Panel_Operate : null;
    }

    private static bool FrameworkReady()
    {
        return TexCore_ConfigManager.self != null &&
               TexCore_ConfigManager.self.datas != null &&
               TexCore_ConfigManager.self.datas.datas != null &&
               TexCore_PlaneManager.self != null &&
               TexCore_PlaneManager.self.isResourcesLoad &&
               TexCore_HeadMenu.self != null &&
               !string.IsNullOrEmpty(TexCore_HeadMenu.self.node);
    }

    private void Fail(string reason)
    {
        isCompleting = false;
        buttonText = "DEBUG: Retry 05 Power On";
        BpfLog.Error(BpfLogCategory.Diagnostics, "tex_core_server_network04_debug_button.fail", "05开启服务器 debug shortcut failed: " + reason, this);
    }

    private void FailInstallPhase(string reason)
    {
        isPreparingInstallPhase = false;
        installPhaseButtonText = "DEBUG: Retry 04 Install / GNOME";
        BpfLog.Error(BpfLogCategory.Diagnostics, "tex_core_server_network04_debug_button.fail_install_phase", "04系统镜像安装_第一阶段 debug shortcut failed: " + reason, this);
    }

    private void FailStartInstallation(string reason)
    {
        isPreparingStartInstallation = false;
        startInstallationButtonText = "DEBUG: Retry 04 Start Installation";
        BpfLog.Error(BpfLogCategory.Diagnostics, "tex_core_server_network04_debug_button.fail_start_installation", "04系统镜像安装_第一阶段 start-install debug shortcut failed: " + reason, this);
    }

    private static bool ShowOnlyServerInstallPage(string pageName)
    {
        GameObject screen = FindSceneGameObject(ServerInstallScreenName);
        if (screen == null)
        {
            return false;
        }

        screen.SetActive(true);
        Transform canvas = FindChild(screen.transform, ServerInstallCanvasName);
        if (canvas == null)
        {
            return false;
        }

        bool foundPage = false;
        foreach (Transform child in canvas)
        {
            if (!child.name.StartsWith("Page_"))
            {
                continue;
            }

            bool isTargetPage = child.name == pageName;
            child.gameObject.SetActive(isTargetPage);
            foundPage |= isTargetPage;
        }

        return foundPage;
    }

    private static GameObject FindSceneGameObject(string objectName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform candidate in transforms)
        {
            if (candidate.name == objectName &&
                candidate.gameObject.scene.handle == activeScene.handle)
            {
                return candidate.gameObject;
            }
        }

        return null;
    }

    private static T FindSceneComponent<T>(string objectName) where T : Component
    {
        Scene activeScene = SceneManager.GetActiveScene();
        T[] components = Resources.FindObjectsOfTypeAll<T>();
        foreach (T candidate in components)
        {
            if (candidate.name == objectName &&
                candidate.gameObject.scene.handle == activeScene.handle)
            {
                return candidate;
            }
        }

        return null;
    }

    private static Transform FindChild(Transform root, string childName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }
#endif
}
