using System;
using System.Collections.Generic;
using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene-local correction for the 08 unpacking topic, used by both the 03
/// mainline scene and the standalone 0809 scene. The shared TexCore_StepObj
/// marks later steps as finished when it receives the current step event, and
/// it hides a screw as soon as that screw's step finishes. This component
/// restores the authored 1-to-8 order without changing the shared framework
/// implementation, and keeps all eight screws visible for the whole unpacking
/// step: only interaction is restricted, never visibility.
/// </summary>
[DisallowMultipleComponent]
public sealed class TexCore_0809M08ScrewSequence : MonoBehaviour
{
    private const string MainlineSceneName = "03SingleplayerScene";
    private const string SceneName = "0809";
    private const string NodeName = "08充电桩安装";
    private const string TopicName = "00开箱取出充电桩";
    private const string QrTopicName = "01粘贴二维码";
    private const string ExpansionTopicName = "02安装膨胀螺丝";

    private const string StepPrefix =
        "08_00_使用手电钻钻开包装箱螺丝";

    private const int ScrewCount = 8;

    private const string LidStepName = "08_00_移开包装箱盖";
    private const string TakeOutStepName = "08_00_取出箱内充电桩";

    private readonly List<TexCore_OrderedPlacementStep> screwSteps =
        new List<TexCore_OrderedPlacementStep>(ScrewCount);
    private readonly List<GameObject> skipStepUIs = new List<GameObject>();
    private TexCore_ClickObjectStep lidStep;
    private TexCore_ClickObjectStep takeOutStep;
    private GameObject stationCopy;
    private bool lidAssemblyNormalized;
    private float diagnosticNextTime;
    private bool qrTopicAdvanceRequested;

    /// <summary>已经为哪一颗螺丝显式触发过动画；-1 表示还没有触发过。</summary>
    private int animatorTriggeredIndex = -1;

    /// <summary>入场抑制只执行一次，避免每帧覆盖动画状态。</summary>
    private bool screwAnimatorsSuppressed;

    private void LateUpdate()
    {
        // 0809 独立场景专用的修正只在 0809 生效，03 主线流程保持不变。
        if (IsStandaloneScene())
        {
            // 临时诊断：定位箱盖/充电桩问题后删除。
            LogDiagnostics();

            DisableSkipStepUIs();

            // 4 个侧盖与顶盖合为一整块，并让箱体外壳不再独立可抓。
            NormalizeBoxLidAssembly();

            // 用户移开包装箱盖后，保持箱内充电桩可见可交互。
            KeepChargingStationVisible();
        }

        // 03 主线和 0809 都要执行 08 开箱螺丝的顺序控制。
        if (!IsScrewSequenceScene())
        {
            return;
        }

        NodePanelBase m08Node = FindActive08Node();
        bool unpackTopicActive = IsUnpackTopicActive(m08Node);

        if (IsStandaloneScene())
        {
            // 0809 不再要求玩家手动“取出箱内充电桩”：箱盖移开后该步骤
            // 一旦成为当前步骤就立即按正常业务路径完成。
            CompleteTakeOutStep(unpackTopicActive);

            // 开箱题全部完成后自动进入“08_01_粘贴二维码”，玩家不需要再自己
            // 去找题目的“下一步”按钮。共享题目与配置保持不变，03 主线不受影响。
            AdvancePastUnpackTopic(m08Node);

            // “08_01_粘贴二维码”完成后同样自动进入“02安装膨胀螺丝”。
            TryAdvanceFromQrTopic(m08Node);
        }

        // 解析 8 颗螺丝。
        ResolveScrewSteps();

        if (screwSteps.Count != ScrewCount)
        {
            return;
        }

        // 入场抑制：8 颗螺丝的动画一律先关闭，禁止进入关卡后自动播放。
        SuppressScrewAnimatorAutoPlay();

        // 找到第一颗尚未完成的螺丝。
        int firstIncomplete = FindFirstIncomplete();

        // 交互状态与动画反馈：只有“00开箱取出充电桩”主题激活时才按照
        // 1 → 8 驱动。题目未激活时不干预共享框架的步骤状态。
        if (unpackTopicActive)
        {
            ApplyScrewInteractionStates(firstIncomplete);
            ApplyScrewAnimators(firstIncomplete);
        }

        // 显示状态独立处理，并且必须放在所有 SetState 调用之后。
        // 共享框架在收到设置物体状态广播后会把后续螺丝标记为“已完成”，
        // 进而隐藏它们；这里在每帧末尾统一恢复，避免被再次覆盖。
        ApplyScrewVisibility();
    }

    /// <summary>
    /// 按照 1 → 8 的顺序设置 8 颗螺丝的交互状态。
    /// 只有第一颗未完成的螺丝进入“等待触发”，后续螺丝保持“未完成”：
    /// 未完成不等于隐藏，后续螺丝仍然可见，只是不能提前交互。
    /// </summary>
    private void ApplyScrewInteractionStates(int firstIncomplete)
    {
        for (int index = 0; index < screwSteps.Count; index++)
        {
            TexCore_OrderedPlacementStep step = screwSteps[index];

            if (step == null)
            {
                continue;
            }

            FinishState desiredState = step.WasCompletedByUser
                ? FinishState.已完成
                : index == firstIncomplete
                    ? FinishState.等待触发
                    : FinishState.未完成;

            SetStepState(step, desiredState);
        }
    }

    /// <summary>
    /// 关闭 0809 场景题目面板中用于跳过真实交互步骤的“下一步”平面按钮。
    /// 题目按钮仍可能通过持久化 SetActive(true) 事件尝试显示它们，
    /// 因此每帧在场景专用脚本中再次关闭，避免影响 03 主场景的共享逻辑。
    /// </summary>
    private void DisableSkipStepUIs()
    {
        if (skipStepUIs.Count == 0)
        {
            Transform[] transforms = FindObjectsOfType<Transform>(true);

            foreach (Transform value in transforms)
            {
                if (value != null && value.name == "Next")
                {
                    skipStepUIs.Add(value.gameObject);
                }
            }
        }

        foreach (GameObject value in skipStepUIs)
        {
            if (value == null)
            {
                continue;
            }

            // 禁用按钮及其子物体上的交互、动画和绘制组件。
            // 这样即使题目面板的旧 SetActive(true) 事件再次激活根物体，
            // 也不会出现可点击或可见的跳步 UI。
            foreach (Behaviour component in
                     value.GetComponentsInChildren<Behaviour>(true))
            {
                if (component != null)
                {
                    component.enabled = false;
                }
            }

            if (value.activeSelf)
            {
                value.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 查找并按照 1~8 的顺序解析螺丝步骤。
    /// </summary>
    private void ResolveScrewSteps()
    {
        if (screwSteps.Count == ScrewCount)
        {
            return;
        }

        screwSteps.Clear();

        TexCore_OrderedPlacementStep[] allSteps =
            FindObjectsOfType<TexCore_OrderedPlacementStep>(true);

        for (int index = 1; index <= ScrewCount; index++)
        {
            string expectedName = StepPrefix + index;

            foreach (TexCore_OrderedPlacementStep step in allSteps)
            {
                if (step != null && step.name == expectedName)
                {
                    screwSteps.Add(step);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 查找第一颗尚未完成的螺丝。
    /// </summary>
    private int FindFirstIncomplete()
    {
        for (int index = 0; index < screwSteps.Count; index++)
        {
            if (!screwSteps[index].WasCompletedByUser)
            {
                return index;
            }
        }

        return screwSteps.Count;
    }

    /// <summary>
    /// 只负责显示状态：8 颗螺丝在整个开箱步骤中始终可见，包括已经拧下的螺丝。
    /// 顺序限制只通过交互状态实现，不能用隐藏物体代替。
    /// 使用步骤自带的放置目标作为螺丝本体：08 场景中 8 个步骤的 Target
    /// 正好按顺序对应 chaixibufen 下的 ZiGongSi～ZiGongSi15。
    /// </summary>
    private void ApplyScrewVisibility()
    {
        foreach (TexCore_OrderedPlacementStep step in screwSteps)
        {
            if (step == null || step.Target == null)
            {
                continue;
            }

            GameObject screw = step.Target.gameObject;

            if (!screw.activeSelf)
            {
                screw.SetActive(true);
            }

            foreach (Renderer renderer in
                     screw.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null && !renderer.enabled)
                {
                    renderer.enabled = true;
                }
            }
        }
    }

    /// <summary>
    /// 入场抑制：进入关卡后 8 颗螺丝的动画都不允许自动播放。
    /// 步骤解析完成后统一关闭一次，之后只有当前步骤会显式打开。
    /// </summary>
    private void SuppressScrewAnimatorAutoPlay()
    {
        if (screwAnimatorsSuppressed)
        {
            return;
        }

        screwAnimatorsSuppressed = true;

        foreach (TexCore_OrderedPlacementStep step in screwSteps)
        {
            Animator animator = ResolveScrewAnimator(step);

            if (animator != null && animator.enabled)
            {
                animator.enabled = false;
            }
        }
    }

    /// <summary>
    /// 动画反馈：只保留“拧下螺丝”的运动效果，并且只在当前第一颗未完成螺丝
    /// 开始它的步骤时显式触发一次。写 Renderer.enabled / GameObject.active 的
    /// 曲线已经在构建阶段从动画片段里移除，所以动画不可能再把螺丝隐藏。
    /// </summary>
    private void ApplyScrewAnimators(int firstIncomplete)
    {
        for (int index = 0; index < screwSteps.Count; index++)
        {
            Animator animator = ResolveScrewAnimator(screwSteps[index]);

            if (animator == null)
            {
                continue;
            }

            if (index != firstIncomplete)
            {
                if (animator.enabled)
                {
                    animator.enabled = false;
                }

                continue;
            }

            if (animatorTriggeredIndex == index)
            {
                continue;
            }

            animatorTriggeredIndex = index;

            // 先关闭再打开，保证动画从第 0 帧重新播放一次，
            // 而不是沿用入场时可能已经播完的旧状态。
            animator.enabled = false;
            animator.enabled = true;
        }
    }

    /// <summary>
    /// 取螺丝自身的 Animator。08 的 8 颗螺丝各自带一个控制器，
    /// 都挂在螺丝本体上，所以直接按步骤目标查找。
    /// </summary>
    private static Animator ResolveScrewAnimator(
        TexCore_OrderedPlacementStep step)
    {
        if (step == null || step.Target == null)
        {
            return null;
        }

        return step.Target.GetComponent<Animator>();
    }

    /// <summary>
    /// 03 主线和 0809 都执行 08 开箱螺丝的顺序控制。
    /// </summary>
    private static bool IsScrewSequenceScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        return sceneName == MainlineSceneName || sceneName == SceneName;
    }

    /// <summary>
    /// 只有 0809 独立场景使用“移开箱盖后自动取桩 / 自动换题”等场景专用修正，
    /// 03 主线保持共享框架原本的题目流程。
    /// </summary>
    private static bool IsStandaloneScene()
    {
        return SceneManager.GetActiveScene().name == SceneName;
    }

    /// <summary>
    /// 只设置交互状态。显示状态由 ApplyScrewVisibility 统一处理，
    /// 避免状态切换和可见性恢复互相覆盖。
    /// </summary>
    private void SetStepState(
        TexCore_OrderedPlacementStep step,
        FinishState desiredState)
    {
        if (step == null)
        {
            return;
        }

        TexCore_InteracterBase[] components =
            step.GetComponents<TexCore_InteracterBase>();

        foreach (TexCore_InteracterBase component in components)
        {
            if (component != null && component.state != desiredState)
            {
                component.SetState(desiredState);
            }
        }
    }

    /// <summary>
    /// 找到当前激活的“08充电桩安装”节点面板。
    /// </summary>
    private static NodePanelBase FindActive08Node()
    {
        NodePanelBase[] nodes = FindObjectsOfType<NodePanelBase>(true);

        foreach (NodePanelBase node in nodes)
        {
            if (node == null ||
                !node.isActiveAndEnabled ||
                node.datas == null ||
                node.datas.name != NodeName)
            {
                continue;
            }

            return node;
        }

        return null;
    }

    /// <summary>
    /// 判断 08 节点当前是否正停在“00开箱取出充电桩”题目上。
    /// </summary>
    private static bool IsUnpackTopicActive(NodePanelBase node)
    {
        return node != null &&
               node.activeChild != null &&
               node.activeChild.isActiveAndEnabled &&
               node.activeChild.datas != null &&
               node.activeChild.datas.name == TopicName;
    }

    /// <summary>
    /// 开箱题（8 颗螺丝 + 移开箱盖 + 取出充电桩）全部完成后，自动走题目的
    /// “下一步”进入“08_01_粘贴二维码”。框架原本要求玩家自己点题目里的
    /// “下一步”按钮才能换题，0809 里这一步经常卡住，所以这里直接调用同一个
    /// 入口 NodePanelBase.ClickNext()，流程与玩家点按钮完全一致。
    /// 换题后 activeChild 不再是开箱题，本方法自然停止。
    /// </summary>
    private static void AdvancePastUnpackTopic(NodePanelBase m08Node)
    {
        if (!IsUnpackTopicActive(m08Node))
        {
            return;
        }

        PanelTopicBase topic = m08Node.activeChild;
        if (topic == null || !topic.isSubmit)
        {
            return;
        }

        m08Node.ClickNext();
    }

    /// <summary>
    /// “08_01_粘贴二维码”完成后自动进入“02安装膨胀螺丝”。
    /// 01粘贴二维码.prefab 的 isNextShow = 1，框架完成题目后只会弹出
    /// “下一步”按钮等待玩家点击，0809 里这一步同样会卡住；这里直接调用
    /// 同一个入口 NodePanelBase.ClickNext()，走的是与玩家点按钮一致的流程。
    /// 只处理一个题目：切换到 02安装膨胀螺丝 后 activeChild 不再是二维码题，
    /// 本方法随即复位状态，不会继续往后跳。
    /// </summary>
    private void TryAdvanceFromQrTopic(NodePanelBase node)
    {
        if (node == null || node.activeChild == null ||
            node.activeChild.datas == null)
        {
            qrTopicAdvanceRequested = false;
            return;
        }

        PanelTopicBase topic = node.activeChild;

        if (topic.datas.name != QrTopicName)
        {
            qrTopicAdvanceRequested = false;
            return;
        }

        if (!topic.isSubmit || qrTopicAdvanceRequested)
        {
            return;
        }

        qrTopicAdvanceRequested = true;

        Debug.Log("[0809] 二维码步骤已完成，自动进入 " + ExpansionTopicName + "。");

        node.ClickNext();

        if (node.activeChild == null ||
            node.activeChild.datas == null ||
            node.activeChild.datas.name != ExpansionTopicName)
        {
            qrTopicAdvanceRequested = false;
            Debug.LogError("[0809] 二维码完成后未能切换到 " + ExpansionTopicName + "。");
            return;
        }

        Debug.Log("[0809] 当前 Topic 已切换为 " + ExpansionTopicName + "。");
    }

    /// <summary>
    /// 8 颗螺丝全部完成、用户移开包装箱盖之后，
    /// 让箱内充电桩保持可见、可交互，不被背包控制器隐藏，
    /// 以便玩家自行把充电桩取出后再进行“08_01_粘贴二维码”。
    /// </summary>
    private void KeepChargingStationVisible()
    {
        ResolveLidReferences();

        if (lidStep == null || !lidStep.WasCompletedByUser || stationCopy == null)
        {
            return;
        }

        if (!stationCopy.activeSelf)
        {
            stationCopy.SetActive(true);
        }

        foreach (Collider value in
                 stationCopy.GetComponentsInChildren<Collider>(true))
        {
            if (value != null)
            {
                value.enabled = true;
            }
        }

        foreach (ObjectManipulator value in
                 stationCopy.GetComponentsInChildren<ObjectManipulator>(true))
        {
            if (value != null)
            {
                value.enabled = true;
            }
        }
    }

    /// <summary>
    /// 0809 专用的开箱收尾：箱盖已被用户移开，且“08_00_取出箱内充电桩”
    /// 刚好成为当前步骤时，直接走共享业务入口完成它，让题目立即进入
    /// “08_01_粘贴二维码”，玩家不需要再单独执行取桩交互。
    /// 只在 0809 生效：本组件的 LateUpdate 已经按场景名过滤，
    /// 共享的 03 题目 Prefab 与 Config.xml 保持原样。
    /// </summary>
    private void CompleteTakeOutStep(bool unpackTopicActive)
    {
        if (!unpackTopicActive)
        {
            return;
        }

        ResolveLidReferences();

        if (lidStep == null || !lidStep.WasCompletedByUser ||
            takeOutStep == null || takeOutStep.WasCompletedByUser ||
            takeOutStep.state != FinishState.等待触发)
        {
            return;
        }

        TexCore_BusinessManager.OnClick(takeOutStep.gameObject);
    }

    /// <summary>
    /// 解析“08_00_移开包装箱盖”步骤、其后续“08_00_取出箱内充电桩”步骤，
    /// 以及箱内充电桩对象本身；只解析一次并缓存。
    /// </summary>
    private void ResolveLidReferences()
    {
        if (lidStep != null && takeOutStep != null && stationCopy != null)
        {
            return;
        }

        foreach (TexCore_ClickObjectStep step in
                 FindObjectsOfType<TexCore_ClickObjectStep>(true))
        {
            if (step == null)
            {
                continue;
            }

            if (lidStep == null && step.name == LidStepName)
            {
                lidStep = step;
            }
            else if (takeOutStep == null && step.name == TakeOutStepName)
            {
                takeOutStep = step;
            }
        }

        // 直接取步骤自己的交互对象，避免同名物体认错。
        if (stationCopy == null && takeOutStep != null)
        {
            stationCopy = takeOutStep.InteractionSource;
        }
    }

    /// <summary>
    /// 包装箱盖由“顶盖 DingGai + 4 个侧盖 CeGai”组成，但它们在场景里是
    /// xiangti 的同级子物体，且只有顶盖带 Rigidbody；抓住顶盖只会把顶盖
    /// 单独拿走，侧盖留在原地。这里把侧盖统一挂到顶盖下，作为一整块移开。
    /// 同时取消箱体外壳自身的可抓取与碰撞：否则玩家抓到的是外壳，
    /// “08_00_移开包装箱盖”步骤监听不到，步骤不会完成；去掉外壳 Collider
    /// 也能避免与顶盖解穿插时把顶盖弹飞。
    /// </summary>
    private void NormalizeBoxLidAssembly()
    {
        if (lidAssemblyNormalized)
        {
            return;
        }

        ResolveLidReferences();

        if (lidStep == null)
        {
            return;
        }

        Transform boxLid = lidStep.InteractionSource != null
            ? lidStep.InteractionSource.transform
            : null;
        Transform shell = boxLid != null ? boxLid.parent : null;

        // 只有确认是包装箱的 xiangti 结构时才动手，避免认错层级。
        if (boxLid == null || shell == null || shell.name != "xiangti")
        {
            return;
        }

        for (int index = shell.childCount - 1; index >= 0; index--)
        {
            Transform child = shell.GetChild(index);

            if (child == null || child == boxLid ||
                !child.name.StartsWith("CeGai", StringComparison.Ordinal))
            {
                continue;
            }

            child.SetParent(boxLid, true);
        }

        foreach (ObjectManipulator manipulator in
                 shell.GetComponents<ObjectManipulator>())
        {
            if (manipulator != null)
            {
                manipulator.enabled = false;
            }
        }

        foreach (Collider collider in shell.GetComponents<Collider>())
        {
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        lidAssemblyNormalized = true;
    }

    // TEMPORARY diagnostic. Remove once the lid / charging-station issue is fixed.
    private void LogDiagnostics()
    {
        if (Time.unscaledTime < diagnosticNextTime)
        {
            return;
        }

        diagnosticNextTime = Time.unscaledTime + 1f;

        Transform boxLid = lidStep != null && lidStep.InteractionSource != null
            ? lidStep.InteractionSource.transform
            : null;
        Transform shell = boxLid != null ? boxLid.parent : null;

        string shellState = "-";
        if (shell != null)
        {
            ObjectManipulator shellManipulator = shell.GetComponent<ObjectManipulator>();
            Collider shellCollider = shell.GetComponent<Collider>();
            shellState = "manip=" + (shellManipulator != null
                    ? shellManipulator.enabled.ToString()
                    : "none") +
                " col=" + (shellCollider != null
                    ? shellCollider.enabled.ToString()
                    : "none");
        }

        string lidState = "-";
        if (boxLid != null)
        {
            ObjectManipulator lidManipulator = boxLid.GetComponent<ObjectManipulator>();
            Collider lidCollider = boxLid.GetComponent<Collider>();
            lidState = "manip=" + (lidManipulator != null
                    ? lidManipulator.enabled.ToString()
                    : "none") +
                " col=" + (lidCollider != null
                    ? lidCollider.enabled.ToString()
                    : "none");
        }

        Debug.Log("[0809DIAG] scene=" + SceneManager.GetActiveScene().name +
            " lidStep=" + (lidStep != null ? lidStep.name : "NULL") +
            " lidStepState=" + (lidStep != null ? lidStep.state.ToString() : "-") +
            " lidUserDone=" + (lidStep != null ? lidStep.WasCompletedByUser.ToString() : "-") +
            " takeOutState=" + (takeOutStep != null ? takeOutStep.state.ToString() : "NULL") +
            " station=" + (stationCopy != null ? stationCopy.activeSelf.ToString() : "NULL") +
            " normalized=" + lidAssemblyNormalized +
            " shell[" + (shell != null ? shell.name : "-") + "] " + shellState +
            " lid[" + (boxLid != null ? boxLid.name : "-") + "] " + lidState);
    }
}
