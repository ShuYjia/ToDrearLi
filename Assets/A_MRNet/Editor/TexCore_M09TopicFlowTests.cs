using System.Collections.Generic;
using System.Linq;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using MixedReality.Toolkit.UX;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TexCore_M09TopicFlowTests
{
    private const string MainlineScenePath = "Assets/A_MRNet/Sence/03SingleplayerScene.unity";
    private const string Scene0809Path = "Assets/A_MRNet/Sence/0809.unity";
    private const string Scene09Path = "Assets/A_MRNet/Sence/09.unity";
    private const string BusinessRoot =
        "Assets/Resources/BusinessPrefabs/MR混合现实多人协作平台/01货到人拣选电商仓储实施安装与调试实训系统";
    private const string KnowledgeTopicName = "01知识点答题_待出题";
    private sealed class M09NetworkCheckpoint
    {
        public readonly string sourcePath;
        public readonly int sourceSiblingIndex;
        public readonly bool isInput;

        public M09NetworkCheckpoint(string path, int siblingIndex, bool input)
        {
            sourcePath = path;
            sourceSiblingIndex = siblingIndex;
            isInput = input;
        }
    }

    private static readonly M09NetworkCheckpoint[] M09NetworkCheckpoints =
    {
        new M09NetworkCheckpoint("AGV1/EmptyButton", 0, false),
        new M09NetworkCheckpoint("AGV2/InputField(Legacy)", 0, true),
        new M09NetworkCheckpoint("AGV2/EmptyButton", 1, false),
        new M09NetworkCheckpoint("AGV3/yonghuming/InputField", 3, true),
        new M09NetworkCheckpoint("AGV3/mima/InputField", 3, true),
        new M09NetworkCheckpoint("AGV3/EmptyButton", 2, false),
        new M09NetworkCheckpoint("AGV4/EmptyButton", 0, false),
        new M09NetworkCheckpoint("AGV5/EmptyButton", 1, false),
        new M09NetworkCheckpoint("AGV6/EmptyButton", 2, false),
        new M09NetworkCheckpoint("AGV6/EmptyButton", 3, false),
        new M09NetworkCheckpoint("AGV7/EmptyButton", 2, false),
        new M09NetworkCheckpoint("AGV7/EmptyButton(1)", 3, false),
        new M09NetworkCheckpoint("AGV8/EmptyButton", 5, false),
        new M09NetworkCheckpoint("AGV9/EmptyButton", 1, false),
        new M09NetworkCheckpoint("AGV8/EmptyButton(1)", 6, false),
        new M09NetworkCheckpoint("AGV10/EmptyButton", 1, false),
        new M09NetworkCheckpoint("AGV8/EmptyButton(2)", 7, false),
        new M09NetworkCheckpoint("AGV11/EmptyButton", 1, false),
        new M09NetworkCheckpoint("AGV8/EmptyButton(3)", 8, false),
        new M09NetworkCheckpoint("AGV12/EmptyButton", 1, false),
        new M09NetworkCheckpoint("AGV8/EmptyButton(4)", 9, false),
        new M09NetworkCheckpoint("AGV14/EmptyButton", 2, false),
        new M09NetworkCheckpoint("AGV14/EmptyButton(1)", 3, false),
        new M09NetworkCheckpoint("AGV15/EmptyButton", 1, false),
        new M09NetworkCheckpoint("AGV16/EmptyButton", 2, false),
        new M09NetworkCheckpoint("AGV16/EmptyButton(1)", 3, false),
        new M09NetworkCheckpoint("AGV17/EmptyButton", 1, false),
        new M09NetworkCheckpoint("AGV18/EmptyButton", 3, false),
        new M09NetworkCheckpoint("AGV19/EmptyButton", 1, false),
        new M09NetworkCheckpoint("AGV18/EmptyButton(2)", 5, false),
        new M09NetworkCheckpoint("AGV20/EmptyButton", 1, false),
        new M09NetworkCheckpoint("AGV18/ESC", 4, false)
    };

    [TestCase(MainlineScenePath)]
    [TestCase(Scene0809Path)]
    public void M09NetworkTopic_UsesM09NetworkRoot_WithoutKnowledgeTopic(string scenePath)
    {
        Scene scene = SceneManager.GetSceneByPath(scenePath);
        bool closeSceneAfterTest = !scene.IsValid() || !scene.isLoaded;
        if (closeSceneAfterTest)
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        }

        try
        {
            TexCore_StepObj tree = FindComponentInScene<TexCore_StepObj>(scene);
            Assert.That(tree, Is.Not.Null);

            NodeObjs module = tree.interacterObjs.Single(value =>
                value.nodePrefab != null && value.nodePrefab.name == "09机器人入场");
            string[] topicNames = module.nodeObjs
                .Select(value => value.topicPrefab != null ? value.topicPrefab.name : string.Empty)
                .ToArray();
            CollectionAssert.AreEqual(new[]
            {
                "00介绍视频", "02从货箱取出机器人", "03检查机器人", "04AGV配网",
                "05机器人推至二维码", "06完成整个模块"
            }, topicNames);

            TopicObjs networkTopic = module.nodeObjs.Single(value =>
                value.topicPrefab != null && value.topicPrefab.name == "04AGV配网");
            Assert.That(networkTopic.topicObjs, Has.Count.EqualTo(1),
                "AGV 配网必须作为一个 30 分操作题，不能被拆成多个 TexCore 计分步骤。");
            StepObjs stepObject = networkTopic.topicObjs[0];
            Assert.That(stepObject.stepIndex, Is.Zero);
            Assert.That(stepObject.stepObjs, Has.Count.EqualTo(1));
            TexCore_PassiveUIEventStep networkStep = stepObject.stepObjs[0]
                .GetComponent<TexCore_PassiveUIEventStep>();
            Assert.That(networkStep, Is.Not.Null);

            Transform networkRoot = FindM09NetworkRoot(scene);
            Assert.That(networkStep.InteractionSource, Is.EqualTo(networkRoot.gameObject));
            Assert.That(networkStep.CheckpointCount, Is.EqualTo(M09NetworkCheckpoints.Length),
                "AGV 配网必须依次监听 29 个按钮和 3 个输入框，不能只完成首个入口。");
            Assert.That(stepObject.stepObjs[0].GetComponent<TexCore_StepObjectState>(), Is.Null,
                "配网步骤只能监听旧 UI，不能接管页面显隐。");

            var sources = new HashSet<GameObject>();
            for (int index = 0; index < M09NetworkCheckpoints.Length; index++)
            {
                M09NetworkCheckpoint expected = M09NetworkCheckpoints[index];
                GameObject source = networkStep.GetCheckpointSource(index);
                Assert.That(source, Is.Not.Null);
                Assert.That(NormalizeNetworkPath(GetRelativePath("AGV配网", source.transform)),
                    Is.EqualTo(expected.sourcePath));
                Assert.That(source.transform.GetSiblingIndex(), Is.EqualTo(expected.sourceSiblingIndex));
                Assert.That(networkStep.IsTextInputCheckpoint(index), Is.EqualTo(expected.isInput));
                if (expected.isInput)
                {
                    Assert.That(source.GetComponent<InputField>(), Is.Not.Null);
                }
                else
                {
                    Assert.That(source.GetComponent<PressableButton>(), Is.Not.Null);
                }

                Assert.That(sources.Add(source), Is.True,
                    "每个配网检查点都必须绑定到独立的原始 UI 控件。");
            }

            GameObject nodePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                BusinessRoot + "/09机器人入场.prefab");
            Aa_Node node = nodePrefab.GetComponent<Aa_Node>();
            Assert.That(node.datas.childData.Any(value =>
                value != null && value.name == KnowledgeTopicName), Is.False);
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(
                BusinessRoot + "/09机器人入场/" + KnowledgeTopicName + ".prefab"), Is.Null);
        }
        finally
        {
            if (closeSceneAfterTest)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    [TestCase(MainlineScenePath)]
    [TestCase(Scene0809Path)]
    public void M09NetworkTopic_RestoresEveryAuthoredPageTransition(string scenePath)
    {
        Scene scene = SceneManager.GetSceneByPath(scenePath);
        bool closeSceneAfterTest = !scene.IsValid() || !scene.isLoaded;
        if (closeSceneAfterTest)
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        }

        try
        {
            Transform networkRoot = FindM09NetworkRoot(scene);
            Transform agv8 = FindDirectChild(networkRoot, "AGV8", 0);
            PressableButton agv8FourthButton = FindDirectChild(agv8, "Empty Button (3)", 0)
                .GetComponent<PressableButton>();
            PressableButton agv8FifthButton = FindDirectChild(agv8, "Empty Button (4)", 0)
                .GetComponent<PressableButton>();
            PressableButton agv11Button = FindDirectChild(
                FindDirectChild(networkRoot, "AGV11", 0), "Empty Button", 0)
                .GetComponent<PressableButton>();
            PressableButton agv12Button = FindDirectChild(
                FindDirectChild(networkRoot, "AGV12", 0), "Empty Button", 0)
                .GetComponent<PressableButton>();

            Assert.That(agv11Button.OnClicked.GetPersistentEventCount(), Is.GreaterThan(1));
            Assert.That(agv11Button.OnClicked.GetPersistentTarget(1),
                Is.EqualTo(agv8FourthButton.gameObject));
            Assert.That(agv8FourthButton.OnClicked.GetPersistentEventCount(), Is.GreaterThan(1));
            Assert.That(agv8FourthButton.OnClicked.GetPersistentTarget(0),
                Is.EqualTo(agv8FourthButton.gameObject));
            Assert.That(agv8FourthButton.OnClicked.GetPersistentTarget(1),
                Is.EqualTo(FindDirectChild(networkRoot, "AGV12", 0).gameObject));
            Assert.That(agv12Button.OnClicked.GetPersistentEventCount(), Is.GreaterThan(1));
            Assert.That(agv12Button.OnClicked.GetPersistentTarget(1),
                Is.EqualTo(agv8FifthButton.gameObject));

            if (scenePath == MainlineScenePath)
            {
                // ESC 是配网步骤的最后一个检查点。它一旦顺手隐藏承载场景的
                // ChangJing-8，跳题后整个场景都会消失，所以这里锁死这条约束。
                Transform roomRoot = networkRoot.parent;
                Transform esc = FindDirectChild(
                    FindDirectChild(networkRoot, "AGV18", 0), "ESC", 0);
                PressableButton escButton = esc.GetComponent<PressableButton>();
                for (int index = 0; index < escButton.OnClicked.GetPersistentEventCount(); index++)
                {
                    Assert.That(escButton.OnClicked.GetPersistentTarget(index),
                        Is.Not.EqualTo(roomRoot.gameObject),
                        "AGV 配网的 ESC 不得隐藏承载场景的 ChangJing-8。");
                }

                TexCore_TopicAdvanceOnExitButton advance =
                    esc.GetComponent<TexCore_TopicAdvanceOnExitButton>();
                Assert.That(advance, Is.Not.Null,
                    "ESC 必须挂载 TexCore_TopicAdvanceOnExitButton 才能完成后自动跳题。");
                Assert.That(new SerializedObject(advance).FindProperty("requiredStep")
                        .objectReferenceValue, Is.Not.Null,
                    "ESC 的自动跳题必须绑定 04AGV配网 的计分步骤。");
            }
        }
        finally
        {
            if (closeSceneAfterTest)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    [TestCase(MainlineScenePath)]
    public void M09DrillSteps_AreWiredToTheNearestScrewController(string scenePath)
    {
        Scene scene = SceneManager.GetSceneByPath(scenePath);
        bool closeSceneAfterTest = !scene.IsValid() || !scene.isLoaded;
        if (closeSceneAfterTest)
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        }

        try
        {
            // 8 颗固定钉可以任意顺序拆，每步拆哪一颗由运行期的“离钻头最近”
            // 分配器决定；这里锁死它必须被接线，且吸附点名与固定钉名一一对应。
            TexCore_M09DrillNearestScrew controller =
                FindComponentInScene<TexCore_M09DrillNearestScrew>(scene);
            Assert.That(controller, Is.Not.Null,
                "09 拆钉必须挂载 TexCore_M09DrillNearestScrew。");

            SerializedObject serialized = new SerializedObject(controller);
            SerializedProperty steps = serialized.FindProperty("drillSteps");
            Assert.That(steps, Is.Not.Null);
            Assert.That(steps.arraySize, Is.EqualTo(8),
                "分配器必须接线 8 个拆钉步骤。");
            Assert.That(serialized.FindProperty("snapRoot").objectReferenceValue, Is.Not.Null);
            Transform screwRoot =
                serialized.FindProperty("screwRoot").objectReferenceValue as Transform;
            Assert.That(screwRoot, Is.Not.Null);

            var snapNames = new HashSet<string>();
            for (int index = 0; index < steps.arraySize; index++)
            {
                TexCore_OrderedPlacementStep step =
                    steps.GetArrayElementAtIndex(index).objectReferenceValue as
                        TexCore_OrderedPlacementStep;
                Assert.That(step, Is.Not.Null, "第 " + (index + 1) + " 个拆钉步骤没有接线。");
                Assert.That(step.Target, Is.Not.Null);
                Assert.That(screwRoot.Find(step.Target.name), Is.Not.Null,
                    "固定钉显隐依赖“吸附点与固定钉同名”的约定，找不到：" + step.Target.name);
                Assert.That(snapNames.Add(step.Target.name), Is.True,
                    "8 个拆钉步骤必须各绑定一个不同的吸附点：" + step.Target.name);
            }
        }
        finally
        {
            if (closeSceneAfterTest)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    [TestCase(MainlineScenePath)]
    [TestCase(Scene0809Path)]
    public void M09RobotMove_AllowsPicoControllerRayAndPreservesOriginalPhysics(string scenePath)
    {
        Scene scene = SceneManager.GetSceneByPath(scenePath);
        bool closeSceneAfterTest = !scene.IsValid() || !scene.isLoaded;
        if (closeSceneAfterTest)
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        }

        try
        {
            TexCore_StepObj tree = FindComponentInScene<TexCore_StepObj>(scene);
            NodeObjs module = tree.interacterObjs.Single(value =>
                value.nodePrefab != null && value.nodePrefab.name == "09机器人入场");
            TopicObjs moveTopic = module.nodeObjs.Single(value =>
                value.topicPrefab != null && value.topicPrefab.name == "05机器人推至二维码");
            TexCore_OrderedPlacementStep step = moveTopic.topicObjs.Single().stepObjs.Single()
                .GetComponent<TexCore_OrderedPlacementStep>();

            Assert.That(step, Is.Not.Null);
            Assert.That(GetHierarchyPath(step.InteractionSource.transform),
                Does.EndWith("/ChangJing-8/AGV/AGV1"));

            ObjectManipulator manipulator = step.InteractionSource.GetComponent<ObjectManipulator>();
            Rigidbody body = step.InteractionSource.GetComponent<Rigidbody>();
            Assert.That(manipulator, Is.Not.Null);
            Assert.That(body, Is.Not.Null);
            Assert.That(manipulator.AllowedInteractionTypes.HasFlag(InteractionFlags.Ray), Is.True,
                "09 机器人必须允许 PICO 手柄射线选中并移动。");
            Assert.That(manipulator.AllowedInteractionTypes.HasFlag(InteractionFlags.Generic), Is.True,
                "非 MRTK 标准 PICO 控制器也必须能选中 09 机器人。");
            Assert.That(body.isKinematic, Is.True,
                "09 机器人必须保留 ChangJing-8 的运动学刚体设置，不能因手柄交互飞起。");
            Assert.That(body.useGravity, Is.False);
            Assert.That(body.constraints & RigidbodyConstraints.FreezeRotation,
                Is.EqualTo(RigidbodyConstraints.FreezeRotation),
                "VelocityTracking 在抓取期间会把刚体切成非运动学，三个旋转轴必须锁死，" +
                "否则与货箱/地面的接触力矩会让机器人一边移动一边打转。");
            Assert.That(body.interpolation, Is.EqualTo(RigidbodyInterpolation.Interpolate));
            Assert.That(body.collisionDetectionMode, Is.EqualTo(CollisionDetectionMode.Continuous));
            Assert.That((int)manipulator.RigidbodyMovementType, Is.Zero,
                "09 机器人必须保留原场景的 VelocityTracking 操控方式。");
            Assert.That(manipulator.AllowedManipulations, Is.EqualTo(TransformFlags.Move),
                "09 机器人必须保留原场景仅允许平移的操控设置。");
            TexCore_M09RobotTranslationOnly translationOnly =
                step.InteractionSource.GetComponent<TexCore_M09RobotTranslationOnly>();
            Assert.That(translationOnly, Is.Not.Null,
                "09 机器人必须挂载 TexCore_M09RobotTranslationOnly，运行期持续保证只平移不旋转。");
            Assert.That(new SerializedObject(translationOnly).FindProperty("body")
                    .objectReferenceValue, Is.EqualTo(body),
                "只平移策略必须绑定机器人自己的刚体。");

            TopicObjs unpackTopic = module.nodeObjs.Single(value =>
                value.topicPrefab != null && value.topicPrefab.name == "02从货箱取出机器人");
            TexCore_OrderedPlacementStep pushOutStep = unpackTopic.topicObjs.Last().stepObjs.Single()
                .GetComponent<TexCore_OrderedPlacementStep>();
            AssertPlacementDoesNotResetWhileJumping(pushOutStep);
            AssertPlacementDoesNotResetWhileJumping(step);
        }
        finally
        {
            if (closeSceneAfterTest)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    [TestCase(MainlineScenePath)]
    [TestCase(Scene0809Path)]
    [TestCase(Scene09Path)]
    public void M09RobotMove_TargetsTheSinglePlacedQrDecal(string scenePath)
    {
        Scene scene = SceneManager.GetSceneByPath(scenePath);
        bool closeSceneAfterTest = !scene.IsValid() || !scene.isLoaded;
        if (closeSceneAfterTest)
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        }

        try
        {
            TexCore_StepObj tree = FindComponentInScene<TexCore_StepObj>(scene);
            NodeObjs module = tree.interacterObjs.Single(value =>
                value.nodePrefab != null && value.nodePrefab.name == "09机器人入场");
            TopicObjs moveTopic = module.nodeObjs.Single(value =>
                value.topicPrefab != null && value.topicPrefab.name == "05机器人推至二维码");
            TexCore_OrderedPlacementStep step = moveTopic.topicObjs.Single().stepObjs.Single()
                .GetComponent<TexCore_OrderedPlacementStep>();

            Assert.That(step.Target, Is.Not.Null);
            Assert.That(GetHierarchyPath(step.Target),
                Does.EndWith("/ChangJing-8/MR_ChangJing/ErWeiMa/ErWeiMa16"),
                "09 二维码步骤必须对准地面网格中唯一可放置的二维码贴片，而不是 123 张贴片共用的网格容器。");
            Assert.That(step.Target.GetComponent<Collider>(), Is.Not.Null,
                "目标二维码贴片必须保留原始触发器碰撞体，才能被识别为可放置位置。");
            Assert.That(step.IgnoreVerticalForTolerance, Is.True,
                "二维码贴片位于地面，机器人原点悬在其上方，必须只比较水平距离。");

            Transform parkingSpot = FindByHierarchySuffix(scene, "/ChangJing-8/AGV/AGV1吸附位置");
            float pushDistance = Vector2.Distance(
                new Vector2(parkingSpot.position.x, parkingSpot.position.z),
                new Vector2(step.Target.position.x, step.Target.position.z));
            Assert.That(pushDistance, Is.GreaterThan(step.PositionTolerance),
                "机器人推出货箱后必须仍要真正推动一段距离才能到达二维码。");
            Assert.That(pushDistance, Is.LessThan(2f),
                "二维码必须在机器人可推动的范围内。");
        }
        finally
        {
            if (closeSceneAfterTest)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    [TestCase(MainlineScenePath)]
    [TestCase(Scene0809Path)]
    [TestCase(Scene09Path)]
    public void M09BoxFasteners_StayVisibleUntilTheirDrillStep(string scenePath)
    {
        Scene scene = SceneManager.GetSceneByPath(scenePath);
        bool closeSceneAfterTest = !scene.IsValid() || !scene.isLoaded;
        if (closeSceneAfterTest)
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        }

        try
        {
            TexCore_StepObj tree = FindComponentInScene<TexCore_StepObj>(scene);
            NodeObjs module = tree.interacterObjs.Single(value =>
                value.nodePrefab != null && value.nodePrefab.name == "09机器人入场");
            TopicObjs unpackTopic = module.nodeObjs.Single(value =>
                value.topicPrefab != null && value.topicPrefab.name == "02从货箱取出机器人");

            string[] fastenerNames =
            {
                "ZiGongSi", "ZiGongSi3", "ZiGongSi5", "ZiGongSi7",
                "ZiGongSi9", "ZiGongSi11", "ZiGongSi13", "ZiGongSi15"
            };
            Transform fastenerRoot = FindByHierarchySuffix(scene, "/ChangJing-8/AGV/XiangZi/ZiGongSi");
            Assert.That(fastenerRoot.childCount, Is.EqualTo(fastenerNames.Length),
                "货箱固定钉必须与 8 个拆卸步骤一一对应。");

            for (int index = 0; index < fastenerNames.Length; index++)
            {
                Transform fastener = fastenerRoot.Find(fastenerNames[index]);
                Assert.That(fastener, Is.Not.Null, "找不到固定钉：" + fastenerNames[index]);
                Assert.That(fastener.gameObject.activeSelf, Is.True,
                    "固定钉 " + fastenerNames[index] + " 在拆卸前必须显示。");

                Renderer renderer = fastener.GetComponent<Renderer>();
                Assert.That(renderer, Is.Not.Null);
                Assert.That(renderer.enabled, Is.True,
                    "固定钉 " + fastenerNames[index] +
                    " 的模型必须已启用；步骤状态只用 SetActive 显隐，无法打开被关闭的 Renderer。");

                StepObjs stepObject = unpackTopic.topicObjs.Single(value => value.stepIndex == index);
                TexCore_StepObjectState state = stepObject.stepObjs.Single()
                    .GetComponent<TexCore_StepObjectState>();
                Assert.That(state, Is.Not.Null);
                Assert.That(SerializedObjectList(state, "showWhenUnfinished"),
                    Contains.Item(fastener.gameObject),
                    "轮到之前的固定钉必须保持显示。");
                Assert.That(SerializedObjectList(state, "hideWhenFinished"),
                    Contains.Item(fastener.gameObject),
                    "手电钻拆掉之后该固定钉必须消失。");
            }
        }
        finally
        {
            if (closeSceneAfterTest)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static List<GameObject> SerializedObjectList(Component component, string propertyName)
    {
        SerializedObject serialized = new SerializedObject(component);
        SerializedProperty property = serialized.FindProperty(propertyName);
        Assert.That(property, Is.Not.Null, "找不到序列化字段：" + propertyName);
        var values = new List<GameObject>();
        for (int index = 0; index < property.arraySize; index++)
        {
            values.Add(property.GetArrayElementAtIndex(index).objectReferenceValue as GameObject);
        }

        return values;
    }

    private static T FindComponentInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T value = root.GetComponentInChildren<T>(true);
            if (value != null)
            {
                return value;
            }
        }

        return null;
    }

    private static Transform FindByHierarchySuffix(Scene scene, string suffix)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform value = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(current =>
                GetHierarchyPath(current).EndsWith(suffix));
            if (value != null)
            {
                return value;
            }
        }

        Assert.Fail("场景缺少对象：" + suffix);
        return null;
    }

    private static void AssertPlacementDoesNotResetWhileJumping(
        TexCore_OrderedPlacementStep step)
    {
        Assert.That(step, Is.Not.Null);
        SerializedObject serializedStep = new SerializedObject(step);
        SerializedProperty resetWhenInactive = serializedStep.FindProperty("resetWhenInactive");
        Assert.That(resetWhenInactive, Is.Not.Null);
        Assert.That(resetWhenInactive.boolValue, Is.False,
            "09 共享机器人在跳转步骤时不得自动回到初始位置。");
        SerializedProperty snapOnFrameworkCompletion = serializedStep.FindProperty(
            "snapOnFrameworkCompletion");
        Assert.That(snapOnFrameworkCompletion, Is.Not.Null);
        Assert.That(snapOnFrameworkCompletion.boolValue, Is.False,
            "09 共享机器人在跳转到后续步骤时不得吸附到其他位置。");
        SerializedProperty restrictManipulationsToMove = serializedStep.FindProperty(
            "restrictManipulationsToMove");
        Assert.That(restrictManipulationsToMove, Is.Not.Null);
        Assert.That(restrictManipulationsToMove.boolValue, Is.False,
            "09 机器人必须保持场景原有的操控设置。");
    }

    private static string GetRelativePath(string ancestorName, Transform value)
    {
        List<string> names = new List<string>();
        bool foundAncestor = false;
        for (Transform current = value; current != null; current = current.parent)
        {
            if (current.name == ancestorName)
            {
                foundAncestor = true;
                break;
            }

            names.Add(current.name);
        }

        Assert.That(foundAncestor, Is.True,
            "配网控件必须位于 ChangJing-8/AGV配网 原始界面下。");
        names.Reverse();
        return string.Join("/", names);
    }

    private static string NormalizeNetworkPath(string value)
    {
        return (value ?? string.Empty).Replace(" ", string.Empty).Replace("　", string.Empty);
    }

    private static Transform FindM09NetworkRoot(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform value = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(current =>
                GetHierarchyPath(current).EndsWith("/ChangJing-8/AGV配网"));
            if (value != null)
            {
                return value;
            }
        }

        Assert.Fail("场景缺少 ChangJing-8/AGV配网 原始界面。");
        return null;
    }

    private static Transform FindDirectChild(Transform parent, string name, int occurrence)
    {
        int matches = 0;
        for (int index = 0; parent != null && index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (!NormalizeNetworkPath(child.name).Equals(NormalizeNetworkPath(name)))
            {
                continue;
            }

            if (matches == occurrence)
            {
                return child;
            }

            matches++;
        }

        Assert.Fail("找不到 AGV 配网子控件：" + name + "，序号：" + occurrence);
        return null;
    }

    private static string GetHierarchyPath(Transform value)
    {
        List<string> names = new List<string>();
        for (Transform current = value; current != null; current = current.parent)
        {
            names.Add(current.name);
        }

        names.Reverse();
        return string.Join("/", names);
    }
}
