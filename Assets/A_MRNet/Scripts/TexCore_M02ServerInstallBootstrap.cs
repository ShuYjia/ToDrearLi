using System;
using System.Collections.Generic;
using System.Linq;
using MixedReality.Toolkit.SpatialManipulation;
using MixedReality.Toolkit.UX;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Bpf.Diagnostics;

/// <summary>
/// Runtime bridge for M02. It reuses the authored WHY_FuWuQi-6 USB, socket and
/// installation UI instead of the SYS_* migration proxies.
/// </summary>
[DisallowMultipleComponent]
public sealed class TexCore_M02ServerInstallBootstrap : MonoBehaviour
{
    private const string SceneName = "03SingleplayerScene";
    private const string NodeName = "02服务器系统安装";
    private const string ModelName = "WHY_FuWuQi-6";
    private const string UiName = "UI_XiTongJingXiangAnZhuang";
    private const string UsbTopic = "02U盘启动盘插入";
    private const string InstallTopic = "03系统安装操作";
    private const string IpTopic = "04设置正确IP";
    private const string CompletionTopic = "05完成整个模块";
    private const string SystemName = "01货到人拣选电商仓储实施安装与调试实训系统";
    private const string CompletedStateName = "NetWork-03_2";

    private static readonly InstallPage[] InstallPages =
    {
        // The authored UI is ordered by the direct-child order of
        // UI_XiTongJingXiangAnZhuang. KeyboardVariantPosition (index 0) is
        // only a shared keyboard anchor and is intentionally not a step.
        new InstallPage(1, "XuanZeXiTong", 0, "M02_Install_F11"),
        new InstallPage(2, "Video1", 0, "M02_Install_Video1"),
        new InstallPage(3, "YuYanXuanZe1", 0, "M02_Install_Language"),
        new InstallPage(4, "ZhuYe2", 0, "M02_Install_LanguageSummary"),
        new InstallPage(5, "RiQi", 0, "M02_Install_DateTime"),
        new InstallPage(6, "JianPan", 0, "M02_Install_Keyboard"),
        new InstallPage(7, "AnZhuangYuan", 0, "M02_Install_Source"),
        new InstallPage(8, "RuanJianXuanZe", 0, "M02_Install_Software"),
        new InstallPage(9, "GNONEME", 0, "M02_Install_GNOME"),
        new InstallPage(10, "ZhuYe3", 0, "M02_Install_InstallationSummary"),
        new InstallPage(11, "AnZhuangWeiZhi", 0, "M02_Install_InstallLocation"),
        new InstallPage(12, "ZhuYe4", 0, "M02_Install_InstallationReady"),
        new InstallPage(13, "ROOTMiMa", 0, "M02_Install_RootPassword"),
        new InstallPage(14, "MiMa", 0, "M02_Install_UserPassword"),
        new InstallPage(15, "Video2", 0, "M02_Install_Video2"),
        new InstallPage(16, "ChongQi", 0, "M02_Install_Restart"),
        new InstallPage(17, "XuKe", 0, "M02_Install_License"),
        new InstallPage(18, "WanChengPeiZhi", 0, "M02_Install_CompleteConfiguration"),
        new InstallPage(19, "YuYan", 0, "M02_Install_LanguageConfirm"),
        new InstallPage(20, "ShuRu", 0, "M02_Install_Input"),
        new InstallPage(21, "YinSi", 0, "M02_Install_Privacy"),
        new InstallPage(22, "ShiQu2", 0, "M02_Install_Region"),
        new InstallPage(23, "ZaiXianZhangHao", 0, "M02_Install_OnlineAccount"),
        new InstallPage(24, "YongHuMing", 0, "M02_Install_UserName"),
        // There are two authored MiMa pages. Use the direct-child index so
        // the second password page cannot resolve to the first one.
        new InstallPage(25, "MiMa", 0, "M02_Install_FinalPassword", 1),
        new InstallPage(26, "WanCheng", 0, "M02_Install_UserSummary"),
        new InstallPage(27, "Start", 0, "M02_Install_Start")
    };

    private static readonly string[] IpPages =
    {
        "YouXian1", "YouXian2", "YouXian3", "YouXian6",
        "YouXian7", "YouXian8", "YouXian9", "YouXian10"
    };

    /// <summary>The three authored address containers of the IP form page.</summary>
    private static readonly string[] IpFieldNames = { "DiZhi", "ZiWang", "WangGuan" };

    // The three authored pages that ask the player to type values. Each entry
    // keeps one TexCore step: the step only completes when every bound input
    // passes its own rule set. The field order follows the mode contract in
    // TexCore_M02InputRules.
    private static readonly InstallInputRule[] InstallInputRules =
    {
        // Step 14, first password page (RootMiMa + QueRenRoot).
        new InstallInputRule(
            13,
            M02InputValidationMode.RootPasswordPair,
            new InputFieldLocator("RootMiMa", "InputField_2"),
            new InputFieldLocator("QueRenRoot", "InputField_3")),
        // Step 24, user identity page (user name + full name).
        new InstallInputRule(
            23,
            M02InputValidationMode.UserIdentityPair,
            new InputFieldLocator("YongHu", "InputField_10"),
            new InputFieldLocator("QuanMing", "InputField_9")),
        // Step 25, second password page (MiMa + QueRen).
        new InstallInputRule(
            24,
            M02InputValidationMode.PasswordPair,
            new InputFieldLocator("MiMa", "InputField_4"),
            new InputFieldLocator("QueRen", "InputField_5"))
    };

    private TexCore_StepObj stepObjects;
    private NodeObjs m02Node;
    private int m02NodeIndex = -1;
    private Transform nodeRoot;
    private Transform modelRoot;
    private Transform uiRoot;
    private Transform usb;
    private Transform usbCollider;
    private Transform completedState;
    private TexCore_OrderedPlacementStep usbStep;
    private TexCore_M02IpConfigurationStep ipStep;
    private TexCore_ClickObjectStep ipGuidance;
    private readonly List<List<TexCore_ClickObjectStep>> ipPageGuidances =
        new List<List<TexCore_ClickObjectStep>>();
    private TexCore_ClickObjectStep completionStep;
    private readonly List<TexCore_InteracterBase> installReporters = new List<TexCore_InteracterBase>();
    private GameObject usbBackpackPanel;
    private bool configured;
    private bool usbSummoned;
    private bool usbConfigured;
    private bool usbInitialStateConfigured;
    private int ipPageIndex;
    private string lastTopicName;
    private string visiblePageName;
    private bool warnedMissingBinding;
    private bool bindingReported;
    private NodePanelBase cachedM02Node;
    private PanelTopicBase cachedM02Topic;
    private NodePanelBase presentedM02Node;
    private PanelTopicBase presentedM02Topic;
    private int presentedTopicStepIndex = -1;
    private bool hasPresentedPresentation;
    private bool presentationDirty = true;
    private bool configCleanupDone;
    private bool configCleanupRetryScheduled;
    private bool eventsRegistered;
    private bool delayedConfigureScheduled;
    private int deferredConfigureRetries;
    private const int MaxDeferredConfigureRetries = 40;


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallIntoScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !IsTargetScene(scene.name)) return;
        TexCore_StepObj steps = FindSceneComponent<TexCore_StepObj>("StepObjs");
        if (steps != null && steps.GetComponent<TexCore_M02ServerInstallBootstrap>() == null)
            steps.gameObject.AddComponent<TexCore_M02ServerInstallBootstrap>();
    }

    private static bool IsTargetScene(string sceneName)
    {
        return string.Equals(sceneName, SceneName, StringComparison.Ordinal) ||
               string.Equals(sceneName, TexCore_LoadScene.Test09SceneName, StringComparison.Ordinal);
    }

    private void Awake()
    {
        RegisterEvents();
        TryCleanConfigOnce();
        ScheduleConfigCleanupRetry();
        Configure();
        ScheduleDelayedConfigure();
    }

    private void OnEnable()
    {
        UnregisterEvents();
        RegisterEvents();
        deferredConfigureRetries = 0;
        TryCleanConfigOnce();
        ScheduleConfigCleanupRetry();
        ScheduleDelayedConfigure();
        presentationDirty = true;
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(DelayedConfigure));
        CancelInvoke(nameof(DeferredConfigCleanup));
        delayedConfigureScheduled = false;
        configCleanupRetryScheduled = false;
        UnregisterEvents();
        cachedM02Node = null;
        cachedM02Topic = null;
        presentedM02Node = null;
        presentedM02Topic = null;
        presentedTopicStepIndex = -1;
        hasPresentedPresentation = false;
        presentationDirty = true;
    }

    private void Update()
    {
        if (!configured && !usbConfigured) return;
        if (!presentationDirty) return;

        presentationDirty = false;
        // The USB backpack is usable before all of the later screen buttons
        // finish their delayed binding. Keep its first step visible while the
        // remaining setup retries instead of leaving the player with no U盘.
        RefreshPresentation();
    }

    private void RegisterEvents()
    {
        if (eventsRegistered) return;
        TexCore_EventCenter.AddListener<string>(TexCore_EventType.更新节点, OnFlowEvent);
        TexCore_EventCenter.AddListener<string>(TexCore_EventType.更新题目, OnFlowEvent);
        TexCore_EventCenter.AddListener<string>(TexCore_EventType.完成题目, OnFlowEvent);
        TexCore_EventCenter.AddListener<int, int, int>(
            TexCore_EventType.设置物体状态, OnObjectStateChanged);
        SceneManager.sceneLoaded += OnSceneLoaded;
        eventsRegistered = true;
    }

    private void UnregisterEvents()
    {
        if (!eventsRegistered) return;
        TryRemoveNodeListener(TexCore_EventType.更新节点);
        TryRemoveNodeListener(TexCore_EventType.更新题目);
        TryRemoveNodeListener(TexCore_EventType.完成题目);
        try
        {
            TexCore_EventCenter.RemoveListener<int, int, int>(
                TexCore_EventType.设置物体状态, OnObjectStateChanged);
        }
        catch (Exception)
        {
            // Another scene component may have cleared the shared event table.
        }
        SceneManager.sceneLoaded -= OnSceneLoaded;
        eventsRegistered = false;
    }

    private void TryRemoveNodeListener(TexCore_EventType eventType)
    {
        try
        {
            TexCore_EventCenter.RemoveListener<string>(eventType, OnFlowEvent);
        }
        catch (Exception)
        {
            // Another scene component may have cleared the shared event table.
        }
    }

    private void OnFlowEvent(string _)
    {
        cachedM02Node = null;
        cachedM02Topic = null;
        presentationDirty = true;
        TryCleanConfigOnce();
        ScheduleConfigCleanupRetry();
        if (!configured) Configure();
    }

    private void OnObjectStateChanged(int _, int __, int ___)
    {
        cachedM02Topic = null;
        presentationDirty = true;
        TryCleanConfigOnce();
        ScheduleConfigCleanupRetry();
        if (!configured) Configure();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode _)
    {
        if (scene != gameObject.scene) return;
        cachedM02Node = null;
        cachedM02Topic = null;
        presentedM02Node = null;
        presentedM02Topic = null;
        presentedTopicStepIndex = -1;
        hasPresentedPresentation = false;
        presentationDirty = scene.name == SceneName;
        if (presentationDirty)
        {
            TryCleanConfigOnce();
            ScheduleConfigCleanupRetry();
            if (!configured) Configure();
        }
    }

    private void ScheduleDelayedConfigure()
    {
        if (delayedConfigureScheduled || configured ||
            deferredConfigureRetries >= MaxDeferredConfigureRetries) return;
        deferredConfigureRetries++;
        delayedConfigureScheduled = true;
        Invoke(nameof(DelayedConfigure), 0.25f);
    }

    private void DelayedConfigure()
    {
        delayedConfigureScheduled = false;
        if (!configured)
        {
            TryCleanConfigOnce();
            Configure();
            if (!configured) ScheduleDelayedConfigure();
        }
    }

    private void ScheduleConfigCleanupRetry()
    {
        if (configCleanupDone || configCleanupRetryScheduled) return;
        configCleanupRetryScheduled = true;
        InvokeRepeating(nameof(DeferredConfigCleanup), 0f, 0.5f);
    }

    private void DeferredConfigCleanup()
    {
        TryCleanConfigOnce();
        if (configCleanupDone)
        {
            CancelInvoke(nameof(DeferredConfigCleanup));
            configCleanupRetryScheduled = false;
        }
    }

    private void TryCleanConfigOnce()
    {
        if (configCleanupDone || TexCore_ConfigManager.self == null ||
            TexCore_ConfigManager.self.datas == null ||
            TexCore_ConfigManager.self.datas.datas == null)
        {
            return;
        }

        TexCore_DataClass root = TexCore_ConfigManager.self.datas.datas;
        TexCore_DataClass system = root.Find(SystemName);
        TexCore_DataClass node = system != null ? system.Find(NodeName) : null;
        if (system == null || node == null) return;
        if (node.childData == null) return;

        TexCore_M02TopicFlow.RemoveKnowledgeQuizTopicFromConfig(SystemName, NodeName);
        configCleanupDone = true;
        CancelInvoke(nameof(DeferredConfigCleanup));
        configCleanupRetryScheduled = false;
    }

    private void Configure()
    {
        TryCleanConfigOnce();
        stepObjects = stepObjects != null ? stepObjects : GetComponent<TexCore_StepObj>();
        if (stepObjects == null || stepObjects.interacterObjs == null)
        {
            WarnOnce("M02 原始交互桥接需要场景中的 StepObjs。");
            ScheduleDelayedConfigure();
            return;
        }

        m02NodeIndex = stepObjects.interacterObjs.FindIndex(value =>
            value != null && value.nodePrefab != null && value.nodePrefab.name == NodeName);
        if (m02NodeIndex < 0)
        {
            WarnOnce($"M02 原始交互桥接找不到步骤树节点“{NodeName}”。");
            ScheduleDelayedConfigure();
            return;
        }

        m02Node = stepObjects.interacterObjs[m02NodeIndex];
        cachedM02Node = null;
        cachedM02Topic = null;
        presentedM02Node = null;
        presentedM02Topic = null;
        presentedTopicStepIndex = -1;
        hasPresentedPresentation = false;
        deferredConfigureRetries = 0;
        TexCore_M02TopicFlow.RemoveKnowledgeQuizTopic(m02Node.nodeObjs);
        nodeRoot = FindM02NodeRoot();
        modelRoot = FindDirectChild(nodeRoot, ModelName);
        uiRoot = FindDirectChild(modelRoot, UiName);
        usb = FindDirectChild(modelRoot, "UPan");
        usbCollider = FindDirectChild(modelRoot, "UPanCollider");
        completedState = FindM02CompletedState(nodeRoot);
        if (nodeRoot == null || modelRoot == null || uiRoot == null || usb == null || usbCollider == null || completedState == null)
        {
            WarnOnce("M02 原始交互桥接找不到 WHY_FuWuQi-6、UPan、UPanCollider、安装界面或 02 完成状态。");
            ScheduleDelayedConfigure();
            return;
        }

        ConfigureUsb();
        ConfigureUsbBackpack();
        ConfigureInstallSteps();
        ConfigureIpStep();
        ConfigureCompletionStep();
        if (usbStep == null || installReporters.Count != InstallPages.Length || ipStep == null || completionStep == null)
        {
            ScheduleDelayedConfigure();
            return;
        }

        SetTopicTasks(UsbTopic, new[] { usb.name });
        SetTopicTasks(InstallTopic, InstallPages.Select(page => page.TaskName).ToArray());
        SetTopicTasks(IpTopic, new[] { ipStep.name });
        SetTopicTasks(CompletionTopic, new[] { completionStep.name });
        NormalizeTopicSteps(UsbTopic, 1);
        NormalizeTopicSteps(InstallTopic, InstallPages.Length);
        NormalizeTopicSteps(IpTopic, 1);
        NormalizeTopicSteps(CompletionTopic, 1);
        HideOriginalPages();
        nodeRoot.gameObject.SetActive(false);
        configured = true;
        presentationDirty = true;
        ReportBindingDiagnostics();
        BpfLog.Info(BpfLogCategory.Startup, "tex_core_m02_server_install_bootstrap.configure", "M02 已绑定原始 WHY_FuWuQi-6、UPan 与 UI_XiTongJingXiangAnZhuang。", this);
    }

    private void ConfigureUsb()
    {
        if (usbConfigured) return;

        GameObject source = usb.gameObject;
        if (source.GetComponent<Collider>() == null) source.AddComponent<BoxCollider>();
        if (source.GetComponent<Rigidbody>() == null) source.AddComponent<Rigidbody>();
        TexCore_BackpackPhysics.Stabilize(source);
        ObjectManipulator manipulator = source.GetComponent<ObjectManipulator>() ?? source.AddComponent<ObjectManipulator>();
        manipulator.AllowedManipulations = MixedReality.Toolkit.TransformFlags.Move;
        TexCore_03NoScalePolicy.Enforce(manipulator);

        usbStep = source.GetComponent<TexCore_OrderedPlacementStep>() ?? source.AddComponent<TexCore_OrderedPlacementStep>();
        usbStep.ConfigureSource(source);
        // UPanCollider is the authored USB-port target. UPanPosition is only
        // a legacy marker and must not be used as the placement destination.
        usbStep.Target = usbCollider;
        usbStep.PositionTolerance = 0.12f;
        usbStep.RotationTolerance = 180f;
        BindStep(UsbTopic, 0, source, usbStep);
        HideUsbBeforeFirstSummon();
        // Keep the authored trigger volume in the hierarchy. Placement snaps
        // to UPanCollider, which is also the physical USB port.
        usbCollider.gameObject.SetActive(true);
        usbConfigured = true;
    }

    private void HideUsbBeforeFirstSummon()
    {
        if (usb == null || usbInitialStateConfigured) return;

        usb.gameObject.SetActive(false);
        usbInitialStateConfigured = true;
    }

    private void ConfigureInstallSteps()
    {
        if (installReporters.Count == InstallPages.Length) return;

        // Resolve every authored page button before any of them is renamed to
        // its M02 task name. A later retry must still be able to find the
        // authored "Empty Button" children.
        List<PressableButton> buttons = new List<PressableButton>();
        for (int index = 0; index < InstallPages.Length; index++)
        {
            InstallPage page = InstallPages[index];
            Transform pageTransform = FindInstallPageTransform(page);
            PressableButton button = FindDirectPageButton(pageTransform, page.ButtonIndex);
            if (button == null)
            {
                WarnOnce($"M02 原始页面“{page.PageName}”没有预期的可按按钮。");
                return;
            }

            buttons.Add(button);
        }

        installReporters.Clear();
        for (int index = 0; index < InstallPages.Length; index++)
        {
            InstallPage page = InstallPages[index];
            PressableButton button = buttons[index];
            button.gameObject.name = page.TaskName;
            if (TryGetInstallInputRule(index, out InstallInputRule inputRule))
            {
                TexCore_M02InputValidationStep validation = AttachInputValidation(button, inputRule);
                if (validation == null)
                {
                    installReporters.Clear();
                    return;
                }

                BindStep(InstallTopic, index, button.gameObject, validation);
                installReporters.Add(validation);
                continue;
            }

            TexCore_ClickObjectStep step = button.GetComponent<TexCore_ClickObjectStep>() ??
                                            button.gameObject.AddComponent<TexCore_ClickObjectStep>();
            step.Configure(false, true, false, false);
            step.ConfigureSource(button.gameObject);
            BindStep(InstallTopic, index, button.gameObject, step);
            installReporters.Add(step);
        }
    }

    private static bool TryGetInstallInputRule(int stepIndex, out InstallInputRule rule)
    {
        for (int index = 0; index < InstallInputRules.Length; index++)
        {
            if (InstallInputRules[index].StepIndex == stepIndex)
            {
                rule = InstallInputRules[index];
                return true;
            }
        }

        rule = default;
        return false;
    }

    /// <summary>
    /// Binds the typed-input step to its authored page. The submit button keeps
    /// a single completing reporter; the guidance reporter only draws the
    /// current step highlight, so one click can never advance twice.
    /// </summary>
    private TexCore_M02InputValidationStep AttachInputValidation(
        PressableButton button, InstallInputRule rule)
    {
        List<InputField> fields = new List<InputField>();
        List<TexCore_ClickObjectStep> fieldGuidances = new List<TexCore_ClickObjectStep>();
        Transform page = button.transform.parent;
        foreach (InputFieldLocator locator in rule.Fields)
        {
            InputField field = FindPageInputField(page, locator);
            if (field == null)
            {
                WarnOnce($"M02 原始页面“{page.name}”缺少输入框 {locator.ParentName}/{locator.FieldName}。");
                return null;
            }

            fields.Add(field);
            Transform container = FindDirectChild(page, locator.ParentName);
            NormalizeInputHitBox(container, page.name, button);
            // The player is pointed at the fields first; the submit button only
            // lights up once every value passes its rule.
            if (container != null)
            {
                fieldGuidances.Add(AttachGuidance(container.GetComponent<PressableButton>()));
            }
        }

        TexCore_M02InputValidationStep validation =
            button.GetComponent<TexCore_M02InputValidationStep>() ??
            button.gameObject.AddComponent<TexCore_M02InputValidationStep>();
        validation.Configure(button, fields.ToArray(), rule.Mode, page);
        // AttachGuidance reuses a stale click reporter as guidance only, so the
        // submit button cannot keep a second completing component.
        validation.SetGuidance(AttachGuidance(button));
        for (int index = 0; index < fieldGuidances.Count; index++)
        {
            validation.AddInputGuidance(fieldGuidances[index]);
        }

        DisableAuthoredClickCallbacks(button, page.name);
        return validation;
    }

    /// <summary>
    /// The authored input pages still carry serialized OnClicked callbacks that
    /// hide the page and activate a legacy page. They are RuntimeOnly, so in
    /// Play Mode they ran before the rules could report an error and made a
    /// rejected submit look like a page jump. Disable them at runtime only: the
    /// shared Panel_Operate, the other steps' authored page events and the
    /// scene asset stay untouched.
    /// </summary>
    private void DisableAuthoredClickCallbacks(PressableButton button, string pageName)
    {
        if (button == null) return;

        UnityEvent clicked = button.OnClicked;
        int disabled = 0;
        for (int index = 0; index < clicked.GetPersistentEventCount(); index++)
        {
            // Only the authored page switches are removed; the press sounds on
            // the other serialized events stay as they are.
            if (!(clicked.GetPersistentTarget(index) is GameObject)) continue;
            if (clicked.GetPersistentMethodName(index) != "SetActive") continue;
            clicked.SetPersistentListenerState(index, UnityEventCallState.Off);
            disabled++;
        }

        if (disabled == 0) return;
        BpfLog.Info(BpfLogCategory.Startup,
            "tex_core_m02_server_install_bootstrap.disable_authored_click",
            $"M02 输入页面“{pageName}”已禁用 {disabled} 个原始点击回调。",
            this,
            BpfLogField.Of("page", pageName),
            BpfLogField.Of("callbacks", disabled));
    }

    /// <summary>
    /// The authored input containers keep a stale BoxCollider: in world space
    /// its height is roughly a fiftieth of a button and its depth is stretched
    /// far behind the panel, so the field is nearly impossible to point at.
    /// Calibrate the hit volume against the hit volume of the page's own submit
    /// button: the height gets a button-like floor, the depth becomes a thin
    /// slab at the panel surface. The RectTransform is never touched, so the
    /// page keeps its looks.
    /// </summary>
    private void NormalizeInputHitBox(
        Transform container, string pageName, PressableButton referenceButton)
    {
        if (container == null) return;
        BoxCollider collider = container.GetComponent<BoxCollider>();
        if (collider == null) return;

        BoxCollider reference = referenceButton != null
            ? referenceButton.GetComponent<BoxCollider>()
            : null;
        if (reference == null) return;

        Vector3 scale = container.lossyScale;
        float scaleY = Mathf.Max(0.0001f, Mathf.Abs(scale.y));
        float scaleZ = Mathf.Max(0.0001f, Mathf.Abs(scale.z));

        Vector3 referenceScale = referenceButton.transform.lossyScale;
        float referenceHeight = reference.size.y * Mathf.Abs(referenceScale.y);
        float referenceDepth = reference.size.z * Mathf.Abs(referenceScale.z);
        float referenceFront =
            (reference.center.z + reference.size.z * 0.5f) * Mathf.Abs(referenceScale.z);
        if (referenceHeight <= 0f || referenceDepth <= 0f) return;

        float previousWorldHeight = collider.size.y * scaleY;
        float previousWorldDepth = collider.size.z * scaleZ;
        float previousWorldFront =
            (collider.center.z + collider.size.z * 0.5f) * scaleZ;

        // Width stays as authored; only the height gets a floor and the depth
        // becomes a thin slab flush with the panel surface.
        float height = Mathf.Max(collider.size.y, referenceHeight / scaleY);
        collider.size = new Vector3(collider.size.x, height, referenceDepth / scaleZ);
        collider.center = new Vector3(
            collider.center.x,
            collider.center.y,
            (referenceFront - referenceDepth * 0.5f) / scaleZ);

        BpfLog.Info(BpfLogCategory.Startup,
            "tex_core_m02_server_install_bootstrap.normalize_input_hit_box",
            $"M02 输入框“{container.name}”命中体已按同页提交按钮归一化。",
            this,
            BpfLogField.Of("page", pageName),
            BpfLogField.Of("container", container.name),
            BpfLogField.Of("height_world", $"{previousWorldHeight:0.0000}->{height * scaleY:0.0000}"),
            BpfLogField.Of("depth_world", $"{previousWorldDepth:0.0000}->{referenceDepth:0.0000}"),
            BpfLogField.Of("front_world", $"{previousWorldFront:0.0000}->{referenceFront:0.0000}"));
    }

    private static InputField FindPageInputField(Transform page, InputFieldLocator locator)
    {
        Transform parent = FindDirectChild(page, locator.ParentName);
        Transform field = FindDirectChild(parent, locator.FieldName);
        return field != null ? field.GetComponent<InputField>() : null;
    }

    private void ConfigureIpStep()
    {
        Transform page = FindDirectChild(uiRoot, "YouXian10");
        PressableButton apply = FindDirectPageButton(page, 0);
        InputField address = FindInputField(page, "DiZhi");
        InputField subnetMask = FindInputField(page, "ZiWang");
        InputField gateway = FindInputField(page, "WangGuan");
        if (apply == null || address == null || subnetMask == null || gateway == null)
        {
            WarnOnce("M02 原始 YouXian10 页面缺少应用按钮或 IP、掩码、网关输入框。");
            return;
        }

        apply.gameObject.name = "M02_IP_Apply";
        // The IP fields are squashed the same way as the password pages, so
        // their hit boxes get the same slab normalization.
        List<TexCore_ClickObjectStep> fieldGuidances = new List<TexCore_ClickObjectStep>();
        foreach (string fieldName in IpFieldNames)
        {
            Transform container = FindDirectChild(page, fieldName);
            NormalizeInputHitBox(container, page.name, apply);
            // The player is pointed at the addresses first; the apply button
            // only lights up once the configuration passes its rule.
            if (container != null)
            {
                fieldGuidances.Add(AttachGuidance(container.GetComponent<PressableButton>()));
            }
        }

        ipStep = apply.GetComponent<TexCore_M02IpConfigurationStep>() ??
                 apply.gameObject.AddComponent<TexCore_M02IpConfigurationStep>();
        ipStep.Configure(address, subnetMask, gateway, apply);
        // The apply button keeps TexCore_M02IpConfigurationStep as its only
        // completing reporter. The guidance reporter only draws the highlight
        // so the module cannot be advanced twice by one click.
        ipGuidance = AttachGuidance(apply);
        ipStep.SetGuidance(ipGuidance);
        for (int index = 0; index < fieldGuidances.Count; index++)
        {
            ipStep.AddInputGuidance(fieldGuidances[index]);
        }
        // The authored apply button also hides this page and activates the next
        // one. A rejected configuration must keep the page the player has to
        // correct, so those page switches are turned off for the run, exactly
        // like the three password pages.
        DisableAuthoredClickCallbacks(apply, page.name);
        BindStep(IpTopic, 0, apply.gameObject, ipStep);

        ConfigureIpPageGuidance();
    }

    private void ConfigureIpPageGuidance()
    {
        ipPageGuidances.Clear();
        for (int index = 0; index < IpPages.Length - 1; index++)
        {
            List<TexCore_ClickObjectStep> guides = new List<TexCore_ClickObjectStep>();
            List<PressableButton> buttons = FindDirectPageButtons(FindDirectChild(uiRoot, IpPages[index]));
            // Only the navigation button of the page advances the flow and
            // carries the highlight. The remaining buttons of the page keep their
            // own authored behaviour (hints, sub pages) and stay unhighlighted,
            // so the player is never told that every button is the next step.
            PressableButton navigation = null;
            foreach (PressableButton button in buttons)
            {
                button.OnClicked.RemoveListener(AdvanceIpPage);
                if (navigation == null && button.gameObject.activeSelf) navigation = button;
            }

            if (navigation != null)
            {
                navigation.OnClicked.AddListener(AdvanceIpPage);
                TexCore_ClickObjectStep guide = AttachGuidance(navigation);
                if (guide != null) guides.Add(guide);
            }

            // A previous run may have left a highlight on another button of the
            // page; only the navigation button may stay highlighted.
            foreach (PressableButton button in buttons)
            {
                if (button == navigation) continue;
                TexCore_ClickObjectStep stale = button.GetComponent<TexCore_ClickObjectStep>();
                if (stale != null) stale.SetState(FinishState.未完成);
            }

            ipPageGuidances.Add(guides);
        }
    }

    /// <summary>
    /// Adds a guidance-only reporter that never completes a step and never
    /// calls TexCore_BusinessManager.OnClick.
    /// </summary>
    private static TexCore_ClickObjectStep AttachGuidance(PressableButton button)
    {
        if (button == null) return null;
        TexCore_ClickObjectStep guide = button.GetComponent<TexCore_ClickObjectStep>() ??
                                        button.gameObject.AddComponent<TexCore_ClickObjectStep>();
        guide.Configure(false, false, false, false);
        guide.ConfigureSource(button.gameObject);
        return guide;
    }

    private void ConfigureCompletionStep()
    {
        PressableButton close = FindDirectPageButton(FindDirectChild(uiRoot, "END"), 0);
        if (close == null)
        {
            WarnOnce("M02 原始 END 页面没有关闭按钮。");
            return;
        }

        close.gameObject.name = "M02_Complete_Close";
        completionStep = close.GetComponent<TexCore_ClickObjectStep>() ??
                         close.gameObject.AddComponent<TexCore_ClickObjectStep>();
        completionStep.Configure(false, true, false, false);
        completionStep.ConfigureSource(close.gameObject);
        BindStep(CompletionTopic, 0, close.gameObject, completionStep);
    }

    private void ConfigureUsbBackpack()
    {
        if (usbBackpackPanel != null) return;
        GameObject backpackRoot = FindSceneObject("背包UI");
        if (backpackRoot == null)
        {
            WarnOnce("M02 U盘背包入口找不到场景中的背包UI根节点。");
            return;
        }

        Transform panel = FindDirectChild(backpackRoot.transform, "02服务器系统安装背包面板");
        if (panel == null)
        {
            WarnOnce("M02 U盘背包入口找不到场景中的02服务器系统安装背包面板。");
            return;
        }
        usbBackpackPanel = panel.gameObject;

        PressableButton button = usbBackpackPanel.GetComponentInChildren<PressableButton>(true);
        if (button == null)
        {
            WarnOnce("M02 U盘背包面板没有可点击按钮。");
            return;
        }
        button.OnClicked.RemoveListener(SummonUsbFromBackpack);
        button.OnClicked.AddListener(SummonUsbFromBackpack);
        SetButtonLabel(button.gameObject, "U盘");
        usbBackpackPanel.SetActive(false);
        TexCore_BackpackChapterTabs.EnsureInitialized();
    }

    private void RefreshPresentation()
    {
        NodePanelBase activeNode = FindActiveM02Node();
        PanelTopicBase topic = activeNode != null ? activeNode.activeChild : null;
        string topicName = topic != null && topic.datas != null ? topic.datas.name : string.Empty;
        int topicStepIndex = topic != null ? topic.stepIndex : -1;

        // NodePanelBase instantiates topic prefabs after this bootstrap has
        // configured the serialized step tree. Reapply the M02 task names to
        // the live topic so the authored Panel_Operate cannot still compare
        // the player's UPan click against its legacy "确定" task.
        ApplyRuntimeTopicTasks(topic, topicName);

        if (hasPresentedPresentation && activeNode == presentedM02Node && topic == presentedM02Topic &&
            topicStepIndex == presentedTopicStepIndex &&
            !(topicName == IpTopic && visiblePageName == null))
        {
            // The page identity did not change, but the authored target button
            // may still have been left inactive or disabled by an earlier step.
            // Re-assert the current target so this early return cannot skip the
            // repair and leave the step without a highlight.
            ReassertCurrentTarget(topicName, topic);
            return;
        }

        cachedM02Topic = topic;
        presentedM02Node = activeNode;
        presentedM02Topic = topic;
        presentedTopicStepIndex = topicStepIndex;
        hasPresentedPresentation = true;
        bool m02Active = !string.IsNullOrEmpty(topicName);
        if (nodeRoot != null && nodeRoot.gameObject.activeSelf != m02Active)
            nodeRoot.gameObject.SetActive(m02Active);
        if (!m02Active)
        {
            lastTopicName = string.Empty;
            usbSummoned = false;
            if (usbBackpackPanel != null) usbBackpackPanel.SetActive(false);
            HideOriginalPages();
            return;
        }

        ShowM02CompletedState();

        if (!string.Equals(lastTopicName, topicName, StringComparison.Ordinal))
        {
            lastTopicName = topicName;
            if (topicName == UsbTopic) usbSummoned = false;
            if (topicName == IpTopic) ipPageIndex = 0;
        }

        if (topicName == UsbTopic)
        {
            ShowUsbBackpackPanel();
            return;
        }

        if (usbBackpackPanel != null) usbBackpackPanel.SetActive(false);
        if (topicName == InstallTopic)
        {
            PresentInstallPage(topic.stepIndex);
        }
        else if (topicName == IpTopic)
        {
            ShowOnlyOriginalPage(IpPages[Mathf.Clamp(ipPageIndex, 0, IpPages.Length - 1)]);
            ShowIpPageGuidance();
        }
        else if (topicName == CompletionTopic)
        {
            ShowOnlyOriginalPage("END");
        }
        else
        {
            HideOriginalPages();
        }
    }

    private void ApplyRuntimeTopicTasks(PanelTopicBase topic, string topicName)
    {
        if (topic == null || string.IsNullOrEmpty(topicName)) return;

        SuppressM02StandardNext(topic, topicName);

        if (topicName == UsbTopic)
        {
            ApplyTopicData(topic, new[] { usb != null ? usb.name : "UPan" });
            Panel_Operate operate = topic.GetComponent<Panel_Operate>();
            if (operate != null && operate.btn_Confirm != null)
            {
                operate.btn_Confirm.SetActive(false);
            }
        }
        else if (topicName == InstallTopic)
        {
            ApplyTopicData(topic, InstallPages.Select(page => page.TaskName).ToArray());
        }
        else if (topicName == IpTopic)
        {
            ApplyTopicData(topic, new[] { ipStep != null ? ipStep.name : "M02_IP_Apply" });
        }
        else if (topicName == CompletionTopic)
        {
            ApplyTopicData(topic, new[] { completionStep != null ? completionStep.name : "M02_Complete_Close" });
        }
    }

    private static void SuppressM02StandardNext(PanelTopicBase topic, string topicName)
    {
        if (topicName != UsbTopic &&
            topicName != InstallTopic &&
            topicName != IpTopic &&
            topicName != CompletionTopic)
        {
            return;
        }

        Panel_Operate operate = topic.GetComponent<Panel_Operate>();
        if (operate == null) return;

        // M02 advances from the physical object/screen action itself. Keep the
        // generic topic-level button hidden while preserving Panel_Operate's
        // Submit() auto-advance path when isNextShow is false.
        operate.isNextShow = false;
        if (operate.btn_Next != null)
        {
            operate.btn_Next.SetActive(false);
        }
    }

    private void SummonUsbFromBackpack()
    {
        PanelTopicBase topic = FindActiveM02Topic();
        if (topic == null || topic.datas == null || topic.datas.name != UsbTopic || topic.stepIndex != 0)
        {
            ShowHint("请先进入U盘启动盘插入步骤");
            return;
        }

        nodeRoot.gameObject.SetActive(true);
        usb.gameObject.SetActive(true);
        TexCore_BackpackPhysics.Stabilize(usb.gameObject);
        usbSummoned = true;
        if (usbBackpackPanel != null) usbBackpackPanel.SetActive(false);
        // The USB was inactive when the TexCore state was broadcast. Apply it
        // again after OnEnable so the original ObjectManipulator is usable.
        usbStep.SetState(FinishState.等待触发);
        ShowHint("已取出U盘，请放入服务器USB接口");
    }

    private void AdvanceIpPage()
    {
        PanelTopicBase topic = FindActiveM02Topic();
        if (topic == null || topic.datas == null || topic.datas.name != IpTopic) return;
        ipPageIndex = Mathf.Min(ipPageIndex + 1, IpPages.Length - 1);
        visiblePageName = null;
        presentationDirty = true;
    }

    private void ShowOnlyOriginalPage(string pageName)
    {
        if (visiblePageName == pageName) return;
        uiRoot.gameObject.SetActive(true);
        for (int index = 0; index < uiRoot.childCount; index++)
        {
            Transform page = uiRoot.GetChild(index);
            page.gameObject.SetActive(page.name == pageName);
        }
        visiblePageName = pageName;
    }

    private void PresentInstallPage(int stepIndex)
    {
        stepIndex = Mathf.Clamp(stepIndex, 0, InstallPages.Length - 1);
        InstallPage page = InstallPages[stepIndex];
        string pageKey = $"{page.PageName}[{page.UiChildIndex}]";
        if (visiblePageName != pageKey)
        {
            Transform selected = FindInstallPageTransform(page);
            uiRoot.gameObject.SetActive(true);
            for (int index = 0; index < uiRoot.childCount; index++)
            {
                Transform child = uiRoot.GetChild(index);
                child.gameObject.SetActive(child == selected);
            }

            visiblePageName = pageKey;
        }

        EnsureInstallTargetReady(stepIndex);
    }

    private void ReassertCurrentTarget(string topicName, PanelTopicBase topic)
    {
        if (topicName == InstallTopic && topic != null)
        {
            EnsureInstallTargetReady(topic.stepIndex);
        }
        else if (topicName == IpTopic)
        {
            ShowIpPageGuidance();
        }
    }

    /// <summary>
    /// The authored Video2, ChongQi, YuYan and second MiMa pages serialize
    /// their target button inactive, and an earlier step can leave the bound
    /// reporter disabled. Wake the current step target before its highlight is
    /// expected so the page is never presented without a usable target.
    /// </summary>
    private void EnsureInstallTargetReady(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= installReporters.Count || installReporters[stepIndex] == null)
        {
            WarnOnce($"M02 系统安装步骤 {stepIndex + 1} 没有已绑定的目标按钮。");
            return;
        }

        TexCore_InteracterBase reporter = installReporters[stepIndex];
        GameObject button = reporter.gameObject;
        if (!button.activeSelf) button.SetActive(true);
        // Only the mapped target is woken; the sibling buttons of the same page
        // keep their authored state.
        if (reporter.state != FinishState.等待触发) reporter.SetState(FinishState.等待触发);
    }

    private void ShowIpPageGuidance()
    {
        int navigationPageCount = ipPageGuidances.Count;
        if (navigationPageCount == 0 && ipGuidance == null) return;

        // The last IP page owns the apply button instead of a navigation
        // button, so only one of the two highlights can be visible.
        bool navigationVisible = ipPageIndex < navigationPageCount;
        int currentPage = Mathf.Clamp(ipPageIndex, 0, Mathf.Max(0, navigationPageCount - 1));
        for (int index = 0; index < navigationPageCount; index++)
        {
            foreach (TexCore_ClickObjectStep guide in ipPageGuidances[index])
            {
                if (guide == null) continue;
                if (navigationVisible && index == currentPage) ShowGuide(guide);
                else HideGuide(guide);
            }
        }

        if (ipGuidance != null)
        {
            if (navigationVisible) HideGuide(ipGuidance);
            else ShowGuide(ipGuidance);
        }
    }

    private static void ShowGuide(TexCore_ClickObjectStep guide)
    {
        if (!guide.gameObject.activeSelf) guide.gameObject.SetActive(true);
        if (guide.state != FinishState.等待触发) guide.SetState(FinishState.等待触发);
    }

    private static void HideGuide(TexCore_ClickObjectStep guide)
    {
        if (guide.state != FinishState.未完成) guide.SetState(FinishState.未完成);
    }

    private void HideOriginalPages()
    {
        if (uiRoot == null) return;
        uiRoot.gameObject.SetActive(false);
        visiblePageName = null;
    }

    private void ShowUsbBackpackPanel()
    {
        HideOriginalPages();
        if (usbBackpackPanel == null) ConfigureUsbBackpack();
        if (usbBackpackPanel == null) return;

        if (usbSummoned)
        {
            usbBackpackPanel.SetActive(false);
            return;
        }

        TexCore_BackpackChapterTabs.ShowM02Panel(usbBackpackPanel);
    }

    private void BindStep(string topicName, int stepIndex, GameObject source, TexCore_InteracterBase interacter)
    {
        int topicIndex = m02Node.nodeObjs.FindIndex(value =>
            value != null && value.topicPrefab != null && value.topicPrefab.name == topicName);
        if (topicIndex < 0)
        {
            WarnOnce($"M02 步骤树中缺少主题“{topicName}”。");
            return;
        }

        TopicObjs topic = m02Node.nodeObjs[topicIndex];
        topic.topicObjs = topic.topicObjs ?? new List<StepObjs>();
        StepObjs step = topic.topicObjs.FirstOrDefault(value => value != null && value.stepIndex == stepIndex);
        if (step == null)
        {
            step = new StepObjs { stepIndex = stepIndex, stepObjs = new List<GameObject>() };
            topic.topicObjs.Add(step);
        }
        step.stepIndex = stepIndex;
        step.stepObjs = new List<GameObject> { source };
        step.InvalidateStepScripts();
        interacter.enterTopic = new EnterTopic(m02Node.nodePrefab, topic.topicPrefab, stepIndex)
        {
            nodeIndex = m02NodeIndex,
            topicIndex = topicIndex
        };
    }

    private void NormalizeTopicSteps(string topicName, int expectedCount)
    {
        if (m02Node == null || m02Node.nodeObjs == null || expectedCount < 0) return;

        TopicObjs topic = m02Node.nodeObjs.FirstOrDefault(value =>
            value != null && value.topicPrefab != null &&
            value.topicPrefab.name == topicName);
        if (topic == null) return;

        topic.topicObjs = topic.topicObjs ?? new List<StepObjs>();
        topic.topicObjs.RemoveAll(value => value == null ||
                                           value.stepIndex < 0 ||
                                           value.stepIndex >= expectedCount);
        topic.topicObjs = topic.topicObjs
            .GroupBy(value => value.stepIndex)
            .Select(group => group.Last())
            .OrderBy(value => value.stepIndex)
            .ToList();

        for (int index = 0; index < expectedCount; index++)
        {
            StepObjs step = topic.topicObjs.FirstOrDefault(value => value.stepIndex == index);
            if (step == null)
            {
                topic.topicObjs.Add(new StepObjs
                {
                    stepIndex = index,
                    stepObjs = new List<GameObject>()
                });
            }
        }

        topic.topicObjs.Sort((left, right) => left.stepIndex.CompareTo(right.stepIndex));
    }

    private void SetTopicTasks(string topicName, IReadOnlyList<string> tasks)
    {
        TopicObjs topic = m02Node.nodeObjs.FirstOrDefault(value =>
            value != null && value.topicPrefab != null && value.topicPrefab.name == topicName);
        ApplyTopicData(topic != null ? topic.topicPrefab.GetComponent<PanelTopicBase>() : null, tasks);
        foreach (PanelTopicBase panel in Resources.FindObjectsOfTypeAll<PanelTopicBase>())
        {
            if (panel != null && panel.gameObject.scene == gameObject.scene && panel.datas != null && panel.datas.name == topicName)
                ApplyTopicData(panel, tasks);
        }
    }

    private static void ApplyTopicData(PanelTopicBase panel, IReadOnlyList<string> tasks)
    {
        if (panel == null) return;
        List<string> mapped = tasks.ToList();
        DataClass_Topic serialized = panel.topicData;
        if (serialized == null && panel.datas != null && panel.datas.selfDatas != null)
        {
            serialized = panel.datas.selfDatas.OfType<DataClass_Topic>().FirstOrDefault();
        }
        if (serialized == null) return;
        serialized.tasks = mapped;
        serialized.rightTasks = string.Join("_", mapped);
        panel.topicData = serialized;
    }

    private PanelTopicBase FindActiveM02Topic()
    {
        if (cachedM02Node != null && cachedM02Node.activeChild == cachedM02Topic &&
            cachedM02Topic != null && cachedM02Topic.datas != null)
            return cachedM02Topic;

        NodePanelBase node = FindActiveM02Node();
        cachedM02Topic = node != null ? node.activeChild : null;
        return cachedM02Topic;
    }

    private NodePanelBase FindActiveM02Node()
    {
        if (IsActiveM02Node(cachedM02Node)) return cachedM02Node;

        cachedM02Node = Resources.FindObjectsOfTypeAll<NodePanelBase>().FirstOrDefault(node =>
            node != null && node.gameObject.scene == gameObject.scene && node.datas != null &&
            node.isActiveAndEnabled && node.datas.name == NodeName && node.activeChild != null &&
            node.activeChild.datas != null);
        return cachedM02Node;
    }

    private static bool IsActiveM02Node(NodePanelBase node)
    {
        return node != null && node.isActiveAndEnabled && node.datas != null &&
               node.datas.name == NodeName && node.activeChild != null &&
               node.activeChild.datas != null;
    }

    private static InputField FindInputField(Transform page, string name)
    {
        Transform field = FindDirectChild(page, name);
        return field != null ? field.GetComponentInChildren<InputField>(true) : null;
    }

    private static PressableButton FindDirectPageButton(Transform page, int index)
    {
        List<PressableButton> buttons = FindDirectPageButtons(page);
        return index >= 0 && index < buttons.Count ? buttons[index] : null;
    }

    private Transform FindInstallPageTransform(InstallPage page)
    {
        if (uiRoot == null) return null;
        if (page.UiChildIndex >= 0 && page.UiChildIndex < uiRoot.childCount)
        {
            Transform candidate = uiRoot.GetChild(page.UiChildIndex);
            if (candidate.name == page.PageName) return candidate;
        }

        // Keep a name fallback for prefab variants that do not preserve the
        // authored child order, while still selecting the correct duplicate
        // page by occurrence.
        int matchingPageCount = 0;
        for (int index = 0; index < uiRoot.childCount; index++)
        {
            Transform candidate = uiRoot.GetChild(index);
            if (candidate.name != page.PageName) continue;
            if (matchingPageCount == page.NameOccurrence) return candidate;
            matchingPageCount++;
        }

        return null;
    }

    private static List<PressableButton> FindDirectPageButtons(Transform page)
    {
        List<PressableButton> result = new List<PressableButton>();
        if (page == null) return result;
        for (int index = 0; index < page.childCount; index++)
        {
            Transform child = page.GetChild(index);
            if (!child.name.StartsWith("Empty Button", StringComparison.Ordinal)) continue;
            PressableButton button = child.GetComponent<PressableButton>();
            if (button != null) result.Add(button);
        }
        return result;
    }

    private static Transform FindDirectChild(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (child.name == name) return child;
        }
        return null;
    }

    private static T FindSceneComponent<T>(string name) where T : Component
    {
        Scene scene = SceneManager.GetActiveScene();
        return Resources.FindObjectsOfTypeAll<T>().FirstOrDefault(value =>
            value != null && value.gameObject.scene == scene && value.name == name);
    }

    private static Transform FindSceneTransform(string name)
    {
        Scene scene = SceneManager.GetActiveScene();
        return Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(value =>
            value != null && value.gameObject.scene == scene && value.name == name);
    }

    private static Transform FindM02NodeRoot()
    {
        return FindScenePath("无交互物体", SystemName, NodeName);
    }

    private static Transform FindM02CompletedState(Transform root)
    {
        return FindDirectChild(root, CompletedStateName);
    }

    private static Transform FindScenePath(params string[] path)
    {
        Scene scene = SceneManager.GetActiveScene();
        return FindPathFromRoots(scene.GetRootGameObjects().Select(root => root.transform), path);
    }

    private static Transform FindPathFromRoots(IEnumerable<Transform> roots, IReadOnlyList<string> path)
    {
        if (roots == null || path == null || path.Count == 0) return null;

        Transform current = roots.FirstOrDefault(root => root != null && root.name == path[0]);
        for (int index = 1; current != null && index < path.Count; index++)
        {
            current = FindDirectChild(current, path[index]);
        }
        return current;
    }

    private void ShowM02CompletedState()
    {
        if (completedState != null && !completedState.gameObject.activeSelf)
        {
            completedState.gameObject.SetActive(true);
        }
    }

    private static GameObject FindSceneObject(string name)
    {
        Transform transform = FindSceneTransform(name);
        return transform != null ? transform.gameObject : null;
    }

    private static void SetButtonLabel(GameObject button, string label)
    {
        TMP_Text text = button.GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault(value => value.name == "Label" || value.name == "Text");
        if (text != null) text.text = label;
    }

    private static void ShowHint(string text) =>
        TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, text, 1.5f);

    /// <summary>
    /// One-shot startup report for the 27 authored install pages. It never runs
    /// from Update and only reports missing bindings, duplicate reporters or a
    /// duplicated step binding once per session.
    /// </summary>
    private void ReportBindingDiagnostics()
    {
        if (bindingReported) return;
        bindingReported = true;

        List<string> pageReports = new List<string>();
        int resolved = 0;
        int duplicateReporters = 0;
        for (int index = 0; index < InstallPages.Length; index++)
        {
            InstallPage page = InstallPages[index];
            TexCore_InteracterBase reporter =
                index < installReporters.Count ? installReporters[index] : null;
            GameObject button = reporter != null ? reporter.gameObject : null;
            int completingReporters = CountCompletingReporters(button);
            bool hasGuidance = button != null &&
                button.GetComponents<TexCore_ClickObjectStep>()
                    .Any(value => value != null && !value.CompletesStep);
            if (button != null) resolved++;
            if (completingReporters > 1)
            {
                duplicateReporters++;
            }

            pageReports.Add(
                $"{page.PageName}[{index}] button={(button != null ? button.name : "<missing>")}" +
                $" active={(button != null && button.gameObject.activeSelf)}" +
                $" reporter={(reporter != null ? reporter.GetType().Name : "<missing>")}" +
                $" guidance={hasGuidance} completing={completingReporters}");
        }

        int duplicateBindings = CountDuplicateStepBindings();
        BpfLog.Info(BpfLogCategory.Startup, "tex_core_m02_server_install_bootstrap.binding_report",
            $"M02 安装页面绑定：{resolved}/{InstallPages.Length}，IP指导={(ipGuidance != null)}，IP导航页={ipPageGuidances.Count}，重复步骤绑定={duplicateBindings}，重复完成组件={duplicateReporters}。",
            this,
            BpfLogField.Of("pages", string.Join(" | ", pageReports)));
        if (resolved != InstallPages.Length || duplicateBindings > 0 || duplicateReporters > 0 ||
            ipGuidance == null)
        {
            WarnOnce("M02 页面绑定诊断发现缺失的目标按钮、点击步骤或重复绑定。");
        }
    }

    /// <summary>
    /// Counts the components on one install button that are allowed to complete
    /// its step. The submit button must always keep exactly one.
    /// </summary>
    private static int CountCompletingReporters(GameObject button)
    {
        if (button == null) return 0;

        int count = button.GetComponents<TexCore_M02InputValidationStep>().Length;
        foreach (TexCore_ClickObjectStep step in button.GetComponents<TexCore_ClickObjectStep>())
        {
            if (step != null && step.CompletesStep) count++;
        }

        return count;
    }

    private int CountDuplicateStepBindings()
    {
        if (m02Node == null || m02Node.nodeObjs == null) return 0;

        Dictionary<GameObject, int> counts = new Dictionary<GameObject, int>();
        foreach (TopicObjs topic in m02Node.nodeObjs)
        {
            if (topic == null || topic.topicObjs == null) continue;
            foreach (StepObjs step in topic.topicObjs)
            {
                if (step == null || step.stepObjs == null) continue;
                foreach (GameObject source in step.stepObjs)
                {
                    if (source == null) continue;
                    counts.TryGetValue(source, out int count);
                    counts[source] = count + 1;
                }
            }
        }

        return counts.Count(entry => entry.Value > 1);
    }

    private void WarnOnce(string message)
    {
        if (warnedMissingBinding) return;
        warnedMissingBinding = true;
        BpfLog.Warning(BpfLogCategory.Startup, "tex_core_m02_server_install_bootstrap.warn_once", null, this, BpfLogField.Of("message", message));
    }

    private readonly struct InstallPage
    {
        public InstallPage(int uiChildIndex, string pageName, int buttonIndex, string taskName)
            : this(uiChildIndex, pageName, buttonIndex, taskName, 0)
        {
        }

        public InstallPage(
            int uiChildIndex,
            string pageName,
            int buttonIndex,
            string taskName,
            int nameOccurrence)
        {
            UiChildIndex = uiChildIndex;
            PageName = pageName;
            ButtonIndex = buttonIndex;
            TaskName = taskName;
            NameOccurrence = nameOccurrence;
        }
        public int UiChildIndex { get; }
        public string PageName { get; }
        public int ButtonIndex { get; }
        public string TaskName { get; }
        public int NameOccurrence { get; }
    }

    /// <summary>
    /// One authored input field, addressed the same way the pages are authored:
    /// a direct child of the page plus a direct child of that child.
    /// </summary>
    private readonly struct InputFieldLocator
    {
        public InputFieldLocator(string parentName, string fieldName)
        {
            ParentName = parentName;
            FieldName = fieldName;
        }

        public string ParentName { get; }
        public string FieldName { get; }
    }

    private readonly struct InstallInputRule
    {
        public InstallInputRule(
            int stepIndex,
            M02InputValidationMode mode,
            params InputFieldLocator[] fields)
        {
            StepIndex = stepIndex;
            Mode = mode;
            Fields = fields ?? new InputFieldLocator[0];
        }

        public int StepIndex { get; }
        public M02InputValidationMode Mode { get; }
        public InputFieldLocator[] Fields { get; }
    }
}

public static class TexCore_M02TopicFlow
{
    public const string KnowledgeQuizTopic = "01知识点答题_待出题";

    public static bool RemoveKnowledgeQuizTopic(List<TopicObjs> topics)
    {
        return topics != null && topics.RemoveAll(topic =>
            topic != null && topic.topicPrefab != null &&
            string.Equals(topic.topicPrefab.name, KnowledgeQuizTopic,
                StringComparison.Ordinal)) > 0;
    }

    public static bool RemoveKnowledgeQuizTopicFromConfig(string systemName, string nodeName)
    {
        TexCore_DataClass root = TexCore_ConfigManager.self != null &&
                                 TexCore_ConfigManager.self.datas != null
            ? TexCore_ConfigManager.self.datas.datas
            : null;
        TexCore_DataClass system = root != null ? root.Find(systemName) : null;
        TexCore_DataClass node = system != null ? system.Find(nodeName) : null;
        return node != null && node.childData != null &&
               node.childData.RemoveAll(topic => topic != null &&
                   string.Equals(topic.name, KnowledgeQuizTopic,
                       StringComparison.Ordinal)) > 0;
    }
}
