using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine;
using UnityEngine.SceneManagement;
using Bpf.Diagnostics;

/// <summary>
/// Development-only shortcuts for the 0809 charging-station module.
/// The first button marks the eight packaging screws as taken out by the player
/// so the box lid can be removed without drilling every screw while testing the
/// later flow. The second button jumps straight to the "02安装膨胀螺丝" topic at
/// its first step "08_02_安装充电桩" and leaves the manual placement, front-cover
/// and four drill steps to the tester.
/// </summary>
[DisallowMultipleComponent]
public sealed class TexCore_0809M08ScrewSkipDebugButton : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private const string SceneName = "0809";
    private const string NodeName = "08充电桩安装";
    private const string UnpackTopicName = "00开箱取出充电桩";
    private const string ScrewStepPrefix = "08_00_使用手电钻钻开包装箱螺丝";
    private const string UserCompletedFieldName = "userCompleted";
    private const int ScrewCount = 8;

    private const string ExpansionTopicName = "02安装膨胀螺丝";
    private const string QrStepName = "08_01_粘贴二维码";
    private const string LidStepName = "08_00_移开包装箱盖";
    private const string TakeOutStepName = "08_00_取出箱内充电桩";
    private const string PlacementStepName = "08_02_安装充电桩";
    private const string FrontCoverStepName = "08_03_打开充电桩前盖";
    private const string DrillStepPrefix = "08_04_钻孔";
    private const int DrillStepCount = 4;
    private const string PlacementSourceName = "ChongDianZhuang_fuben";
    private const string PlacementTargetName = "ChongDianZhuang";

    // Upper bound for every framework wait, so a stuck scene cannot leave the
    // placement shortcut coroutine waiting for ever.
    private const float WaitTimeoutSeconds = 20f;

    // The shared step exposes WasCompletedByUser read-only. The debug shortcut
    // writes the same flag the player interaction writes, so the box-lid gate
    // and the 08 flow policy accept the screws as done.
    private static readonly FieldInfo UserCompletedField = ResolveUserCompletedField();

    private static bool sceneCallbackRegistered;

    private string buttonText = "DEBUG: Skip 8 Box Screws";
    private string statusText = string.Empty;
    private bool isSkipping;
    private bool isCompleted;

    private string placementButtonText = "DEBUG: 跳到放置充电桩 / 前盖 / 4个钻孔";
    private string placementStatusText = string.Empty;
    private bool isPreparingPlacement;
    private bool isPlacementPrepared;
    private GUIStyle placementStatusStyle;

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

        TexCore_0809M08ScrewSkipDebugButton[] existingButtons =
            Resources.FindObjectsOfTypeAll<TexCore_0809M08ScrewSkipDebugButton>();
        foreach (TexCore_0809M08ScrewSkipDebugButton button in existingButtons)
        {
            if (button.gameObject.scene.handle == scene.handle)
            {
                return;
            }
        }

        GameObject debugRoot = new GameObject("Debug_0809SkipScrews");
        SceneManager.MoveGameObjectToScene(debugRoot, scene);
        debugRoot.AddComponent<TexCore_0809M08ScrewSkipDebugButton>();
    }

    private void Awake()
    {
        if (UserCompletedField == null)
        {
            buttonText = "DEBUG: Skip 8 Box Screws (unavailable)";
            statusText = "TexCore_OrderedPlacementStep." + UserCompletedFieldName + " was not found.";
            BpfLog.Error(BpfLogCategory.Diagnostics,
                "tex_core_0809_m08_screw_skip_debug_button.missing_field",
                "0809 screw skip shortcut is unavailable: " + statusText, this);
        }
    }

    private void OnGUI()
    {
        if (SceneManager.GetActiveScene().name != SceneName)
        {
            return;
        }

        GUI.enabled = !isSkipping && !isCompleted && UserCompletedField != null &&
                      IsUnpackTopicActive();
        if (GUI.Button(new Rect(Screen.width - 364f, 24f, 340f, 56f), buttonText))
        {
            StartCoroutine(SkipScrews());
        }
        GUI.enabled = true;

        if (!string.IsNullOrEmpty(statusText))
        {
            GUI.Label(new Rect(Screen.width - 364f, 84f, 340f, 48f), statusText);
        }

        GUI.enabled = !isPreparingPlacement && !isPlacementPrepared && FrameworkReady();
        if (GUI.Button(new Rect(Screen.width - 364f, 140f, 340f, 56f), placementButtonText))
        {
            StartCoroutine(PrepareExpansionTopic());
        }
        GUI.enabled = true;

        if (!string.IsNullOrEmpty(placementStatusText))
        {
            if (placementStatusStyle == null)
            {
                placementStatusStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
            }

            GUI.Label(new Rect(Screen.width - 364f, 200f, 340f, 80f),
                placementStatusText, placementStatusStyle);
        }
    }

    private IEnumerator SkipScrews()
    {
        isSkipping = true;
        buttonText = "DEBUG: Skipping...";
        statusText = string.Empty;

        yield return new WaitUntil(FrameworkReady);
        yield return new WaitUntil(IsUnpackTopicActive);

        Panel_Operate topic = GetActiveUnpackTopic();
        if (topic == null)
        {
            Fail("Enter " + NodeName + " / " + UnpackTopicName + " first.");
            yield break;
        }

        List<TexCore_OrderedPlacementStep> screws = FindScrewSteps();
        if (screws.Count != ScrewCount)
        {
            Fail("Found " + screws.Count + " box screw steps, expected " + ScrewCount + ".");
            yield break;
        }

        int skipped = 0;
        for (int index = topic.stepIndex; index < ScrewCount; index++)
        {
            TexCore_OrderedPlacementStep screw = screws[index];
            UserCompletedField.SetValue(screw, true);
            TexCore_BusinessManager.OnClick(screw.gameObject);
            skipped++;
            yield return null;
        }

        isSkipping = false;
        isCompleted = true;
        buttonText = "DEBUG: 8 Box Screws Skipped";
        statusText = skipped > 0
            ? "Skipped " + skipped + " screw(s). Remove the box lid next."
            : "All 8 screws were already done.";
        BpfLog.Info(BpfLogCategory.Diagnostics,
            "tex_core_0809_m08_screw_skip_debug_button.skip_screws",
            "Debug shortcut skipped " + skipped + " packaging screws in 0809.", this);
    }

    /// <summary>
    /// Jumps straight to "02安装膨胀螺丝" / "08_02_安装充电桩" so the tester can
    /// perform the charging-station placement, the front cover and the four
    /// drill steps by hand. Everything before that is marked as already done
    /// without going through <see cref="TexCore_BusinessManager.OnClick"/>,
    /// because the current topic must change instead of stepping itself.
    /// </summary>
    private IEnumerator PrepareExpansionTopic()
    {
        isPreparingPlacement = true;
        placementButtonText = "DEBUG: 跳转中...";
        placementStatusText = string.Empty;

        yield return WaitFor(FrameworkReady);
        if (!FrameworkReady())
        {
            FailPlacement("TexCore 框架在 " + WaitTimeoutSeconds + " 秒内没有加载完成。");
            yield break;
        }

        BpfLog.Info(BpfLogCategory.Diagnostics,
            "tex_core_0809_m08_screw_skip_debug_button.prepare_expansion",
            "[0809 DEBUG] 开始跳转到 " + ExpansionTopicName, this);

        TexCore_PlaneManager planeManager = TexCore_PlaneManager.self;
        NodePanelBase nodePanel = GetActiveNode(planeManager);
        if (nodePanel == null)
        {
            TexCore_DataClass root =
                TexCore_ConfigManager.self.datas.datas.FindRoot(TexCore_HeadMenu.self.node);
            TexCore_DataClass nodeData = root != null ? root.Find(NodeName) : null;
            if (nodeData == null)
            {
                FailPlacement("配置中找不到节点 " + NodeName + "。");
                yield break;
            }

            planeManager.ShowPlane(nodeData.name, nodeData.GetUrl(), true);
            yield return WaitFor(() => GetActiveNode(planeManager) != null);
            nodePanel = GetActiveNode(planeManager);
        }

        if (nodePanel == null)
        {
            FailPlacement("未能在 " + WaitTimeoutSeconds + " 秒内进入节点 " + NodeName + "。");
            yield break;
        }

        // The target topic is loaded first on purpose: the placement step has to
        // be the active step before the earlier steps are marked, otherwise the
        // take-out step would switch off the interaction of the charging-station
        // object that this step is about to share.
        nodePanel.LoadChild(ExpansionTopicName);
        yield return WaitFor(() => IsExpansionTopicReady(nodePanel));

        Panel_Operate expansion = nodePanel.activeChild as Panel_Operate;
        if (expansion == null || !IsExpansionTopicReady(nodePanel))
        {
            FailPlacement("未能加载子题目 " + ExpansionTopicName + "。");
            yield break;
        }

        if (!HasExpectedTasks(expansion))
        {
            FailPlacement(ExpansionTopicName + " 的 tasks 前 6 步与预期不一致。");
            yield break;
        }

        expansion.isSubmit = false;
        expansion.stepIndex = 0;
        expansion.steps = new List<string>();

        // Broadcasts 设置物体状态, which puts "08_02_安装充电桩" into 等待触发.
        // The topic's own "下一步" button is deliberately not used here.
        expansion.NextStepState();

        if (!MarkPrerequisiteSteps())
        {
            yield break;
        }

        // The placement state has to settle first, then the step binding is
        // inspected once the 0809 scene helpers ran their frame compensation.
        yield return new WaitForEndOfFrame();

        TexCore_OrderedPlacementStep placement = FindPlacementStep();
        if (placement == null)
        {
            FailPlacement("场景中找不到放置步骤 " + PlacementStepName + "。");
            yield break;
        }

        GameObject source = placement.InteractionSource;
        Transform target = placement.Target;
        if (source == null || source.name != PlacementSourceName)
        {
            FailPlacement(PlacementStepName + " 的 source 不是 " + PlacementSourceName +
                          "（实际：" + (source != null ? source.name : "null") + "）。");
            yield break;
        }

        if (target == null || target.name != PlacementTargetName)
        {
            FailPlacement(PlacementStepName + " 的 target 不是 " + PlacementTargetName +
                          "（实际：" + (target != null ? target.name : "null") + "）。");
            yield break;
        }

        if (placement.state != FinishState.等待触发)
        {
            FailPlacement(PlacementStepName + " 的状态是 " + placement.state + "，不是等待触发。");
            yield break;
        }

        if (!source.activeInHierarchy)
        {
            source.SetActive(true);
        }

        string interactionDetail;
        bool interactable = IsSourceInteractable(source, out interactionDetail);

        isPreparingPlacement = false;
        isPlacementPrepared = true;
        placementButtonText = "DEBUG: 已跳到 08_02 放置充电桩";
        placementStatusText = "当前可手动完成：放置充电桩、打开前盖、4个钻孔";
        if (!interactable)
        {
            placementStatusText += "（注意：充电桩当前不可抓取，请看 Console）";
        }

        BpfLog.Info(BpfLogCategory.Diagnostics,
            "tex_core_0809_m08_screw_skip_debug_button.placement_ready",
            "[0809 DEBUG] active topic = " + expansion.datas.name +
            "\n[0809 DEBUG] active step = " + expansion.topicData.tasks[expansion.stepIndex] +
            "\n[0809 DEBUG] source = " + source.name +
            "\n[0809 DEBUG] target = " + target.name +
            "\n[0809 DEBUG] placement state = " + placement.state +
            "\n[0809 DEBUG] source active = " + source.activeInHierarchy + ", " + interactionDetail +
            "\n[0809 DEBUG] target active = " + target.gameObject.activeInHierarchy +
            " (0809 放置步骤进行期间安装位由场景脚本隐藏)", this);

        if (!interactable)
        {
            BpfLog.Warning(BpfLogCategory.Diagnostics,
                "tex_core_0809_m08_screw_skip_debug_button.placement_not_interactable",
                "0809 placement shortcut reached " + PlacementStepName + " but " + source.name +
                " is not grabbable (" + interactionDetail + ").", this);
        }
    }

    /// <summary>
    /// Marks the eight packaging screws, the box lid, the take-out step and the
    /// QR step as done, so only the placement, front-cover and drill steps are
    /// left for the tester. Runs in a single frame on purpose.
    /// </summary>
    private bool MarkPrerequisiteSteps()
    {
        List<string> stepNames = new List<string>(ScrewCount + 3);
        for (int index = 1; index <= ScrewCount; index++)
        {
            stepNames.Add(ScrewStepPrefix + index);
        }

        stepNames.Add(LidStepName);
        stepNames.Add(TakeOutStepName);
        stepNames.Add(QrStepName);

        TexCore_InteracterBase[] sceneSteps = FindObjectsOfType<TexCore_InteracterBase>(true);
        foreach (string stepName in stepNames)
        {
            TexCore_InteracterBase reporter = FindStepByName(sceneSteps, stepName);
            if (reporter == null)
            {
                FailPlacement("场景中找不到前置步骤 " + stepName + "。");
                return false;
            }

            if (!MarkDebugCompleted(reporter))
            {
                FailPlacement("前置步骤 " + stepName + " 没有 " + UserCompletedFieldName + " 字段。");
                return false;
            }
        }

        return true;
    }

    private static Panel_Operate GetActiveUnpackTopic()
    {
        TexCore_PlaneManager planeManager = TexCore_PlaneManager.self;
        NodePanelBase nodePanel = planeManager != null
            ? planeManager.activePlane as NodePanelBase
            : null;
        if (nodePanel == null || nodePanel.datas == null ||
            nodePanel.datas.name != NodeName)
        {
            return null;
        }

        PanelTopicBase topic = nodePanel.activeChild;
        if (topic == null || topic.datas == null || topic.datas.name != UnpackTopicName)
        {
            return null;
        }

        return topic as Panel_Operate;
    }

    private static bool IsUnpackTopicActive()
    {
        return GetActiveUnpackTopic() != null;
    }

    private static NodePanelBase GetActiveNode(TexCore_PlaneManager planeManager)
    {
        NodePanelBase nodePanel = planeManager != null
            ? planeManager.activePlane as NodePanelBase
            : null;
        return nodePanel != null && nodePanel.datas != null && nodePanel.datas.name == NodeName
            ? nodePanel
            : null;
    }

    private static bool IsExpansionTopicReady(NodePanelBase nodePanel)
    {
        PanelTopicBase topic = nodePanel != null ? nodePanel.activeChild : null;
        return topic != null && topic.datas != null &&
               topic.datas.name == ExpansionTopicName && topic.topicData != null;
    }

    private static bool HasExpectedTasks(Panel_Operate expansion)
    {
        List<string> tasks = expansion.topicData != null ? expansion.topicData.tasks : null;
        if (tasks == null || tasks.Count < DrillStepCount + 2)
        {
            return false;
        }

        if (tasks[0] != PlacementStepName || tasks[1] != FrontCoverStepName)
        {
            return false;
        }

        for (int index = 0; index < DrillStepCount; index++)
        {
            if (tasks[2 + index] != DrillStepPrefix + (index + 1))
            {
                return false;
            }
        }

        return true;
    }

    private static TexCore_InteracterBase FindStepByName(
        TexCore_InteracterBase[] sceneSteps,
        string stepName)
    {
        foreach (TexCore_InteracterBase candidate in sceneSteps)
        {
            if (candidate != null && candidate.name == stepName)
            {
                return candidate;
            }
        }

        return null;
    }

    private static TexCore_OrderedPlacementStep FindPlacementStep()
    {
        TexCore_OrderedPlacementStep[] allSteps =
            FindObjectsOfType<TexCore_OrderedPlacementStep>(true);
        foreach (TexCore_OrderedPlacementStep step in allSteps)
        {
            if (step != null && step.name == PlacementStepName)
            {
                return step;
            }
        }

        return null;
    }

    /// <summary>
    /// Reports whether the placement source can actually be grabbed: TexCore
    /// steps complete through the manipulator, MRTK still needs a collider.
    /// </summary>
    private static bool IsSourceInteractable(GameObject source, out string detail)
    {
        bool hasCollider = false;
        foreach (Collider value in source.GetComponentsInChildren<Collider>(true))
        {
            if (value != null && value.enabled)
            {
                hasCollider = true;
                break;
            }
        }

        bool hasManipulator = false;
        foreach (ObjectManipulator value in
                 source.GetComponentsInChildren<ObjectManipulator>(true))
        {
            if (value != null && value.enabled)
            {
                hasManipulator = true;
                break;
            }
        }

        detail = "collider=" + hasCollider + " manipulator=" + hasManipulator;
        return hasCollider && hasManipulator;
    }

    private static List<TexCore_OrderedPlacementStep> FindScrewSteps()
    {
        List<TexCore_OrderedPlacementStep> result =
            new List<TexCore_OrderedPlacementStep>(ScrewCount);
        TexCore_OrderedPlacementStep[] allSteps =
            FindObjectsOfType<TexCore_OrderedPlacementStep>(true);

        for (int index = 1; index <= ScrewCount; index++)
        {
            string expectedName = ScrewStepPrefix + index;
            TexCore_OrderedPlacementStep match = null;
            foreach (TexCore_OrderedPlacementStep candidate in allSteps)
            {
                if (candidate != null && candidate.name == expectedName)
                {
                    match = candidate;
                    break;
                }
            }

            if (match == null)
            {
                return new List<TexCore_OrderedPlacementStep>();
            }

            result.Add(match);
        }

        return result;
    }

    private static FieldInfo ResolveUserCompletedField()
    {
        return ResolveUserCompletedField(typeof(TexCore_OrderedPlacementStep));
    }

    private static FieldInfo ResolveUserCompletedField(System.Type type)
    {
        while (type != null)
        {
            FieldInfo field = type.GetField(UserCompletedFieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                return field;
            }

            type = type.BaseType;
        }

        return null;
    }

    /// <summary>
    /// Writes the same user-completion flag a real interaction writes and moves
    /// every TexCore reporter on the step object to 已完成. A step usually
    /// carries its own step script plus a TexCore_StepObjectState, so both the
    /// user state, the TexCore state and the object visibility are updated.
    /// </summary>
    private static bool MarkDebugCompleted(TexCore_InteracterBase reporter)
    {
        if (reporter == null)
        {
            return false;
        }

        FieldInfo field = ResolveUserCompletedField(reporter.GetType());
        if (field != null)
        {
            field.SetValue(reporter, true);
        }

        foreach (TexCore_InteracterBase component in
                 reporter.GetComponents<TexCore_InteracterBase>())
        {
            component.SetState(FinishState.已完成);
        }

        return field != null;
    }

    private static IEnumerator WaitFor(System.Func<bool> condition)
    {
        float deadline = Time.realtimeSinceStartup + WaitTimeoutSeconds;
        while (!condition() && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }
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
        isSkipping = false;
        buttonText = "DEBUG: Retry Skip 8 Box Screws";
        statusText = reason;
        BpfLog.Error(BpfLogCategory.Diagnostics,
            "tex_core_0809_m08_screw_skip_debug_button.fail",
            "0809 screw skip shortcut failed: " + reason, this);
    }

    private void FailPlacement(string reason)
    {
        isPreparingPlacement = false;
        isPlacementPrepared = false;
        placementButtonText = "DEBUG: 重试跳到放置充电桩";
        placementStatusText = reason;
        BpfLog.Error(BpfLogCategory.Diagnostics,
            "tex_core_0809_m08_screw_skip_debug_button.placement_fail",
            "0809 placement shortcut failed: " + reason, this);
    }
#endif
}
