using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MixedReality.Toolkit.UX;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TexCore_M02InputValidationTests
{
    private const string MainlineScenePath = "Assets/A_MRNet/Sence/03SingleplayerScene.unity";
    private const string InstallUiRootName = "UI_XiTongJingXiangAnZhuang";

    // --- Password pairs: PasswordPair and RootPasswordPair share one rule set ---

    [TestCase(M02InputValidationMode.PasswordPair, "Abcdef1!", "Abcdef1!", true, "")]
    [TestCase(M02InputValidationMode.PasswordPair, "Ab1!", "Ab1!", false, TexCore_M02InputRules.PasswordLengthError)]
    [TestCase(M02InputValidationMode.PasswordPair, "abcdef1!", "abcdef1!", false, TexCore_M02InputRules.PasswordComplexityError)]
    [TestCase(M02InputValidationMode.PasswordPair, "ABCDEF1!", "ABCDEF1!", false, TexCore_M02InputRules.PasswordComplexityError)]
    [TestCase(M02InputValidationMode.PasswordPair, "Abcdefg!", "Abcdefg!", false, TexCore_M02InputRules.PasswordComplexityError)]
    [TestCase(M02InputValidationMode.PasswordPair, "Abcdef12", "Abcdef12", false, TexCore_M02InputRules.PasswordComplexityError)]
    [TestCase(M02InputValidationMode.PasswordPair, "Abc def1!", "Abc def1!", false, TexCore_M02InputRules.PasswordWhitespaceError)]
    [TestCase(M02InputValidationMode.PasswordPair, "Abcdef1!", "Abcdef1?", false, TexCore_M02InputRules.PasswordMismatchError)]
    [TestCase(M02InputValidationMode.PasswordPair, "", "", false, TexCore_M02InputRules.PasswordEmptyError)]
    [TestCase(M02InputValidationMode.PasswordPair, "Abcdef1中", "Abcdef1中", false, TexCore_M02InputRules.PasswordComplexityError)]
    [TestCase(M02InputValidationMode.RootPasswordPair, "Root@123", "Root@123", true, "")]
    [TestCase(M02InputValidationMode.RootPasswordPair, "Root@123", "Root@124", false, TexCore_M02InputRules.PasswordMismatchError)]
    [TestCase(M02InputValidationMode.RootPasswordPair, "root@123", "root@123", false, TexCore_M02InputRules.PasswordComplexityError)]
    public void PasswordPairRules_MatchTheFixedRuleSet(
        M02InputValidationMode mode,
        string password,
        string confirmation,
        bool expectedValid,
        string expectedError)
    {
        AssertValidation(mode, new[] { password, confirmation }, expectedValid, expectedError);
    }

    // --- User name ---

    [TestCase("zhang_san-01", true, "")]
    [TestCase("ZhangSan", false, TexCore_M02InputRules.UserNameStartError)]
    [TestCase("张三丰", false, TexCore_M02InputRules.UserNameStartError)]
    [TestCase("1zhang", false, TexCore_M02InputRules.UserNameStartError)]
    [TestCase("zhang.san", false, TexCore_M02InputRules.UserNameCharacterError)]
    [TestCase("zhang san", false, TexCore_M02InputRules.UserNameCharacterError)]
    [TestCase("ab", false, TexCore_M02InputRules.UserNameLengthError)]
    [TestCase("", false, TexCore_M02InputRules.UserNameLengthError)]
    public void UserNameRules_MatchTheFixedRuleSet(string userName, bool expectedValid, string expectedError)
    {
        AssertValidation(
            M02InputValidationMode.UserIdentityPair,
            new[] { userName, "张三" },
            expectedValid,
            expectedError);
    }

    // --- Full name ---

    [TestCase("张三", true, "")]
    [TestCase("Zhang San", true, "")]
    [TestCase("Jean-Luc", true, "")]
    [TestCase("O'Connor", true, "")]
    [TestCase("Zhang 3", false, TexCore_M02InputRules.FullNameCharacterError)]
    [TestCase("12", false, TexCore_M02InputRules.FullNameCharacterError)]
    [TestCase("Zhang  San", false, TexCore_M02InputRules.FullNameDoubleSpaceError)]
    [TestCase(" Zhang", false, TexCore_M02InputRules.FullNameEdgeSpaceError)]
    [TestCase("Zhang ", false, TexCore_M02InputRules.FullNameEdgeSpaceError)]
    [TestCase("张", false, TexCore_M02InputRules.FullNameLengthError)]
    [TestCase("A-", false, TexCore_M02InputRules.FullNameLetterError)]
    [TestCase("", false, TexCore_M02InputRules.FullNameLengthError)]
    public void FullNameRules_MatchTheFixedRuleSet(string fullName, bool expectedValid, string expectedError)
    {
        AssertValidation(
            M02InputValidationMode.UserIdentityPair,
            new[] { "zhangsan", fullName },
            expectedValid,
            expectedError);
    }

    [Test]
    public void PasswordRules_EnforceTheAuthoredLengthWindow()
    {
        string maxLengthPassword = new string('a', 61) + "A1!";
        Assert.That(maxLengthPassword.Length, Is.EqualTo(TexCore_M02InputRules.PasswordMaxLength));
        AssertValidation(
            M02InputValidationMode.PasswordPair,
            new[] { maxLengthPassword, maxLengthPassword },
            true,
            string.Empty);

        string tooLongPassword = new string('a', 63) + "A1!";
        AssertValidation(
            M02InputValidationMode.PasswordPair,
            new[] { tooLongPassword, tooLongPassword },
            false,
            TexCore_M02InputRules.PasswordLengthError);
    }

    [Test]
    public void UserNameRules_RejectNamesLongerThanTheAuthoredMaximum()
    {
        AssertValidation(
            M02InputValidationMode.UserIdentityPair,
            new[] { new string('a', 33), "张三" },
            false,
            TexCore_M02InputRules.UserNameLengthError);
    }

    [Test]
    public void FullNameRules_RejectNamesLongerThanTheAuthoredMaximum()
    {
        AssertValidation(
            M02InputValidationMode.UserIdentityPair,
            new[] { "zhangsan", new string('张', 41) },
            false,
            TexCore_M02InputRules.FullNameLengthError);
    }

    // --- Step binding, checked against the authored scene ---

    [Test]
    public void InputRules_CoverOnlyTheThreeAuthoredInputSteps()
    {
        InstallInputRuleView[] rules = ReadInstallInputRules();

        CollectionAssert.AreEquivalent(
            new[] { 13, 23, 24 },
            rules.Select(rule => rule.StepIndex).ToArray(),
            "只有第 14、24、25 步允许使用输入校验组件。");
        Assert.That(rules.Single(rule => rule.StepIndex == 13).Mode,
            Is.EqualTo(M02InputValidationMode.RootPasswordPair));
        Assert.That(rules.Single(rule => rule.StepIndex == 23).Mode,
            Is.EqualTo(M02InputValidationMode.UserIdentityPair));
        Assert.That(rules.Single(rule => rule.StepIndex == 24).Mode,
            Is.EqualTo(M02InputValidationMode.PasswordPair));
        Assert.That(ReadInstallPages().Length, Is.EqualTo(27),
            "第 14、24、25 步改动不允许改变 27 步结构。");
    }

    [Test]
    public void InputRulePages_ResolveTheirAuthoredInputFields()
    {
        WithMainlineScene(scene =>
        {
            Transform uiRoot = FindSceneTransform(scene, InstallUiRootName);
            Assert.That(uiRoot, Is.Not.Null, "场景缺少安装界面根节点。");
            TexCore_M02ServerInstallBootstrap bootstrap = CreateBootstrap(uiRoot);
            try
            {
                AssertField(bootstrap, uiRoot, 13, 0, "RootMiMa", "InputField_2");
                AssertField(bootstrap, uiRoot, 13, 1, "QueRenRoot", "InputField_3");
                AssertField(bootstrap, uiRoot, 23, 0, "YongHu", "InputField_10");
                AssertField(bootstrap, uiRoot, 23, 1, "QuanMing", "InputField_9");
                AssertField(bootstrap, uiRoot, 24, 0, "MiMa", "InputField_4");
                AssertField(bootstrap, uiRoot, 24, 1, "QueRen", "InputField_5");
            }
            finally
            {
                Object.DestroyImmediate(bootstrap.gameObject);
            }
        });
    }

    [Test]
    public void InputRulePages_KeepASingleCompletingReporterOnTheirSubmitButton()
    {
        WithMainlineScene(scene =>
        {
            Transform uiRoot = FindSceneTransform(scene, InstallUiRootName);
            Assert.That(uiRoot, Is.Not.Null);
            TexCore_M02ServerInstallBootstrap bootstrap = CreateBootstrap(uiRoot);
            try
            {
                foreach (InstallInputRuleView rule in ReadInstallInputRules())
                {
                    object installPage = ReadInstallPages().GetValue(rule.StepIndex);
                    MethodInfo findPage = Private(bootstrap, "FindInstallPageTransform");
                    Transform page = (Transform)findPage.Invoke(bootstrap, new[] { installPage });
                    Assert.That(page, Is.Not.Null, $"第 {rule.StepIndex + 1} 步找不到页面。");

                    PressableButton button = FindDirectPageButton(page, 0);
                    Assert.That(button, Is.Not.Null, $"第 {rule.StepIndex + 1} 步找不到提交按钮。");
                    button.gameObject.name = $"M02_InputStep_{rule.StepIndex}";

                    MethodInfo attach = Private(bootstrap, "AttachInputValidation");
                    TexCore_M02InputValidationStep validation = (TexCore_M02InputValidationStep)
                        attach.Invoke(bootstrap, new object[] { button, rule.Value });
                    Assert.That(validation, Is.Not.Null,
                        $"第 {rule.StepIndex + 1} 步必须绑定专用输入校验组件。");
                    Assert.That(validation.Mode, Is.EqualTo(rule.Mode));
                    Assert.That(validation.InputFieldCount, Is.EqualTo(2));

                    MethodInfo counter = Private(bootstrap, "CountCompletingReporters");
                    Assert.That(counter.Invoke(bootstrap, new object[] { button.gameObject }),
                        Is.EqualTo(1),
                        $"第 {rule.StepIndex + 1} 步的提交按钮只允许一个完成组件。");
                    Assert.That(
                        button.GetComponents<TexCore_ClickObjectStep>()
                            .Count(step => step != null && step.CompletesStep),
                        Is.Zero,
                        $"第 {rule.StepIndex + 1} 步不允许保留会推进 TexCore 的普通点击组件。");
                    Assert.That(button.enabled, Is.False,
                        "未进入等待触发的输入步骤不允许提交。");
                }
            }
            finally
            {
                Object.DestroyImmediate(bootstrap.gameObject);
            }
        });
    }

    [Test]
    public void OtherInstallSteps_AreNotInputValidationSteps()
    {
        InstallInputRuleView[] rules = ReadInstallInputRules();
        int[] expected = { 13, 23, 24 };
        for (int stepIndex = 0; stepIndex < 27; stepIndex++)
        {
            bool isInputStep = expected.Contains(stepIndex);
            Assert.That(
                rules.Any(rule => rule.StepIndex == stepIndex),
                Is.EqualTo(isInputStep),
                $"第 {stepIndex + 1} 步的输入校验归属不正确。");
        }
    }

    [Test]
    public void InputHitBoxes_AreNormalizedWithoutTouchingTheVisuals()
    {
        WithMainlineScene(scene =>
        {
            Transform uiRoot = FindSceneTransform(scene, InstallUiRootName);
            Transform stepObjects = FindSceneTransform(scene, "StepObjs");
            Assert.That(uiRoot, Is.Not.Null);
            Assert.That(stepObjects, Is.Not.Null);

            // Snapshot the authored rects before the bootstrap can touch the
            // colliders, and resolve pages the same way the bootstrap does
            // without adding a component yet.
            List<Transform> containers = FindInputContainers(uiRoot);
            Assert.That(containers.Count, Is.EqualTo(9),
                "三个输入页的六个输入框与 IP 页的三个输入框都必须参与归一化。");

            Dictionary<Transform, RectSnapshot> authored = new Dictionary<Transform, RectSnapshot>();
            Dictionary<Transform, BoxCollider> references = new Dictionary<Transform, BoxCollider>();
            foreach (Transform container in containers)
            {
                authored[container] = RectSnapshot.Capture(container);
                // Read the reference collider before Configure renames the page
                // buttons to their M02 task names.
                references[container] = FindReferenceCollider(container);
                Assert.That(references[container], Is.Not.Null,
                    $"{container.name} 找不到同页按钮参照。");
            }

            TexCore_M02ServerInstallBootstrap bootstrap = ConfigureBootstrapOnScene(stepObjects);
            try
            {
                foreach (Transform container in containers)
                {
                    BoxCollider collider = container.GetComponent<BoxCollider>();
                    Assert.That(collider, Is.Not.Null, $"{container.name} 缺少 BoxCollider。");

                    Vector3 scale = container.lossyScale;
                    float worldHeight = collider.size.y * Mathf.Abs(scale.y);
                    float worldDepth = collider.size.z * Mathf.Abs(scale.z);
                    float worldFront = (collider.center.z + collider.size.z * 0.5f) * Mathf.Abs(scale.z);

                    // A button on the same page is the calibrated reference.
                    BoxCollider reference = references[container];
                    float referenceHeight = reference.size.y *
                                            Mathf.Abs(reference.transform.lossyScale.y);

                    Assert.That(worldHeight, Is.GreaterThanOrEqualTo(referenceHeight * 0.9f),
                        $"{container.name} 的命中高度仍不足一个按钮。");
                    Assert.That(worldDepth, Is.LessThanOrEqualTo(
                            reference.size.z * Mathf.Abs(reference.transform.lossyScale.z) * 1.2f),
                        $"{container.name} 的命中体仍然比按钮更厚。");
                    Assert.That(worldFront, Is.LessThanOrEqualTo(
                            (reference.center.z + reference.size.z * 0.5f) *
                            Mathf.Abs(reference.transform.lossyScale.z) * 1.2f),
                        $"{container.name} 的命中体仍然探出面板之前。");

                    // The visual rect must be untouched: only the hit box changes.
                    authored[container].AssertUnchanged(container);
                }
            }
            finally
            {
                Object.DestroyImmediate(bootstrap);
            }
        });
    }

    private static List<Transform> FindInputContainers(Transform uiRoot)
    {
        List<Transform> containers = new List<Transform>();
        System.Array pages = ReadInstallPages();
        foreach (InstallInputRuleView rule in ReadInstallInputRules())
        {
            Transform page = ResolvePage(uiRoot, pages.GetValue(rule.StepIndex));
            Assert.That(page, Is.Not.Null, $"第 {rule.StepIndex + 1} 步找不到页面。");
            foreach (object locator in (IList)rule.Fields)
            {
                string parentName = (string)locator.GetType().GetProperty("ParentName").GetValue(locator);
                Transform container = FindDirectChild(page, parentName);
                Assert.That(container, Is.Not.Null, $"{page.name} 缺少 {parentName}。");
                containers.Add(container);
            }
        }

        Transform ipPage = FindDirectChild(uiRoot, "YouXian10");
        Assert.That(ipPage, Is.Not.Null, "场景缺少 YouXian10。");
        foreach (string fieldName in new[] { "DiZhi", "ZiWang", "WangGuan" })
        {
            Transform container = FindDirectChild(ipPage, fieldName);
            Assert.That(container, Is.Not.Null, $"YouXian10 缺少 {fieldName}。");
            containers.Add(container);
        }

        return containers;
    }

    /// <summary>Mirrors TexCore_M02ServerInstallBootstrap.FindInstallPageTransform.</summary>
    private static Transform ResolvePage(Transform uiRoot, object installPage)
    {
        System.Type type = installPage.GetType();
        int childIndex = (int)type.GetProperty("UiChildIndex").GetValue(installPage);
        string pageName = (string)type.GetProperty("PageName").GetValue(installPage);
        int occurrence = (int)type.GetProperty("NameOccurrence").GetValue(installPage);

        if (childIndex >= 0 && childIndex < uiRoot.childCount &&
            uiRoot.GetChild(childIndex).name == pageName)
        {
            return uiRoot.GetChild(childIndex);
        }

        int seen = 0;
        for (int index = 0; index < uiRoot.childCount; index++)
        {
            Transform candidate = uiRoot.GetChild(index);
            if (candidate.name != pageName) continue;
            if (seen == occurrence) return candidate;
            seen++;
        }

        return null;
    }

    /// <summary>The page's own submit button is the calibrated hit box reference.</summary>
    private static BoxCollider FindReferenceCollider(Transform container)
    {
        Transform page = container.parent;
        if (page == null) return null;
        PressableButton button = FindDirectPageButton(page, 0);
        return button != null ? button.GetComponent<BoxCollider>() : null;
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null) return null;
        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (child.name == childName) return child;
        }

        return null;
    }

    /// <summary>Authored RectTransform values, so a test can prove they stay.</summary>
    private readonly struct RectSnapshot
    {
        private RectSnapshot(Vector2 size, Vector2 anchored, Vector3 localScale)
        {
            this.size = size;
            this.anchored = anchored;
            this.localScale = localScale;
        }

        private readonly Vector2 size;
        private readonly Vector2 anchored;
        private readonly Vector3 localScale;

        public static RectSnapshot Capture(Transform container)
        {
            RectTransform rect = container as RectTransform;
            Assert.That(rect, Is.Not.Null, $"{container.name} 缺少 RectTransform。");
            return new RectSnapshot(rect.sizeDelta, rect.anchoredPosition, rect.localScale);
        }

        public void AssertUnchanged(Transform container)
        {
            RectTransform rect = (RectTransform)container;
            Assert.That(rect.sizeDelta, Is.EqualTo(size), $"{container.name} 的视觉尺寸不允许变化。");
            Assert.That(rect.anchoredPosition, Is.EqualTo(anchored), $"{container.name} 的位置不允许变化。");
            Assert.That(rect.localScale, Is.EqualTo(localScale), $"{container.name} 的缩放不允许变化。");
        }
    }

    [Test]
    public void EmptySubmit_OnTheFirstPasswordPage_KeepsThePageAndDoesNotAdvance()
    {
        WithMainlineScene(scene =>
        {
            Transform uiRoot = FindSceneTransform(scene, InstallUiRootName);
            Assert.That(uiRoot, Is.Not.Null, "场景缺少安装界面根节点。");
            Transform stepObjects = FindSceneTransform(scene, "StepObjs");
            Assert.That(stepObjects, Is.Not.Null, "场景缺少 StepObjs。");

            TexCore_M02ServerInstallBootstrap bootstrap =
                stepObjects.gameObject.GetComponent<TexCore_M02ServerInstallBootstrap>() ??
                stepObjects.gameObject.AddComponent<TexCore_M02ServerInstallBootstrap>();
            try
            {
                // The runtime retries Configure() until the authored UI is
                // ready, so the test mirrors that instead of assuming the first
                // attempt already resolved every page.
                MethodInfo configure = Private(bootstrap, "Configure");
                for (int attempt = 0; attempt < 40; attempt++)
                {
                    if ((bool)GetPrivateField(bootstrap, "configured")) break;
                    configure.Invoke(bootstrap, null);
                }

                Assert.That(GetPrivateField(bootstrap, "configured"), Is.EqualTo(true),
                    "M02 引导必须在真实场景中完成绑定。");

                IList reporters = (IList)GetPrivateField(bootstrap, "installReporters");
                Assert.That(reporters.Count, Is.EqualTo(27));
                TexCore_M02InputValidationStep validation =
                    reporters[13] as TexCore_M02InputValidationStep;
                Assert.That(validation, Is.Not.Null,
                    "第 14 步必须由输入校验组件完成。");

                MethodInfo present = Private(bootstrap, "PresentInstallPage");
                present.Invoke(bootstrap, new object[] { 13 });

                Assert.That(GetPrivateField(bootstrap, "visiblePageName"), Is.EqualTo("MiMa[14]"));

                PressableButton submit = validation.GetComponent<PressableButton>();
                Assert.That(submit, Is.Not.Null);
                Assert.That(submit.gameObject.activeInHierarchy, Is.True,
                    "输入页面的提交按钮必须显示。");
                Assert.That(submit.enabled, Is.True,
                    "进入等待触发后提交按钮必须可点。");
                Assert.That(validation.state, Is.EqualTo(FinishState.等待触发));

                foreach (InputField field in ReadBoundInputFields(validation))
                {
                    Assert.That(field.text, Is.Empty);
                }

                // Click the submit button with every bound input still empty.
                // The reporter entry point is invoked directly because EditMode
                // does not run the OnEnable that registers the pressable
                // listener at runtime.
                List<string> hints = new List<string>();
                List<string> titles = new List<string>();
                CallBack<string, float> hintHandler = (text, _) => hints.Add(text);
                CallBack<string> titleHandler = text => titles.Add(text);
                TexCore_EventCenter.AddListener<string, float>(TexCore_EventType.显示提示, hintHandler);
                TexCore_EventCenter.AddListener<string>(TexCore_EventType.显示标题, titleHandler);
                try
                {
                    Private(validation, "Submit").Invoke(validation, null);
                }
                finally
                {
                    TexCore_EventCenter.RemoveListener<string, float>(TexCore_EventType.显示提示, hintHandler);
                    TexCore_EventCenter.RemoveListener<string>(TexCore_EventType.显示标题, titleHandler);
                }

                Assert.That(validation.WasCompletedByUser, Is.False,
                    "空输入点击提交不允许完成步骤。");
                Assert.That(validation.state, Is.EqualTo(FinishState.等待触发),
                    "空输入点击提交后步骤必须保持等待触发。");
                Assert.That(GetPrivateField(bootstrap, "visiblePageName"), Is.EqualTo("MiMa[14]"),
                    "空输入点击提交不允许切换页面。");
                Assert.That(hints, Is.Empty,
                    "校验失败不允许走会弹出模态对话框的“显示提示”通道。");
                Assert.That(titles, Does.Contain(TexCore_M02InputRules.PasswordEmptyError),
                    "校验失败必须在非模态标题栏显示具体原因。");

                // The pressable chain must never advance the step either.
                int completing = (int)Private(bootstrap, "CountCompletingReporters")
                    .Invoke(bootstrap, new object[] { submit.gameObject });
                Assert.That(completing, Is.EqualTo(1),
                    "提交按钮只允许一个完成组件，否则一次点击会推进两次。");
                submit.OnClicked.Invoke();
                Assert.That(validation.WasCompletedByUser, Is.False,
                    "真实点击链路不允许在空输入时完成步骤。");
                Assert.That(GetPrivateField(bootstrap, "visiblePageName"), Is.EqualTo("MiMa[14]"),
                    "真实点击链路不允许在空输入时切换页面。");
            }
            finally
            {
                Object.DestroyImmediate(bootstrap);
            }
        });
    }

    private static readonly int[] InputStepIndexes = { 13, 23, 24 };

    [Test]
    public void InputSubmitButtons_DisableTheirAuthoredPageSwitchCallbacks()
    {
        WithMainlineScene(scene =>
        {
            Transform stepObjects = FindSceneTransform(scene, "StepObjs");
            Assert.That(stepObjects, Is.Not.Null, "场景缺少 StepObjs。");
            TexCore_M02ServerInstallBootstrap bootstrap = ConfigureBootstrapOnScene(stepObjects);
            try
            {
                foreach (int stepIndex in InputStepIndexes)
                {
                    TexCore_M02InputValidationStep validation =
                        ReadInstallReporter(bootstrap, stepIndex) as TexCore_M02InputValidationStep;
                    Assert.That(validation, Is.Not.Null,
                        $"第 {stepIndex + 1} 步必须由输入校验组件完成。");

                    PressableButton submit = validation.GetComponent<PressableButton>();
                    Assert.That(submit, Is.Not.Null);
                    UnityEvent clicked = submit.OnClicked;
                    Assert.That(clicked.GetPersistentEventCount(), Is.GreaterThan(0),
                        $"第 {stepIndex + 1} 步的提交按钮应有原始切页回调可供禁用。");

                    List<string> methods = ReadPersistentCallMethods(clicked);
                    List<UnityEventCallState> states = ReadPersistentCallStates(clicked);
                    int pageSwitches = 0;
                    for (int index = 0; index < methods.Count; index++)
                    {
                        if (methods[index] != "SetActive") continue;
                        if (!(clicked.GetPersistentTarget(index) is GameObject)) continue;
                        pageSwitches++;
                        Assert.That(states[index], Is.EqualTo(UnityEventCallState.Off),
                            $"第 {stepIndex + 1} 步的原始页面切换回调必须在运行时禁用。");
                    }

                    Assert.That(pageSwitches, Is.GreaterThan(0),
                        $"第 {stepIndex + 1} 步的提交按钮应带有原始页面切换回调。");
                }
            }
            finally
            {
                Object.DestroyImmediate(bootstrap);
            }
        });
    }

    [Test]
    public void InvalidSubmits_OnEveryInputPage_KeepThePageAndTheStep()
    {
        WithMainlineScene(scene =>
        {
            Transform stepObjects = FindSceneTransform(scene, "StepObjs");
            Assert.That(stepObjects, Is.Not.Null, "场景缺少 StepObjs。");
            TexCore_M02ServerInstallBootstrap bootstrap = ConfigureBootstrapOnScene(stepObjects);
            try
            {
                MethodInfo present = Private(bootstrap, "PresentInstallPage");
                foreach (int stepIndex in InputStepIndexes)
                {
                    present.Invoke(bootstrap, new object[] { stepIndex });
                    string pageBefore = (string)GetPrivateField(bootstrap, "visiblePageName");
                    Assert.That(pageBefore, Is.Not.Null.And.Not.Empty);

                    TexCore_M02InputValidationStep validation =
                        ReadInstallReporter(bootstrap, stepIndex) as TexCore_M02InputValidationStep;
                    Assert.That(validation, Is.Not.Null);
                    PressableButton submit = validation.GetComponent<PressableButton>();
                    MethodInfo submitMethod = Private(validation, "Submit");

                    foreach (string[] values in InvalidValueSets(stepIndex))
                    {
                        SetInputText(validation, values);
                        submitMethod.Invoke(validation, null);

                        Assert.That(validation.WasCompletedByUser, Is.False,
                            $"第 {stepIndex + 1} 步的非法输入不允许完成步骤。");
                        Assert.That(validation.state, Is.EqualTo(FinishState.等待触发),
                            $"第 {stepIndex + 1} 步的非法输入必须保持等待触发。");
                        Assert.That(GetPrivateField(bootstrap, "visiblePageName"),
                            Is.EqualTo(pageBefore),
                            $"第 {stepIndex + 1} 步的非法输入不允许切换页面。");
                        Assert.That(submit.gameObject.activeInHierarchy, Is.True,
                            $"第 {stepIndex + 1} 步的提交按钮必须保持显示。");
                        Assert.That(submit.enabled, Is.True,
                            $"第 {stepIndex + 1} 步的提交按钮必须保持可点。");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(bootstrap);
            }
        });
    }

    [Test]
    public void ValidSubmit_OnTheFirstPasswordPage_CompletesOnlyOnce()
    {
        WithMainlineScene(scene =>
        {
            Transform stepObjects = FindSceneTransform(scene, "StepObjs");
            Assert.That(stepObjects, Is.Not.Null, "场景缺少 StepObjs。");
            TexCore_M02ServerInstallBootstrap bootstrap = ConfigureBootstrapOnScene(stepObjects);
            try
            {
                MethodInfo present = Private(bootstrap, "PresentInstallPage");
                present.Invoke(bootstrap, new object[] { 13 });

                TexCore_M02InputValidationStep validation =
                    ReadInstallReporter(bootstrap, 13) as TexCore_M02InputValidationStep;
                Assert.That(validation, Is.Not.Null);
                PressableButton submit = validation.GetComponent<PressableButton>();
                Assert.That(
                    (int)Private(bootstrap, "CountCompletingReporters")
                        .Invoke(bootstrap, new object[] { submit.gameObject }),
                    Is.EqualTo(1),
                    "合规提交也只允许一个完成组件。");

                SetInputText(validation, new[] { "Root@123", "Root@123" });
                MethodInfo submitMethod = Private(validation, "Submit");
                try
                {
                    submitMethod.Invoke(validation, null);
                }
                catch (TargetInvocationException exception)
                    when (exception.InnerException is System.NullReferenceException)
                {
                    // EditMode has no TexCore_PlaneManager singleton, so the topic
                    // advance itself stays a Play Mode check. Everything before
                    // that call must already be correct.
                }

                Assert.That(validation.WasCompletedByUser, Is.True,
                    "合规密码必须完成步骤。");
                Assert.That(submit.enabled, Is.False,
                    "完成后提交按钮必须关闭，避免重复提交。");

                // A second submit must not complete or advance a second time.
                // The TexCore step state itself is driven by the framework
                // broadcast, which is a Play Mode check.
                Assert.DoesNotThrow(() => submitMethod.Invoke(validation, null));
                Assert.That(validation.WasCompletedByUser, Is.True);
                Assert.That(submit.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(bootstrap);
            }
        });
    }

    /// <summary>
    /// Mirrors the reported PICO run: six digits, then the submit button. The
    /// reason must reach the player where they are looking, which the shared
    /// Latin-only title channel cannot do for Chinese text.
    /// </summary>
    [Test]
    public void RejectedSubmit_ShowsTheReasonOnThePageItself()
    {
        WithMainlineScene(scene =>
        {
            Transform stepObjects = FindSceneTransform(scene, "StepObjs");
            Assert.That(stepObjects, Is.Not.Null, "场景缺少 StepObjs。");
            TexCore_M02ServerInstallBootstrap bootstrap = ConfigureBootstrapOnScene(stepObjects);
            TexCore_M02InputValidationStep validation = null;
            try
            {
                Private(bootstrap, "PresentInstallPage").Invoke(bootstrap, new object[] { 13 });
                validation = ReadInstallReporter(bootstrap, 13) as TexCore_M02InputValidationStep;
                Assert.That(validation, Is.Not.Null, "第 14 步必须由输入校验组件完成。");

                SetInputText(validation, new[] { "123456", "123456" });
                MethodInfo submit = Private(validation, "Submit");
                Assert.DoesNotThrow(() => submit.Invoke(validation, null));

                Assert.That(validation.WasCompletedByUser, Is.False, "六位密码必须被规则拒绝。");
                Assert.That(validation.state, Is.EqualTo(FinishState.等待触发));
                Assert.That(GetPrivateField(bootstrap, "visiblePageName"), Is.EqualTo("MiMa[14]"),
                    "校验失败不允许切换页面。");

                Assert.That(validation.HasVisibleMessage, Is.True,
                    "校验失败必须在玩家正在看的页面上给出可见反馈。");
                Assert.That(validation.ValidationMessage,
                    Is.EqualTo(TexCore_M02InputRules.PasswordLengthError));

                Text label = validation.MessageLabel;
                Assert.That(label, Is.Not.Null);
                Assert.That(label.text, Is.EqualTo(TexCore_M02InputRules.PasswordLengthError));
                Assert.That(label.raycastTarget, Is.False,
                    "页面提示不允许拦截输入框与提交按钮的点击。");

                // The message is only readable if it borrows the CJK font of the
                // page's own input text instead of the Latin-only title font.
                InputField[] boundFields = ReadBoundInputFields(validation);
                Assert.That(boundFields.Length, Is.EqualTo(2), "第 14 步必须绑定两个输入框。");
                Text referenceText = null;
                foreach (InputField field in boundFields)
                {
                    if (field.textComponent != null && field.textComponent.font != null)
                    {
                        referenceText = field.textComponent;
                        break;
                    }

                    if (field.placeholder is Text placeholder && placeholder.font != null)
                    {
                        referenceText = placeholder;
                        break;
                    }
                }

                Assert.That(referenceText, Is.Not.Null, "输入框必须有带字体的文本组件。");
                Assert.That(label.font, Is.EqualTo(referenceText.font),
                    "页面提示必须复用输入框的中文字体。");
                Assert.That(label.font.HasCharacter('密'), Is.True,
                    "页面提示的字体必须包含中文字形。");

                // A legacy text rasterizes at fontSize and the transform scales
                // that bitmap, so a small font size is drawn as a blur.
                Assert.That(label.fontSize, Is.GreaterThanOrEqualTo(20),
                    "页面提示必须按可读分辨率光栅化。");
                Assert.That(label.rectTransform.localScale.x, Is.LessThan(1f),
                    "页面提示必须整体缩小，而不是用大字号去铺满页面。");

                // The label lives on the page and must not cover any hit volume.
                RectTransform pageRect = validation.transform.parent as RectTransform;
                Assert.That(pageRect, Is.Not.Null);
                Assert.That(label.rectTransform.parent, Is.EqualTo(pageRect),
                    "页面提示必须是输入页的子物体。");

                List<Collider> hitVolumes = new List<Collider>();
                Collider buttonCollider = validation.GetComponent<Collider>();
                if (buttonCollider != null) hitVolumes.Add(buttonCollider);
                foreach (InputField field in boundFields)
                {
                    Collider container = field.transform.parent.GetComponent<Collider>();
                    if (container != null) hitVolumes.Add(container);
                }

                Assert.That(hitVolumes.Count, Is.GreaterThan(1), "输入页必须保留按钮与输入框命中体。");
                Vector3[] corners = new Vector3[4];
                label.rectTransform.GetWorldCorners(corners);
                foreach (Collider hitVolume in hitVolumes)
                {
                    foreach (Vector3 corner in corners)
                    {
                        Assert.That(hitVolume.bounds.Contains(corner), Is.False,
                            $"页面提示不允许覆盖 {hitVolume.name} 的命中体。");
                    }
                }

                // Typing again answers the message, so it must not stay stale.
                label.gameObject.SetActive(true);
                SetInputText(validation, new[] { "1234567", "1234567" });
                Assert.That(label.gameObject.activeSelf, Is.False,
                    "玩家重新输入时旧的校验提示必须消失。");

                SetInputText(validation, new[] { "Root@123", "Root@123" });
                try
                {
                    submit.Invoke(validation, null);
                }
                catch (TargetInvocationException exception)
                    when (exception.InnerException is System.NullReferenceException)
                {
                    // EditMode has no TexCore_PlaneManager singleton.
                }

                Assert.That(validation.WasCompletedByUser, Is.True, "合规密码必须完成步骤。");
                Assert.That(validation.HasVisibleMessage, Is.False,
                    "合规提交后不允许残留校验提示。");
            }
            finally
            {
                if (validation != null && validation.MessageLabel != null)
                {
                    Object.DestroyImmediate(validation.MessageLabel.gameObject);
                }

                Object.DestroyImmediate(bootstrap);
            }
        });
    }

    /// <summary>
    /// The authored IP apply button hides its own page and activates the next
    /// one. When those switches stay on, a rejected configuration jumps away from
    /// the page the player has to correct and walks the flow to the end page.
    /// </summary>
    [Test]
    public void RejectedIpConfiguration_KeepsTheIpPageInsteadOfJumpingAway()
    {
        WithMainlineScene(scene =>
        {
            Transform stepObjects = FindSceneTransform(scene, "StepObjs");
            Assert.That(stepObjects, Is.Not.Null, "场景缺少 StepObjs。");
            TexCore_M02ServerInstallBootstrap bootstrap = ConfigureBootstrapOnScene(stepObjects);
            try
            {
                TexCore_M02IpConfigurationStep ipStep =
                    GetPrivateField(bootstrap, "ipStep") as TexCore_M02IpConfigurationStep;
                Assert.That(ipStep, Is.Not.Null, "M02 引导必须绑定 IP 配置步骤。");

                PressableButton apply = ipStep.GetComponent<PressableButton>();
                Assert.That(apply, Is.Not.Null, "IP 配置页面必须有应用按钮。");

                // The authored navigation is the reason the page used to move on
                // its own, so it has to be off for the run.
                List<string> methods = ReadPersistentCallMethods(apply.OnClicked);
                List<UnityEventCallState> states = ReadPersistentCallStates(apply.OnClicked);
                List<Object> targets = ReadPersistentCallTargets(apply.OnClicked);
                Assert.That(methods, Does.Contain("SetActive"),
                    "IP 应用按钮必须保留作者回调以便核对。");
                int pageSwitches = 0;
                for (int index = 0; index < methods.Count; index++)
                {
                    if (methods[index] != "SetActive") continue;
                    // A call without a GameObject target cannot switch a page, so
                    // the bootstrap leaves it alone.
                    if (!(targets[index] is GameObject)) continue;
                    pageSwitches++;
                    Assert.That(states[index], Is.EqualTo(UnityEventCallState.Off),
                        "IP 应用按钮的原始翻页回调必须在运行时关闭。");
                }

                Assert.That(pageSwitches, Is.GreaterThan(0), "IP 应用按钮必须存在原始翻页回调。");

                SetPrivateField(bootstrap, "ipPageIndex", 7);
                Private(bootstrap, "ShowOnlyOriginalPage").Invoke(bootstrap, new object[] { "YouXian10" });
                Assert.That(GetPrivateField(bootstrap, "visiblePageName"), Is.EqualTo("YouXian10"));

                ipStep.UnderWay();
                List<string> titles = new List<string>();
                CallBack<string> titleHandler = text => titles.Add(text);
                TexCore_EventCenter.AddListener<string>(TexCore_EventType.显示标题, titleHandler);
                try
                {
                    Private(ipStep, "Submit").Invoke(ipStep, null);
                }
                finally
                {
                    TexCore_EventCenter.RemoveListener<string>(TexCore_EventType.显示标题, titleHandler);
                }

                Assert.That(ipStep.WasCompletedByUser, Is.False,
                    "空 IP 配置不允许完成步骤。");
                Assert.That(GetPrivateField(bootstrap, "visiblePageName"), Is.EqualTo("YouXian10"),
                    "IP 校验失败后必须停留在 IP 配置页面。");
                Assert.That(titles, Is.Not.Empty, "IP 校验失败必须给出原因。");
            }
            finally
            {
                Object.DestroyImmediate(bootstrap);
            }
        });
    }

    /// <summary>
    /// The player is pointed at the fields first and only then at the submit
    /// button, and both stay usable while their highlight is off.
    /// </summary>
    [Test]
    public void InputPageGuidance_PointsAtTheFieldsFirstAndAtTheSubmitButtonAfterwards()
    {
        WithMainlineScene(scene =>
        {
            Transform stepObjects = FindSceneTransform(scene, "StepObjs");
            Assert.That(stepObjects, Is.Not.Null, "场景缺少 StepObjs。");
            TexCore_M02ServerInstallBootstrap bootstrap = ConfigureBootstrapOnScene(stepObjects);
            try
            {
                Private(bootstrap, "PresentInstallPage").Invoke(bootstrap, new object[] { 13 });
                TexCore_M02InputValidationStep validation =
                    ReadInstallReporter(bootstrap, 13) as TexCore_M02InputValidationStep;
                Assert.That(validation, Is.Not.Null);

                TexCore_ClickObjectStep submitGuide =
                    GetPrivateField(validation, "guidance") as TexCore_ClickObjectStep;
                List<TexCore_ClickObjectStep> fieldGuides =
                    GetPrivateField(validation, "inputGuidances") as List<TexCore_ClickObjectStep>;
                Assert.That(submitGuide, Is.Not.Null, "提交按钮必须有指导组件。");
                Assert.That(fieldGuides, Has.Count.EqualTo(2), "两个输入框容器都必须有指导组件。");

                PressableButton submit = validation.GetComponent<PressableButton>();
                Assert.That(submit.enabled, Is.True, "输入未完成时提交按钮仍必须可点。");
                Assert.That(submitGuide.state, Is.EqualTo(FinishState.未完成),
                    "输入未完成时提交按钮不允许高亮。");
                foreach (TexCore_ClickObjectStep guide in fieldGuides)
                {
                    Assert.That(guide.state, Is.EqualTo(FinishState.等待触发),
                        "先高亮的是需要输入的输入框。");
                    Assert.That(guide.GetComponent<PressableButton>().enabled, Is.True,
                        "输入框在等待输入时必须可点。");
                }

                // Completing the input moves the highlight to the submit button.
                SetInputText(validation, new[] { "Root@123", "Root@123" });
                Assert.That(submitGuide.state, Is.EqualTo(FinishState.等待触发),
                    "输入合规后必须高亮提交按钮。");
                foreach (TexCore_ClickObjectStep guide in fieldGuides)
                {
                    Assert.That(guide.state, Is.EqualTo(FinishState.未完成),
                        "输入合规后输入框高亮必须关闭。");
                    Assert.That(guide.GetComponent<PressableButton>().enabled, Is.True,
                        "关闭高亮后输入框仍必须可点。");
                }

                Assert.That(submit.enabled, Is.True);

                // Correcting a value moves the highlight back to the fields.
                SetInputText(validation, new[] { "Root@123", "Root@1234" });
                Assert.That(submitGuide.state, Is.EqualTo(FinishState.未完成),
                    "输入再次不合规时按钮高亮必须关闭。");
                foreach (TexCore_ClickObjectStep guide in fieldGuides)
                {
                    Assert.That(guide.state, Is.EqualTo(FinishState.等待触发));
                }
            }
            finally
            {
                Object.DestroyImmediate(bootstrap);
            }
        });
    }

    /// <summary>
    /// The IP navigation pages used to highlight every button of the page. Only
    /// the button that actually advances the flow may carry the highlight.
    /// </summary>
    [Test]
    public void IpNavigationPages_HighlightOnlyTheirNavigationButton()
    {
        WithMainlineScene(scene =>
        {
            Transform stepObjects = FindSceneTransform(scene, "StepObjs");
            Assert.That(stepObjects, Is.Not.Null, "场景缺少 StepObjs。");
            TexCore_M02ServerInstallBootstrap bootstrap = ConfigureBootstrapOnScene(stepObjects);
            try
            {
                IList pages = (IList)GetPrivateField(bootstrap, "ipPageGuidances");
                Assert.That(pages.Count, Is.EqualTo(7), "IP 导航页必须全部完成绑定。");
                foreach (object entry in pages)
                {
                    IList guides = (IList)entry;
                    Assert.That(guides.Count, Is.EqualTo(1),
                        "IP 导航页只允许高亮真正翻页的那个按钮。");
                    TexCore_ClickObjectStep guide = (TexCore_ClickObjectStep)guides[0];
                    // Only one page is shown at a time, so the other pages' own
                    // buttons are inactive in the hierarchy; what matters is that
                    // the highlighted button is not one the page hides.
                    Assert.That(guide.gameObject.activeSelf, Is.True,
                        "高亮必须落在作者可见的按钮上。");
                }
            }
            finally
            {
                Object.DestroyImmediate(bootstrap);
            }
        });
    }

    /// <summary>
    /// The U-disk is summoned from the 02 backpack panel, and a click only
    /// reaches the button while it is enabled and really visible: the backpack UI
    /// shares its position with other canvases, so a panel that is merely active
    /// can still receive no input at all.
    /// </summary>
    [Test]
    public void UsbBackpackEntry_StaysClickableWhileItsStepIsPresented()
    {
        WithMainlineScene(scene =>
        {
            Transform stepObjects = FindSceneTransform(scene, "StepObjs");
            Assert.That(stepObjects, Is.Not.Null, "场景缺少 StepObjs。");
            TexCore_M02ServerInstallBootstrap bootstrap = ConfigureBootstrapOnScene(stepObjects);
            try
            {
                GameObject panel = GetPrivateField(bootstrap, "usbBackpackPanel") as GameObject;
                Assert.That(panel, Is.Not.Null, "M02 必须绑定 U 盘背包面板。");
                Assert.That(panel.activeInHierarchy, Is.False, "未进入 U 盘步骤前面板必须关闭。");

                Private(bootstrap, "ShowUsbBackpackPanel").Invoke(bootstrap, null);
                Assert.That(panel.activeInHierarchy, Is.True, "U 盘步骤必须自动打开背包面板。");

                PressableButton entry = panel.GetComponentInChildren<PressableButton>(true);
                Assert.That(entry, Is.Not.Null, "背包面板必须有可点按钮。");
                Assert.That(entry.name, Is.EqualTo("02_U盘"), "面板里点的必须是 U 盘按钮。");
                Assert.That(entry.gameObject.activeInHierarchy, Is.True);
                Assert.That(entry.enabled, Is.True,
                    "U 盘按钮必须可点，否则点了不会有任何反应。");
                Assert.That(TexCore_FocusHighlight.IsUiVisible(entry.gameObject), Is.True,
                    "U 盘按钮必须真的可见：父链、Canvas 与 CanvasGroup 都要开着，否则点击会被别的画布吃掉。");

                Collider collider = entry.GetComponent<Collider>();
                Assert.That(collider, Is.Not.Null, "U 盘按钮必须有碰撞体才能被指点到。");
                Assert.That(collider.enabled, Is.True, "U 盘按钮的碰撞体必须开着。");
            }
            finally
            {
                Object.DestroyImmediate(bootstrap);
            }
        });
    }

    private static IEnumerable<string[]> InvalidValueSets(int stepIndex)
    {
        switch (stepIndex)
        {
            case 13:
            case 24:
                yield return new[] { string.Empty, string.Empty };
                yield return new[] { "Ab1!", "Ab1!" };
                yield return new[] { "abcdef1!", "abcdef1!" };
                yield return new[] { "Abcdef1!", "Abcdef1?" };
                break;
            default:
                yield return new[] { string.Empty, string.Empty };
                yield return new[] { "ZhangSan", "张三" };
                yield return new[] { "zhangsan", "Zhang 3" };
                break;
        }
    }

    /// <summary>
    /// The reporter sits on the submit button, so the bound InputFields only
    /// exist in its own field array, not on its GameObject.
    /// </summary>
    private static InputField[] ReadBoundInputFields(TexCore_M02InputValidationStep validation)
    {
        FieldInfo field = typeof(TexCore_M02InputValidationStep).GetField(
            "inputFields", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (InputField[])field.GetValue(validation);
    }

    private static void SetInputText(TexCore_M02InputValidationStep validation, string[] values)
    {
        // The reporter re-reads the live InputField text on every submit.
        InputField[] fields = ReadBoundInputFields(validation);
        Assert.That(fields.Length, Is.EqualTo(values.Length));
        for (int index = 0; index < fields.Length; index++)
        {
            fields[index].text = values[index];
        }
    }

    /// <summary>
    /// Binds the M02 bootstrap on the real StepObjs object, retrying Configure()
    /// the same way the runtime does until the authored UI is resolved.
    /// </summary>
    private static TexCore_M02ServerInstallBootstrap ConfigureBootstrapOnScene(Transform stepObjects)
    {
        TexCore_M02ServerInstallBootstrap bootstrap =
            stepObjects.gameObject.GetComponent<TexCore_M02ServerInstallBootstrap>() ??
            stepObjects.gameObject.AddComponent<TexCore_M02ServerInstallBootstrap>();
        MethodInfo configure = Private(bootstrap, "Configure");
        for (int attempt = 0; attempt < 40; attempt++)
        {
            if ((bool)GetPrivateField(bootstrap, "configured")) break;
            configure.Invoke(bootstrap, null);
        }

        Assert.That(GetPrivateField(bootstrap, "configured"), Is.EqualTo(true),
            "M02 引导必须在真实场景中完成绑定。");
        return bootstrap;
    }

    private static object ReadInstallReporter(
        TexCore_M02ServerInstallBootstrap bootstrap, int stepIndex)
    {
        IList reporters = (IList)GetPrivateField(bootstrap, "installReporters");
        Assert.That(reporters.Count, Is.EqualTo(27));
        return reporters[stepIndex];
    }

    /// <summary>
    /// UnityEventBase serializes PersistentCallGroup m_PersistentCalls, whose
    /// m_Calls entries carry the call state, which has no public getter.
    /// </summary>
    private static List<UnityEventCallState> ReadPersistentCallStates(UnityEventBase unityEvent)
    {
        List<UnityEventCallState> states = new List<UnityEventCallState>();
        foreach (object call in ReadPersistentCalls(unityEvent))
        {
            FieldInfo stateField = call.GetType().GetField(
                "m_State", BindingFlags.Instance | BindingFlags.NonPublic) ??
                call.GetType().GetField("m_CallState", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(stateField, Is.Not.Null, "无法读取持久化回调状态。");
            states.Add((UnityEventCallState)stateField.GetValue(call));
        }

        return states;
    }

    private static List<string> ReadPersistentCallMethods(UnityEventBase unityEvent)
    {
        List<string> methods = new List<string>();
        foreach (object call in ReadPersistentCalls(unityEvent))
        {
            FieldInfo methodField = call.GetType().GetField(
                "m_MethodName", BindingFlags.Instance | BindingFlags.NonPublic);
            methods.Add(methodField != null ? (string)methodField.GetValue(call) : string.Empty);
        }

        return methods;
    }

    private static List<Object> ReadPersistentCallTargets(UnityEventBase unityEvent)
    {
        List<Object> targets = new List<Object>();
        foreach (object call in ReadPersistentCalls(unityEvent))
        {
            FieldInfo targetField = call.GetType().GetField(
                "m_Target", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(targetField, Is.Not.Null, "无法读取持久化回调目标。");
            targets.Add(targetField.GetValue(call) as Object);
        }

        return targets;
    }

    private static List<object> ReadPersistentCalls(UnityEventBase unityEvent)
    {
        List<object> calls = new List<object>();
        FieldInfo groupField = typeof(UnityEventBase).GetField(
            "m_PersistentCalls", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(groupField, Is.Not.Null, "无法读取持久化回调组。");
        object group = groupField.GetValue(unityEvent);
        Assert.That(group, Is.Not.Null);
        FieldInfo listField = group.GetType().GetField(
            "m_Calls", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(listField, Is.Not.Null, "无法读取持久化回调列表。");
        foreach (object call in (IEnumerable)listField.GetValue(group))
        {
            calls.Add(call);
        }

        return calls;
    }

    private static void AssertValidation(
        M02InputValidationMode mode,
        string[] values,
        bool expectedValid,
        string expectedError)
    {
        bool valid = TexCore_M02InputRules.TryValidate(mode, values, out string error);
        Assert.That(valid, Is.EqualTo(expectedValid));
        Assert.That(error, Is.EqualTo(expectedError ?? string.Empty));
    }

    private static void AssertField(
        TexCore_M02ServerInstallBootstrap bootstrap,
        Transform uiRoot,
        int stepIndex,
        int fieldIndex,
        string expectedParentName,
        string expectedFieldName)
    {
        InstallInputRuleView rule = ReadInstallInputRules()
            .Single(value => value.StepIndex == stepIndex);
        object installPage = ReadInstallPages().GetValue(stepIndex);
        MethodInfo findPage = Private(bootstrap, "FindInstallPageTransform");
        Transform page = (Transform)findPage.Invoke(bootstrap, new[] { installPage });
        Assert.That(page, Is.Not.Null, $"第 {stepIndex + 1} 步找不到页面。");

        object locator = ((IList)rule.Fields)[fieldIndex];
        MethodInfo resolve = Private(bootstrap, "FindPageInputField");
        InputField field = (InputField)resolve.Invoke(bootstrap, new[] { page, locator });

        Assert.That(field, Is.Not.Null,
            $"第 {stepIndex + 1} 步缺少输入框 {expectedParentName}/{expectedFieldName}。");
        Assert.That(field.name, Is.EqualTo(expectedFieldName));
        Assert.That(field.transform.parent.name, Is.EqualTo(expectedParentName));
        Assert.That(uiRoot.GetComponentsInChildren<InputField>(true), Does.Contain(field));
    }

    private static TexCore_M02ServerInstallBootstrap CreateBootstrap(Transform uiRoot)
    {
        GameObject root = new GameObject("M02 Input Validation Bootstrap");
        root.SetActive(false);
        TexCore_M02ServerInstallBootstrap bootstrap =
            root.AddComponent<TexCore_M02ServerInstallBootstrap>();
        SetPrivateField(bootstrap, "uiRoot", uiRoot);
        return bootstrap;
    }

    private static void WithMainlineScene(System.Action<Scene> body)
    {
        Scene previousScene = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(MainlineScenePath);
        if (scene.IsValid() && scene.isLoaded)
        {
            // The M02 bootstrap renames the authored page buttons and hides the
            // pages while binding, so every test needs its own pristine copy.
            EditorSceneManager.CloseScene(scene, true);
        }

        scene = EditorSceneManager.OpenScene(MainlineScenePath, OpenSceneMode.Additive);

        try
        {
            SceneManager.SetActiveScene(scene);
            body(scene);
        }
        finally
        {
            if (previousScene.IsValid() && previousScene.isLoaded && previousScene != scene)
            {
                SceneManager.SetActiveScene(previousScene);
            }

            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static Transform FindSceneTransform(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform match = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(value => value.name == objectName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static PressableButton FindDirectPageButton(Transform page, int index)
    {
        if (page == null)
        {
            return null;
        }

        List<PressableButton> buttons = new List<PressableButton>();
        for (int childIndex = 0; childIndex < page.childCount; childIndex++)
        {
            Transform child = page.GetChild(childIndex);
            if (!child.name.StartsWith("Empty Button", System.StringComparison.Ordinal))
            {
                continue;
            }

            PressableButton button = child.GetComponent<PressableButton>();
            if (button != null)
            {
                buttons.Add(button);
            }
        }

        return index >= 0 && index < buttons.Count ? buttons[index] : null;
    }

    private static MethodInfo Private(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, methodName);
        return method;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }

    private static object GetPrivateField(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        return field.GetValue(target);
    }

    private static System.Array ReadInstallPages()
    {
        FieldInfo field = typeof(TexCore_M02ServerInstallBootstrap).GetField(
            "InstallPages", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (System.Array)field.GetValue(null);
    }

    private static InstallInputRuleView[] ReadInstallInputRules()
    {
        FieldInfo field = typeof(TexCore_M02ServerInstallBootstrap).GetField(
            "InstallInputRules", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        System.Array raw = (System.Array)field.GetValue(null);
        InstallInputRuleView[] views = new InstallInputRuleView[raw.Length];
        for (int index = 0; index < raw.Length; index++)
        {
            views[index] = new InstallInputRuleView(raw.GetValue(index));
        }

        return views;
    }

    /// <summary>Reflection view over the private nested InstallInputRule value.</summary>
    private readonly struct InstallInputRuleView
    {
        public InstallInputRuleView(object value)
        {
            Value = value;
            System.Type type = value.GetType();
            StepIndex = (int)type.GetProperty("StepIndex").GetValue(value);
            Mode = (M02InputValidationMode)type.GetProperty("Mode").GetValue(value);
            Fields = (IList)type.GetProperty("Fields").GetValue(value);
        }

        public object Value { get; }
        public int StepIndex { get; }
        public M02InputValidationMode Mode { get; }
        public IList Fields { get; }
    }
}
