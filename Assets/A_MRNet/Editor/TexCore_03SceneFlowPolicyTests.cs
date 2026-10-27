using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Bpf.Diagnostics;
using MixedReality.Toolkit.SpatialManipulation;
using MixedReality.Toolkit.UX;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TexCore_03SceneFlowPolicyTests
{
    [Test]
    public void Update_DoesNotPerformGlobalObjectScans()
    {
        string source = File.ReadAllText(
            Path.Combine(Application.dataPath, "A_MRNet", "Scripts",
                "TexCore_03SceneFlowPolicy.cs"));
        string update = ExtractMethod(source, "private void Update()");

        Assert.That(update, Does.Not.Contain("FindObjectsOfType"));
    }

    [Test]
    public void StandardConfirmButtonNames_AreLimitedToConfirmAndPositiveButton()
    {
        Assert.That(TexCore_03SceneFlowPolicy.IsStandardConfirmButtonName("确定"), Is.True);
        Assert.That(TexCore_03SceneFlowPolicy.IsStandardConfirmButtonName("PositiveButton"), Is.True);

        // 名称不同的业务按钮不能被当成标准确认按钮隐藏。
        Assert.That(TexCore_03SceneFlowPolicy.IsStandardConfirmButtonName("确定按钮"), Is.False);
        Assert.That(TexCore_03SceneFlowPolicy.IsStandardConfirmButtonName("01_底座_01_绿色确认"), Is.False);
        Assert.That(TexCore_03SceneFlowPolicy.IsStandardConfirmButtonName("M02_IP_Apply"), Is.False);
        Assert.That(TexCore_03SceneFlowPolicy.IsStandardConfirmButtonName("跳过"), Is.False);
        Assert.That(TexCore_03SceneFlowPolicy.IsStandardConfirmButtonName("UPan"), Is.False);
        Assert.That(TexCore_03SceneFlowPolicy.IsStandardConfirmButtonName("下一步"), Is.False);
    }

    [Test]
    public void SelectPanelConfirmButton_IsHiddenByPolicy()
    {
        GameObject topic = new GameObject("Choice Topic");
        GameObject confirm = new GameObject("确定");
        confirm.transform.SetParent(topic.transform);

        try
        {
            topic.AddComponent<Panel_Select>();

            Assert.That(
                TexCore_03SceneFlowPolicy.ShouldHideConfirmButton(confirm.transform),
                Is.True,
                "选择题的“确定”同样属于标准确认按钮，必须隐藏。");
        }
        finally
        {
            Object.DestroyImmediate(topic);
        }
    }

    [Test]
    public void OperationPanelConfirmButton_IsStillHiddenByPolicy()
    {
        GameObject topic = new GameObject("Operate Topic");
        GameObject confirm = new GameObject("确定");
        confirm.transform.SetParent(topic.transform);

        try
        {
            topic.AddComponent<Panel_Operate>();

            Assert.That(
                TexCore_03SceneFlowPolicy.ShouldHideConfirmButton(confirm.transform),
                Is.True,
                "普通操作题的标准“确定”仍按现有 03 场景策略隐藏。");
        }
        finally
        {
            Object.DestroyImmediate(topic);
        }
    }

    [Test]
    public void DialogConfirmButton_IsStandardOnlyUnderDialogOrGuidanceRoot()
    {
        GameObject dialogRoot = new GameObject("BPF_Dialog_168x140mm");
        GameObject dialogButton = new GameObject("确定按钮");
        dialogButton.transform.SetParent(dialogRoot.transform);

        // 同名的真实业务按钮不在对白/指引层下，必须保留可交互。
        GameObject businessButton = new GameObject("确定按钮");
        GameObject businessRoot = new GameObject("UI-操作台");
        businessButton.transform.SetParent(businessRoot.transform);

        try
        {
            // “确定按钮”不是无条件命中的名称。
            Assert.That(
                TexCore_StandardButtonPolicy.IsStandardConfirmButtonName("确定按钮"),
                Is.False,
                "“确定按钮”只能按父级路径判定，不能按名称全场隐藏。");
            Assert.That(
                TexCore_StandardButtonPolicy.IsStandardConfirmButton(dialogButton.transform),
                Is.True,
                "对白根节点下的“确定按钮”属于标准确认按钮。");
            Assert.That(
                TexCore_StandardButtonPolicy.IsStandardConfirmButton(businessButton.transform),
                Is.False,
                "非对白层的同名业务按钮必须保留。");
        }
        finally
        {
            Object.DestroyImmediate(dialogRoot);
            Object.DestroyImmediate(businessRoot);
        }
    }

    [Test]
    public void DialogPositiveAlias_IsRecognizedAsStandardConfirmButton()
    {
        GameObject dialogRoot = new GameObject("ButtonParent");
        GameObject positiveAlias = new GameObject("Positive");
        positiveAlias.transform.SetParent(dialogRoot.transform);

        try
        {
            Assert.That(
                TexCore_StandardButtonPolicy.IsStandardConfirmButton(positiveAlias.transform),
                Is.True,
                "BPF Dialog 运行期别名 Positive 也必须识别。");
        }
        finally
        {
            Object.DestroyImmediate(dialogRoot);
        }
    }

    [Test]
    public void StandardButtonGuard_HidesNextAndConfirmWhenGuidanceRootEnables()
    {
        GameObject guidanceRoot = new GameObject("UI-指引");

        try
        {
            guidanceRoot.AddComponent<TexCore_StandardButtonGuard>();
            GameObject confirm = new GameObject("确定");
            confirm.transform.SetParent(guidanceRoot.transform);
            GameObject next = new GameObject("下一步");
            next.transform.SetParent(guidanceRoot.transform);
            // 真实业务按钮不能被门禁误伤。
            GameObject keep = new GameObject("M02_IP_Apply");
            keep.transform.SetParent(guidanceRoot.transform);
            confirm.SetActive(true);
            next.SetActive(true);
            keep.SetActive(true);

            guidanceRoot.GetComponent<TexCore_StandardButtonGuard>().Apply();

            Assert.That(confirm.activeSelf, Is.False, "指引子树里的“确定”必须被隐藏。");
            Assert.That(next.activeSelf, Is.False, "指引子树里的“下一步”必须被隐藏。");
            Assert.That(keep.activeSelf, Is.True, "业务按钮不能被门禁隐藏。");
        }
        finally
        {
            Object.DestroyImmediate(guidanceRoot);
        }
    }

    [Test]
    public void SelectPanel_AutoSubmitsOnSingleChoiceSelection()
    {
        GameObject topic = CreateSelectTopic(
            SelecionCount.单选, "B.HTTP", "A.FTP_B.HTTP_C.SMTP", true);

        try
        {
            Panel_Select panel = topic.GetComponent<Panel_Select>();
            GameObject confirm = topic.transform.Find("确定").gameObject;

            InvokeSelectLifecycle(panel, "Awake");
            InvokeSelectLifecycle(panel, "OnEnable");
            Assert.That(confirm.activeSelf, Is.False,
                "未选择时“确定”保持隐藏。");

            ClickOption(panel, "B.HTTP");

            Assert.That(panel.isSubmit, Is.True,
                "单选选中答案后应立即自动提交。");
            Assert.That(panel.haveScore, Is.EqualTo(10));
            Assert.That(confirm.activeSelf, Is.False,
                "自动提交路径不再显示“确定”。");
        }
        finally
        {
            Object.DestroyImmediate(topic);
        }
    }

    [Test]
    public void SelectPanel_MultiChoiceSubmitsOnceAnswerCountIsReached()
    {
        GameObject topic = CreateSelectTopic(
            SelecionCount.多选, "A.FTP_B.HTTP", "A.FTP_B.HTTP_C.SMTP_D.POP3", false);

        try
        {
            Panel_Select panel = topic.GetComponent<Panel_Select>();
            GameObject confirm = topic.transform.Find("确定").gameObject;
            InvokeSelectLifecycle(panel, "Awake");
            InvokeSelectLifecycle(panel, "OnEnable");

            ClickOption(panel, "A.FTP");
            Assert.That(panel.isSubmit, Is.False,
                "多选只选到一个答案时不能提交。");

            ClickOption(panel, "B.HTTP");

            Assert.That(panel.isSubmit, Is.True,
                "多选选满答案数量后应自动提交。");
            Assert.That(panel.haveScore, Is.EqualTo(10));
            Assert.That(confirm.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(topic);
        }
    }

    /// <summary>
    /// 不定项选择题与多选共用“选满 rightTasks 配置数量即自动提交”的判定分支。
    /// </summary>
    [Test]
    public void SelectPanel_IndefiniteChoiceSubmitsOnceAnswerCountIsReached()
    {
        GameObject topic = CreateSelectTopic(
            SelecionCount.不定项选择, "A.FTP_B.HTTP", "A.FTP_B.HTTP_C.SMTP_D.POP3", false);

        try
        {
            Panel_Select panel = topic.GetComponent<Panel_Select>();
            GameObject confirm = topic.transform.Find("确定").gameObject;
            InvokeSelectLifecycle(panel, "Awake");
            InvokeSelectLifecycle(panel, "OnEnable");

            ClickOption(panel, "A.FTP");
            Assert.That(panel.isSubmit, Is.False,
                "不定项只选到一个答案时不能提交。");

            ClickOption(panel, "B.HTTP");

            Assert.That(panel.isSubmit, Is.True,
                "不定项选满答案数量后应自动提交。");
            Assert.That(panel.haveScore, Is.EqualTo(10));
            Assert.That(confirm.activeSelf, Is.False,
                "自动提交路径不再显示“确定”。");
        }
        finally
        {
            Object.DestroyImmediate(topic);
        }
    }

    [Test]
    public void SelectPanel_WrongAnswerInPracticeModeKeepsRetryInsteadOfAdvancing()
    {
        GameObject topic = CreateSelectTopic(
            SelecionCount.单选, "B.HTTP", "A.FTP_B.HTTP_C.SMTP_D.POP3", false);
        TopicCompletionCounter completions = null;

        // 练习模式才保留重答次数；显式设置并还原，避免依赖静态默认值。
        Mode previousMode = TexCore_Const.mode;
        TexCore_Const.mode = Mode.练习;

        try
        {
            Panel_Select panel = topic.GetComponent<Panel_Select>();
            GameObject confirm = topic.transform.Find("确定").gameObject;
            InvokeSelectLifecycle(panel, "Awake");
            InvokeSelectLifecycle(panel, "OnEnable");

            completions = new TopicCompletionCounter("选择题");

            ClickOption(panel, "A.FTP");

            Assert.That(panel.isSubmit, Is.False,
                "练习模式首次答错不能立刻锁定题目。");
            Assert.That(panel.errorNum, Is.EqualTo(1),
                "答错应保留原有练习模式重答次数计数。");
            Assert.That(completions.Count, Is.EqualTo(0),
                "答错不能广播“完成题目”，否则场景流程会提前跳题。");
            Assert.That(confirm.activeSelf, Is.False,
                "答错后不再出现“确定”，玩家直接重新选择。");

            ClickOption(panel, "B.HTTP");

            Assert.That(panel.isSubmit, Is.True,
                "改正后应自动提交。");
            Assert.That(panel.haveScore, Is.EqualTo(10));
            Assert.That(completions.Count, Is.EqualTo(1),
                "整道题只广播一次“完成题目”。");
        }
        finally
        {
            TexCore_Const.mode = previousMode;
            if (completions != null) completions.Dispose();
            Object.DestroyImmediate(topic);
        }
    }

    [Test]
    public void SelectPanel_ThirdWrongAnswerInPracticeModeStillCompletes()
    {
        GameObject topic = CreateSelectTopic(
            SelecionCount.单选, "B.HTTP", "A.FTP_B.HTTP_C.SMTP_D.POP3", false);
        TopicCompletionCounter completions = null;

        Mode previousMode = TexCore_Const.mode;
        TexCore_Const.mode = Mode.练习;

        try
        {
            Panel_Select panel = topic.GetComponent<Panel_Select>();
            InvokeSelectLifecycle(panel, "Awake");
            InvokeSelectLifecycle(panel, "OnEnable");

            completions = new TopicCompletionCounter("选择题");

            ClickOption(panel, "A.FTP");
            Assert.That(panel.isSubmit, Is.False, "第 1 次错误仍可重答。");
            ClickOption(panel, "C.SMTP");
            Assert.That(panel.isSubmit, Is.False, "第 2 次错误仍可重答。");
            ClickOption(panel, "D.POP3");

            Assert.That(panel.isSubmit, Is.True,
                "第 3 次错误达到上限后必须提交，否则题目会卡死。");
            Assert.That(panel.haveScore, Is.EqualTo(0),
                "达到错误上限时按 0 分结算。");
            Assert.That(completions.Count, Is.EqualTo(1),
                "达到错误上限只广播一次“完成题目”。");
        }
        finally
        {
            TexCore_Const.mode = previousMode;
            if (completions != null) completions.Dispose();
            Object.DestroyImmediate(topic);
        }
    }

    [Test]
    public void SelectPanel_CorrectAnswerBroadcastsCompletionExactlyOnce()
    {
        GameObject topic = CreateSelectTopic(
            SelecionCount.单选, "B.HTTP", "A.FTP_B.HTTP_C.SMTP", false);
        TopicCompletionCounter completions = null;

        try
        {
            Panel_Select panel = topic.GetComponent<Panel_Select>();
            InvokeSelectLifecycle(panel, "Awake");
            InvokeSelectLifecycle(panel, "OnEnable");

            completions = new TopicCompletionCounter("选择题");

            ClickOption(panel, "B.HTTP");
            // 提交后的重复点击不能再入队一次完成事件。
            ClickOption(panel, "B.HTTP");
            ClickOption(panel, "A.FTP");

            Assert.That(completions.Count, Is.EqualTo(1),
                "正确答案提交后只应触发一次完成事件。");
        }
        finally
        {
            if (completions != null) completions.Dispose();
            Object.DestroyImmediate(topic);
        }
    }

    [Test]
    public void SelectPanel_IgnoresClicksAfterSubmit()
    {
        GameObject topic = CreateSelectTopic(
            SelecionCount.单选, "B.HTTP", "A.FTP_B.HTTP_C.SMTP", true);

        try
        {
            Panel_Select panel = topic.GetComponent<Panel_Select>();
            InvokeSelectLifecycle(panel, "Awake");
            InvokeSelectLifecycle(panel, "OnEnable");

            ClickOption(panel, "B.HTTP");
            int scoreAfterSubmit = panel.haveScore;
            ClickOption(panel, "A.FTP");

            Assert.That(panel.haveScore, Is.EqualTo(scoreAfterSubmit),
                "已提交的题目不能被后续点击重新评分。");
            Assert.That(panel.steps, Has.Count.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(topic);
        }
    }

    [Test]
    public void ChoiceTopicPrefab_KeepsConfirmButtonInactiveByDefault()
    {
        string prefab = File.ReadAllText(Path.Combine(
            Application.dataPath, "Resources", "TopicPrefabs", "选择题.prefab"));

        // 与 操作题.prefab 保持一致：嵌套的 确定.prefab 实例必须带
        // m_IsActive: 0 覆盖，否则实例化的同一帧就会渲染出白底绿字按钮。
        int confirmInstance = prefab.IndexOf(
            "guid: 33ba4e56fa87d4f45b7c0a31f4838ce9", System.StringComparison.Ordinal);
        Assert.That(confirmInstance, Is.GreaterThanOrEqualTo(0),
            "选择题.prefab 应嵌套标准“确定”预制体。");

        int nameOverride = prefab.IndexOf(
            "propertyPath: m_Name", confirmInstance, System.StringComparison.Ordinal);
        int activeOverride = prefab.IndexOf(
            "propertyPath: m_IsActive", confirmInstance, System.StringComparison.Ordinal);
        Assert.That(nameOverride, Is.GreaterThan(confirmInstance));
        Assert.That(activeOverride, Is.GreaterThan(nameOverride),
            "选择题.prefab 的“确定”必须带 m_IsActive: 0 覆盖。");
        Assert.That(prefab.Substring(activeOverride, 80), Does.Contain("value: 0"));
    }

    [Test]
    public void OperationConfirmButton_IsSuppressedByScenePolicy()
    {
        GameObject confirm = new GameObject("确定");

        try
        {
            Assert.That(
                TexCore_03SceneFlowPolicy.ShouldHideConfirmButton(confirm.transform),
                Is.True);
        }
        finally
        {
            Object.DestroyImmediate(confirm);
        }
    }

    [TestCase(4, 4)]
    [TestCase(4, 5)]
    [TestCase(4, 6)]
    public void M05OperationTopics_AreNotSkippedBeforeRuntimeBinding(int nodeIndex, int topicIndex)
    {
        GameObject root = new GameObject("M05 Flow Policy Test");

        try
        {
            TexCore_StepObj steps = root.AddComponent<TexCore_StepObj>();
            steps.interacterObjs = CreateEmptyStepTree();
            TexCore_03SceneFlowPolicy policy = root.AddComponent<TexCore_03SceneFlowPolicy>();
            // 编辑模式下 AddComponent 不会执行 Awake，需要手动注入步骤表。
            SetPolicyField(policy, "stepObjects", steps);
            MethodInfo method = typeof(TexCore_03SceneFlowPolicy).GetMethod(
                "TryGetEmptyStep",
                BindingFlags.Instance | BindingFlags.NonPublic);

            bool shouldSkip = (bool)method.Invoke(policy, new object[] { nodeIndex, topicIndex, 0 });

            Assert.That(shouldSkip, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void M02OperationTopic_IsNotSkippedBeforeRuntimeBinding()
    {
        GameObject root = new GameObject("M02 Flow Policy Test");
        GameObject m02NodePrefab = new GameObject("02服务器系统安装");

        try
        {
            TexCore_StepObj steps = root.AddComponent<TexCore_StepObj>();
            steps.interacterObjs = CreateEmptyStepTree();
            steps.interacterObjs[1].nodePrefab = m02NodePrefab;
            TexCore_03SceneFlowPolicy policy = root.AddComponent<TexCore_03SceneFlowPolicy>();
            // 编辑模式下 AddComponent 不会执行 Awake，需要手动注入步骤表。
            SetPolicyField(policy, "stepObjects", steps);
            MethodInfo method = typeof(TexCore_03SceneFlowPolicy).GetMethod(
                "TryGetEmptyStep",
                BindingFlags.Instance | BindingFlags.NonPublic);

            bool shouldSkip = (bool)method.Invoke(policy, new object[] { 1, 1, 0 });

            Assert.That(shouldSkip, Is.False,
                "M02 的原始界面按钮会在运行时绑定，绑定前不能被全局空步骤策略跳过。");
        }
        finally
        {
            Object.DestroyImmediate(m02NodePrefab);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void OtherEmptyOperationTopic_IsStillSkipped()
    {
        GameObject root = new GameObject("Other Flow Policy Test");

        try
        {
            TexCore_StepObj steps = root.AddComponent<TexCore_StepObj>();
            steps.interacterObjs = CreateEmptyStepTree();
            TexCore_03SceneFlowPolicy policy = root.AddComponent<TexCore_03SceneFlowPolicy>();
            // 编辑模式下 AddComponent 不会执行 Awake，需要手动注入步骤表。
            SetPolicyField(policy, "stepObjects", steps);
            MethodInfo method = typeof(TexCore_03SceneFlowPolicy).GetMethod(
                "TryGetEmptyStep",
                BindingFlags.Instance | BindingFlags.NonPublic);

            bool shouldSkip = (bool)method.Invoke(policy, new object[] { 4, 3, 0 });

            Assert.That(shouldSkip, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void StepScripts_CanBeInvalidatedAfterLateBinding()
    {
        GameObject original = new GameObject("Original Reporter");
        GameObject replacement = new GameObject("Replacement Reporter");
        original.SetActive(false);
        replacement.SetActive(false);

        try
        {
            TexCore_InteracterBase originalReporter = original.AddComponent<TexCore_InteracterBase>();
            TexCore_InteracterBase replacementReporter = replacement.AddComponent<TexCore_InteracterBase>();
            StepObjs step = new StepObjs
            {
                stepIndex = 0,
                stepObjs = new List<GameObject> { original }
            };

            Assert.That(step.StepScripts, Is.EquivalentTo(new[] { originalReporter }));

            step.stepObjs = new List<GameObject> { replacement };
            MethodInfo invalidate = typeof(StepObjs).GetMethod("InvalidateStepScripts");
            Assert.That(invalidate, Is.Not.Null);
            invalidate.Invoke(step, null);

            Assert.That(step.StepScripts, Is.EquivalentTo(new[] { replacementReporter }));
        }
        finally
        {
            Object.DestroyImmediate(original);
            Object.DestroyImmediate(replacement);
        }
    }

    [Test]
    public void M05BackpackController_CreatesAllSummonButtonsInsideUnifiedBackpackPanel()
    {
        GameObject backpack = new GameObject("背包UI");
        GameObject workflow = new GameObject("M05 Draw Workflow");

        try
        {
            GameObject templatePanel = new GameObject("01通用物品背包面板");
            templatePanel.transform.SetParent(backpack.transform);
            GameObject grid = new GameObject("Grid");
            grid.transform.SetParent(templatePanel.transform);
            GameObject buttonTemplate = new GameObject("Template Button");
            buttonTemplate.transform.SetParent(grid.transform);
            buttonTemplate.AddComponent<PressableButton>();
            GameObject visibleText = new GameObject("Text");
            visibleText.transform.SetParent(buttonTemplate.transform);
            visibleText.AddComponent<TextMeshPro>().text = "Button";

            backpack.AddComponent<TexCore_05BackpackController>();
            TexCore_M05DrawStep step = workflow.AddComponent<TexCore_M05DrawStep>();
            TexCore_05BackpackController.Present(step);

            Transform panel = backpack.transform.Find("05划线贴码背包面板");
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.parent, Is.EqualTo(backpack.transform));
            Assert.That(
                panel.GetComponentsInChildren<PressableButton>(true),
                Has.Length.EqualTo(5));
            Assert.That(panel.Find("Grid/05背包_激光水平仪"), Is.Not.Null);
            Assert.That(panel.Find("Grid/05背包_开启射线"), Is.Not.Null);
            Assert.That(panel.Find("Grid/05背包_记号笔"), Is.Not.Null);
            Assert.That(panel.Find("Grid/05背包_墨斗"), Is.Not.Null);
            Assert.That(panel.Find("Grid/05背包_二维码"), Is.Not.Null);
            Assert.That(
                panel.Find("Grid/05背包_激光水平仪").GetComponentInChildren<TMP_Text>(true).text,
                Is.EqualTo("激光水平仪"));
        }
        finally
        {
            Object.DestroyImmediate(backpack);
            Object.DestroyImmediate(workflow);
        }
    }

    [Test]
    public void BackpackChapterTabs_CreateAnM10TabAndKeepItsPanelExclusive()
    {
        GameObject backpack = new GameObject("背包UI");
        GameObject chapterBar = new GameObject("Button Bar_Horizontal");
        chapterBar.transform.SetParent(backpack.transform);
        GameObject template = new GameObject("06货架安装切换按钮");
        template.transform.SetParent(chapterBar.transform);
        template.AddComponent<PressableButton>();
        GameObject text = new GameObject("Text");
        text.transform.SetParent(template.transform);
        text.AddComponent<TextMeshPro>().text = "06货架安装";
        GameObject defaultPanel = new GameObject("01通用物品背包面板");
        defaultPanel.transform.SetParent(backpack.transform);
        GameObject rackPanel = new GameObject("06货架安装背包面板");
        rackPanel.transform.SetParent(backpack.transform);
        GameObject m10Panel = new GameObject("10货架入场背包面板");
        m10Panel.transform.SetParent(backpack.transform);
        m10Panel.SetActive(false);

        try
        {
            MethodInfo ensureTab = typeof(TexCore_BackpackChapterTabs).GetMethod(
                "EnsureM10ChapterButton",
                BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo configureButton = typeof(TexCore_BackpackChapterTabs).GetMethod(
                "ConfigureButton",
                BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo showPanel = typeof(TexCore_BackpackChapterTabs).GetMethod(
                "ShowPanel",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new[] { typeof(Transform), typeof(string) },
                null);

            PressableButton m10Button = (PressableButton)ensureTab.Invoke(
                null, new object[] { chapterBar.transform });
            configureButton.Invoke(null, new object[]
            {
                m10Button,
                "10货架入场",
                "10货架入场背包面板"
            });
            showPanel.Invoke(null, new object[]
            {
                backpack.transform,
                "10货架入场背包面板"
            });

            Assert.That(m10Button.name, Is.EqualTo("10货架入场切换按钮"));
            Assert.That(
                m10Button.GetComponentInChildren<TMP_Text>(true).text,
                Is.EqualTo("10货架入场"));
            Assert.That(m10Panel.activeSelf, Is.True);
            Assert.That(defaultPanel.activeSelf, Is.False);
            Assert.That(rackPanel.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(backpack);
        }
    }

    [Test]
    public void M05BackpackController_UsesWorkflowSceneWhenActiveSceneDiffers()
    {
        Scene originalScene = SceneManager.GetActiveScene();
        Scene anchorScene = default;
        bool closeAnchorScene = false;
        if (string.IsNullOrEmpty(originalScene.path))
        {
            // 编辑模式测试场景是未保存的临时场景，必须先保存到 Temp
            // 才能继续 additive 新建场景。
            EditorSceneManager.SaveScene(originalScene,
                "Temp/" + originalScene.name + "_" + originalScene.handle + ".unity");
            originalScene = SceneManager.GetActiveScene();
        }
        if (string.IsNullOrEmpty(originalScene.path))
        {
            anchorScene = SceneManager.GetSceneByPath("Assets/A_MRNet/Sence/01StartUp.unity");
            if (!anchorScene.IsValid() || !anchorScene.isLoaded)
            {
                anchorScene = EditorSceneManager.OpenScene(
                    "Assets/A_MRNet/Sence/01StartUp.unity",
                    OpenSceneMode.Additive);
                closeAnchorScene = true;
            }
            SceneManager.SetActiveScene(anchorScene);
            originalScene = anchorScene;
        }

        Scene m05Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        GameObject backpack = new GameObject("背包UI");
        GameObject workflow = new GameObject("M05 Draw Workflow");
        SceneManager.MoveGameObjectToScene(backpack, m05Scene);
        SceneManager.MoveGameObjectToScene(workflow, m05Scene);

        try
        {
            GameObject templatePanel = new GameObject("01通用物品背包面板");
            templatePanel.transform.SetParent(backpack.transform);
            GameObject grid = new GameObject("Grid");
            grid.transform.SetParent(templatePanel.transform);
            GameObject buttonTemplate = new GameObject("Template Button");
            buttonTemplate.transform.SetParent(grid.transform);
            buttonTemplate.AddComponent<PressableButton>();
            backpack.AddComponent<TexCore_05BackpackController>();
            TexCore_M05DrawStep step = workflow.AddComponent<TexCore_M05DrawStep>();

            SceneManager.SetActiveScene(originalScene);
            TexCore_05BackpackController.Present(step);

            Transform panel = backpack.transform.Find("05划线贴码背包面板");
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.gameObject.activeSelf, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(backpack);
            Object.DestroyImmediate(workflow);
            EditorSceneManager.CloseScene(m05Scene, true);
            SceneManager.SetActiveScene(originalScene);
            if (closeAnchorScene)
            {
                EditorSceneManager.CloseScene(anchorScene, true);
            }
        }
    }

    [Test]
    public void M05QrPasteStep_RequiresQrSummonBeforePairsAreInteractable()
    {
        GameObject huaXian = new GameObject("HuaXian");

        try
        {
            GameObject qrRoot = new GameObject("QRCode");
            qrRoot.transform.SetParent(huaXian.transform);
            GameObject targetRoot = new GameObject("Pasting");
            targetRoot.transform.SetParent(huaXian.transform);
            GameObject qr = new GameObject("ErWeiMa1");
            qr.transform.SetParent(qrRoot.transform);
            ObjectManipulator manipulator = qr.AddComponent<ObjectManipulator>();
            GameObject target = new GameObject("ZhanTieDian1");
            target.transform.SetParent(targetRoot.transform);
            TexCore_M05QrPasteStep step = huaXian.AddComponent<TexCore_M05QrPasteStep>();
            step.Configure(qrRoot.transform, targetRoot.transform);

            step.UnderWay();

            Assert.That(step.RequiredBackpackAction, Is.EqualTo("二维码"));
            Assert.That(qr.activeSelf, Is.False);
            Assert.That(target.activeSelf, Is.False);
            Assert.That(manipulator.enabled, Is.False);

            step.UseBackpackAction("二维码");

            Assert.That(step.RequiredBackpackAction, Is.Empty);
            Assert.That(qr.activeSelf, Is.True);
            Assert.That(target.activeSelf, Is.True);
            Assert.That(manipulator.enabled, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(huaXian);
        }
    }

    [Test]
    public void TopicLevelAndNodeLevelNextButtons_AreCollectedAndHidden()
    {
        GameObject policyRoot = new GameObject("Next Button Policy Root");
        GameObject nodeRoot = new GameObject("Next Button Node");
        GameObject topicRoot = new GameObject("Next Button Topic");
        Dictionary<GameObject, bool> sceneButtonStates = SnapshotNextButtonStates();
        GameObject nodeNext = null;
        GameObject topicNext = null;

        try
        {
            TexCore_03SceneFlowPolicy policy =
                policyRoot.AddComponent<TexCore_03SceneFlowPolicy>();
            NodePanelBase node = nodeRoot.AddComponent<SpyNodePanel>();
            PanelTopicBase topic = topicRoot.AddComponent<Panel_Select>();

            nodeNext = new GameObject("下一步");
            nodeNext.transform.SetParent(nodeRoot.transform);
            node.btn_Next = nodeNext;
            topicNext = new GameObject("下一步");
            topicNext.transform.SetParent(topicRoot.transform);
            topic.btn_Next = topicNext;

            InvokePolicy(policy, "RebuildCaches");
            List<GameObject> cached = ReadNextButtons(policy);

            Assert.That(cached, Has.Member(nodeNext), "节点级“下一步”应进入隐藏缓存。");
            Assert.That(cached, Has.Member(topicNext), "题目级“下一步”应进入隐藏缓存。");

            nodeNext.SetActive(true);
            topicNext.SetActive(true);
            InvokePolicy(policy, "HideNextButtons");

            Assert.That(nodeNext.activeSelf, Is.False, "节点级“下一步”应被策略隐藏。");
            Assert.That(topicNext.activeSelf, Is.False, "题目级“下一步”应被策略隐藏。");
        }
        finally
        {
            RestoreForeignButtonStates(sceneButtonStates, nodeNext, topicNext);
            Object.DestroyImmediate(policyRoot);
            Object.DestroyImmediate(nodeRoot);
            Object.DestroyImmediate(topicRoot);
        }
    }

    [Test]
    public void ConfirmSkipBackpackAndScreenButtons_AreNotTreatedAsNextButtons()
    {
        Assert.That(TexCore_03SceneFlowPolicy.IsStandardNextButtonName("确定"), Is.False);
        Assert.That(TexCore_03SceneFlowPolicy.IsStandardNextButtonName("跳过"), Is.False);
        Assert.That(TexCore_03SceneFlowPolicy.IsStandardNextButtonName("U盘"), Is.False);
        Assert.That(TexCore_03SceneFlowPolicy.IsStandardNextButtonName("02服务器系统安装背包面板"), Is.False);
        Assert.That(TexCore_03SceneFlowPolicy.IsStandardNextButtonName("M02_IP_Apply"), Is.False);
        Assert.That(TexCore_03SceneFlowPolicy.IsStandardNextButtonName("下一步"), Is.True);

        GameObject policyRoot = new GameObject("Next Button Exclusion Root");
        GameObject confirm = new GameObject("确定");
        GameObject skip = new GameObject("跳过");
        GameObject usb = new GameObject("U盘");
        GameObject backpackPanel = new GameObject("02服务器系统安装背包面板");
        GameObject screenButton = new GameObject("M02_IP_Apply");
        Dictionary<GameObject, bool> sceneButtonStates = SnapshotNextButtonStates();

        try
        {
            TexCore_03SceneFlowPolicy policy =
                policyRoot.AddComponent<TexCore_03SceneFlowPolicy>();
            InvokePolicy(policy, "RebuildCaches");

            List<GameObject> cached = ReadNextButtons(policy);
            Assert.That(cached, Has.No.Member(confirm));
            Assert.That(cached, Has.No.Member(skip));
            Assert.That(cached, Has.No.Member(usb));
            Assert.That(cached, Has.No.Member(backpackPanel));
            Assert.That(cached, Has.No.Member(screenButton));

            InvokePolicy(policy, "HideNextButtons");
            Assert.That(confirm.activeSelf, Is.True, "“确定”不应被“下一步”策略隐藏。");
            Assert.That(skip.activeSelf, Is.True, "“跳过”不应被“下一步”策略隐藏。");
            Assert.That(usb.activeSelf, Is.True, "U 盘按钮不应被“下一步”策略隐藏。");
            Assert.That(backpackPanel.activeSelf, Is.True, "背包面板不应被“下一步”策略隐藏。");
            Assert.That(screenButton.activeSelf, Is.True, "屏幕内部按钮不应被“下一步”策略隐藏。");
        }
        finally
        {
            RestoreForeignButtonStates(sceneButtonStates);
            Object.DestroyImmediate(policyRoot);
            Object.DestroyImmediate(confirm);
            Object.DestroyImmediate(skip);
            Object.DestroyImmediate(usb);
            Object.DestroyImmediate(backpackPanel);
            Object.DestroyImmediate(screenButton);
        }
    }

    [Test]
    public void CompletedTopic_AutoAdvancesToNextTopicAfterOneFrame()
    {
        GameObject policyRoot = new GameObject("Auto Advance Policy Root");
        GameObject nodeRoot = new GameObject("Auto Advance Node");
        GameObject topicRoot = new GameObject("Auto Advance Topic");

        try
        {
            TexCore_03SceneFlowPolicy policy =
                policyRoot.AddComponent<TexCore_03SceneFlowPolicy>();
            SpyNodePanel node = nodeRoot.AddComponent<SpyNodePanel>();
            PanelTopicBase topic = topicRoot.AddComponent<Panel_Select>();
            TexCore_DataClass topicA;
            TexCore_DataClass topicB;
            node.datas = CreateTwoTopicNodeData(out topicA, out topicB);
            topic.datas = topicA;
            node.activeChild = topic;

            InvokePolicy(policy, "RebuildCaches");
            InvokePolicy(policy, "QueueTopicCompletion", topicA.name, 10);

            // 同帧只入队，等待完成状态写入。
            InvokePolicy(policy, "ProcessPendingTopicCompletions", 10);
            Assert.That(node.NextTopicCallCount, Is.EqualTo(0),
                "完成事件当帧不应推进，需要延迟一帧。");

            InvokePolicy(policy, "ProcessPendingTopicCompletions", 11);
            Assert.That(node.NextTopicCallCount, Is.EqualTo(1),
                "题目完成后应延迟一帧自动进入下一题。");
        }
        finally
        {
            Object.DestroyImmediate(policyRoot);
            Object.DestroyImmediate(nodeRoot);
            Object.DestroyImmediate(topicRoot);
        }
    }

    [Test]
    public void LastCompletedTopic_ResolvesToClickNextForNextNode()
    {
        GameObject nodeRoot = new GameObject("Last Topic Node");
        GameObject topicRoot = new GameObject("Last Topic Panel");

        try
        {
            NodePanelBase node = nodeRoot.AddComponent<SpyNodePanel>();
            PanelTopicBase topic = topicRoot.AddComponent<Panel_Select>();
            TexCore_DataClass topicA;
            TexCore_DataClass topicB;
            node.datas = CreateTwoTopicNodeData(out topicA, out topicB);
            topic.datas = topicB;
            node.activeChild = topic;

            MethodInfo resolve = typeof(TexCore_03SceneFlowPolicy).GetMethod(
                "ResolveNextAdvance",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(resolve, Is.Not.Null);

            object lastTopicResult = resolve.Invoke(null, new object[] { node, topicB.name });
            Assert.That(lastTopicResult.ToString(), Is.EqualTo("ClickNext"),
                "节点最后一题完成后应通过 ClickNext 进入下一个节点。");

            // 中间题完成时活动题目仍是它自身，才会解析为 NextTopic。
            topic.datas = topicA;
            object middleTopicResult = resolve.Invoke(null, new object[] { node, topicA.name });
            Assert.That(middleTopicResult.ToString(), Is.EqualTo("NextTopic"),
                "非最后一题完成后应调用 NextTopic 切换下一题。");
        }
        finally
        {
            Object.DestroyImmediate(nodeRoot);
            Object.DestroyImmediate(topicRoot);
        }
    }

    [Test]
    public void TopicAlreadyAdvancedByOwnLogic_IsNotAdvancedAgain()
    {
        GameObject policyRoot = new GameObject("M02 Advance Policy Root");
        GameObject nodeRoot = new GameObject("M02 Advance Node");
        GameObject topicRoot = new GameObject("M02 Advance Topic");

        try
        {
            TexCore_03SceneFlowPolicy policy =
                policyRoot.AddComponent<TexCore_03SceneFlowPolicy>();
            SpyNodePanel node = nodeRoot.AddComponent<SpyNodePanel>();
            PanelTopicBase topic = topicRoot.AddComponent<Panel_Select>();
            TexCore_DataClass topicA;
            TexCore_DataClass topicB;
            node.datas = CreateTwoTopicNodeData(out topicA, out topicB);
            // 模拟 M02 / 视频题已经自行切题：活动题目已从 A 变为 B。
            topic.datas = topicB;
            node.activeChild = topic;

            InvokePolicy(policy, "RebuildCaches");
            InvokePolicy(policy, "QueueTopicCompletion", topicA.name, 10);
            InvokePolicy(policy, "ProcessPendingTopicCompletions", 11);

            Assert.That(node.NextTopicCallCount, Is.EqualTo(0),
                "已自行切题的题目不能被场景策略重复推进。");
        }
        finally
        {
            Object.DestroyImmediate(policyRoot);
            Object.DestroyImmediate(nodeRoot);
            Object.DestroyImmediate(topicRoot);
        }
    }

    [Test]
    public void PendingCompletionQueue_DeduplicatesSameTopic()
    {
        GameObject policyRoot = new GameObject("Pending Dedupe Root");

        try
        {
            TexCore_03SceneFlowPolicy policy =
                policyRoot.AddComponent<TexCore_03SceneFlowPolicy>();

            InvokePolicy(policy, "QueueTopicCompletion", "题目A", 1);
            InvokePolicy(policy, "QueueTopicCompletion", "题目A", 2);
            InvokePolicy(policy, "QueueTopicCompletion", "题目B", 2);

            IList pending = ReadPendingCompletions(policy);
            Assert.That(pending.Count, Is.EqualTo(2),
                "同一题目的重复完成事件只应入队一次。");
        }
        finally
        {
            Object.DestroyImmediate(policyRoot);
        }
    }

    /// <summary>
    /// 回归：<see cref="TexCore_03SceneFlowPolicy"/> 的提前返回必须同时看完成队列。
    /// </summary>
    /// <remarks>
    /// 完成事件与本次 Update 落在同一帧时，这次 Update 只会消费掉 flowDirty，
    /// 而队列表项要等下一帧才满足延迟一帧条件。若提前返回条件只看 flowDirty，
    /// 下一帧没有任何新事件再把流程标脏，队列就再也排不掉——这正是所有选择题
    /// 点击正确答案后不自动跳题的现场。本用例全程走真实 Update() 入口。
    /// </remarks>
    [Test]
    public void PendingCompletionQueue_DrainsThroughUpdateWhenFlowIsNotDirty()
    {
        GameObject policyRoot = new GameObject("Stranded Queue Policy Root");
        GameObject nodeRoot = new GameObject("Stranded Queue Node");
        GameObject topicRoot = new GameObject("Stranded Queue Topic");

        try
        {
            TexCore_03SceneFlowPolicy policy =
                policyRoot.AddComponent<TexCore_03SceneFlowPolicy>();
            SetPolicyField(policy, "targetSceneName", GetTargetableSceneName());

            SpyNodePanel node = nodeRoot.AddComponent<SpyNodePanel>();
            PanelTopicBase topic = topicRoot.AddComponent<Panel_Select>();
            TexCore_DataClass topicA;
            TexCore_DataClass topicB;
            node.datas = CreateTwoTopicNodeData(out topicA, out topicB);
            topic.datas = topicA;
            node.activeChild = topic;

            InvokePolicy(policy, "RebuildCaches");

            // 完成事件先于本次 Update 落地。帧号取 -1，等价于“早已满足延迟一帧”，
            // 不依赖编辑模式里会自己跳动的 Time.frameCount。
            InvokePolicy(policy, "QueueTopicCompletion", topicA.name, -1);
            // 入队会把流程标脏，这里还原成“脏标记已被上一帧的 Update 消费掉”的现场：
            // 队列非空、flowDirty 为 false，正是线上卡死现场在下一帧的等价状态。
            SetPolicyField(policy, "flowDirty", false);
            Assert.That(ReadPendingCompletions(policy).Count, Is.EqualTo(1),
                "未推进的完成事件必须留在队列里。");

            InvokePolicy(policy, "Update");
            Assert.That(node.NextTopicCallCount, Is.EqualTo(1),
                "完成队列非空时 Update 必须继续排空，不能因 flowDirty 为 false 提前返回。");
            Assert.That(ReadPendingCompletions(policy).Count, Is.EqualTo(0),
                "已推进的完成事件必须从队列移除。");

            // 排空之后没有新事件，Update 必须恢复空转，不能重复推进同一题。
            InvokePolicy(policy, "Update");
            Assert.That(node.NextTopicCallCount, Is.EqualTo(1),
                "队列清空后 Update 不得重复推进同一题。");
        }
        finally
        {
            Object.DestroyImmediate(policyRoot);
            Object.DestroyImmediate(nodeRoot);
            Object.DestroyImmediate(topicRoot);
        }
    }

    /// <summary>
    /// 同一道题重复广播“完成题目”时，队列只保留一条，Update 也只推进一次。
    /// </summary>
    [Test]
    public void DuplicateCompletionEvents_AdvanceOnlyOnceThroughUpdate()
    {
        GameObject policyRoot = new GameObject("Duplicate Completion Policy Root");
        GameObject nodeRoot = new GameObject("Duplicate Completion Node");
        GameObject topicRoot = new GameObject("Duplicate Completion Topic");

        try
        {
            TexCore_03SceneFlowPolicy policy =
                policyRoot.AddComponent<TexCore_03SceneFlowPolicy>();
            SetPolicyField(policy, "targetSceneName", GetTargetableSceneName());

            SpyNodePanel node = nodeRoot.AddComponent<SpyNodePanel>();
            PanelTopicBase topic = topicRoot.AddComponent<Panel_Select>();
            TexCore_DataClass topicA;
            TexCore_DataClass topicB;
            node.datas = CreateTwoTopicNodeData(out topicA, out topicB);
            topic.datas = topicA;
            node.activeChild = topic;

            InvokePolicy(policy, "RebuildCaches");
            InvokePolicy(policy, "QueueTopicCompletion", topicA.name, -1);
            InvokePolicy(policy, "QueueTopicCompletion", topicA.name, -1);

            Assert.That(ReadPendingCompletions(policy).Count, Is.EqualTo(1),
                "同一题目的重复完成事件只应入队一次。");

            InvokePolicy(policy, "Update");
            Assert.That(node.NextTopicCallCount, Is.EqualTo(1),
                "重复完成事件不能造成重复跳题。");

            InvokePolicy(policy, "Update");
            Assert.That(node.NextTopicCallCount, Is.EqualTo(1),
                "队列排空后不得再次推进。");
        }
        finally
        {
            Object.DestroyImmediate(policyRoot);
            Object.DestroyImmediate(nodeRoot);
            Object.DestroyImmediate(topicRoot);
        }
    }

    /// <summary>
    /// 整链回归：选择题点击正确答案 → 自动提交 → 广播“完成题目” → <see cref="Update"/>
    /// 排空队列并推进到下一题。
    /// </summary>
    /// <remarks>
    /// 前面的用例分别验证了“点选后自动提交”和“队列非空时 Update 会排空”，
    /// 但把两段接起来的入口一直是手工调用 <c>QueueTopicCompletion</c>。本用例让
    /// <see cref="Panel_Select"/> 自己广播完成事件、再由真实 <c>Update()</c> 处理，
    /// 覆盖 01/05/06/07 选择题“选对答案后不跳题”的现场：完成事件与完成当帧的
    /// Update 落在同一帧，队列表项要等下一帧才满足延迟一帧条件。
    /// </remarks>
    [Test]
    public void SelectTopic_SelfBroadcastCompletion_AdvancesThroughUpdate()
    {
        string topicName = "策略回归端到端选择题_" +
            System.Guid.NewGuid().ToString("N");
        GameObject topic = CreateSelectTopic(
            SelecionCount.单选, "B.HTTP", "A.FTP_B.HTTP_C.SMTP_D.POP3", true);
        GameObject policyRoot = new GameObject("Select Advance Policy Root");
        GameObject nodeRoot = new GameObject("Select Advance Node");
        TopicCompletionCounter completions = null;

        try
        {
            topic.name = topicName;
            Panel_Select panel = topic.GetComponent<Panel_Select>();
            panel.datas.name = topicName;
            TexCore_03SceneFlowPolicy policy =
                policyRoot.AddComponent<TexCore_03SceneFlowPolicy>();
            SetPolicyField(policy, "targetSceneName", GetTargetableSceneName());

            SpyNodePanel node = nodeRoot.AddComponent<SpyNodePanel>();
            TexCore_DataClass topicA;
            TexCore_DataClass topicB;
            // 活动题目的数据对象必须与题目同名，策略才认得出“这题仍是活动题目”。
            node.datas = CreateTwoTopicNodeData(topicName, out topicA, out topicB);
            panel.datas = topicA;
            node.activeChild = panel;

            InvokeSelectLifecycle(panel, "Awake");
            InvokeSelectLifecycle(panel, "OnEnable");
            completions = new TopicCompletionCounter(topicName);
            InvokePolicy(policy, "RebuildCaches");

            // 只点正确答案，不再点“确定”：题目必须自己提交。
            ClickOption(panel, "B.HTTP");

            Assert.That(panel.isSubmit, Is.True,
                "选中正确答案后选择题应自动提交。");
            Assert.That(completions.Count, Is.EqualTo(1),
                "自动提交只广播一次“完成题目”。");
            Assert.That(node.NextTopicCallCount, Is.EqualTo(0),
                "提交当帧只入队，不推进。");

            // Update 是真实入口：完成事件已经落在上一帧，队列必须能排空。
            InvokePolicy(policy, "Update");

            Assert.That(node.NextTopicCallCount, Is.EqualTo(1),
                "自动提交的完成事件必须经 Update 推进到下一题。");
            Assert.That(ReadPendingCompletions(policy).Count, Is.EqualTo(0),
                "推进后完成队列必须清空。");

            InvokePolicy(policy, "Update");
            Assert.That(node.NextTopicCallCount, Is.EqualTo(1),
                "队列清空后 Update 不得重复推进同一题。");
        }
        finally
        {
            if (completions != null) completions.Dispose();
            Object.DestroyImmediate(policyRoot);
            Object.DestroyImmediate(nodeRoot);
            Object.DestroyImmediate(topic);
        }
    }

    /// <summary>
    /// 选项名前缀回归：<c>A.</c> / <c>B.</c> / <c>C.</c> / <c>D.</c> 四种选项都要走完
    /// “点击 → 自动提交 → 完成事件 → Update 推进”，前缀不能吞掉点击。
    /// </summary>
    [Test]
    public void SelectTopic_LetterPrefixedOptions_AllAutoSubmitThenAdvanceThroughUpdate()
    {
        string topicName = "策略回归前缀选择题_" +
            System.Guid.NewGuid().ToString("N");
        GameObject topic = CreateSelectTopic(
            SelecionCount.多选, "A.FTP_B.HTTP_C.SMTP_D.POP3",
            "A.FTP_B.HTTP_C.SMTP_D.POP3", true);
        GameObject policyRoot = new GameObject("Prefix Policy Root");
        GameObject nodeRoot = new GameObject("Prefix Node");
        TopicCompletionCounter completions = null;

        try
        {
            topic.name = topicName;
            Panel_Select panel = topic.GetComponent<Panel_Select>();
            panel.datas.name = topicName;
            TexCore_03SceneFlowPolicy policy =
                policyRoot.AddComponent<TexCore_03SceneFlowPolicy>();
            SetPolicyField(policy, "targetSceneName", GetTargetableSceneName());

            SpyNodePanel node = nodeRoot.AddComponent<SpyNodePanel>();
            TexCore_DataClass topicA;
            TexCore_DataClass topicB;
            node.datas = CreateTwoTopicNodeData(topicName, out topicA, out topicB);
            panel.datas = topicA;
            node.activeChild = panel;

            InvokeSelectLifecycle(panel, "Awake");
            InvokeSelectLifecycle(panel, "OnEnable");
            completions = new TopicCompletionCounter(topicName);
            InvokePolicy(policy, "RebuildCaches");

            string[] optionNames = { "A.FTP", "B.HTTP", "C.SMTP", "D.POP3" };
            for (int index = 0; index < optionNames.Length; index++)
            {
                ClickOption(panel, optionNames[index]);

                Assert.That(panel.steps, Does.Contain(optionNames[index]),
                    "前缀选项点击必须进入活动选择题：" + optionNames[index]);
                Assert.That(panel.isSubmit, Is.EqualTo(index == optionNames.Length - 1),
                    "多选只有选满答案数量才提交：" + optionNames[index]);
            }

            Assert.That(completions.Count, Is.EqualTo(1),
                "四个前缀选项走完后只应广播一次“完成题目”。");

            InvokePolicy(policy, "Update");
            Assert.That(node.NextTopicCallCount, Is.EqualTo(1),
                "选满正确答案数量后必须通过 Update 推进到下一题。");
        }
        finally
        {
            if (completions != null) completions.Dispose();
            Object.DestroyImmediate(policyRoot);
            Object.DestroyImmediate(nodeRoot);
            Object.DestroyImmediate(topic);
        }
    }

    [Test]
    public void TopicAndStateEvents_OnlyMarkFlowDirtyWithoutCacheRebuild()
    {
        GameObject policyRoot = new GameObject("Dirty Split Policy Root");

        try
        {
            TexCore_03SceneFlowPolicy policy =
                policyRoot.AddComponent<TexCore_03SceneFlowPolicy>();
            SetPolicyField(policy, "cacheDirty", false);
            SetPolicyField(policy, "flowDirty", false);

            InvokePolicy(policy, "OnTopicShown", "题目A");
            Assert.That(GetPolicyField<bool>(policy, "flowDirty"), Is.True,
                "“更新题目”应标记流程刷新。");
            Assert.That(GetPolicyField<bool>(policy, "cacheDirty"), Is.False,
                "“更新题目”不应触发场景级缓存重建。");
            Assert.That(GetPolicyField<bool>(policy, "topicSubtreeScanPending"), Is.True,
                "“更新题目”应请求活动题目子树扫描。");

            SetPolicyField(policy, "flowDirty", false);
            InvokePolicy(policy, "OnObjectStateChanged", 0, 1, 2);
            Assert.That(GetPolicyField<bool>(policy, "flowDirty"), Is.True,
                "“设置物体状态”应标记流程刷新。");
            Assert.That(GetPolicyField<bool>(policy, "cacheDirty"), Is.False,
                "“设置物体状态”不应触发场景级缓存重建。");

            SetPolicyField(policy, "flowDirty", false);
            InvokePolicy(policy, "QueueTopicCompletion", "题目A", 1);
            Assert.That(GetPolicyField<bool>(policy, "flowDirty"), Is.True,
                "“完成题目”应标记流程刷新。");
            Assert.That(GetPolicyField<bool>(policy, "cacheDirty"), Is.False,
                "“完成题目”不应触发场景级缓存重建。");
        }
        finally
        {
            Object.DestroyImmediate(policyRoot);
        }
    }

    [Test]
    public void NodeChangeEvent_StillTriggersCacheRebuild()
    {
        GameObject policyRoot = new GameObject("Node Cache Policy Root");

        try
        {
            TexCore_03SceneFlowPolicy policy =
                policyRoot.AddComponent<TexCore_03SceneFlowPolicy>();
            SetPolicyField(policy, "cacheDirty", false);
            SetPolicyField(policy, "flowDirty", false);

            InvokePolicy(policy, "OnNodeChanged", "节点A");

            Assert.That(GetPolicyField<bool>(policy, "cacheDirty"), Is.True,
                "“更新节点”应允许重新建立节点缓存。");
            Assert.That(GetPolicyField<bool>(policy, "flowDirty"), Is.True,
                "“更新节点”应同时标记流程刷新。");
        }
        finally
        {
            Object.DestroyImmediate(policyRoot);
        }
    }

    [Test]
    public void Update_DoesNotCallM08GuardConfigurationEveryFrame()
    {
        string source = File.ReadAllText(
            Path.Combine(Application.dataPath, "A_MRNet", "Scripts",
                "TexCore_03SceneFlowPolicy.cs"));
        string update = ExtractMethod(source, "private void Update()");

        Assert.That(update, Does.Not.Contain("ConfigureM08UnpackGuard"),
            "Update 不能每帧调用 M08 全局查找初始化。");
        Assert.That(update, Does.Contain("ProcessM08GuardRetry"),
            "Update 应通过有限重试入口处理 M08 初始化。");

        Assert.That(GetStaticField<int>("M08GuardMaxRetries"), Is.EqualTo(40),
            "M08 延迟绑定最多重试 40 次。");
        Assert.That(GetStaticField<float>("M08GuardRetryInterval"), Is.EqualTo(0.25f),
            "M08 延迟绑定重试间隔为 0.25 秒。");
    }

    [Test]
    public void M08GuardRetry_IsBoundedInsteadOfPerFrame()
    {
        GameObject policyRoot = new GameObject("M08 Retry Policy Root");

        try
        {
            TexCore_03SceneFlowPolicy policy =
                policyRoot.AddComponent<TexCore_03SceneFlowPolicy>();
            SetPolicyField(policy, "m08GuardInitPending", true);
            SetPolicyField(policy, "m08GuardRetryCount", 0);

            bool configured = false;
            int guardedCount = 0;
            for (int attempt = 0; attempt < 60; attempt++)
            {
                InvokePolicy(policy, "TryConfigureM08GuardOnce");
                configured = GetPolicyField<bool>(policy, "m08UnpackGuardConfigured");
                guardedCount = GetPolicyField<int>(policy, "m08GuardRetryCount");
                if (configured || guardedCount >= 40)
                {
                    break;
                }
            }

            Assert.That(configured || guardedCount == 40, Is.True,
                "M08 初始化失败时应重试到 40 次上限，成功后立即停止。");

            if (configured)
            {
                for (int attempt = 0; attempt < 5; attempt++)
                {
                    InvokePolicy(policy, "TryConfigureM08GuardOnce");
                }

                Assert.That(GetPolicyField<int>(policy, "m08GuardRetryCount"),
                    Is.EqualTo(0), "配置成功后不应继续累计重试计数。");
            }
        }
        finally
        {
            Object.DestroyImmediate(policyRoot);
        }
    }

    [Test]
    public void M08GuardCache_PreservesScrewOrderAndUsesCachedLidComponents()
    {
        string source = File.ReadAllText(
            Path.Combine(Application.dataPath, "A_MRNet", "Scripts",
                "TexCore_03SceneFlowPolicy.cs"));
        string gateMethod = ExtractMethod(
            source, "private void EnforceM08BoxLidInteractionGate(");
        Assert.That(gateMethod, Does.Not.Contain("GetComponentsInChildren"),
            "盖子门禁不应每帧执行 GetComponentsInChildren。");

        GameObject policyRoot = new GameObject("M08 Guard Policy Root");
        GameObject m08NodeRoot = new GameObject("08充电桩安装");
        List<GameObject> created = new List<GameObject> { policyRoot, m08NodeRoot };
        Dictionary<Component, bool> componentSnapshot = new Dictionary<Component, bool>();

        try
        {
            for (int index = 1; index <= 8; index++)
            {
                GameObject screw = new GameObject(
                    "08_00_使用手电钻钻开包装箱螺丝" + index);
                screw.transform.SetParent(m08NodeRoot.transform);
                screw.AddComponent<TexCore_OrderedPlacementStep>();
                created.Add(screw);
            }

            GameObject lid = new GameObject("08_00_移开包装箱盖");
            lid.transform.SetParent(m08NodeRoot.transform);
            lid.AddComponent<TexCore_ClickObjectStep>();
            created.Add(lid);

            GameObject dingGai = new GameObject("DingGai");
            dingGai.transform.SetParent(m08NodeRoot.transform);
            ObjectManipulator manipulator = dingGai.AddComponent<ObjectManipulator>();
            BoxCollider lidCollider = dingGai.AddComponent<BoxCollider>();
            created.Add(dingGai);

            foreach (GameObject dingGaiObject in Object.FindObjectsOfType<GameObject>(true))
            {
                if (dingGaiObject == null || dingGaiObject.name != "DingGai") continue;
                foreach (ObjectManipulator cached in
                         dingGaiObject.GetComponentsInChildren<ObjectManipulator>(true))
                {
                    if (cached != null) componentSnapshot[cached] = cached.enabled;
                }

                foreach (Collider cached in dingGaiObject.GetComponentsInChildren<Collider>(true))
                {
                    if (cached != null) componentSnapshot[cached] = cached.enabled;
                }
            }

            TexCore_03SceneFlowPolicy policy =
                policyRoot.AddComponent<TexCore_03SceneFlowPolicy>();
            InvokePolicy(policy, "ConfigureM08UnpackGuard");

            Assert.That(GetPolicyField<bool>(policy, "m08UnpackGuardConfigured"), Is.True,
                "完整的 M08 测试夹具应完成防误操作初始化。");

            List<TexCore_OrderedPlacementStep> cachedScrews =
                GetPolicyField<List<TexCore_OrderedPlacementStep>>(policy, "m08ScrewSteps");
            Assert.That(cachedScrews.Count, Is.EqualTo(8), "应缓存八颗螺丝步骤。");
            for (int index = 0; index < 8; index++)
            {
                Assert.That(cachedScrews[index].name,
                    Is.EqualTo("08_00_使用手电钻钻开包装箱螺丝" + (index + 1)),
                    "M08 缓存必须保持八颗螺丝的顺序。");
            }

            IList entries =
                GetPolicyField<IList>(policy, "m08LidGateEntries");
            bool entryFound = false;
            foreach (object entry in entries)
            {
                GameObject lidObject = (GameObject)entry.GetType()
                    .GetField("LidObject").GetValue(entry);
                if (lidObject != dingGai) continue;
                List<ObjectManipulator> manipulators = (List<ObjectManipulator>)entry
                    .GetType().GetField("Manipulators").GetValue(entry);
                entryFound = manipulators.Contains(manipulator);
                break;
            }

            Assert.That(entryFound, Is.True,
                "盖子组件应在初始化时进入缓存，而不是每帧重新查找。");

            GameObject topicRoot = new GameObject("M08 Gate Topic");
            created.Add(topicRoot);
            SpyNodePanel node = m08NodeRoot.AddComponent<SpyNodePanel>();
            PanelTopicBase topic = topicRoot.AddComponent<Panel_Select>();
            node.activeChild = topic;
            topic.stepIndex = 0;

            InvokePolicy(policy, "EnforceM08BoxLidInteractionGate", node, 0);

            Assert.That(manipulator.enabled, Is.False,
                "螺丝未完成时盖子的 ObjectManipulator 应被禁用。");
            Assert.That(lidCollider.enabled, Is.False,
                "螺丝未完成时盖子的 Collider 应被禁用。");
        }
        finally
        {
            foreach (KeyValuePair<Component, bool> entry in componentSnapshot)
            {
                if (entry.Key == null) continue;
                if (entry.Key is ObjectManipulator manipulator)
                {
                    manipulator.enabled = entry.Value;
                }
                else if (entry.Key is Collider collider)
                {
                    collider.enabled = entry.Value;
                }
            }

            foreach (GameObject item in created)
            {
                if (item != null) Object.DestroyImmediate(item);
            }
        }
    }

    [Test]
    public void UnconfiguredM08Guard_DoesNotMisjudgeBusinessState()
    {
        GameObject policyRoot = new GameObject("M08 Guard Off Policy Root");
        GameObject nodeRoot = new GameObject("M08 Guard Off Node");
        GameObject topicRoot = new GameObject("M08 Guard Off Topic");

        try
        {
            TexCore_03SceneFlowPolicy policy =
                policyRoot.AddComponent<TexCore_03SceneFlowPolicy>();
            SpyNodePanel node = nodeRoot.AddComponent<SpyNodePanel>();
            PanelTopicBase topic = topicRoot.AddComponent<Panel_Select>();
            TexCore_DataClass nodeData = new TexCore_DataClass { name = "08充电桩安装" };
            TexCore_DataClass topicData = new TexCore_DataClass
            {
                name = "00开箱取出充电桩",
                parent = nodeData
            };
            nodeData.childData = new List<TexCore_DataClass> { topicData };
            node.datas = nodeData;
            topic.datas = topicData;
            node.activeChild = topic;

            InvokePolicy(policy, "RebuildCaches");
            // 强制处于“初始化失败”状态：找不到 M08 对象时不能误改业务状态。
            SetPolicyField(policy, "m08UnpackGuardConfigured", false);
            topic.stepIndex = 2;

            InvokePolicy(policy, "EnforceM08UnpackSequence");

            Assert.That(topic.stepIndex, Is.EqualTo(2),
                "M08 初始化失败时不能把步骤状态误判为完成。");
        }
        finally
        {
            Object.DestroyImmediate(policyRoot);
            Object.DestroyImmediate(nodeRoot);
            Object.DestroyImmediate(topicRoot);
        }
    }

    [Test]
    public void ActiveSelectTopicConfirm_IsCachedAndHiddenLikeOperateConfirm()
    {
        GameObject policyRoot = new GameObject("Select Confirm Policy Root");
        GameObject quizRoot = null;
        GameObject operateRoot = null;

        try
        {
            TexCore_03SceneFlowPolicy policy =
                policyRoot.AddComponent<TexCore_03SceneFlowPolicy>();

            // 真实题目预制体里 Panel_Select 挂在“选择题”子物体上，
            // “确定”是它的子节点；夹具保持同一层级关系。
            GameObject quizConfirm = null;
            Panel_Select quiz = CreateQuizTopic(
                "Quiz Topic Root", out quizRoot, out quizConfirm);
            SpyNodePanel quizNode = quizRoot.AddComponent<SpyNodePanel>();
            quizNode.activeChild = quiz;

            GameObject operateConfirm = null;
            Panel_Operate operate = CreateOperateTopic(
                "Operate Topic Root", out operateRoot, out operateConfirm);
            SpyNodePanel operateNode = operateRoot.AddComponent<SpyNodePanel>();
            operateNode.activeChild = operate;

            InvokePolicy(policy, "RebuildCaches");
            List<Transform> cached =
                GetPolicyField<List<Transform>>(policy, "confirmButtons");

            Assert.That(TexCore_03SceneFlowPolicy.ShouldHideConfirmButton(
                quizConfirm.transform), Is.True,
                "选择题的“确定”也属于标准确认按钮。");
            Assert.That(TexCore_03SceneFlowPolicy.ShouldHideConfirmButton(
                operateConfirm.transform), Is.True,
                "普通操作题的“确定”仍按现有策略隐藏。");
            Assert.That(cached, Has.Member(quizConfirm.transform),
                "选择题的“确定”应进入确认按钮缓存。");
            Assert.That(cached, Has.Member(operateConfirm.transform),
                "操作题的“确定”应进入确认按钮缓存。");

            quizConfirm.SetActive(true);
            operateConfirm.SetActive(true);
            InvokePolicy(policy, "HideConfirmButtons");

            Assert.That(quizConfirm.activeSelf, Is.False,
                "场景策略必须把选择题的“确定”压下去。");
            Assert.That(operateConfirm.activeSelf, Is.False,
                "普通操作题的“确定”仍按现有策略隐藏。");
        }
        finally
        {
            Object.DestroyImmediate(policyRoot);
            if (quizRoot != null) Object.DestroyImmediate(quizRoot);
            if (operateRoot != null) Object.DestroyImmediate(operateRoot);
        }
    }

    [Test]
    public void DynamicTopicButtons_AreCollectedFromActiveTopicSubtreeOnly()
    {
        GameObject policyRoot = new GameObject("Subtree Policy Root");
        GameObject nodeRoot = new GameObject("Subtree Node");
        GameObject oldTopicRoot = new GameObject("Subtree Old Topic");
        Dictionary<GameObject, bool> sceneButtonStates = SnapshotNextButtonStates();

        try
        {
            TexCore_03SceneFlowPolicy policy =
                policyRoot.AddComponent<TexCore_03SceneFlowPolicy>();
            SpyNodePanel node = nodeRoot.AddComponent<SpyNodePanel>();
            PanelTopicBase oldTopic = oldTopicRoot.AddComponent<Panel_Select>();
            node.activeChild = oldTopic;

            InvokePolicy(policy, "RebuildCaches");
            SetPolicyField(policy, "nextButtons", new List<GameObject>());
            SetPolicyField(policy, "confirmButtons", new List<Transform>());

            // 动态题目出现：更新题目事件后只扫描活动题目子树。
            GameObject newTopicRoot = new GameObject("Subtree New Topic");
            Panel_Operate newTopic = newTopicRoot.AddComponent<Panel_Operate>();
            node.activeChild = newTopic;
            GameObject topicNext = new GameObject("下一步");
            topicNext.transform.SetParent(newTopicRoot.transform);
            GameObject topicConfirm = new GameObject("确定");
            topicConfirm.transform.SetParent(newTopicRoot.transform);

            InvokePolicy(policy, "RefreshActiveTopicSubtreeCaches");

            List<GameObject> cachedNext =
                GetPolicyField<List<GameObject>>(policy, "nextButtons");
            List<Transform> cachedConfirm =
                GetPolicyField<List<Transform>>(policy, "confirmButtons");
            Assert.That(cachedNext, Has.Member(topicNext),
                "动态题目的“下一步”应通过子树扫描进入隐藏缓存。");
            Assert.That(cachedConfirm, Has.Member(topicConfirm.transform),
                "普通操作题的“确定”应通过子树扫描进入隐藏缓存。");

            InvokePolicy(policy, "HideNextButtons");
            InvokePolicy(policy, "HideConfirmButtons");
            Assert.That(topicNext.activeSelf, Is.False);
            Assert.That(topicConfirm.activeSelf, Is.False);
        }
        finally
        {
            RestoreForeignButtonStates(sceneButtonStates);
            Object.DestroyImmediate(policyRoot);
            Object.DestroyImmediate(nodeRoot);
            Object.DestroyImmediate(oldTopicRoot);
        }
    }

    [Test]
    public void PlaneManagerPrefabCache_StoresPrefabAssetsNotRuntimeInstances()
    {
        string source = File.ReadAllText(Path.Combine(
            Application.dataPath, "A_MRNet", "ProgramResources", "ProgramResources",
            "CommonScripts", "PlaneNodeScripts", "TexCore_PlaneManager.cs"));

        string loadPlane = ExtractMethod(source, "public TexCore_PlaneBase LoadPlane(");
        Assert.That(loadPlane, Does.Contain("GetOrLoadPlanePrefab"),
            "LoadPlane 应通过资源路径缓存读取 Prefab。");
        Assert.That(loadPlane, Does.Contain("Instantiate"),
            "每次进入题目仍必须执行新的 Instantiate，保持实例化语义。");

        string loader = ExtractMethod(
            source, "private static GameObject GetOrLoadPlanePrefab(");
        Assert.That(loader, Does.Contain("Resources.Load"));
        Assert.That(loader, Does.Not.Contain("Instantiate"),
            "资源缓存只能缓存 Prefab 资源，不能缓存运行时题目实例。");

        FieldInfo cacheField = typeof(TexCore_PlaneManager).GetField(
            "planePrefabResourceCache",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(cacheField, Is.Not.Null);
        Assert.That(cacheField.FieldType,
            Is.EqualTo(typeof(Dictionary<string, GameObject>)));
        Assert.That(cacheField.GetValue(null), Is.Not.Null);
    }

    private static void SetPolicyField(
        TexCore_03SceneFlowPolicy policy, string fieldName, object value)
    {
        FieldInfo field = typeof(TexCore_03SceneFlowPolicy).GetField(
            fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(policy, value);
    }

    private static T GetPolicyField<T>(
        TexCore_03SceneFlowPolicy policy, string fieldName)
    {
        FieldInfo field = typeof(TexCore_03SceneFlowPolicy).GetField(
            fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        return (T)field.GetValue(policy);
    }

    private static T GetStaticField<T>(string fieldName)
    {
        FieldInfo field = typeof(TexCore_03SceneFlowPolicy).GetField(
            fieldName, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        return (T)field.GetValue(null);
    }

    /// <summary>
    /// 构造一道真实层级的选择题主题：根物体与 Panel_Select 分处两层，
    /// “确定”挂在 Panel_Select 之下，和 选择题.prefab 保持一致。
    /// </summary>
    private static Panel_Select CreateQuizTopic(
        string rootName, out GameObject root, out GameObject confirm)
    {
        root = new GameObject(rootName);
        GameObject panelObject = new GameObject("选择题");
        panelObject.transform.SetParent(root.transform);
        confirm = new GameObject("确定");
        confirm.transform.SetParent(panelObject.transform);

        Panel_Select panel = panelObject.AddComponent<Panel_Select>();
        panel.btn_Confirm = confirm;
        return panel;
    }

    /// <summary>
    /// 构造一道普通操作题主题：操作题占位“确定”仍是场景策略的隐藏对象。
    /// </summary>
    private static Panel_Operate CreateOperateTopic(
        string rootName, out GameObject root, out GameObject confirm)
    {
        root = new GameObject(rootName);
        GameObject panelObject = new GameObject("操作题");
        panelObject.transform.SetParent(root.transform);
        confirm = new GameObject("确定");
        confirm.transform.SetParent(panelObject.transform);

        Panel_Operate panel = panelObject.AddComponent<Panel_Operate>();
        panel.btn_Confirm = confirm;
        return panel;
    }

    /// <summary>
    /// 用真实题目预制体验证“选择 → 自动提交 → 完成事件 → 自动推进”整链，
    /// 而不是用手搓夹具替代序列化字段，同时确认预制体的“确定”始终不显示。
    /// </summary>
    [Test]
    public void RealQuizPrefab_SelectAutoSubmitsOnceAndAdvances()
    {
        GameObject prefab = Resources.Load<GameObject>(
            "BusinessPrefabs/MR混合现实多人协作平台/" +
            "01货到人拣选电商仓储实施安装与调试实训系统/" +
            "01服务器网络搭建/01服务器网络搭建练习题");
        Assert.That(prefab, Is.Not.Null, "必须能按运行期路径加载真实题目预制体。");

        GameObject nodeRoot = new GameObject("Real Quiz Node");
        GameObject policyRoot = new GameObject("Real Quiz Policy Root");
        GameObject quizRoot = null;
        TopicCompletionCounter completions = null;

        try
        {
            TexCore_03SceneFlowPolicy policy =
                policyRoot.AddComponent<TexCore_03SceneFlowPolicy>();
            SpyNodePanel node = nodeRoot.AddComponent<SpyNodePanel>();

            quizRoot = Object.Instantiate(prefab, nodeRoot.transform);
            quizRoot.name = "01服务器网络搭建练习题";

            Panel_Select panel = quizRoot.GetComponent<Panel_Select>();
            Assert.That(panel, Is.Not.Null, "预制体根物体应带 Panel_Select。");
            Assert.That(panel.btn_Confirm, Is.Not.Null,
                "预制体必须序列化 btn_Confirm。");
            Assert.That(panel.topicData, Is.Not.Null,
                "预制体必须序列化 topicData。");
            Assert.That(panel.topicData.rightTasks, Is.EqualTo("B.HTTP"));

            node.datas = CreateTwoTopicNodeData(out _, out _);
            node.activeChild = panel;
            panel.controlPlane = node;

            // Runtime 下 datas.selfDatas 由 Config.xml 装配；预制体里是空的，
            // 题目真正的题干/选项/答案则序列化在 topicData 上。
            DataClass_Option optionData = panel.selfData;
            Assert.That(optionData, Is.Not.Null,
                "预制体必须序列化 selfData（题型）。");
            panel.datas.selfDatas = new List<SelfDataBase>
            {
                panel.topicData,
                optionData
            };

            InvokeSelectLifecycle(panel, "Start");
            InvokeSelectLifecycle(panel, "OnEnable");

            completions = new TopicCompletionCounter(panel.datas.name);
            InvokePolicy(policy, "RebuildCaches");

            Assert.That(TexCore_03SceneFlowPolicy.ShouldHideConfirmButton(
                panel.btn_Confirm.transform), Is.True,
                "真实选择题的“确定”属于标准确认按钮，必须隐藏。");
            Assert.That(panel.btn_Confirm.activeSelf, Is.False,
                "未选择时“确定”应保持隐藏。");

            // 选中正确答案：选完即自动提交，不再经过“确定”按钮。
            ClickOptionOnObject(panel, "B.HTTP");

            Assert.That(panel.isSubmit, Is.True, "选中正确答案后应自动提交。");
            Assert.That(panel.haveScore, Is.EqualTo(panel.topicData.score));
            Assert.That(panel.btn_Confirm.activeSelf, Is.False,
                "自动提交路径不会显示“确定”。");
            Assert.That(completions.Count, Is.EqualTo(1),
                "真实题目只应广播一次“完成题目”。");

            // 即使旧引用再次把按钮打开，场景策略也必须压回去。
            panel.btn_Confirm.SetActive(true);
            InvokePolicy(policy, "HideConfirmButtons");
            Assert.That(panel.btn_Confirm.activeSelf, Is.False,
                "场景策略必须持续压制真实题目的“确定”。");

            InvokePolicy(policy, "QueueTopicCompletion", panel.datas.name, 10);
            InvokePolicy(policy, "ProcessPendingTopicCompletions", 11);
            Assert.That(node.NextTopicCallCount, Is.EqualTo(1),
                "完成事件应延迟一帧推进到下一题。");
        }
        finally
        {
            if (completions != null) completions.Dispose();
            if (quizRoot != null) Object.DestroyImmediate(quizRoot);
            Object.DestroyImmediate(policyRoot);
            Object.DestroyImmediate(nodeRoot);
        }
    }

    /// <summary>
    /// 选择题选项名为 “A.xxx”“B.xxx”，前缀正好落在 BusinessManager 的 A/B 分支。
    /// 这条分派必须贯穿到当前活动题目，否则答案写不进 steps。
    /// </summary>
    [Test]
    public void BusinessManager_ForwardsLetterPrefixedOptionClicksToActivePlane()
    {
        GameObject planeRoot = new GameObject("Business Dispatch Plane");
        List<GameObject> options = new List<GameObject>();

        try
        {
            ForwardingPlaneBase plane =
                planeRoot.AddComponent<ForwardingPlaneBase>();

            // 复用场景里既有的 PlaneManager 单例，只临时替换 activePlane。
            Assert.That(TexCore_PlaneManager.self, Is.Not.Null,
                "测试场景应已有 TexCore_PlaneManager 单例。");
            TexCore_PlaneBase previous = TexCore_PlaneManager.self.activePlane;
            TexCore_PlaneManager.self.activePlane = plane;

            try
            {
                // 选择题选项只有 A./B./C./D. 四种前缀。
                foreach (string optionName in new[]
                         {
                             "A.FTP", "B.HTTP", "C.SMTP", "D.POP3",
                             "A.CPU", "B.RAM"
                         })
                {
                    GameObject option = new GameObject(optionName);
                    options.Add(option);
                    TexCore_BusinessManager.OnClick(option, "03SingleplayerScene");
                }

                Assert.That(plane.ClickedNames,
                    Is.EqualTo(new List<string>
                    {
                        "A.FTP", "B.HTTP", "C.SMTP", "D.POP3", "A.CPU", "B.RAM"
                    }),
                    "A/B 前缀的选项点击必须贯穿到活动平面，不能只在 C/D 上生效。");
            }
            finally
            {
                TexCore_PlaneManager.self.activePlane = previous;
            }
        }
        finally
        {
            foreach (GameObject option in options)
            {
                if (option != null) Object.DestroyImmediate(option);
            }

            Object.DestroyImmediate(planeRoot);
        }
    }

    /// <summary>
    /// 直接对题目脚本点击指定名字的选项对象。
    /// </summary>
    private static void ClickOptionOnObject(Panel_Select panel, string optionName)
    {
        GameObject option = new GameObject(optionName);
        try
        {
            panel.OnClick(option);
        }
        finally
        {
            Object.DestroyImmediate(option);
        }
    }

    /// <summary>
    /// 构造一道选择题夹具：根物体按真实预制体结构命名为“选择题”，
    /// 内部带一个子物体“确定”作为 btn_Confirm。
    /// </summary>
    private static GameObject CreateSelectTopic(
        SelecionCount selectionCount, string rightTasks, string tasks,
        bool wireConfirmField)
    {
        GameObject topic = new GameObject("选择题");
        GameObject confirm = new GameObject("确定");
        confirm.transform.SetParent(topic.transform);
        GameObject next = new GameObject("下一步");
        next.transform.SetParent(topic.transform);
        GameObject options = new GameObject("Options");
        options.transform.SetParent(topic.transform);

        Panel_Select panel = topic.AddComponent<Panel_Select>();
        panel.datas = new TexCore_DataClass { name = "选择题" };
        panel.datas.selfDatas = new List<SelfDataBase>
        {
            new DataClass_Topic
            {
                rightTasks = rightTasks,
                tasks = new List<string>(tasks.Split('_')),
                score = 10
            },
            new DataClass_Option { selecionCount = selectionCount }
        };
        panel.itemParent = options.transform;
        panel.btn_Next = next;
        // Start() 会从配置数据解析题目与题型；夹具直接写入同一结果。
        panel.topicData = (DataClass_Topic)panel.datas.selfDatas[0];
        panel.selfData = (DataClass_Option)panel.datas.selfDatas[1];
        // PanelTopicBase.OnEnable/OnDisable 会遍历 selfObjs，实例化出来的题目
        // 一定有这个对象；裸夹具必须补齐，否则生命周期回调会先抛空引用。
        panel.selfObjs = new Unity.IProperties { _properties = new Property[0] };
        if (wireConfirmField)
        {
            // 真实预制体里 btn_Confirm 是序列化字段；这里复现同一状态。
            panel.btn_Confirm = confirm;
        }

        return topic;
    }

    /// <summary>
    /// 编辑模式下 AddComponent 不会执行生命周期回调，测试按需手动调用。
    /// </summary>
    private static void InvokeSelectLifecycle(Panel_Select panel, string methodName)
    {
        MethodInfo method = typeof(Panel_Select).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, methodName);
        method.Invoke(panel, null);
    }

    private static void ClickOption(Panel_Select panel, string optionName)
    {
        GameObject option = new GameObject(optionName);
        try
        {
            panel.OnClick(option);
        }
        finally
        {
            Object.DestroyImmediate(option);
        }
    }

    /// <summary>
    /// 创建一个只在调用期间存在的选项对象，调用方负责销毁，
    /// 用于需要在一次测试里点击多个不同选项的场景。
    /// </summary>
    private static GameObject NewOption(string optionName)
    {
        return new GameObject(optionName);
    }

    private static TexCore_DataClass CreateTwoTopicNodeData(
        out TexCore_DataClass topicA, out TexCore_DataClass topicB)
    {
        return CreateTwoTopicNodeData(null, out topicA, out topicB);
    }

    /// <summary>
    /// 与 <see cref="CreateTwoTopicNodeData(out TexCore_DataClass, out TexCore_DataClass)"/>
    /// 相同，但允许指定首题名称。走真实 <c>Update()</c> 的用例必须用唯一题目名，
    /// 否则本地缓存会同时命中当前打开场景里的同名真实节点，推进到别的题目上。
    /// </summary>
    private static TexCore_DataClass CreateTwoTopicNodeData(
        string firstTopicName, out TexCore_DataClass topicA, out TexCore_DataClass topicB)
    {
        TexCore_DataClass nodeData = new TexCore_DataClass { name = "测试节点" };
        string firstName = string.IsNullOrEmpty(firstTopicName) ? "题目A" : firstTopicName;
        topicA = new TexCore_DataClass { name = firstName, parent = nodeData };
        topicB = new TexCore_DataClass { name = "题目B", parent = nodeData };
        nodeData.childData = new List<TexCore_DataClass> { topicA, topicB };
        return nodeData;
    }

    /// <summary>
    /// 记录打开场景中已有“下一步”按钮的激活状态。编辑模式测试里的全场景
    /// 缓存重建可能命中真实场景对象，结束后必须恢复，避免污染未保存场景。
    /// </summary>
    private static Dictionary<GameObject, bool> SnapshotNextButtonStates()
    {
        Dictionary<GameObject, bool> snapshot = new Dictionary<GameObject, bool>();
        foreach (Transform item in Object.FindObjectsOfType<Transform>(true))
        {
            if (item != null && item.name == "下一步")
            {
                snapshot[item.gameObject] = item.gameObject.activeSelf;
            }
        }

        return snapshot;
    }

    private static void RestoreForeignButtonStates(
        Dictionary<GameObject, bool> snapshot, params GameObject[] owned)
    {
        foreach (KeyValuePair<GameObject, bool> entry in snapshot)
        {
            if (entry.Key == null) continue;
            bool isOwned = false;
            foreach (GameObject own in owned)
            {
                if (entry.Key == own)
                {
                    isOwned = true;
                    break;
                }
            }

            if (!isOwned) entry.Key.SetActive(entry.Value);
        }
    }

    private static void InvokePolicy(
        TexCore_03SceneFlowPolicy policy, string methodName, params object[] args)
    {
        MethodInfo method = typeof(TexCore_03SceneFlowPolicy).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, methodName);
        method.Invoke(method.IsStatic ? null : policy, args);
    }

    private static List<GameObject> ReadNextButtons(TexCore_03SceneFlowPolicy policy)
    {
        FieldInfo field = typeof(TexCore_03SceneFlowPolicy).GetField(
            "nextButtons", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (List<GameObject>)field.GetValue(policy);
    }

    private static IList ReadPendingCompletions(TexCore_03SceneFlowPolicy policy)
    {
        FieldInfo field = typeof(TexCore_03SceneFlowPolicy).GetField(
            "pendingCompletions", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (IList)field.GetValue(policy);
    }

    /// <summary>
    /// 让 Update() 认得出当前场景，并返回可用于 targetSceneName 的场景名。
    /// </summary>
    /// <remarks>
    /// Update() 只在本策略所属场景为目标场景时才排空流程，而 IsTargetScene 对空
    /// 场景名直接判否。编辑模式测试的临时场景没有保存过、名字为空串，因此先按本
    /// 文件既有做法把它存到 Temp 拿到名字；已保存场景（例如已打开的 03 主场景）
    /// 不动。
    /// </remarks>
    private static string GetTargetableSceneName()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.name))
        {
            EditorSceneManager.SaveScene(scene,
                "Temp/TexCore03FlowPolicyProbe_" + scene.handle + ".unity");
            scene = SceneManager.GetActiveScene();
        }

        Assert.That(string.IsNullOrEmpty(scene.name), Is.False,
            "探针场景必须有名字，否则 Update() 不会进入流程排空。");
        return scene.name;
    }

    private static List<NodeObjs> CreateEmptyStepTree()
    {
        List<NodeObjs> nodes = new List<NodeObjs>();
        for (int nodeIndex = 0; nodeIndex < 5; nodeIndex++)
        {
            List<TopicObjs> topics = new List<TopicObjs>();
            for (int topicIndex = 0; topicIndex < 7; topicIndex++)
            {
                topics.Add(new TopicObjs
                {
                    topicObjs = new List<StepObjs>
                    {
                        new StepObjs { stepIndex = 0, stepObjs = new List<GameObject>() }
                    }
                });
            }

            nodes.Add(new NodeObjs { nodeObjs = topics });
        }

        return nodes;
    }

    private static string ExtractMethod(string source, string signature)
    {
        int start = source.IndexOf(signature);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), signature);
        int bodyStart = source.IndexOf('{', start);
        int depth = 0;
        for (int index = bodyStart; index < source.Length; index++)
        {
            if (source[index] == '{') depth++;
            else if (source[index] == '}' && --depth == 0)
                return source.Substring(bodyStart, index - bodyStart + 1);
        }

        Assert.Fail("方法体未闭合：" + signature);
        return string.Empty;
    }
}

/// <summary>
/// 统计指定题目的“完成题目”事件次数。退出 using 块时必须解除监听，
/// 否则残留处理器会污染同一程序集里的其他用例。
/// </summary>
public sealed class TopicCompletionCounter : System.IDisposable
{
    private readonly string topicName;
    private readonly CallBack<string> handler;
    private int count;

    public TopicCompletionCounter(string topicName)
    {
        this.topicName = topicName;
        handler = OnCompleted;
        TexCore_EventCenter.AddListener(TexCore_EventType.完成题目, handler);
    }

    public int Count => count;

    private void OnCompleted(string completedTopicName)
    {
        if (completedTopicName == topicName)
        {
            count++;
        }
    }

    public void Dispose()
    {
        TexCore_EventCenter.RemoveListener(TexCore_EventType.完成题目, handler);
    }
}

/// <summary>
/// NodePanelBase 测试替身：仅重写虚方法 NextTopic 做计数，不真正加载下一题，
/// 让测试在没有 PlaneManager 的编辑模式下验证场景策略的自动推进决策。
/// </summary>
public sealed class SpyNodePanel : NodePanelBase
{
    public int NextTopicCallCount;

    public override void NextTopic()
    {
        NextTopicCallCount++;
    }
}

/// <summary>
/// 平面替身：记录 BusinessManager 实际贯穿过来的点击对象名，
/// 用于验证 A/B 前缀的选项点击不再被分派层吞掉。
/// </summary>
public sealed class ForwardingPlaneBase : TexCore_PlaneBase
{
    public readonly List<string> ClickedNames = new List<string>();

    public override void OnClick(GameObject obj)
    {
        BpfLog.Info(BpfLogCategory.TexCore, "test.forwarding_plane.on_click", obj.name);
        if (obj != null)
        {
            ClickedNames.Add(obj.name);
        }
    }
}
