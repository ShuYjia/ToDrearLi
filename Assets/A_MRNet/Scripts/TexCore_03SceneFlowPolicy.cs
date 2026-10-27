using System.Collections.Generic;
using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene-local flow policy for a configured single-player scene.
/// </summary>
/// <remarks>
/// 03 场景策略：
/// 1. 运行时隐藏所有标准绿色“下一步”（题目级 PanelTopicBase.btn_Next、
///    节点级 NodePanelBase.btn_Next 以及名称为“下一步”的标准按钮），
///    并且每帧用缓存持续压制，拦截旧协程或旧事件的重新显示；
///    “跳过”、U 盘背包、屏幕内部与菜单导航按钮不受影响。
/// 2. 运行时隐藏所有标准确认按钮：名称为“确定”“PositiveButton”“Positive”，
///    以及位于对白/指引根节点下的“确定按钮”。选择题已改为“选完即自动提交”，
///    操作题占位任务“确定”由各自模块的运行时桥接触发合成点击完成，因此都
///    不再需要可见按钮；名称不同的业务按钮（“M02_IP_Apply”“跳过”“UPan”
///    以及非对白层的“确定按钮”）不在此列。
///    隐藏不再只依赖下一帧 Update：Awake / OnEnable / 场景加载 / 更新节点 /
///    更新题目都会在同帧立即压制一次，避免首帧闪现。
/// 3. 监听“完成题目”并延迟一帧自动推进：非最后一题调用 NextTopic()，
///    最后一题直接调用 ClickNext()。M02 / 视频题等已自行切题的题目
///    会因活动题目变化而被跳过，不会重复推进。
/// </remarks>
public sealed class TexCore_03SceneFlowPolicy : MonoBehaviour
{
    private const string M02NodeName = "02服务器系统安装";
    private const string M08NodeName = "08充电桩安装";
    private const string M08UnpackTopicName = "00开箱取出充电桩";
    private const string M08ScrewStepPrefix = "08_00_使用手电钻钻开包装箱螺丝";
    private const string M08LidStepName = "08_00_移开包装箱盖";
    private const string NextButtonName = TexCore_StandardButtonPolicy.NextButtonName;
    private const int M08ScrewCount = 8;

    [SerializeField] private TexCore_StepObj stepObjects;
    [SerializeField] private string targetSceneName = "03SingleplayerScene";
    private readonly List<Transform> confirmButtons = new List<Transform>();
    private readonly List<NodePanelBase> nodePanels = new List<NodePanelBase>();
    private readonly List<GameObject> nextButtons = new List<GameObject>();
    private readonly List<PendingTopicCompletion> pendingCompletions =
        new List<PendingTopicCompletion>();
    private bool flowDirty = true;
    private bool cacheDirty = true;
    private bool eventsRegistered;

    private readonly struct PendingTopicCompletion
    {
        public PendingTopicCompletion(string topicName, int frame)
        {
            TopicName = topicName;
            Frame = frame;
        }

        public string TopicName { get; }
        public int Frame { get; }
    }

    private enum NextAdvanceMode
    {
        None,
        AlreadyAdvanced,
        NextTopic,
        ClickNext
    }
    private readonly List<TexCore_OrderedPlacementStep> m08ScrewSteps =
        new List<TexCore_OrderedPlacementStep>();
    private readonly List<GameObject> m08LidObjects = new List<GameObject>();
    private TexCore_ClickObjectStep m08LidStep;
    private bool m08UnpackGuardConfigured;

    // M08 初始化改为“脏标记 + 有限重试”：不再每帧全局查找，
    // 由场景加载、节点/题目更新事件触发，最多重试 40 次、间隔 0.25 秒。
    private const int M08GuardMaxRetries = 40;
    private const float M08GuardRetryInterval = 0.25f;
    private bool m08GuardInitPending;
    private int m08GuardRetryCount;
    private float m08GuardNextRetryTime;

    /// <summary>
    /// 盖子门禁的组件缓存：初始化时一次性收集每块“DingGai”下的
    /// ObjectManipulator 与 Collider，运行期只改 enabled，不再每帧
    /// GetComponentsInChildren。
    /// </summary>
    private sealed class M08LidGateEntry
    {
        public GameObject LidObject;
        public readonly List<ObjectManipulator> Manipulators = new List<ObjectManipulator>();
        public readonly List<Collider> Colliders = new List<Collider>();
    }
    private readonly List<M08LidGateEntry> m08LidGateEntries = new List<M08LidGateEntry>();
    private GameObject m08LastAuthorizedLidSource;
    private bool m08GateAppliedOnce;

    // “更新题目”后动态题目里可能带新的“下一步”/“确定”按钮，
    // 只扫描当前活动节点/题目子树，不重新扫描全场景。
    private bool topicSubtreeScanPending;

    private void Awake()
    {
        if (stepObjects == null)
        {
            stepObjects = GetComponent<TexCore_StepObj>();
        }

        RegisterEvents();
        RebuildCaches();
        // 建缓存时同步压一次：场景里已经激活的标准按钮不能等到下一帧 Update。
        HideStandardButtonsImmediately();
        RequestM08GuardInit();
    }

    private void OnEnable()
    {
        UnregisterEvents();
        RegisterEvents();
        flowDirty = true;
        cacheDirty = true;
        HideStandardButtonsImmediately();
        RequestM08GuardInit();
    }

    private void OnDisable()
    {
        UnregisterEvents();
        ClearCaches();
    }

    /// <summary>
    /// 立即隐藏缓存里的标准“下一步/确定”。用于 Awake、OnEnable、场景加载、
    /// 更新节点与更新题目这些“按钮刚被创建或刚被激活”的时机，避免只靠
    /// 下一帧 Update 压制而产生一帧闪现。
    /// </summary>
    private void HideStandardButtonsImmediately()
    {
        HideNextButtons();
        HideConfirmButtons();
    }

    private void Update()
    {
        if (!IsTargetScene(SceneManager.GetActiveScene().name))
        {
            return;
        }

        if (cacheDirty)
        {
            RebuildCaches();
        }

        // 持续压制缓存中的“下一步”，拦截旧协程（如 Panel_Operate.ShowNext）
        // 或旧事件（如 PanelTopicBase.OnEnable 的 isSubmit 分支）重新显示。
        HideNextButtons();

        ProcessM08GuardRetry();
        EnforceM08UnpackSequence();

        // 完成队列非空同样是“还需要排空一次”的依据，不能只看 flowDirty：
        // 完成事件与本次 Update 落在同一帧时，本次 Update 只会把 flowDirty 消费掉，
        // 而队列表项要等下一帧才满足延迟一帧条件。若此时提前返回，下一帧没有任何
        // 新事件再把流程标脏，整条完成队列就会被永久搁置——选择题因此不再自动跳题。
        if (!flowDirty && pendingCompletions.Count == 0)
        {
            return;
        }

        // Clear before processing.  SkipEmptyActiveSteps can broadcast another
        // state event, which must leave the policy dirty for the next frame.
        flowDirty = false;
        if (topicSubtreeScanPending)
        {
            topicSubtreeScanPending = false;
            RefreshActiveTopicSubtreeCaches();
        }

        HideConfirmButtons();
        SkipEmptyActiveSteps();
        ProcessPendingTopicCompletions(Time.frameCount);
        EnforceM08UnpackSequence();
    }

    /// <summary>
    /// 请求一次 M08 防误操作初始化。只在未配置成功时生效；
    /// 每次触发事件（场景加载、节点/题目更新）都会重新赋予完整的重试预算。
    /// </summary>
    private void RequestM08GuardInit()
    {
        if (m08UnpackGuardConfigured)
        {
            return;
        }

        m08GuardInitPending = true;
        m08GuardRetryCount = 0;
    }

    /// <summary>
    /// Update 里只做重试计时与缓存有效性判断，真正的全局查找被限制在
    /// 最多 40 次、间隔 0.25 秒的延迟绑定窗口内，不再每帧执行。
    /// </summary>
    private void ProcessM08GuardRetry()
    {
        if (m08UnpackGuardConfigured)
        {
            if (HasM08GuardCacheBeenDestroyed())
            {
                // 缓存的步骤/盖子对象已被销毁（如题目重新实例化），
                // 重新进入有限重试，而不是带着失效引用误判业务状态。
                m08UnpackGuardConfigured = false;
                m08GateAppliedOnce = false;
                m08LastAuthorizedLidSource = null;
                m08GuardInitPending = true;
                m08GuardRetryCount = 0;
                m08GuardNextRetryTime = Time.unscaledTime + M08GuardRetryInterval;
            }
            else
            {
                m08GuardInitPending = false;
                return;
            }
        }

        if (!m08GuardInitPending ||
            m08GuardRetryCount >= M08GuardMaxRetries ||
            Time.unscaledTime < m08GuardNextRetryTime)
        {
            return;
        }

        TryConfigureM08GuardOnce();
    }

    /// <summary>
    /// 执行一次 M08 初始化尝试。无论成败都会推进重试计数；
    /// 成功后清除待处理标记，失败到上限后停止，等待下一次事件重新触发。
    /// </summary>
    private void TryConfigureM08GuardOnce()
    {
        m08GuardRetryCount++;
        m08GuardNextRetryTime = Time.unscaledTime + M08GuardRetryInterval;
        ConfigureM08UnpackGuard();
        if (m08UnpackGuardConfigured)
        {
            m08GuardInitPending = false;
            m08GuardRetryCount = 0;
        }
    }

    private bool HasM08GuardCacheBeenDestroyed()
    {
        if (m08LidStep == null)
        {
            return true;
        }

        for (int index = 0; index < m08ScrewSteps.Count; index++)
        {
            if (m08ScrewSteps[index] == null)
            {
                return true;
            }
        }

        return false;
    }

    private void ConfigureM08UnpackGuard()
    {
        if (m08UnpackGuardConfigured)
        {
            return;
        }

        m08ScrewSteps.Clear();
        TexCore_OrderedPlacementStep[] allScrews =
            FindObjectsOfType<TexCore_OrderedPlacementStep>(true);
        for (int index = 1; index <= M08ScrewCount; index++)
        {
            string stepName = M08ScrewStepPrefix + index;
            TexCore_OrderedPlacementStep screw = null;
            foreach (TexCore_OrderedPlacementStep candidate in allScrews)
            {
                if (candidate != null && candidate.name == stepName)
                {
                    screw = candidate;
                    break;
                }
            }

            if (screw == null)
            {
                return;
            }

            m08ScrewSteps.Add(screw);
        }

        TexCore_ClickObjectStep lid = null;
        foreach (TexCore_ClickObjectStep candidate in
                 FindObjectsOfType<TexCore_ClickObjectStep>(true))
        {
            if (candidate != null && candidate.name == M08LidStepName)
            {
                lid = candidate;
                break;
            }
        }

        if (lid == null)
        {
            return;
        }

        m08LidStep = lid;
        m08LidObjects.Clear();
        m08LidGateEntries.Clear();
        foreach (Transform candidate in FindObjectsOfType<Transform>(true))
        {
            if (candidate != null && candidate.name == "DingGai" &&
                IsUnderM08Node(candidate))
            {
                m08LidObjects.Add(candidate.gameObject);
                M08LidGateEntry entry = new M08LidGateEntry
                {
                    LidObject = candidate.gameObject
                };
                entry.Manipulators.AddRange(
                    candidate.GetComponentsInChildren<ObjectManipulator>(true));
                entry.Colliders.AddRange(
                    candidate.GetComponentsInChildren<Collider>(true));
                m08LidGateEntries.Add(entry);
            }
        }

        lid.ConfigurePrerequisites(m08ScrewSteps,
            "请先按顺序使用手电钻拆除包装箱上的 8 颗螺丝。");
        m08GateAppliedOnce = false;
        m08LastAuthorizedLidSource = null;
        m08UnpackGuardConfigured = true;
    }

    private void EnforceM08UnpackSequence()
    {
        if (!m08UnpackGuardConfigured)
        {
            return;
        }

        foreach (NodePanelBase node in nodePanels)
        {
            if (node == null || !node.isActiveAndEnabled || node.datas == null ||
                node.datas.name != M08NodeName || node.activeChild == null ||
                !node.activeChild.isActiveAndEnabled || node.activeChild.datas == null ||
                node.activeChild.datas.name != M08UnpackTopicName)
            {
                continue;
            }

            int firstIncomplete = FindFirstIncompleteM08Screw();
            EnforceM08BoxLidInteractionGate(node, firstIncomplete);
            if (firstIncomplete == M08ScrewCount)
            {
                if (m08LidStep != null && m08LidStep.WasCompletedByUser)
                {
                    return;
                }

                if (node.activeChild.stepIndex == M08ScrewCount)
                {
                    return;
                }

                node.activeChild.stepIndex = M08ScrewCount;
                BroadcastM08StepState(node, M08ScrewCount);
                return;
            }

            if (node.activeChild.stepIndex == firstIncomplete)
            {
                return;
            }

            node.activeChild.stepIndex = firstIncomplete;
            BroadcastM08StepState(node, firstIncomplete);
            return;
        }
    }

    private void EnforceM08BoxLidInteractionGate(NodePanelBase node,
        int firstIncomplete)
    {
        if (m08LidStep == null || m08LidGateEntries.Count == 0)
        {
            return;
        }

        // 盖子已被用户移开后不再锁定，保证可以继续搬动。
        // 只对 0809 测试副本生效，03 主线保持原有行为不变。
        if (m08LidStep.WasCompletedByUser &&
            gameObject.scene.name == TexCore_LoadScene.Test0809SceneName)
        {
            return;
        }

        bool allowBoxLid = firstIncomplete == M08ScrewCount &&
            node.activeChild.stepIndex == M08ScrewCount &&
            m08LidStep.state == FinishState.等待触发 &&
            !m08LidStep.WasCompletedByUser;
        GameObject authorizedSource = allowBoxLid
            ? m08LidStep.InteractionSource
            : null;

        // 授权对象没有变化时，组件的启停状态不会变，跳过重新应用，
        // 避免每帧遍历盖子子树的组件列表。
        if (m08GateAppliedOnce && m08LastAuthorizedLidSource == authorizedSource)
        {
            return;
        }

        m08LastAuthorizedLidSource = authorizedSource;
        m08GateAppliedOnce = true;
        ApplyM08LidGate(authorizedSource);
    }

    /// <summary>
    /// 使用初始化时缓存的组件引用启停盖子交互；
    /// 只修改 enabled，不改变盖子对象层级与业务状态。
    /// </summary>
    private void ApplyM08LidGate(GameObject authorizedSource)
    {
        foreach (M08LidGateEntry entry in m08LidGateEntries)
        {
            if (entry == null || entry.LidObject == null)
            {
                continue;
            }

            bool allow = entry.LidObject == authorizedSource;
            foreach (ObjectManipulator manipulator in entry.Manipulators)
            {
                if (manipulator != null)
                {
                    manipulator.enabled = allow;
                }
            }

            foreach (Collider collider in entry.Colliders)
            {
                if (collider != null)
                {
                    collider.enabled = allow;
                }
            }
        }
    }

    private static bool IsUnderM08Node(Transform target)
    {
        Transform current = target;
        while (current != null)
        {
            if (current.name == M08NodeName)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static void BroadcastM08StepState(NodePanelBase node, int stepIndex)
    {
        int nodeIndex = node.datas.GetIndexSelf();
        int topicIndex = node.activeChild.datas.GetIndexSelf();
        if (nodeIndex < 0 || topicIndex < 0)
        {
            return;
        }

        TexCore_EventCenter.Broadcast(TexCore_EventType.设置物体状态,
            nodeIndex, topicIndex, stepIndex);
        TexCore_EventCenter.Broadcast<string>(TexCore_EventType.显示标题,
            GetStepTitle(node.activeChild, stepIndex));
    }

    private int FindFirstIncompleteM08Screw()
    {
        for (int index = 0; index < m08ScrewSteps.Count; index++)
        {
            if (!m08ScrewSteps[index].WasCompletedByUser)
            {
                return index;
            }
        }

        return M08ScrewCount;
    }

    private void RegisterEvents()
    {
        if (eventsRegistered) return;
        TexCore_EventCenter.AddListener<string>(TexCore_EventType.更新节点, OnNodeChanged);
        TexCore_EventCenter.AddListener<string>(TexCore_EventType.更新题目, OnTopicShown);
        TexCore_EventCenter.AddListener<string>(TexCore_EventType.完成题目, OnTopicCompleted);
        TexCore_EventCenter.AddListener<int, int, int>(
            TexCore_EventType.设置物体状态, OnObjectStateChanged);
        SceneManager.sceneLoaded += OnSceneLoaded;
        eventsRegistered = true;
    }

    private void UnregisterEvents()
    {
        if (!eventsRegistered) return;
        TryRemoveStringListener(TexCore_EventType.更新节点, OnNodeChanged);
        TryRemoveStringListener(TexCore_EventType.更新题目, OnTopicShown);
        TryRemoveStringListener(TexCore_EventType.完成题目, OnTopicCompleted);
        try
        {
            TexCore_EventCenter.RemoveListener<int, int, int>(
                TexCore_EventType.设置物体状态, OnObjectStateChanged);
        }
        catch (System.Exception)
        {
            // Another scene component may have cleared the shared event table.
        }
        SceneManager.sceneLoaded -= OnSceneLoaded;
        eventsRegistered = false;
    }

    private void TryRemoveStringListener(
        TexCore_EventType eventType, CallBack<string> handler)
    {
        try
        {
            TexCore_EventCenter.RemoveListener(eventType, handler);
        }
        catch (System.Exception)
        {
            // Another scene component may have cleared the shared event table.
        }
    }

    private void OnTopicCompleted(string topicName)
    {
        if (!IsTargetScene(SceneManager.GetActiveScene().name))
        {
            return;
        }

        QueueTopicCompletion(topicName, Time.frameCount);
    }

    private void OnObjectStateChanged(int _, int __, int ___) => MarkFlowDirty();

    private void OnSceneLoaded(Scene scene, LoadSceneMode _)
    {
        if (scene != gameObject.scene) return;
        ClearCaches();
        if (IsTargetScene(scene.name))
        {
            flowDirty = true;
            RequestM08GuardInit();
        }
    }

    /// <summary>
    /// “更新节点”：新节点可能刚刚被创建或启用，除重建缓存外还要同帧压一次
    /// 标准按钮，避免新节点里的“下一步/确定”先渲染一帧。
    /// </summary>
    private void OnNodeChanged(string _)
    {
        flowDirty = true;
        cacheDirty = true;
        RequestM08GuardInit();
    }

    /// <summary>
    /// “更新题目”：动态题目出现，只标记流程刷新与活动题目子树扫描，
    /// 不重新扫描全场景；同时立即压制缓存里已有的标准按钮，
    /// 新题目的按钮由 <see cref="RefreshActiveTopicSubtreeCaches"/> 在并入
    /// 缓存的同一次调用里隐藏。
    /// </summary>
    private void OnTopicShown(string _)
    {
        flowDirty = true;
        topicSubtreeScanPending = true;
        HideStandardButtonsImmediately();
        RequestM08GuardInit();
    }

    /// <summary>
    /// “完成题目”与“设置物体状态”只改变流程进度，不改变场景对象集合，
    /// 只标记流程刷新，不重建缓存。
    /// </summary>
    private void MarkFlowDirty()
    {
        flowDirty = true;
    }

    public bool IsTargetScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            return false;
        }

        if (sceneName == targetSceneName)
        {
            return true;
        }

        // 0809, 0607 and 09 are test copies of the 03 business scene. Their
        // serialized copy still carries the 03 target value, so recognize the
        // owning scene without changing the mainline scene's default target.
        bool isBusinessTestCopy =
            gameObject.scene.name == TexCore_LoadScene.Test0809SceneName ||
            gameObject.scene.name == TexCore_LoadScene.Test0607SceneName ||
            gameObject.scene.name == TexCore_LoadScene.Test09SceneName;
        return isBusinessTestCopy && sceneName == gameObject.scene.name;
    }

    private void RebuildCaches()
    {
        Scene policyScene = gameObject.scene;
        confirmButtons.Clear();
        foreach (Transform item in FindObjectsOfType<Transform>(true))
        {
            if (item != null && item.gameObject.scene == policyScene &&
                ShouldHideConfirmButton(item))
            {
                confirmButtons.Add(item);
            }
        }

        nodePanels.Clear();
        foreach (NodePanelBase node in FindObjectsOfType<NodePanelBase>(true))
        {
            if (node != null && node.gameObject.scene == policyScene) nodePanels.Add(node);
        }

        CollectNextButtons(policyScene);

        // 03 禁止双手缩放：复用缓存失效时机做一次性限制，
        // 不新增 Update 里的全场景扫描。
        TexCore_03NoScalePolicy.EnforceScene(policyScene);
        cacheDirty = false;
    }

    /// <summary>
    /// 缓存当前场景内所有标准绿色“下一步”：节点级 btn_Next、题目级
    /// btn_Next，以及名称为“下一步”的标准按钮。“跳过”、U 盘背包按钮、
    /// 屏幕内部按钮与菜单导航按钮不在匹配范围内。
    /// </summary>
    private void CollectNextButtons(Scene policyScene)
    {
        nextButtons.Clear();
        HashSet<GameObject> seen = new HashSet<GameObject>();
        foreach (NodePanelBase node in nodePanels)
        {
            if (node != null && node.btn_Next != null && seen.Add(node.btn_Next))
            {
                nextButtons.Add(node.btn_Next);
            }
        }

        foreach (PanelTopicBase topic in FindObjectsOfType<PanelTopicBase>(true))
        {
            if (topic != null && topic.gameObject.scene == policyScene &&
                topic.btn_Next != null && seen.Add(topic.btn_Next))
            {
                nextButtons.Add(topic.btn_Next);
            }
        }

        foreach (Transform item in FindObjectsOfType<Transform>(true))
        {
            if (item != null && item.gameObject.scene == policyScene &&
                ShouldHideNextButton(item) && seen.Add(item.gameObject))
            {
                nextButtons.Add(item.gameObject);
            }
        }
    }

    /// <summary>
    /// 动态题目出现后，只扫描当前活动节点/题目子树，把新出现的
    /// “下一步”“确定”按钮并入缓存。不执行全场景 FindObjectsOfType。
    /// </summary>
    private void RefreshActiveTopicSubtreeCaches()
    {
        Scene policyScene = gameObject.scene;
        HashSet<GameObject> seen = new HashSet<GameObject>();
        foreach (GameObject button in nextButtons)
        {
            if (button != null) seen.Add(button);
        }

        foreach (Transform confirm in confirmButtons)
        {
            if (confirm != null) seen.Add(confirm.gameObject);
        }

        foreach (NodePanelBase node in nodePanels)
        {
            if (node == null || !node.isActiveAndEnabled || node.activeChild == null ||
                !node.activeChild.isActiveAndEnabled)
            {
                continue;
            }

            PanelTopicBase topic = node.activeChild;
            if (topic.btn_Next != null && topic.gameObject.scene == policyScene &&
                seen.Add(topic.btn_Next))
            {
                nextButtons.Add(topic.btn_Next);
            }

            CollectTopicSubtreeButtons(topic.transform, policyScene, seen);
        }

        // 新题目/新指引的按钮刚刚并入缓存，必须同帧压下去，
        // 否则会先渲染一帧再等下一帧 Update 隐藏。
        HideConfirmButtons();
        HideNextButtons();
    }

    private void CollectTopicSubtreeButtons(
        Transform root, Scene policyScene, HashSet<GameObject> seen)
    {
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
        {
            if (item == null || item.gameObject.scene != policyScene)
            {
                continue;
            }

            GameObject buttonObject = item.gameObject;
            if (ShouldHideNextButton(item) && seen.Add(buttonObject))
            {
                nextButtons.Add(buttonObject);
            }

            if (ShouldHideConfirmButton(item) && seen.Add(buttonObject))
            {
                confirmButtons.Add(item);
            }
        }
    }

    private void ClearCaches()
    {
        confirmButtons.Clear();
        nodePanels.Clear();
        nextButtons.Clear();
        pendingCompletions.Clear();
        m08ScrewSteps.Clear();
        m08LidObjects.Clear();
        m08LidGateEntries.Clear();
        m08LidStep = null;
        m08UnpackGuardConfigured = false;
        m08GateAppliedOnce = false;
        m08LastAuthorizedLidSource = null;
        m08GuardInitPending = false;
        m08GuardRetryCount = 0;
        topicSubtreeScanPending = false;
        cacheDirty = true;
    }

    private void HideConfirmButtons()
    {
        foreach (Transform item in confirmButtons)
        {
            if (item != null) item.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 每帧仅遍历缓存并保持“下一步”隐藏；缓存为空或已销毁的引用会被剔除。
    /// </summary>
    private void HideNextButtons()
    {
        for (int index = nextButtons.Count - 1; index >= 0; index--)
        {
            GameObject button = nextButtons[index];
            if (button == null)
            {
                nextButtons.RemoveAt(index);
                continue;
            }

            if (button.activeSelf) button.SetActive(false);
        }
    }

    public static bool ShouldHideNextButton(Transform item)
    {
        return item != null && item.name == NextButtonName;
    }

    public static bool IsStandardNextButtonName(string name)
    {
        return name == NextButtonName;
    }

    /// <summary>
    /// “完成题目”事件驱动的待处理队列。同一题目只入队一次，
    /// 延迟一帧处理以等待 isSubmit 等完成状态写入。
    /// </summary>
    private void QueueTopicCompletion(string topicName, int frame)
    {
        if (string.IsNullOrEmpty(topicName)) return;
        for (int index = 0; index < pendingCompletions.Count; index++)
        {
            if (pendingCompletions[index].TopicName == topicName) return;
        }

        pendingCompletions.Add(new PendingTopicCompletion(topicName, frame));
        MarkFlowDirty();
    }

    /// <summary>
    /// 处理上一帧入队的已完成题目。若题目（M02、视频题等）已自行切题，
    /// 活动题目不再匹配，就不再重复推进。
    /// </summary>
    private void ProcessPendingTopicCompletions(int currentFrame)
    {
        for (int index = pendingCompletions.Count - 1; index >= 0; index--)
        {
            PendingTopicCompletion pending = pendingCompletions[index];
            if (pending.Frame >= currentFrame) continue;

            pendingCompletions.RemoveAt(index);
            TryAdvanceCompletedTopic(pending.TopicName);
        }
    }

    private bool TryAdvanceCompletedTopic(string completedTopicName)
    {
        foreach (NodePanelBase node in nodePanels)
        {
            NextAdvanceMode mode = ResolveNextAdvance(node, completedTopicName);
            switch (mode)
            {
                case NextAdvanceMode.NextTopic:
                    node.NextTopic();
                    return true;
                case NextAdvanceMode.ClickNext:
                    node.ClickNext();
                    return true;
            }
        }

        return false;
    }

    private static NextAdvanceMode ResolveNextAdvance(
        NodePanelBase node, string completedTopicName)
    {
        if (node == null || !node.isActiveAndEnabled || node.datas == null ||
            node.activeChild == null || !node.activeChild.isActiveAndEnabled ||
            node.activeChild.datas == null)
        {
            return NextAdvanceMode.None;
        }

        // 题目已由自身逻辑推进（如 M02 的 ClickNext 自动切题或视频题跳过），
        // 此时活动题目已变化，不能再推进一次。
        if (node.activeChild.datas.name != completedTopicName)
        {
            return NextAdvanceMode.AlreadyAdvanced;
        }

        if (node.datas.childData == null)
        {
            return NextAdvanceMode.ClickNext;
        }

        int nextTopicIndex = node.activeChild.datas.GetIndexSelf() + 1;
        return nextTopicIndex < node.datas.childData.Count
            ? NextAdvanceMode.NextTopic
            : NextAdvanceMode.ClickNext;
    }

    /// <summary>
    /// 只匹配两个标准确认按钮名称：“确定”和 MRTK 对话框的 “PositiveButton”。
    /// 名称不同的业务按钮（“确定按钮”“M02_IP_Apply”“跳过”“UPan”等）
    /// 一律不匹配，避免隐藏屏幕内部与背包的真实交互。
    /// </summary>
    public static bool IsStandardConfirmButtonName(string name)
    {
        return TexCore_StandardButtonPolicy.IsStandardConfirmButtonName(name);
    }

    public static bool ShouldHideConfirmButton(Transform item)
    {
        return TexCore_StandardButtonPolicy.IsStandardConfirmButton(item);
    }

    private void SkipEmptyActiveSteps()
    {
        if (stepObjects == null || stepObjects.interacterObjs == null)
        {
            return;
        }

        foreach (NodePanelBase node in nodePanels)
        {
            if (node == null || !node.isActiveAndEnabled || node.activeChild == null ||
                !node.activeChild.isActiveAndEnabled || node.datas == null ||
                node.activeChild.datas == null || node.activeChild.topicData == null)
            {
                continue;
            }

            int nodeIndex = node.datas.GetIndexSelf();
            int topicIndex = node.activeChild.datas.GetIndexSelf();
            if (nodeIndex < 0 || topicIndex < 0)
            {
                continue;
            }

            while (TryGetEmptyStep(nodeIndex, topicIndex, node.activeChild.stepIndex))
            {
                node.activeChild.stepIndex++;
                if (node.activeChild.topicData.tasks == null ||
                    node.activeChild.stepIndex >= node.activeChild.topicData.tasks.Count)
                {
                    node.NextTopic();
                    break;
                }

                TexCore_EventCenter.Broadcast(
                    TexCore_EventType.设置物体状态,
                    nodeIndex,
                    topicIndex,
                    node.activeChild.stepIndex);
                TexCore_EventCenter.Broadcast<string>(
                    TexCore_EventType.显示标题,
                    GetStepTitle(node.activeChild, node.activeChild.stepIndex));
            }
        }
    }

    private bool TryGetEmptyStep(int nodeIndex, int topicIndex, int stepIndex)
    {
        if (nodeIndex >= stepObjects.interacterObjs.Count ||
            topicIndex >= stepObjects.interacterObjs[nodeIndex].nodeObjs.Count)
        {
            return false;
        }

        // M02 replaces its serialized reporters with the authored U-disk and
        // screen buttons at runtime.  Treating that short binding window as a
        // completed empty step skips the player's required interaction.
        if (IsM02RuntimeBoundNode(nodeIndex))
        {
            return false;
        }

        // M05 reporters are late-bound when 03 is loaded.  The empty scene
        // placeholders must remain active until that binding has completed.
        if (nodeIndex == 4 && topicIndex >= 4 && topicIndex <= 6)
        {
            return false;
        }

        foreach (StepObjs step in stepObjects.interacterObjs[nodeIndex].nodeObjs[topicIndex].topicObjs)
        {
            if (step.stepIndex == stepIndex)
            {
                return step.stepObjs == null || step.stepObjs.Count == 0;
            }
        }

        return false;
    }

    private bool IsM02RuntimeBoundNode(int nodeIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= stepObjects.interacterObjs.Count)
        {
            return false;
        }

        NodeObjs node = stepObjects.interacterObjs[nodeIndex];
        return node != null && node.nodePrefab != null &&
               node.nodePrefab.name == M02NodeName;
    }

    private static string GetStepTitle(PanelTopicBase topic, int stepIndex)
    {
        if (topic.topicData.hintTexts == null || stepIndex < 0 ||
            stepIndex >= topic.topicData.hintTexts.Count)
        {
            return string.Empty;
        }

        return topic.topicData.hintTexts[stepIndex];
    }
}

/// <summary>
/// 标准流程按钮（绿色“下一步”与白底绿色“确定”）的统一识别规则。
/// </summary>
/// <remarks>
/// 这些按钮在预制体里默认是激活的，或者由旧的 <c>SetActive(true)</c> 事件
/// 打开，因此任何“下一帧再隐藏”的做法都会先渲染出一帧。
/// 名称不同的真实业务按钮（<c>UPan</c>、<c>M02_IP_Apply</c>、<c>跳过</c>、
/// 背包按钮、模块导航按钮等）不在识别范围内。
/// </remarks>
public static class TexCore_StandardButtonPolicy
{
    public const string NextButtonName = "下一步";
    public const string ConfirmButtonName = "确定";
    public const string PositiveButtonName = "PositiveButton";
    public const string DialogConfirmButtonName = "确定按钮";

    /// <summary>MRTK/BPF Dialog 运行期可能使用的确认按钮别名。</summary>
    private static readonly string[] DialogButtonAliases =
    {
        PositiveButtonName,
        "Positive"
    };

    /// <summary>对白/指引根节点名称，用于限定“确定按钮”的归属。</summary>
    private static readonly string[] DialogRootNames =
    {
        "BPF_Dialog_168x140mm",
        "Dialog_168x140mm",
        "Dialog_168x88mm",
        "Dialog",
        "ButtonParent",
        "UI-指引"
    };

    /// <summary>
    /// 是否为标准绿色“下一步”。该按钮在所有已覆盖场景里都只承担流程推进，
    /// 没有真实业务语义，因此只按名称匹配。
    /// </summary>
    public static bool IsStandardNextButtonName(string name)
    {
        return name == NextButtonName;
    }

    public static bool IsStandardNextButton(Transform item)
    {
        return item != null && IsStandardNextButtonName(item.name);
    }

    /// <summary>
    /// 仅按名称判断是否为标准确认按钮。名称不同的业务按钮一律不匹配，
    /// 例如“确定按钮”“M02_IP_Apply”“跳过”“UPan”。
    /// </summary>
    public static bool IsStandardConfirmButtonName(string name)
    {
        if (name == ConfirmButtonName)
        {
            return true;
        }

        for (int index = 0; index < DialogButtonAliases.Length; index++)
        {
            if (name == DialogButtonAliases[index])
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 按名称与父级路径判断是否为需要隐藏的标准确认按钮。
    /// </summary>
    /// <remarks>
    /// “确定按钮”只在对白/指引根节点下才算标准按钮：M09 的 <c>确定按钮</c>
    /// 是“向系统发送加入机器人”的引导输入，属于会先显示再被隐藏的闪现来源；
    /// 而场景里同名的其它按钮可能是真实业务入口，所以必须结合父级路径判断，
    /// 不能只按名称全场隐藏。
    /// </remarks>
    public static bool IsStandardConfirmButton(Transform item)
    {
        if (item == null)
        {
            return false;
        }

        if (IsStandardConfirmButtonName(item.name))
        {
            return true;
        }

        return item.name == DialogConfirmButtonName && IsUnderDialogRoot(item);
    }

    /// <summary>
    /// 向上查找对白/指引根节点。只沿父级链判断，不执行任何全场景查找。
    /// </summary>
    public static bool IsUnderDialogRoot(Transform item)
    {
        for (Transform current = item; current != null; current = current.parent)
        {
            string name = current.name;
            for (int index = 0; index < DialogRootNames.Length; index++)
            {
                if (name == DialogRootNames[index])
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 把 <paramref name="root"/> 子树里仍处于激活状态的标准按钮全部隐藏。
    /// 只遍历传入子树，供题目与指引根对象在启用当帧调用。
    /// </summary>
    public static void HideInSubtree(Transform root, List<Transform> buffer = null)
    {
        if (root == null)
        {
            return;
        }

        List<Transform> found = buffer ?? new List<Transform>();
        found.Clear();
        root.GetComponentsInChildren(true, found);
        for (int index = 0; index < found.Count; index++)
        {
            Transform item = found[index];
            if (item == null || !item.gameObject.activeSelf)
            {
                continue;
            }

            if (IsStandardNextButton(item) || IsStandardConfirmButton(item))
            {
                item.gameObject.SetActive(false);
            }
        }

        found.Clear();
    }
}

/// <summary>
/// 挂在会被动态启用的指引/题目根对象上：根对象启用的同一帧就把子树内的
/// 标准“下一步/确定”隐藏，避免出现“先显示一帧再被场景策略压下去”的闪现。
/// </summary>
[DisallowMultipleComponent]
public sealed class TexCore_StandardButtonGuard : MonoBehaviour
{
    private readonly List<Transform> buffer = new List<Transform>();

    private void OnEnable()
    {
        Apply();
    }

    private void Start()
    {
        // 指引根有时由协程在场景加载后启用，Start 再兜一次同帧隐藏。
        Apply();
    }

    /// <summary>
    /// 立即隐藏本子树内的标准按钮。可由外部脚本在动态启用后主动调用。
    /// </summary>
    public void Apply()
    {
        TexCore_StandardButtonPolicy.HideInSubtree(transform, buffer);
    }
}
