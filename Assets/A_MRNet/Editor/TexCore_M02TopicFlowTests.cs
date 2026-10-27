using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MixedReality.Toolkit.UX;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class TexCore_M02TopicFlowTests
{
    [Test]
    public void BootstrapUpdate_DoesNotScanOrCleanConfigEveryFrame()
    {
        string source = File.ReadAllText(
            Path.Combine(Application.dataPath, "A_MRNet", "Scripts",
                "TexCore_M02ServerInstallBootstrap.cs"));
        string update = ExtractMethod(source, "private void Update()");

        Assert.That(update, Does.Not.Contain("FindObjectsOfType"));
        Assert.That(update, Does.Not.Contain("Resources.FindObjectsOfTypeAll"));
        Assert.That(update, Does.Not.Contain("RemoveKnowledgeQuizTopicFromConfig"));
        Assert.That(update, Does.Not.Contain("TryCleanConfigOnce"));
    }

    [Test]
    public void ServerInstallNodePrefab_HasNoKnowledgeQuizAndKeepsTheOperationalOrder()
    {
        GameObject nodePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/BusinessPrefabs/MR混合现实多人协作平台/" +
            "01货到人拣选电商仓储实施安装与调试实训系统/02服务器系统安装.prefab");
        NodePanelBase node = nodePrefab != null ? nodePrefab.GetComponent<NodePanelBase>() : null;

        Assert.That(node, Is.Not.Null);
        CollectionAssert.AreEqual(new[]
        {
            "00介绍视频",
            "02U盘启动盘插入",
            "03系统安装操作",
            "04设置正确IP",
            "05完成整个模块"
        }, node.datas.childData.ConvertAll(data => data.name));
    }

    [Test]
    public void ServerInstallBinding_CoversTheAuthoredScreenPageOrder()
    {
        FieldInfo pagesField = typeof(TexCore_M02ServerInstallBootstrap).GetField(
            "InstallPages", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(pagesField, Is.Not.Null);

        System.Array pages = (System.Array)pagesField.GetValue(null);
        string[] expectedNames =
        {
            "XuanZeXiTong", "Video1", "YuYanXuanZe1", "ZhuYe2", "RiQi",
            "JianPan", "AnZhuangYuan", "RuanJianXuanZe", "GNONEME", "ZhuYe3",
            "AnZhuangWeiZhi", "ZhuYe4", "ROOTMiMa", "MiMa", "Video2",
            "ChongQi", "XuKe", "WanChengPeiZhi", "YuYan", "ShuRu", "YinSi",
            "ShiQu2", "ZaiXianZhangHao", "YongHuMing", "MiMa", "WanCheng",
            "Start"
        };
        int[] expectedUiIndices =
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
            16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27
        };

        Assert.That(pages.Length, Is.EqualTo(expectedNames.Length));
        for (int index = 0; index < pages.Length; index++)
        {
            object page = pages.GetValue(index);
            System.Type pageType = page.GetType();
            Assert.That(pageType.GetProperty("PageName").GetValue(page), Is.EqualTo(expectedNames[index]));
            Assert.That(pageType.GetProperty("UiChildIndex").GetValue(page), Is.EqualTo(expectedUiIndices[index]));
        }
    }

    [Test]
    public void ServerInstallUsbBinding_UsesTheAuthoredPortColliderAsPlacementTarget()
    {
        string source = File.ReadAllText(
            Path.Combine(Application.dataPath, "A_MRNet", "Scripts",
                "TexCore_M02ServerInstallBootstrap.cs"));

        Assert.That(source, Does.Contain("usbStep.Target = usbCollider;"));
        Assert.That(source, Does.Not.Contain("usbStep.Target = usbTarget;"));
        Assert.That(source, Does.Not.Contain("FindDirectChild(modelRoot, \"UPanPosition\")"));
    }

    [Test]
    public void ServerInstallStepTree_RemovesTheStaleKnowledgeQuizBeforeBinding()
    {
        GameObject quiz = CreateTemporaryObject(TexCore_M02TopicFlow.KnowledgeQuizTopic);
        GameObject usb = CreateTemporaryObject("02U盘启动盘插入");
        GameObject install = CreateTemporaryObject("03系统安装操作");
        List<TopicObjs> topics = new List<TopicObjs>
        {
            new TopicObjs { topicPrefab = quiz, topicObjs = new List<StepObjs>() },
            new TopicObjs { topicPrefab = usb, topicObjs = new List<StepObjs>() },
            new TopicObjs { topicPrefab = install, topicObjs = new List<StepObjs>() }
        };

        try
        {
            Assert.That(TexCore_M02TopicFlow.RemoveKnowledgeQuizTopic(topics), Is.True);
            CollectionAssert.AreEqual(new[] { usb.name, install.name },
                topics.ConvertAll(topic => topic.topicPrefab.name));
        }
        finally
        {
            Object.DestroyImmediate(quiz);
            Object.DestroyImmediate(usb);
            Object.DestroyImmediate(install);
        }
    }

    [Test]
    public void ServerInstallBinding_ResolvesTheAuthoredM02HierarchyInsteadOfFirstNameMatch()
    {
        GameObject decoy = CreateTemporaryObject("02服务器系统安装");
        GameObject noInteractionRoot = CreateTemporaryObject("无交互物体");
        GameObject systemRoot = CreateTemporaryObject("01货到人拣选电商仓储实施安装与调试实训系统", noInteractionRoot);
        GameObject expectedNode = CreateTemporaryObject("02服务器系统安装", systemRoot);

        try
        {
            MethodInfo resolver = typeof(TexCore_M02ServerInstallBootstrap).GetMethod(
                "FindPathFromRoots",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(resolver, Is.Not.Null);
            Transform resolved = (Transform)resolver.Invoke(null, new object[]
            {
                new[] { decoy.transform, noInteractionRoot.transform },
                new[] { "无交互物体", "01货到人拣选电商仓储实施安装与调试实训系统", "02服务器系统安装" }
            });

            Assert.That(resolved, Is.EqualTo(expectedNode.transform));
        }
        finally
        {
            Object.DestroyImmediate(decoy);
            Object.DestroyImmediate(noInteractionRoot);
        }
    }

    [Test]
    public void ServerInstallBackpackPanel_ShowsFromItsOwnBackpackRootWithoutAnActiveSceneGate()
    {
        GameObject backpack = CreateTemporaryObject("背包UI");
        GameObject defaultPanel = CreateTemporaryObject("01通用物品背包面板", backpack);
        GameObject m02Panel = CreateTemporaryObject("02服务器系统安装背包面板", backpack);
        GameObject rackPanel = CreateTemporaryObject("06货架安装背包面板", backpack);
        m02Panel.SetActive(false);

        try
        {
            MethodInfo show = typeof(TexCore_BackpackChapterTabs).GetMethod(
                "ShowM02PanelFromBackpack",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(show, Is.Not.Null);
            show.Invoke(null, new object[] { backpack.transform, m02Panel });

            Assert.That(m02Panel.activeSelf, Is.True);
            Assert.That(defaultPanel.activeSelf, Is.False);
            Assert.That(rackPanel.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(backpack);
        }
    }

    [Test]
    public void ServerInstallBackpackPanel_ReopensTheClosedBackpackRoot()
    {
        GameObject backpack = CreateTemporaryObject("背包UI");
        GameObject m02Panel = CreateTemporaryObject("02服务器系统安装背包面板", backpack);
        backpack.SetActive(false);

        try
        {
            MethodInfo show = typeof(TexCore_BackpackChapterTabs).GetMethod(
                "ShowM02PanelFromBackpack",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(show, Is.Not.Null);
            show.Invoke(null, new object[] { backpack.transform, m02Panel });

            Assert.That(backpack.activeSelf, Is.True,
                "进入 M02 的 U 盘步骤时必须重新打开被手菜单关闭的背包根节点。");
            Assert.That(m02Panel.activeSelf, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(backpack);
        }
    }

    [Test]
    public void ServerInstallBootstrap_FindsActiveM02NodeWhileTopicPanelIsTemporarilyHidden()
    {
        GameObject bootstrapRoot = CreateTemporaryObject("M02 Bootstrap");
        GameObject nodeRoot = CreateTemporaryObject("M02 Node");
        GameObject topicRoot = CreateTemporaryObject("02U盘启动盘插入", nodeRoot);
        bootstrapRoot.SetActive(false);
        topicRoot.SetActive(false);

        try
        {
            TexCore_M02ServerInstallBootstrap bootstrap =
                bootstrapRoot.AddComponent<TexCore_M02ServerInstallBootstrap>();
            QuietNodePanel node = nodeRoot.AddComponent<QuietNodePanel>();
            node.datas = new TexCore_DataClass { name = "02服务器系统安装" };

            PanelTopicBase topic = topicRoot.AddComponent<PanelTopicBase>();
            topic.datas = new TexCore_DataClass { name = "02U盘启动盘插入" };
            node.activeChild = topic;

            MethodInfo finder = typeof(TexCore_M02ServerInstallBootstrap).GetMethod(
                "FindActiveM02Node", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(finder, Is.Not.Null);
            Assert.That(topic.gameObject.activeInHierarchy, Is.False);
            Assert.That(finder.Invoke(bootstrap, null), Is.EqualTo(node),
                "当前 M02 节点已激活时，主题面板在切换帧暂时隐藏也不能丢失 U 盘背包入口。");
        }
        finally
        {
            Object.DestroyImmediate(bootstrapRoot);
            Object.DestroyImmediate(nodeRoot);
        }
    }

    [Test]
    public void ServerInstallBootstrap_DoesNotHideAUsbThatWasAlreadySummoned()
    {
        GameObject bootstrapRoot = CreateTemporaryObject("M02 Bootstrap");
        GameObject usbRoot = CreateTemporaryObject("UPan");
        bootstrapRoot.SetActive(false);

        try
        {
            TexCore_M02ServerInstallBootstrap bootstrap =
                bootstrapRoot.AddComponent<TexCore_M02ServerInstallBootstrap>();
            SetPrivateField(bootstrap, "usb", usbRoot.transform);
            MethodInfo hide = typeof(TexCore_M02ServerInstallBootstrap).GetMethod(
                "HideUsbBeforeFirstSummon", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(hide, Is.Not.Null);
            hide.Invoke(bootstrap, null);
            Assert.That(usbRoot.activeSelf, Is.False);

            usbRoot.SetActive(true);
            hide.Invoke(bootstrap, null);

            Assert.That(usbRoot.activeSelf, Is.True,
                "屏幕步骤的延迟绑定重试不能再次隐藏玩家已经从背包取出的 U 盘。");
        }
        finally
        {
            Object.DestroyImmediate(bootstrapRoot);
            Object.DestroyImmediate(usbRoot);
        }
    }

    [Test]
    public void ServerInstallBootstrap_PresentsUsbBackpackWhileScreenBindingsRetry()
    {
        GameObject bootstrapRoot = CreateTemporaryObject("M02 Bootstrap");
        GameObject modelRoot = CreateTemporaryObject("M02 Model Root");
        GameObject uiRoot = CreateTemporaryObject("UI_XiTongJingXiangAnZhuang", modelRoot);
        GameObject backpack = CreateTemporaryObject("背包UI");
        GameObject m02Panel = CreateTemporaryObject("02服务器系统安装背包面板", backpack);
        GameObject nodeRoot = CreateTemporaryObject("M02 Node");
        GameObject topicRoot = CreateTemporaryObject("02U盘启动盘插入", nodeRoot);
        bootstrapRoot.SetActive(false);
        backpack.SetActive(false);
        topicRoot.SetActive(false);

        try
        {
            TexCore_M02ServerInstallBootstrap bootstrap =
                bootstrapRoot.AddComponent<TexCore_M02ServerInstallBootstrap>();
            QuietNodePanel node = nodeRoot.AddComponent<QuietNodePanel>();
            node.datas = new TexCore_DataClass { name = "02服务器系统安装" };

            PanelTopicBase topic = topicRoot.AddComponent<PanelTopicBase>();
            topic.datas = new TexCore_DataClass { name = "02U盘启动盘插入" };
            node.activeChild = topic;

            SetPrivateField(bootstrap, "nodeRoot", modelRoot.transform);
            SetPrivateField(bootstrap, "uiRoot", uiRoot.transform);
            SetPrivateField(bootstrap, "usbBackpackPanel", m02Panel);
            SetPrivateField(bootstrap, "usbConfigured", true);

            MethodInfo update = typeof(TexCore_M02ServerInstallBootstrap).GetMethod(
                "Update", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(update, Is.Not.Null);
            update.Invoke(bootstrap, null);

            Assert.That(backpack.activeSelf, Is.True,
                "屏幕按钮延迟绑定时，U盘步骤仍必须打开背包根节点。");
            Assert.That(m02Panel.activeSelf, Is.True,
                "屏幕按钮延迟绑定时，玩家仍必须能看到并点击 U盘 按钮。");
        }
        finally
        {
            Object.DestroyImmediate(bootstrapRoot);
            Object.DestroyImmediate(modelRoot);
            Object.DestroyImmediate(backpack);
            Object.DestroyImmediate(nodeRoot);
        }
    }

    [Test]
    public void ServerInstallCompletionState_UsesTheCopyUnderTheM02Node()
    {
        GameObject m02Node = CreateTemporaryObject("02服务器系统安装");
        GameObject expectedState = CreateTemporaryObject("NetWork-03_2", m02Node);

        try
        {
            MethodInfo finder = typeof(TexCore_M02ServerInstallBootstrap).GetMethod(
                "FindM02CompletedState",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(finder, Is.Not.Null);
            Assert.That(finder.Invoke(null, new object[] { m02Node.transform }), Is.EqualTo(expectedState.transform));
        }
        finally
        {
            Object.DestroyImmediate(m02Node);
        }
    }

    [Test]
    public void ServerInstallIpBinding_FindsTheAuthoredUguiInputField()
    {
        GameObject page = CreateTemporaryObject("YouXian10");
        GameObject addressRoot = CreateTemporaryObject("DiZhi", page);
        InputField expectedField = addressRoot.AddComponent<InputField>();

        try
        {
            MethodInfo finder = typeof(TexCore_M02ServerInstallBootstrap).GetMethod(
                "FindInputField",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(finder, Is.Not.Null);
            Assert.That(finder.Invoke(null, new object[] { page.transform, "DiZhi" }), Is.EqualTo(expectedField));
        }
        finally
        {
            Object.DestroyImmediate(page);
        }
    }

    [Test]
    public void ServerInstallBinding_RefreshesCachedStepReporters()
    {
        GameObject bootstrapRoot = CreateTemporaryObject("M02 Bootstrap");
        GameObject nodePrefab = CreateTemporaryObject("02服务器系统安装");
        GameObject topicPrefab = CreateTemporaryObject("03系统安装操作");
        GameObject originalReporterObject = CreateTemporaryObject("Original Reporter");
        GameObject replacementReporterObject = CreateTemporaryObject("Replacement Reporter");
        bootstrapRoot.SetActive(false);

        try
        {
            TexCore_ClickObjectStep originalReporter =
                originalReporterObject.AddComponent<TexCore_ClickObjectStep>();
            TexCore_ClickObjectStep replacementReporter =
                replacementReporterObject.AddComponent<TexCore_ClickObjectStep>();
            StepObjs step = new StepObjs
            {
                stepIndex = 0,
                stepObjs = new List<GameObject> { originalReporterObject }
            };
            NodeObjs node = new NodeObjs
            {
                nodePrefab = nodePrefab,
                nodeObjs = new List<TopicObjs>
                {
                    new TopicObjs
                    {
                        topicPrefab = topicPrefab,
                        topicObjs = new List<StepObjs> { step }
                    }
                }
            };
            TexCore_M02ServerInstallBootstrap bootstrap =
                bootstrapRoot.AddComponent<TexCore_M02ServerInstallBootstrap>();

            Assert.That(step.StepScripts, Is.EquivalentTo(new[] { originalReporter }));
            SetPrivateField(bootstrap, "m02Node", node);
            SetPrivateField(bootstrap, "m02NodeIndex", 1);
            MethodInfo bindStep = typeof(TexCore_M02ServerInstallBootstrap).GetMethod(
                "BindStep", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(bindStep, Is.Not.Null);
            bindStep.Invoke(bootstrap, new object[]
            {
                "03系统安装操作", 0, replacementReporterObject, replacementReporter
            });

            Assert.That(step.StepScripts, Is.EquivalentTo(new[] { replacementReporter }),
                "替换 M02 步骤对象后，TexCore 必须读取新的交互报告器。");
        }
        finally
        {
            Object.DestroyImmediate(bootstrapRoot);
            Object.DestroyImmediate(nodePrefab);
            Object.DestroyImmediate(topicPrefab);
            Object.DestroyImmediate(originalReporterObject);
            Object.DestroyImmediate(replacementReporterObject);
        }
    }

    [Test]
    public void ServerInstallStepTree_NormalizesThe27StepIndicesInOrder()
    {
        GameObject bootstrapRoot = CreateTemporaryObject("M02 Bootstrap");
        GameObject nodePrefab = CreateTemporaryObject("02服务器系统安装");
        GameObject topicPrefab = CreateTemporaryObject("03系统安装操作");
        bootstrapRoot.SetActive(false);

        try
        {
            NodeObjs node = new NodeObjs
            {
                nodePrefab = nodePrefab,
                nodeObjs = new List<TopicObjs>
                {
                    new TopicObjs
                    {
                        topicPrefab = topicPrefab,
                        topicObjs = new List<StepObjs>
                        {
                            new StepObjs { stepIndex = 30, stepObjs = new List<GameObject>() },
                            new StepObjs { stepIndex = 5, stepObjs = new List<GameObject>() },
                            new StepObjs { stepIndex = 5, stepObjs = new List<GameObject>() },
                            new StepObjs { stepIndex = 0, stepObjs = new List<GameObject>() }
                        }
                    }
                }
            };
            TexCore_M02ServerInstallBootstrap bootstrap =
                bootstrapRoot.AddComponent<TexCore_M02ServerInstallBootstrap>();
            SetPrivateField(bootstrap, "m02Node", node);
            MethodInfo normalize = typeof(TexCore_M02ServerInstallBootstrap).GetMethod(
                "NormalizeTopicSteps", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(normalize, Is.Not.Null);
            normalize.Invoke(bootstrap, new object[] { "03系统安装操作", 27 });

            CollectionAssert.AreEqual(
                Enumerable.Range(0, 27).ToList(),
                node.nodeObjs[0].topicObjs.ConvertAll(step => step.stepIndex),
                "M02 系统安装必须保持 0-26 共 27 个连续步骤索引。");
        }
        finally
        {
            Object.DestroyImmediate(bootstrapRoot);
            Object.DestroyImmediate(nodePrefab);
            Object.DestroyImmediate(topicPrefab);
        }
    }

    [Test]
    public void ServerInstallTarget_WakesTheAuthoredInactiveButton()
    {
        GameObject bootstrapRoot = CreateTemporaryObject("M02 Bootstrap");
        GameObject uiRoot = CreateTemporaryObject("UI_XiTongJingXiangAnZhuang");
        GameObject page = CreateTemporaryObject("Video2", uiRoot);
        GameObject buttonObject = CreateTemporaryObject("M02_Install_Video2", page);
        bootstrapRoot.SetActive(false);
        page.SetActive(false);

        try
        {
            TexCore_ClickObjectStep reporter = PrepareInstallButton(buttonObject);

            TexCore_M02ServerInstallBootstrap bootstrap =
                bootstrapRoot.AddComponent<TexCore_M02ServerInstallBootstrap>();
            SetPrivateField(bootstrap, "uiRoot", uiRoot.transform);
            SetPrivateField(bootstrap, "installReporters",
                new List<TexCore_InteracterBase> { reporter });

            InvokeEnsureInstallTargetReady(bootstrap, 0);

            Assert.That(buttonObject.activeSelf, Is.True,
                "序列化为关闭的 Video2 目标按钮必须被唤醒。");
            Assert.That(page.activeSelf, Is.True,
                "唤醒目标按钮必须同时打开它所在的安装页面。");
            Assert.That(reporter.state, Is.EqualTo(FinishState.等待触发),
                "当前步骤的点击步骤必须回到等待触发。");
            Assert.That(buttonObject.GetComponent<PressableButton>().enabled, Is.True,
                "回到等待触发后目标按钮必须可交互。");

            reporter.SetState(FinishState.未完成);
            InvokeEnsureInstallTargetReady(bootstrap, 0);
            Assert.That(reporter.state, Is.EqualTo(FinishState.等待触发),
                "被其他逻辑禁用过的目标按钮必须能被重新检查并修复。");
        }
        finally
        {
            Object.DestroyImmediate(bootstrapRoot);
            Object.DestroyImmediate(uiRoot);
        }
    }

    [Test]
    public void ServerInstallTarget_LeavesTheSiblingButtonsOfTheSamePageUntouched()
    {
        GameObject bootstrapRoot = CreateTemporaryObject("M02 Bootstrap");
        GameObject uiRoot = CreateTemporaryObject("UI_XiTongJingXiangAnZhuang");
        GameObject page = CreateTemporaryObject("ChongQi", uiRoot);
        GameObject firstButton = CreateTemporaryObject("M02_Install_Restart", page);
        GameObject secondButton = CreateTemporaryObject("Empty Button (1)", page);
        bootstrapRoot.SetActive(false);
        page.SetActive(false);
        // The authored ChongQi page serializes both direct buttons inactive.
        firstButton.SetActive(false);
        secondButton.SetActive(false);

        try
        {
            TexCore_ClickObjectStep firstReporter = PrepareInstallButton(firstButton);
            TexCore_ClickObjectStep secondReporter = PrepareInstallButton(secondButton);

            TexCore_M02ServerInstallBootstrap bootstrap =
                bootstrapRoot.AddComponent<TexCore_M02ServerInstallBootstrap>();
            SetPrivateField(bootstrap, "uiRoot", uiRoot.transform);
            SetPrivateField(bootstrap, "installReporters",
                new List<TexCore_InteracterBase> { firstReporter });

            InvokeEnsureInstallTargetReady(bootstrap, 0);

            Assert.That(firstButton.activeSelf, Is.True);
            Assert.That(firstReporter.state, Is.EqualTo(FinishState.等待触发));
            Assert.That(secondButton.activeSelf, Is.False,
                "同一页面上的其他直接按钮必须保持原始状态。");
            Assert.That(secondReporter.state, Is.EqualTo(FinishState.未完成));
        }
        finally
        {
            Object.DestroyImmediate(bootstrapRoot);
            Object.DestroyImmediate(uiRoot);
        }
    }

    [Test]
    public void ServerInstallTarget_ReportsAMissingBindingOnlyOnce()
    {
        GameObject bootstrapRoot = CreateTemporaryObject("M02 Bootstrap");
        bootstrapRoot.SetActive(false);

        try
        {
            TexCore_M02ServerInstallBootstrap bootstrap =
                bootstrapRoot.AddComponent<TexCore_M02ServerInstallBootstrap>();

            Assert.DoesNotThrow(() => InvokeEnsureInstallTargetReady(bootstrap, 26),
                "缺少绑定只允许记录一次诊断，不允许抛出异常。");
            Assert.That(GetPrivateField(bootstrap, "warnedMissingBinding"), Is.EqualTo(true));
        }
        finally
        {
            Object.DestroyImmediate(bootstrapRoot);
        }
    }

    [Test]
    public void ServerInstallBinding_ResolvesTheSecondMiMaPageByOccurrence()
    {
        GameObject bootstrapRoot = CreateTemporaryObject("M02 Bootstrap");
        GameObject uiRoot = CreateTemporaryObject("UI_XiTongJingXiangAnZhuang");
        CreateTemporaryObject("KeyboardVariantPosition", uiRoot);
        GameObject firstMiMa = CreateTemporaryObject("MiMa", uiRoot);
        GameObject secondMiMa = CreateTemporaryObject("MiMa", uiRoot);
        bootstrapRoot.SetActive(false);

        try
        {
            TexCore_M02ServerInstallBootstrap bootstrap =
                bootstrapRoot.AddComponent<TexCore_M02ServerInstallBootstrap>();
            SetPrivateField(bootstrap, "uiRoot", uiRoot.transform);
            MethodInfo resolver = typeof(TexCore_M02ServerInstallBootstrap).GetMethod(
                "FindInstallPageTransform", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(resolver, Is.Not.Null);
            Assert.That(
                resolver.Invoke(bootstrap, new[]
                {
                    CreateInstallPage(25, "MiMa", 0, "M02_Install_FinalPassword", 1)
                }),
                Is.EqualTo(secondMiMa.transform),
                "第 25 步的 MiMa 必须解析到第二个密码页面。");
            Assert.That(
                resolver.Invoke(bootstrap, new[]
                {
                    CreateInstallPage(2, "MiMa", 0, "M02_Install_FinalPassword", 1)
                }),
                Is.EqualTo(secondMiMa.transform),
                "直接子节点索引命中也必须落在第二个密码页面。");
            Assert.That(
                resolver.Invoke(bootstrap, new[]
                {
                    CreateInstallPage(1, "MiMa", 0, "M02_Install_UserPassword", 0)
                }),
                Is.EqualTo(firstMiMa.transform));
        }
        finally
        {
            Object.DestroyImmediate(bootstrapRoot);
            Object.DestroyImmediate(uiRoot);
        }
    }

    [Test]
    public void IpApplyGuidance_IsGuidanceOnlyAndNeverCompletesTheStep()
    {
        GameObject buttonObject = CreateTemporaryObject("M02_IP_Apply");

        try
        {
            PressableButton button = buttonObject.AddComponent<PressableButton>();
            MethodInfo attach = typeof(TexCore_M02ServerInstallBootstrap).GetMethod(
                "AttachGuidance", BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(attach, Is.Not.Null);
            TexCore_ClickObjectStep guide = (TexCore_ClickObjectStep)attach.Invoke(
                null, new object[] { button });

            Assert.That(guide, Is.Not.Null, "IP 应用按钮必须带有指导组件。");
            Assert.That(guide.CompletesStep, Is.False,
                "IP 应用按钮的指导组件不允许完成步骤。");
            Assert.That(guide.ShowGuidance, Is.True);
            Assert.That(guide.InteractionSource, Is.EqualTo(buttonObject));
            Assert.That(buttonObject.GetComponents<TexCore_ClickObjectStep>().Length, Is.EqualTo(1),
                "IP 应用按钮只允许存在一个指导组件。");

            guide.SetState(FinishState.等待触发);
            try
            {
                button.OnClicked.Invoke();
            }
            catch (System.Exception)
            {
                // A stray completion listener would reach the TexCore business
                // manager here; the assertion below reports the real problem.
            }

            Assert.That(guide.WasCompletedByUser, Is.False,
                "指导组件不允许因为一次点击而推进步骤。");
        }
        finally
        {
            Object.DestroyImmediate(buttonObject);
        }
    }

    [Test]
    public void IpApplyCompletion_KeepsTheSingleCompletingReporter()
    {
        GameObject buttonObject = CreateTemporaryObject("M02_IP_Apply");

        try
        {
            PressableButton button = buttonObject.AddComponent<PressableButton>();
            TexCore_M02IpConfigurationStep ipStep =
                buttonObject.AddComponent<TexCore_M02IpConfigurationStep>();
            ipStep.Configure(null, null, null, button);
            TexCore_ClickObjectStep guide = (TexCore_ClickObjectStep)typeof(TexCore_M02ServerInstallBootstrap)
                .GetMethod("AttachGuidance", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { button });
            ipStep.SetGuidance(guide);

            TexCore_InteracterBase[] reporters =
                buttonObject.GetComponents<TexCore_InteracterBase>();
            Assert.That(reporters.Length, Is.EqualTo(2));
            Assert.That(
                reporters.Count(reporter => reporter is TexCore_ClickObjectStep step && step.CompletesStep),
                Is.EqualTo(0),
                "IP 应用按钮上不允许存在第二个完成监听器。");
            Assert.That(
                reporters.Count(reporter => reporter is TexCore_M02IpConfigurationStep),
                Is.EqualTo(1));

            ipStep.UnderWay();
            Assert.That(button.enabled, Is.True);
            Assert.That(guide.state, Is.EqualTo(FinishState.等待触发),
                "IP 步骤进入等待触发时指导高亮必须同时出现。");

            ipStep.Finish();
            Assert.That(guide.state, Is.EqualTo(FinishState.未完成),
                "IP 步骤结束后指导高亮必须关闭。");
            Assert.That(button.enabled, Is.False);
            Assert.That(ipStep.WasCompletedByUser, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(buttonObject);
        }
    }

    [Test]
    public void IpPageGuidance_ShowsOnlyTheCurrentNavigationPageOrTheApplyButton()
    {
        GameObject bootstrapRoot = CreateTemporaryObject("M02 Bootstrap");
        GameObject firstNav = CreateTemporaryObject("YouXian1 Empty Button");
        GameObject secondNav = CreateTemporaryObject("YouXian2 Empty Button");
        GameObject apply = CreateTemporaryObject("M02_IP_Apply");
        bootstrapRoot.SetActive(false);

        try
        {
            TexCore_ClickObjectStep firstGuide = AttachGuidanceFor(firstNav);
            TexCore_ClickObjectStep secondGuide = AttachGuidanceFor(secondNav);
            TexCore_ClickObjectStep applyGuide = AttachGuidanceFor(apply);
            TexCore_M02ServerInstallBootstrap bootstrap =
                bootstrapRoot.AddComponent<TexCore_M02ServerInstallBootstrap>();
            SetPrivateField(bootstrap, "ipPageGuidances", new List<List<TexCore_ClickObjectStep>>
            {
                new List<TexCore_ClickObjectStep> { firstGuide },
                new List<TexCore_ClickObjectStep> { secondGuide }
            });
            SetPrivateField(bootstrap, "ipGuidance", applyGuide);

            SetPrivateField(bootstrap, "ipPageIndex", 0);
            InvokeShowIpPageGuidance(bootstrap);
            Assert.That(firstGuide.state, Is.EqualTo(FinishState.等待触发));
            Assert.That(secondGuide.state, Is.EqualTo(FinishState.未完成));
            Assert.That(applyGuide.state, Is.EqualTo(FinishState.未完成),
                "应用按钮的高亮不允许停留在其他 IP 页面。");

            SetPrivateField(bootstrap, "ipPageIndex", 1);
            InvokeShowIpPageGuidance(bootstrap);
            Assert.That(secondGuide.state, Is.EqualTo(FinishState.等待触发));
            Assert.That(firstGuide.state, Is.EqualTo(FinishState.未完成));

            SetPrivateField(bootstrap, "ipPageIndex", 2);
            InvokeShowIpPageGuidance(bootstrap);
            Assert.That(firstGuide.state, Is.EqualTo(FinishState.未完成));
            Assert.That(secondGuide.state, Is.EqualTo(FinishState.未完成));
            Assert.That(applyGuide.state, Is.EqualTo(FinishState.等待触发),
                "最后一个 IP 页面必须高亮应用按钮。");
        }
        finally
        {
            Object.DestroyImmediate(bootstrapRoot);
            Object.DestroyImmediate(firstNav);
            Object.DestroyImmediate(secondNav);
            Object.DestroyImmediate(apply);
        }
    }

    [Test]
    public void ServerInstallBackpackEntry_OnlySummonsTheUsbDuringItsOwnStep()
    {
        GameObject bootstrapRoot = CreateTemporaryObject("M02 Bootstrap");
        GameObject nodeRoot = CreateTemporaryObject("M02 Node");
        GameObject topicRoot = CreateTemporaryObject("03系统安装操作", nodeRoot);
        GameObject usbRoot = CreateTemporaryObject("UPan");
        GameObject panelRoot = CreateTemporaryObject("02服务器系统安装背包面板");
        bootstrapRoot.SetActive(false);
        topicRoot.SetActive(false);
        usbRoot.SetActive(false);

        try
        {
            TexCore_M02ServerInstallBootstrap bootstrap =
                bootstrapRoot.AddComponent<TexCore_M02ServerInstallBootstrap>();
            QuietNodePanel node = nodeRoot.AddComponent<QuietNodePanel>();
            node.datas = new TexCore_DataClass { name = "02服务器系统安装" };

            PanelTopicBase topic = topicRoot.AddComponent<PanelTopicBase>();
            topic.datas = new TexCore_DataClass { name = "03系统安装操作" };
            node.activeChild = topic;

            SetPrivateField(bootstrap, "nodeRoot", nodeRoot.transform);
            SetPrivateField(bootstrap, "usb", usbRoot.transform);
            SetPrivateField(bootstrap, "usbBackpackPanel", panelRoot);
            SetPrivateField(bootstrap, "usbStep",
                usbRoot.AddComponent<TexCore_OrderedPlacementStep>());
            MethodInfo summon = typeof(TexCore_M02ServerInstallBootstrap).GetMethod(
                "SummonUsbFromBackpack", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(summon, Is.Not.Null);
            summon.Invoke(bootstrap, null);
            Assert.That(usbRoot.activeSelf, Is.False,
                "在其他步骤打开 02 背包面板并点击 U盘 时不允许召唤。");
            Assert.That(topic.stepIndex, Is.Zero, "点击 U盘 不允许推进步骤。");
            Assert.That(topic.haveScore, Is.Zero, "点击 U盘 不允许加分。");

            topic.datas.name = "02U盘启动盘插入";
            summon.Invoke(bootstrap, null);
            Assert.That(usbRoot.activeSelf, Is.True, "U盘步骤必须能正常召唤 U盘。");
            Assert.That(panelRoot.activeSelf, Is.False, "召唤成功后必须关闭 02 背包面板。");
            Assert.That(topic.stepIndex, Is.Zero, "召唤 U盘 本身不允许推进步骤。");
        }
        finally
        {
            Object.DestroyImmediate(bootstrapRoot);
            Object.DestroyImmediate(nodeRoot);
            Object.DestroyImmediate(usbRoot);
            Object.DestroyImmediate(panelRoot);
        }
    }

    private static TexCore_ClickObjectStep AttachGuidanceFor(GameObject buttonObject)
    {
        buttonObject.AddComponent<PressableButton>();
        MethodInfo attach = typeof(TexCore_M02ServerInstallBootstrap).GetMethod(
            "AttachGuidance", BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(attach, Is.Not.Null);
        return (TexCore_ClickObjectStep)attach.Invoke(null, new object[]
        {
            buttonObject.GetComponent<PressableButton>()
        });
    }

    private static void InvokeShowIpPageGuidance(TexCore_M02ServerInstallBootstrap bootstrap)
    {
        MethodInfo show = typeof(TexCore_M02ServerInstallBootstrap).GetMethod(
            "ShowIpPageGuidance", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(show, Is.Not.Null);
        show.Invoke(bootstrap, null);
    }

    private static TexCore_ClickObjectStep PrepareInstallButton(GameObject buttonObject)
    {
        buttonObject.AddComponent<PressableButton>();
        TexCore_ClickObjectStep reporter = buttonObject.AddComponent<TexCore_ClickObjectStep>();
        reporter.Configure(false, true, false, false);
        reporter.ConfigureSource(buttonObject);
        return reporter;
    }

    private static void InvokeEnsureInstallTargetReady(
        TexCore_M02ServerInstallBootstrap bootstrap, int stepIndex)
    {
        MethodInfo ensure = typeof(TexCore_M02ServerInstallBootstrap).GetMethod(
            "EnsureInstallTargetReady", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(ensure, Is.Not.Null);
        ensure.Invoke(bootstrap, new object[] { stepIndex });
    }

    private static object GetPrivateField(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        return field.GetValue(target);
    }

    private static object CreateInstallPage(
        int uiChildIndex, string pageName, int buttonIndex, string taskName, int nameOccurrence)
    {
        System.Type pageType = typeof(TexCore_M02ServerInstallBootstrap).GetNestedType(
            "InstallPage", BindingFlags.NonPublic);
        Assert.That(pageType, Is.Not.Null);
        return System.Activator.CreateInstance(pageType, new object[]
        {
            uiChildIndex, pageName, buttonIndex, taskName, nameOccurrence
        });
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }

    private static GameObject CreateTemporaryObject(string name, GameObject parent = null)
    {
        GameObject value = new GameObject(name)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        if (parent != null) value.transform.SetParent(parent.transform);
        return value;
    }

    private sealed class QuietNodePanel : NodePanelBase
    {
        public override void OnEnable()
        {
        }
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
