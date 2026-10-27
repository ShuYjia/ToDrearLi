using Bpf.Diagnostics;
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using MixedReality.Toolkit.UX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Temporary M11 "流程测试" builder for 03SingleplayerScene.
/// </summary>
/// <remarks>
/// 11 关卡这次只做流程串联验证：三题按固定顺序复用 09-03、09-05、10-02 的
/// 操作逻辑、提示、任务标识与每题 10 分，题目 Prefab 已经从来源目录复制到
/// <c>11流程测试/</c>，本构建器只负责把它们接到 11 自己的场景对象上。
///
/// 关键约束：<see cref="NodePanelBase"/> 在节点启用时会把
/// <c>有交互物体</c>/<c>无交互物体</c> 下非当前模块的根节点全部关掉，所以 11
/// 的步骤不能直接引用 09/10 模块的场景对象（那些对象在 11 里是未激活的）。
/// 第一版按来源对象闭包独立复制：09 只复制机器人 <c>AGV</c> 加二维码目标贴片，
/// 10 只复制机器人 <c>AGV1</c>、货架目标点、生成点标记和入口二维码标记，再复用
/// 现有的 <c>TexCore_M10RackCarrier</c> 货架 Prefab 与 10 货架背包控制器。
/// 09 与 10 各自保留一台机器人，不合并为同一对象。
///
/// 临时口径：分值沿用来源题目（每题 10 分），不代表正式 M11 积分方案，
/// <c>docs/texcore-module-score-data.xlsx</c> 不改动。
/// </remarks>
internal static class TexCore_11SceneBuilder
{
    private const string ScenePath = "Assets/A_MRNet/Sence/03SingleplayerScene.unity";
    private const string BusinessRoot =
        "Assets/Resources/BusinessPrefabs/MR混合现实多人协作平台/01货到人拣选电商仓储实施安装与调试实训系统";
    private const string SystemName = "01货到人拣选电商仓储实施安装与调试实训系统";
    private const string NodeName = "11流程测试";
    private const string TopicCheckRobot = "01检查机器人";
    private const string TopicPushRobotToQr = "02机器人推至二维码";
    private const string TopicRackCarry = "03机器人搬运至货架点";
    private const string StepsContainerName = "TexCore_11Steps";
    private const string SourcesContainerName = "11来源对象";
    private const string RackPrefabPath = "Assets/A_MRNet/Models/TexCore_M10RackCarrier.prefab";
    private const string EntryQrName = "11_货架入场二维码";
    private const string SpawnPointName = "11_货架生成点";

    private const string CheckLeftTask = "09_11_检查左碰撞检测器";
    private const string CheckRightTask = "09_12_检查右碰撞检测器";
    private const string PowerButtonTask = "09_13_按下机器人开机键";
    private const string PushRobotTask = "09_15_机器人移动至二维码";
    private const string RackCarryTask = "10_机器人搬运到指定货架点";

    private const float PushRobotTolerance = 0.20f;
    private const float RackCarryTolerance = 0.24f;

    /// <summary>
    /// 与 10 构建器相同的生成点修正：legacy 标记 <c>colliderxs</c> 落在 AGV1
    /// 碰撞体内部且比目标点高 0.31 m，直接使用会让货架一放下就吸附到机器人。
    /// </summary>
    private static readonly Vector3 SpawnPointOffset = new Vector3(0f, -0.31f, 0.59f);

    [MenuItem("BPF/TexCore/构建 11 流程测试")]
    private static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            throw new InvalidOperationException("找不到 03SingleplayerScene.unity。");
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Transform interactive11 = FindPath("有交互物体", SystemName, NodeName);
        Transform legacy11 = FindPath("无交互物体", SystemName, NodeName);
        Transform legacy09 = FindPath("无交互物体", SystemName, "09机器人入场", "ChangJing-8");
        Transform legacy10 = FindPath("无交互物体", SystemName, "10货架入场", "changjing-4");

        ClearGenerated(interactive11, StepsContainerName);
        ClearGenerated(legacy11, SourcesContainerName);

        Transform steps = CreateContainer(interactive11, StepsContainerName);
        Transform sources = CreateContainer(legacy11, SourcesContainerName);

        // 09-03 与 09-05 针对同一台 09 机器人：先检查它的碰撞检测器与开机键，
        // 再把它推到二维码上，所以两个题目共用同一个复制体。
        GameObject robot09 = Duplicate(FindPath(legacy09, "AGV"), sources, "09机器人");
        ApplyHorizontalMoveOnly(FindPath(robot09.transform, "AGV1").gameObject, "09机器人");
        // 题目 03 一开始就收起 09 机器人：这一题交棒给 10 机器人，两台机器人不该同场。
        sources.gameObject.AddComponent<TexCore_TopicHideObjects>()
            .Configure(TopicRackCarry, new[] { robot09 });

        List<GameObject> checkSteps = BuildCheckRobot(steps, robot09);
        List<GameObject> pushSteps = BuildPushRobotToQr(steps, sources, legacy09, robot09);
        GameObject revealReporter;
        List<GameObject> rackSteps = BuildRackCarry(steps, sources, legacy10, out revealReporter);

        TopicObjs rackTopic = BuildTopic(TopicRackCarry, rackSteps);
        // 10 机器人等 09 机器人推到二维码之后才出现：把可见性控制挂在题目 03 的
        // 同一个 stepIndex 上，题目 01/02 期间该步骤属于“之后的步骤”，会按
        // hideWhenFinished 把 10 机器人藏起来。
        rackTopic.topicObjs[0].stepObjs.Add(revealReporter);

        ConfigureStepTree(new[]
        {
            BuildTopic(TopicCheckRobot, checkSteps),
            BuildTopic(TopicPushRobotToQr, pushSteps),
            rackTopic
        });

        AllowInHeadMenu();

        // 11 重新回到可进入范围，两个对象树的根节点同步启用（撤销 9421c8aa 的
        // 软下线）。运行期仍由 NodePanelBase 按当前节点重新计算激活状态。
        interactive11.gameObject.SetActive(true);
        legacy11.gameObject.SetActive(true);
        EditorUtility.SetDirty(interactive11);
        EditorUtility.SetDirty(legacy11);

        RegenerateConfig();

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        BpfLog.Info(BpfLogCategory.Diagnostics, "tex_core_11_scene_builder.build",
            "TexCore M11 临时流程测试构建完成：3 题，每题 10 分。");
    }

    /// <summary>
    /// 09-03：左/右碰撞检测器与机器人开机键三个点击步骤，来源对象是 09 机器人
    /// <c>AGV1</c> 上原有的 MRTK 按钮，复制后组件与回调保持不变。
    /// </summary>
    private static List<GameObject> BuildCheckRobot(Transform steps, GameObject agv)
    {
        List<GameObject> topicSteps = new List<GameObject>();
        AddClick(topicSteps, steps, CheckLeftTask,
            FindPath(agv.transform, "AGV1", "AGV2", "Canvas", "Action Button").gameObject);
        AddClick(topicSteps, steps, CheckRightTask,
            FindPath(agv.transform, "AGV1", "AGV9", "Canvas", "Action Button").gameObject);
        AddClick(topicSteps, steps, PowerButtonTask,
            FindPath(agv.transform, "AGV1", "KaiGuan", "Canvas", "Action Button").gameObject);
        return topicSteps;
    }

    /// <summary>
    /// 09-05：把同一台 09 机器人推到二维码贴片上。位姿保护、水平距离容差与
    /// 抓取设置沿用 09 构建器的取值。
    /// </summary>
    private static List<GameObject> BuildPushRobotToQr(Transform steps, Transform sources,
        Transform legacy09, GameObject agv)
    {
        GameObject qrTarget = Duplicate(FindRobotQrTarget(FindPath(legacy09, "MR_ChangJing", "ErWeiMa")),
            sources, "09二维码目标");

        List<GameObject> topicSteps = new List<GameObject>();
        TexCore_OrderedPlacementStep step = AddPlacement(topicSteps, steps, PushRobotTask,
            FindPath(agv.transform, "AGV1").gameObject, qrTarget.gameObject, PushRobotTolerance);
        step.ConfigurePreserveSourcePoseAcrossStateChanges();
        // 二维码贴片贴在地面上，机器人原点悬在其上方，只允许用水平距离判定。
        step.IgnoreVerticalForTolerance = true;
        return topicSteps;
    }

    /// <summary>
    /// 10-02：复用 10 的货架 Prefab、生成点、入口二维码、货架目标点与搬运
    /// 工作流。机器人是 10 自己的 AGV1，与 09 的机器人保持独立，并且要到
    /// 题目 03 开始才出现。
    /// </summary>
    private static List<GameObject> BuildRackCarry(Transform steps, Transform sources,
        Transform legacy10, out GameObject revealReporter)
    {
        GameObject robot = Duplicate(FindPath(legacy10, "MR_ChangJing", "AGV1"), sources,
            "10机器人");
        GameObject targets = Duplicate(FindPath(legacy10, "huojia collision"), sources,
            "10货架目标点");
        GameObject spawnMarker = Duplicate(FindPath(legacy10, "colliderxs"), sources,
            "10货架生成点标记");
        // 货架目标点本身只是不可见的碰撞体，地面对应的二维码来自 10 的地面二维码
        // 网格；没有这层网格，11 的每个货架点就没有自己的二维码。
        Transform qrGrid = Duplicate(FindPath(legacy10, "MR_ChangJing", "ErWeiMa"), sources,
            "10货架二维码").transform;
        Transform entryQrAnchor = FindPath(qrGrid, "ErWeiMa7");

        List<GameObject> targetPoints = targets.GetComponentsInChildren<Transform>(true)
            .Where(value => value.gameObject != targets && value.CompareTag("huojia"))
            .OrderBy(value => NumericName(value.name))
            .ThenBy(value => value.name, StringComparer.Ordinal)
            .Select(value => value.gameObject)
            .ToList();
        if (targetPoints.Count == 0)
        {
            throw new InvalidOperationException("11 搬运工作流缺少 huojia 目标点。");
        }

        ConfigureM10Robot(robot);
        ApplyHorizontalMoveOnly(robot, "10机器人");
        Transform spawnPoint = CreateSpawnPoint(steps, spawnMarker.transform);
        GameObject rack = CreateRack(steps, spawnPoint);
        ApplyHorizontalMoveOnly(rack, "货架");
        TexCore_RackEntryWorkflowStep workflow = rack.AddComponent<TexCore_RackEntryWorkflowStep>();
        workflow.Configure(robot, targetPoints, spawnPoint, RackCarryTolerance);
        ConfigureEntryQr(steps, entryQrAnchor, workflow);

        revealReporter = CreateRevealReporter(steps, robot, rack);
        // 起始隐藏，由题目 03 的步骤状态在真正开始时打开。
        robot.SetActive(false);
        EditorUtility.SetDirty(robot);

        return new List<GameObject> { rack };
    }

    /// <summary>
    /// 机器人/货架只允许在水平面内平移：不允许旋转，也不允许上下平移。
    /// </summary>
    /// <remarks>
    /// MRTK 的 ObjectManipulator 在抓取期间会把刚体切成非运动学并走
    /// MovePosition/MoveRotation，这些调用同样会被刚体约束过滤，所以除了收掉
    /// 操控标志，还必须用刚体约束锁死 Y 轴平移与全部旋转轴。
    /// </remarks>
    private static void ApplyHorizontalMoveOnly(GameObject target, string description)
    {
        ObjectManipulator manipulator = target != null
            ? target.GetComponent<ObjectManipulator>()
            : null;
        Rigidbody body = target != null ? target.GetComponent<Rigidbody>() : null;
        if (manipulator == null || body == null)
        {
            throw new InvalidOperationException(
                "11 的" + description + "缺少操控器或刚体，无法限制为水平平移。");
        }

        manipulator.AllowedManipulations = TransformFlags.Move;
        body.useGravity = false;
        body.isKinematic = true;
        body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        EditorUtility.SetDirty(manipulator);
        EditorUtility.SetDirty(body);
    }

    /// <summary>
    /// 题目 03 的显隐控制：10 机器人要到本题目真正开始才出现，货架在题目 01/02
    /// 期间也不该站在生成点上。
    /// </summary>
    /// <remarks>
    /// 货架在题目 03 里完全交给搬运工作流与背包：工作流的 <c>UnFinished</c> 会把
    /// 货架复位到生成点（SetActive(true)），所以它会在 01/02 期间一直可见，进 03
    /// 时又被背包收走，看起来像“突然消失”。这里在“之后的步骤”阶段把它一起藏起来，
    /// 于是它从一开始就等同于背包里待取的第一台货架；题目 03 开始后本汇报对象不再
    /// 触碰货架，取货架完全由背包按钮触发。
    /// 本对象必须排在货架之后（stepObjs 顺序），否则会被货架自己的复位重新打开。
    /// 完成后（已完成）也不再改动任何对象，避免结算瞬间东西突然消失。
    /// </remarks>
    private static GameObject CreateRevealReporter(Transform container, GameObject robot,
        GameObject rack)
    {
        GameObject reporter = CreateReporter(container, "11_题目03显隐控制");
        reporter.AddComponent<TexCore_StepObjectState>().Configure(
            new[] { robot },
            null,
            null,
            null,
            null,
            new[] { robot, rack });
        return reporter;
    }

    /// <summary>
    /// 复制一个来源对象闭包。使用世界坐标保持，复制体与来源在同一位置外观一致。
    /// </summary>
    private static GameObject Duplicate(Transform source, Transform parent, string name)
    {
        if (source == null)
        {
            throw new InvalidOperationException("11 来源对象为空：" + name);
        }

        GameObject copy = UnityEngine.Object.Instantiate(source.gameObject, parent, true);
        copy.name = name;
        return copy;
    }

    /// <summary>
    /// 09 机器人必须推到的唯一二维码贴片。二维码网格容器本身的原点落在四张贴片
    /// 之间，站上去也永远满足不了容差，所以只能取带碰撞体的那一张。
    /// </summary>
    private static Transform FindRobotQrTarget(Transform qrGrid)
    {
        Transform marked = qrGrid.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(value => value != qrGrid && value.GetComponent<Collider>() != null);
        if (marked == null)
        {
            throw new InvalidOperationException("09 机器人二维码网格缺少带碰撞体的二维码贴片。");
        }

        return marked;
    }

    private static void AddClick(List<GameObject> topicSteps, Transform container, string name,
        GameObject source)
    {
        EnsureClickable(source);
        GameObject reporter = CreateReporter(container, name);
        TexCore_ClickObjectStep step = reporter.AddComponent<TexCore_ClickObjectStep>();
        step.Configure(false, true, false, true);
        step.ConfigureSource(source);
        ConfigureVisibility(reporter, source, null, false);
        topicSteps.Add(reporter);
    }

    private static TexCore_OrderedPlacementStep AddPlacement(List<GameObject> topicSteps,
        Transform container, string name, GameObject source, GameObject target, float tolerance)
    {
        ConfigureM09RobotControllerPush(source);
        GameObject reporter = CreateReporter(container, name);
        TexCore_OrderedPlacementStep step = reporter.AddComponent<TexCore_OrderedPlacementStep>();
        step.ConfigureSource(source);
        step.Target = target != null ? target.transform : null;
        step.PositionTolerance = tolerance;
        step.RotationTolerance = 180f;
        ConfigureVisibility(reporter, source, target, false);
        topicSteps.Add(reporter);
        return step;
    }

    /// <summary>
    /// 步骤可见性状态与 09 构建器一致：source/target 在步骤进行时显示，未进入本
    /// 步骤时隐藏 target，完成后两者都保留。
    /// </summary>
    private static void ConfigureVisibility(GameObject reporter, GameObject source,
        GameObject target, bool keepFinished)
    {
        List<GameObject> active = new List<GameObject> { source };
        if (target != null && target != source)
        {
            active.Add(target);
        }

        List<GameObject> finished = keepFinished ? new List<GameObject>(active) : new List<GameObject>();
        List<GameObject> unfinishedHides = new List<GameObject>(active);
        unfinishedHides.Remove(source);
        reporter.AddComponent<TexCore_StepObjectState>()
            .Configure(active, null, finished, null, null, unfinishedHides);
    }

    private static void EnsureClickable(GameObject source)
    {
        if (source == null)
        {
            throw new InvalidOperationException("11 点击交互源为空。");
        }

        if (source.GetComponentsInChildren<Collider>(true).Length == 0)
        {
            source.AddComponent<BoxCollider>();
        }

        if (source.GetComponentInChildren<PressableButton>(true) == null)
        {
            source.AddComponent<PressableButton>();
        }
    }

    /// <summary>
    /// 09 机器人的刚体/操控器参数是在 09 步骤构建时落到场景对象上的，复制体已经
    /// 带着同样的设置；这里只做校验，避免复制链断了却悄悄退化。
    /// </summary>
    private static void ConfigureM09RobotControllerPush(GameObject robot)
    {
        if (robot == null || robot.GetComponent<ObjectManipulator>() == null ||
            robot.GetComponent<Rigidbody>() == null || robot.GetComponent<Collider>() == null)
        {
            throw new InvalidOperationException("11 的 09 机器人复制体缺少刚体、碰撞体或操控器组件。");
        }
    }

    private static void ConfigureM10Robot(GameObject robot)
    {
        if (robot == null)
        {
            throw new InvalidOperationException("11 搬运工作流的机器人为空。");
        }

        Rigidbody body = robot.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = robot.AddComponent<Rigidbody>();
        }

        body.useGravity = false;
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        ObjectManipulator manipulator = robot.GetComponent<ObjectManipulator>();
        if (manipulator == null)
        {
            manipulator = robot.AddComponent<ObjectManipulator>();
        }

        // 平移/旋转权限统一由 ApplyHorizontalMoveOnly 设定，这里只管驱动方式。
        manipulator.RigidbodyMovementType = XRBaseInteractable.MovementType.VelocityTracking;
        manipulator.AllowedInteractionTypes |=
            InteractionFlags.Near | InteractionFlags.Ray | InteractionFlags.Generic;

        Collider robotCollider = robot.GetComponent<Collider>();
        if (robotCollider != null && !manipulator.colliders.Contains(robotCollider))
        {
            manipulator.colliders.Add(robotCollider);
        }
    }

    private static Transform CreateSpawnPoint(Transform container, Transform source)
    {
        GameObject point = new GameObject(SpawnPointName);
        point.transform.SetParent(container, true);
        point.transform.SetPositionAndRotation(source.position + SpawnPointOffset, source.rotation);
        point.SetActive(true);
        return point.transform;
    }

    private static GameObject CreateRack(Transform container, Transform spawnPoint)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RackPrefabPath);
        if (prefab == null)
        {
            throw new InvalidOperationException("找不到货架模型 Prefab：" + RackPrefabPath);
        }

        GameObject rack = PrefabUtility.InstantiatePrefab(prefab, container) as GameObject;
        if (rack == null)
        {
            throw new InvalidOperationException("无法实例化货架模型。");
        }

        rack.name = RackCarryTask;
        rack.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        rack.SetActive(true);
        return rack;
    }

    /// <summary>
    /// 入口二维码只补一个判定用的触发区域：可见的二维码由 10 的地面二维码网格
    /// 提供，这里不再复制一份贴片网格，避免和网格里的同一张贴片重叠。
    /// 玩家要先把货架放到二维码上机器人才会接驳。
    /// </summary>
    private static void ConfigureEntryQr(Transform container, Transform anchor,
        TexCore_RackEntryWorkflowStep workflow)
    {
        GameObject root = new GameObject(EntryQrName);
        root.transform.SetParent(container, true);
        root.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
        root.layer = anchor.gameObject.layer;

        GameObject areaObject = new GameObject("Area");
        areaObject.transform.SetParent(root.transform, false);
        areaObject.layer = root.layer;
        BoxCollider area = areaObject.AddComponent<BoxCollider>();
        area.isTrigger = true;
        area.size = new Vector3(0.8f, 0.2f, 0.8f);
        area.center = Vector3.zero;

        workflow.SetEntryQr(root.transform, area);
    }

    private static TopicObjs BuildTopic(string topicName, List<GameObject> stepObjects)
    {
        string path = BusinessRoot + "/" + NodeName + "/" + topicName + ".prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            throw new InvalidOperationException("找不到 11 题目 Prefab：" + path);
        }

        if (stepObjects.Count == 0)
        {
            throw new InvalidOperationException("11 题目没有任何步骤对象：" + topicName);
        }

        TopicObjs topic = new TopicObjs
        {
            topicPrefab = prefab,
            topicObjs = new List<StepObjs>()
        };
        for (int index = 0; index < stepObjects.Count; index++)
        {
            topic.topicObjs.Add(new StepObjs
            {
                stepIndex = index,
                stepObjs = new List<GameObject> { stepObjects[index] }
            });
        }

        return topic;
    }

    /// <summary>
    /// 把 11 节点原有的 7 个题目槽位替换成三个新题目。节点本身仍保持在
    /// <c>interacterObjs</c> 的第 11 位，03/04 槽位不动，后续模块索引不会错位。
    /// </summary>
    private static void ConfigureStepTree(IEnumerable<TopicObjs> topics)
    {
        TexCore_StepObj tree = UnityEngine.Object.FindObjectOfType<TexCore_StepObj>(true);
        if (tree == null)
        {
            throw new InvalidOperationException("场景缺少 TexCore_StepObj。");
        }

        NodeObjs node = tree.interacterObjs.FirstOrDefault(value =>
            value.nodePrefab != null && value.nodePrefab.name == NodeName);
        if (node == null)
        {
            throw new InvalidOperationException("步骤树缺少节点：" + NodeName);
        }

        node.nodeObjs = topics.ToList();
        EditorUtility.SetDirty(tree);
    }

    private static void AllowInHeadMenu()
    {
        TexCore_HeadMenu menu = UnityEngine.Object.FindObjectOfType<TexCore_HeadMenu>(true);
        if (menu == null)
        {
            throw new InvalidOperationException("场景缺少 TexCore_HeadMenu。");
        }

        SerializedObject serialized = new SerializedObject(menu);
        SerializedProperty allowed = serialized.FindProperty("allowedModuleNames");
        if (allowed == null)
        {
            throw new InvalidOperationException("TexCore_HeadMenu 缺少 allowedModuleNames。");
        }

        for (int index = 0; index < allowed.arraySize; index++)
        {
            if (allowed.GetArrayElementAtIndex(index).stringValue == NodeName)
            {
                return;
            }
        }

        allowed.InsertArrayElementAtIndex(allowed.arraySize);
        allowed.GetArrayElementAtIndex(allowed.arraySize - 1).stringValue = NodeName;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(menu);
    }

    /// <summary>
    /// Config.xml 只能由 Prefab/场景源数据重新生成：读取现有配置树后，按
    /// <c>BusinessPrefabs</c> 下的节点 Prefab 重新灌入 11 的 childData。
    /// </summary>
    private static void RegenerateConfig()
    {
        TexCore_ConfigManager manager = UnityEngine.Object.FindObjectOfType<TexCore_ConfigManager>(true);
        if (manager == null)
        {
            throw new InvalidOperationException("场景缺少 TexCore_ConfigManager，无法生成 Config.xml。");
        }

        string configPath = Path.Combine(Application.dataPath, "StreamingAssets", "Config.xml");
        ConfigleData config = XmlSerializer.LoadFromXml(configPath, typeof(ConfigleData)) as ConfigleData;
        if (config == null || config.datas == null)
        {
            throw new InvalidOperationException("无法读取 Config.xml 的 TexCore 配置根节点。");
        }

        manager.datas = config;
        manager.ReadDataFromPrefabSingle(config.datas, manager.prefabsPath);
        XmlSerializer.SaveToXml(configPath, config);
    }

    private static GameObject CreateReporter(Transform container, string name)
    {
        GameObject reporter = new GameObject(name);
        reporter.transform.SetParent(container, false);
        reporter.SetActive(true);
        return reporter;
    }

    private static Transform CreateContainer(Transform parent, string name)
    {
        Transform existing = FindName(parent, name);
        if (existing != null)
        {
            return existing;
        }

        GameObject value = new GameObject(name);
        value.transform.SetParent(parent, false);
        return value.transform;
    }

    private static void ClearGenerated(Transform parent, string name)
    {
        Transform existing = FindName(parent, name);
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }
    }

    private static Transform FindPath(params string[] names)
    {
        Transform current = null;
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (NamesEqual(root.name, names[0]))
            {
                current = root.transform;
                break;
            }
        }

        return FindPath(current, names.Skip(1).ToArray());
    }

    private static Transform FindPath(Transform parent, params string[] names)
    {
        Transform current = parent;
        foreach (string name in names)
        {
            current = FindName(current, name);
            if (current == null)
            {
                throw new InvalidOperationException("找不到场景对象：" + name);
            }
        }

        return current;
    }

    private static Transform FindName(Transform parent, string name)
    {
        if (parent == null)
        {
            return null;
        }

        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (NamesEqual(child.name, name))
            {
                return child;
            }
        }

        return parent.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(value => value != parent && NamesEqual(value.name, name));
    }

    private static bool NamesEqual(string left, string right)
    {
        return string.Equals(NormalizeName(left), NormalizeName(right), StringComparison.Ordinal);
    }

    private static string NormalizeName(string value)
    {
        return (value ?? string.Empty).Replace(" ", string.Empty).Replace("　", string.Empty);
    }

    private static int NumericName(string value)
    {
        string digits = new string(value.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out int result) ? result : int.MaxValue;
    }
}
#endif
