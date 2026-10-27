using Bpf.Diagnostics;
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using MixedReality.Toolkit.UX;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;

public static class TexCore_0809SceneBuilder
{
    private const string MainlineScenePath = "Assets/A_MRNet/Sence/03SingleplayerScene.unity";
    private const string Scene0809Path = "Assets/A_MRNet/Sence/0809.unity";
    private const string BusinessRoot = "Assets/Resources/BusinessPrefabs/MR混合现实多人协作平台/01货到人拣选电商仓储实施安装与调试实训系统";
    private const string SystemName = "01货到人拣选电商仓储实施安装与调试实训系统";
    private const string Module08Path = BusinessRoot + "/08充电桩安装";
    private const string Unpack08TopicName = "00开箱取出充电桩";
    private const string Module09Name = "09机器人入场";
    private const string M09KnowledgeTopicName = "01知识点答题_待出题";
    private const string M09KnowledgeTopicPath = BusinessRoot + "/09机器人入场/01知识点答题_待出题.prefab";
    private const float DrillAutoCompleteTolerance = 0.25f;

    private enum M09NetworkControlKind
    {
        PressableButton,
        TextInput
    }

    private sealed class M09NetworkStepDefinition
    {
        public readonly string stepName;
        public readonly string pageName;
        public readonly string[] sourcePath;
        public readonly int finalNameOccurrence;
        public readonly M09NetworkControlKind controlKind;

        public M09NetworkStepDefinition(string name, string page, string[] path,
            int occurrence, M09NetworkControlKind kind)
        {
            stepName = name;
            pageName = page;
            sourcePath = path;
            finalNameOccurrence = occurrence;
            controlKind = kind;
        }
    }

    private static readonly M09NetworkStepDefinition[] M09NetworkSteps =
    {
        new M09NetworkStepDefinition("09_14_AGV1_进入配网", "AGV1",
            new[] { "Empty Button" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV2_输入网络地址", "AGV2",
            new[] { "InputField (Legacy)" }, 0, M09NetworkControlKind.TextInput),
        new M09NetworkStepDefinition("09_14_AGV2_确认网络地址", "AGV2",
            new[] { "Empty Button" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV3_输入用户名", "AGV3",
            new[] { "yonghuming", "InputField" }, 0, M09NetworkControlKind.TextInput),
        new M09NetworkStepDefinition("09_14_AGV3_输入密码", "AGV3",
            new[] { "mima", "InputField" }, 0, M09NetworkControlKind.TextInput),
        new M09NetworkStepDefinition("09_14_AGV3_登录", "AGV3",
            new[] { "Empty Button" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV4_确认", "AGV4",
            new[] { "Empty Button" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV5_确认", "AGV5",
            new[] { "Empty Button" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV6_按钮1", "AGV6",
            new[] { "Empty Button" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV6_按钮2", "AGV6",
            new[] { "Empty Button" }, 1, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV7_按钮1", "AGV7",
            new[] { "Empty Button" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV7_按钮2", "AGV7",
            new[] { "Empty Button (1)" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV8_按钮1", "AGV8",
            new[] { "Empty Button" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV9_确认", "AGV9",
            new[] { "Empty Button" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV8_按钮2", "AGV8",
            new[] { "Empty Button (1)" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV10_确认", "AGV10",
            new[] { "Empty Button" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV8_按钮3", "AGV8",
            new[] { "Empty Button (2)" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV11_确认", "AGV11",
            new[] { "Empty Button" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV8_按钮4", "AGV8",
            new[] { "Empty Button (3)" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV12_确认", "AGV12",
            new[] { "Empty Button" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV8_按钮5", "AGV8",
            new[] { "Empty Button (4)" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV14_按钮1", "AGV14",
            new[] { "Empty Button" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV14_按钮2", "AGV14",
            new[] { "Empty Button (1)" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV15_确认", "AGV15",
            new[] { "Empty Button" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV16_按钮1", "AGV16",
            new[] { "Empty Button" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV16_按钮2", "AGV16",
            new[] { "Empty Button (1)" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV17_确认", "AGV17",
            new[] { "Empty Button" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV18_按钮1", "AGV18",
            new[] { "Empty Button" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV19_确认", "AGV19",
            new[] { "Empty Button" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV18_按钮3", "AGV18",
            new[] { "Empty Button (2)" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV20_提交", "AGV20",
            new[] { "Empty Button" }, 0, M09NetworkControlKind.PressableButton),
        new M09NetworkStepDefinition("09_14_AGV18_按钮2", "AGV18",
            new[] { "Empty Button (1)" }, 0, M09NetworkControlKind.PressableButton)
    };

    [MenuItem("BPF/TexCore/构建 08、09 步骤")]
    public static void Build()
    {
        BuildScene(MainlineScenePath, false);
    }

    [MenuItem("BPF/TexCore/重建 09 机器人入场步骤")]
    public static void Build09Only()
    {
        BuildScene(MainlineScenePath, true);
    }

    [MenuItem("BPF/TexCore/构建 0809 步骤")]
    public static void Build0809()
    {
        BuildScene(Scene0809Path, false);
    }

    [MenuItem("BPF/TexCore/重建 0809 机器人入场步骤")]
    public static void Build0809Only()
    {
        BuildScene(Scene0809Path, true);
    }

    private static void BuildScene(string scenePath, bool only09)
    {
        SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        if (sceneAsset == null)
        {
            throw new InvalidOperationException("找不到场景：" + scenePath);
        }

        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!only09)
        {
            Ensure08UnpackTopicPrefab();
            Ensure08UnpackTopicInStepTree();
        }
        Remove09KnowledgeTopic();

        Transform interactive09 = FindPath("有交互物体", SystemName, "09机器人入场");
        Transform legacy09 = FindPath("无交互物体", SystemName, "09机器人入场", "ChangJing-8");
        Validate09DrillSources(legacy09);
        ClearGenerated(interactive09, "TexCore_09Steps");

        List<TexCore_InteracterBase> required09;
        Build09(interactive09, legacy09, out required09);

        if (only09)
        {
            Configure09TopicPrefabs();
        }
        else
        {
            Transform interactive08 = FindPath("有交互物体", SystemName, "08充电桩安装");
            Transform legacy08 = FindPath("无交互物体", SystemName, "08充电桩安装", "changjing-2");
            ClearGenerated(interactive08, "TexCore_08Steps");

            List<TexCore_InteracterBase> required08;
            Build08(interactive08, legacy08, out required08);
            ConfigureTopicPrefabs(required08, required09);
            Configure08BackpackPanel(legacy08);
        }

        ConfigureStepTree();
        RegenerateConfig();

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        BpfLog.Info(BpfLogCategory.Diagnostics, "tex_core_0809_scene_builder.build_scene", "TexCore 步骤构建完成：" + Path.GetFileNameWithoutExtension(scenePath));
    }

    private static void Validate09DrillSources(Transform legacy09)
    {
        FindPath(legacy09, "ShouDianZuan-JQR");
        Transform drillTargets = FindPath(legacy09, "ShouDianZuan吸附位置");
        Transform nails = FindPath(legacy09, "AGV", "XiangZi", "ZiGongSi");
        string[] nailNames = { "ZiGongSi", "ZiGongSi3", "ZiGongSi5", "ZiGongSi7", "ZiGongSi9", "ZiGongSi11", "ZiGongSi13", "ZiGongSi15" };

        foreach (string nailName in nailNames)
        {
            if (FindName(drillTargets, nailName) == null || FindName(nails, nailName) == null)
            {
                throw new InvalidOperationException("09 钻钉链缺少成对对象：" + nailName);
            }
        }

        FindPath(legacy09, "AGV", "XiangZi", "XiangGai");
        FindPath(legacy09, "AGV", "AGV1");
        FindPath(legacy09, "AGV", "AGV1吸附位置");
        FindPath(legacy09, "MR_ChangJing", "ChongDianZhuang");
        FindPath(legacy09, "MR_ChangJing", "ErWeiMa");
    }

    private static void Build08(Transform interactiveRoot, Transform legacyRoot,
        out List<TexCore_InteracterBase> required)
    {
        required = new List<TexCore_InteracterBase>();
        Transform container = CreateContainer(interactiveRoot, "TexCore_08Steps");
        Transform realStation = FindPath(legacyRoot, "ChongDianZhuang");
        Transform stationCopy = FindPath(legacyRoot, "ChongDianZhuang_fuben");
        Transform drill = FindPath(legacyRoot, "ShouDianZuan-CDZ");

        List<GameObject> unpack = new List<GameObject>();
        Transform boxFasteners = FindPath(legacyRoot, "XiangZi", "chaixibufen");
        string[] boxFastenerNames =
        {
            "ZiGongSi", "ZiGongSi3", "ZiGongSi5", "ZiGongSi7",
            "ZiGongSi9", "ZiGongSi11", "ZiGongSi13", "ZiGongSi15"
        };
        for (int i = 0; i < boxFastenerNames.Length; i++)
        {
            Transform fastener = FindName(boxFasteners, boxFastenerNames[i]);
            if (fastener == null)
            {
                throw new InvalidOperationException(
                    "08 包装箱螺丝对象缺失：" + boxFastenerNames[i]);
            }

            GameObject screw = fastener.gameObject;
            AddDrillScrewRemoval(unpack, required, container,
                "08_00_使用手电钻钻开包装箱螺丝" + (i + 1),
                drill.gameObject, screw, screw, DrillAutoCompleteTolerance);
            ConfigureM08Screw(screw);
        }

        // 8 颗螺丝的 1 → 8 顺序与“始终显示”由同一个场景级控制器维持，
        // 03 主线和 0809 都需要它。
        EnsureScrewSequence();

        GameObject boxLid = FindPath(legacyRoot, "XiangZi", "chaixibufen", "xiangti", "DingGai").gameObject;
        TexCore_ClickObjectStep boxLidStep = AddInitiallyVisibleClick(
            unpack, required, container, "08_00_移开包装箱盖", boxLid,
            null, true, false, required,
            "请先按顺序使用手电钻拆除包装箱上的 8 颗螺丝。");
        List<TexCore_OrderedPlacementStep> boxScrewSteps = new List<TexCore_OrderedPlacementStep>();
        for (int index = 0; index < boxFastenerNames.Length; index++)
        {
            TexCore_OrderedPlacementStep screwStep =
                unpack[index].GetComponent<TexCore_OrderedPlacementStep>();
            if (screwStep == null)
            {
                throw new InvalidOperationException(
                    "08 包装箱螺丝步骤缺少 TexCore_OrderedPlacementStep：" + unpack[index].name);
            }
            boxScrewSteps.Add(screwStep);
        }

        TexCore_M08BoxLidGate boxLidGate = boxLid.GetComponent<TexCore_M08BoxLidGate>();
        if (boxLidGate == null)
        {
            boxLidGate = boxLid.AddComponent<TexCore_M08BoxLidGate>();
        }
        boxLidGate.Configure(boxLidStep, boxScrewSteps);
        EditorUtility.SetDirty(boxLid);

        AddInitiallyVisibleClick(unpack, required, container, "08_00_取出箱内充电桩", stationCopy.gameObject,
            null, true, false);

        List<GameObject> qr = new List<GameObject>();
        required.Add(CreatePlacement(container, "08_01_粘贴二维码", FindPath(legacyRoot, "ErWeiMa").gameObject,
            FindPath(stationCopy, "ErWeiMa_part").gameObject, null, true, true, 0.10f));
        qr.Add(required[required.Count - 1].gameObject);

        List<GameObject> expansion = new List<GameObject>();
        AddPlacement(expansion, required, container, "08_02_安装充电桩", stationCopy.gameObject,
            realStation.gameObject, realStation.gameObject, true, true, 0.25f, false);
        AddClick(expansion, required, container, "08_03_打开充电桩前盖", FindPath(realStation, "DingGai").gameObject,
            realStation.gameObject, true, true);

        Transform drillTargets = FindPath(realStation, "dakong吸附位置");
        ConfigureDrillHoleColliders(drillTargets);
        for (int i = 1; i <= 4; i++)
        {
            AddDrillPlacement(expansion, required, container, "08_04_钻孔" + i, drill.gameObject,
                FindPath(drillTargets, "pCylin" + i).gameObject, DrillAutoCompleteTolerance);
        }

        Transform parts = FindPath(realStation, "pengzhangluosi");
        for (int i = 1; i <= 4; i++)
        {
            string sourceName = "PengZhangLuoSi (" + i + ")";
            string targetName = "PengZhangLuoSi _part（" + i + "）";
            AddPlacement(expansion, required, container, "08_08_安装膨胀管" + i,
                FindPath(parts, sourceName).gameObject,
                FindPath(parts, targetName).gameObject,
                null, false, true, 0.10f,
                hideSourceWhenUnfinished: false);
        }

        AddPlacement(expansion, required, container, "08_12_锤击固定", FindPath(legacyRoot, "YangJiaoChui").gameObject,
            FindPath(legacyRoot, "chuizi_collider_2", "chuiz_part (3)").gameObject, null, false, false, 0.12f);

        for (int i = 1; i <= 4; i++)
        {
            string sourceName = "luosi（" + i + "）";
            string targetName = "luosi_part(" + i + ")";
            AddPlacement(expansion, required, container, "08_13_安装膨胀螺丝" + i,
                FindPath(parts, sourceName).gameObject, FindName(parts, targetName).gameObject,
                null, false, true, 0.08f);
        }

        AddPlacement(expansion, required, container, "08_17_扳手紧固", FindPath(legacyRoot, "banshou_2").gameObject,
            FindPath(legacyRoot, "chuizi_collider_2", "chuiz_part (3)").gameObject, null, false, false, 0.12f);

        List<GameObject> antenna = new List<GameObject>();
        AddPlacement(antenna, required, container, "08_18_安装WiFi天线1", FindPath(realStation, "tianxian1").gameObject,
            FindPath(realStation, "Tianxian1 _part").gameObject, null, false, true, 0.10f);
        AddPlacement(antenna, required, container, "08_19_安装WiFi天线2", FindPath(realStation, "tianxian2").gameObject,
            FindPath(realStation, "Tianxian2 _part").gameObject, null, false, true, 0.10f);

        List<GameObject> power = new List<GameObject>();
        AddPlacement(power, required, container, "08_20_连接电源", FindPath(realStation, "Chazuo").gameObject,
            FindPath(realStation, "Chazuo _part").gameObject, null, false, true, 0.10f);
        AddClick(power, required, container, "08_21_连接网线", FindPath(realStation, "chatou").gameObject,
            null, false, false);
        AddClick(power, required, container, "08_22_充电桩开机", FindPath(realStation, "DingGai", "KaiGuan").gameObject,
            null, false, false);

        List<GameObject> network = new List<GameObject>();
        AddClick(network, required, container, "08_23_AGV配网", FindPath(legacyRoot, "AGV配网", "AGV20", "Empty Button").gameObject,
            FindPath(legacyRoot, "AGV配网").gameObject, false, false);
        AddClick(network, required, container, "08_24_RCS操作", FindPath(realStation, "DingGai", "RCS", "RCS_3", "Empty Button").gameObject,
            FindPath(realStation, "DingGai", "RCS").gameObject, false, false);

        List<GameObject> complete = new List<GameObject>();
        GameObject gate = CreateGate(container, "完成整个模块_08", required);
        complete.Add(gate);

        SetTopicSteps("08充电桩安装", Unpack08TopicName, unpack);
        SetTopicSteps("08充电桩安装", "01粘贴二维码", qr);
        SetTopicSteps("08充电桩安装", "02安装膨胀螺丝", expansion);
        SetTopicSteps("08充电桩安装", "03安装WiFi天线", antenna);
        SetTopicSteps("08充电桩安装", "04连接电源网线并开机", power);
        SetTopicSteps("08充电桩安装", "05充电站配网", network);
        SetTopicSteps("08充电桩安装", "06完成整个模块", complete);
    }

    private static void Build09(Transform interactiveRoot, Transform legacyRoot,
        out List<TexCore_InteracterBase> required)
    {
        required = new List<TexCore_InteracterBase>();
        Transform container = CreateContainer(interactiveRoot, "TexCore_09Steps");
        List<GameObject> unpack = new List<GameObject>();
        Transform drill = FindPath(legacyRoot, "ShouDianZuan-JQR");
        Transform drillTargets = FindPath(legacyRoot, "ShouDianZuan吸附位置");
        Transform nails = FindPath(legacyRoot, "AGV", "XiangZi", "ZiGongSi");
        Transform chargingStation = FindPath(legacyRoot, "MR_ChangJing", "ChongDianZhuang");
        Transform robot = FindPath(legacyRoot, "AGV", "AGV1");
        string[] nailNames = { "ZiGongSi", "ZiGongSi3", "ZiGongSi5", "ZiGongSi7", "ZiGongSi9", "ZiGongSi11", "ZiGongSi13", "ZiGongSi15" };
        for (int i = 0; i < nailNames.Length; i++)
        {
            AddDrillScrewRemoval(unpack, required, container, "09_01_手电钻拆除固定钉" + (i + 1),
                drill.gameObject, FindName(drillTargets, nailNames[i]).gameObject,
                FindName(nails, nailNames[i]).gameObject, DrillAutoCompleteTolerance);
        }
        AddBoxLidRemoval(unpack, required, container, "09_09_移开箱盖",
            FindPath(legacyRoot, "AGV", "XiangZi", "XiangGai").gameObject,
            chargingStation.gameObject);
        TexCore_OrderedPlacementStep pushOutRobotStep = AddPlacement(unpack, required, container,
            "09_10_推出机器人", robot.gameObject,
            FindPath(legacyRoot, "AGV", "AGV1吸附位置").gameObject, null, true, true, 0.20f,
            preserveExistingManipulatorSettings: true);
        pushOutRobotStep.ConfigurePreserveSourcePoseAcrossStateChanges();

        List<GameObject> checks = new List<GameObject>();
        AddClick(checks, required, container, "09_11_检查左碰撞检测器",
            FindPath(legacyRoot, "AGV", "AGV1", "AGV2", "Canvas", "Action Button").gameObject, null, false, false);
        AddClick(checks, required, container, "09_12_检查右碰撞检测器",
            FindPath(legacyRoot, "AGV", "AGV1", "AGV9", "Canvas", "Action Button").gameObject, null, false, false);
        AddClick(checks, required, container, "09_13_按下机器人开机键",
            FindPath(legacyRoot, "AGV", "AGV1", "KaiGuan", "Canvas", "Action Button").gameObject, null, false, false);

        List<GameObject> network = new List<GameObject>();
        Transform networkRoot = FindPath(legacyRoot, "AGV配网");
        RepairM09NetworkTransitions(networkRoot);
        AddM09NetworkWorkflow(network, required, container, networkRoot);

        List<GameObject> move = new List<GameObject>();
        TexCore_OrderedPlacementStep moveRobotToQrStep = AddPlacement(move, required, container,
            "09_15_机器人移动至二维码",
            robot.gameObject,
            FindRobotQrTarget(FindPath(legacyRoot, "MR_ChangJing", "ErWeiMa")).gameObject,
            null, true, true, 0.20f,
            hideSourceWhenUnfinished: false, preserveExistingManipulatorSettings: true);
        moveRobotToQrStep.ConfigurePreserveSourcePoseAcrossStateChanges();
        // The QR decal lies on the floor while the robot origin floats above
        // it, so only the horizontal distance may decide this step.
        moveRobotToQrStep.IgnoreVerticalForTolerance = true;

        List<GameObject> complete = new List<GameObject>();
        complete.Add(CreateGate(container, "完成整个模块_09", required));

        SetTopicSteps("09机器人入场", "02从货箱取出机器人", unpack);
        SetTopicSteps("09机器人入场", "03检查机器人", checks);
        SetTopicSteps("09机器人入场", "04AGV配网", network);
        SetTopicSteps("09机器人入场", "05机器人推至二维码", move);
        SetTopicSteps("09机器人入场", "06完成整个模块", complete);
    }

    /// <summary>
    /// Returns the single QR decal the 09 robot has to be pushed onto. The
    /// legacy ChangJing-8 prefab marks exactly one decal of the floor grid with
    /// a trigger collider and a rotation-locked Rigidbody. The grid container
    /// itself cannot be the target: its origin sits between four decals, so a
    /// robot standing on a code can never satisfy the placement tolerance.
    /// </summary>
    private static Transform FindRobotQrTarget(Transform qrGrid)
    {
        Transform marked = qrGrid.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(value =>
                value != qrGrid && value.GetComponent<Collider>() != null);
        if (marked == null)
        {
            throw new InvalidOperationException("09 机器人二维码网格缺少带碰撞体的二维码贴片。");
        }

        return marked;
    }

    private static TexCore_InteracterBase CreatePlacement(Transform container, string name, GameObject source,
        GameObject target, GameObject visibilityRoot, bool keepFinished, bool grabSource, float tolerance,
        bool hideSourceWhenUnfinished = true, bool preserveExistingManipulatorSettings = false)
    {
        if (preserveExistingManipulatorSettings)
        {
            ConfigureM09RobotControllerPush(source);
        }
        else
        {
            EnsureMovable(source, grabSource);
        }
        GameObject reporter = CreateReporter(container, name);
        TexCore_OrderedPlacementStep step = reporter.AddComponent<TexCore_OrderedPlacementStep>();
        step.ConfigureSource(source);
        step.Target = target != null ? target.transform : null;
        step.PositionTolerance = tolerance;
        step.RotationTolerance = 180f;
        ConfigureVisibility(reporter, source, target, visibilityRoot, keepFinished, hideSourceWhenUnfinished);
        return step;
    }

    private static TexCore_InteracterBase CreateClick(Transform container, string name, GameObject source,
        GameObject visibilityRoot, bool grabSource, bool useMouse)
    {
        EnsureClickable(source, grabSource);
        GameObject reporter = CreateReporter(container, name);
        TexCore_ClickObjectStep step = reporter.AddComponent<TexCore_ClickObjectStep>();
        step.Configure(useMouse, !grabSource, grabSource, true);
        step.ConfigureSource(source);
        ConfigureVisibility(reporter, source, null, visibilityRoot, false);
        return step;
    }

    private static TexCore_InteracterBase CreateDrillScrewRemoval(Transform container, string name,
        GameObject drill, GameObject drillTarget, GameObject screw, float tolerance)
    {
        EnsureMovable(drill, true);
        EnsureFastenerVisible(screw);
        GameObject reporter = CreateReporter(container, name);
        TexCore_OrderedPlacementStep step = reporter.AddComponent<TexCore_OrderedPlacementStep>();
        step.ConfigureSource(drill);
        step.Target = drillTarget.transform;
        ConfigureDrillStep(step, drill, tolerance);
        step.RotationTolerance = 180f;

        // A screw stays in place until the held drill reaches its matching
        // attraction point; after success it disappears before the next step.
        TexCore_StepObjectState state = reporter.AddComponent<TexCore_StepObjectState>();
        if (drillTarget == screw)
        {
            // 08 uses one object for both the screw mesh and its attraction
            // point. It must remain visible until this step is completed.
            state.Configure(
                new[] { drill, screw },
                null,
                null,
                new[] { screw },
                new[] { screw },
                null);
        }
        else
        {
            state.Configure(
                new[] { drill, drillTarget, screw },
                null,
                null,
                new[] { drillTarget, screw },
                new[] { screw },
                new[] { drillTarget });
        }
        return step;
    }

    /// <summary>
    /// The drill steps may only hide a fastener with <c>SetActive</c>, which
    /// cannot reveal a mesh whose renderer was switched off. A fastener that
    /// still has to be drilled therefore has to start out visible.
    /// </summary>
    private static void EnsureFastenerVisible(GameObject screw)
    {
        if (screw == null)
        {
            return;
        }

        foreach (Renderer renderer in screw.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = true;
        }
    }

    /// <summary>
    /// 08 开箱螺丝的固定配置。8 颗螺丝在整个开箱步骤里始终显示，动画只保留
    /// “拧下螺丝”的运动效果，因此构建结果必须保证：
    /// 1) 螺丝对象和它的所有 Renderer 都是打开的；
    /// 2) Animator 默认关闭，禁止进入关卡后自动播放，改由运行时在当前步骤
    ///    开始时显式触发；
    /// 3) 动画片段不再写 Renderer.enabled / GameObject.active，避免动画播完
    ///    之后把螺丝隐藏。
    /// 只作用于 08 的包装箱螺丝，09 的同名固定钉保持原有配置。
    /// </summary>
    private static void ConfigureM08Screw(GameObject screw)
    {
        if (screw == null)
        {
            return;
        }

        screw.SetActive(true);

        foreach (Renderer renderer in screw.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = true;
        }

        Animator animator = screw.GetComponent<Animator>();
        if (animator == null)
        {
            return;
        }

        animator.enabled = false;
        EditorUtility.SetDirty(animator);
        StripAnimatorVisibilityCurves(animator.runtimeAnimatorController);
    }

    /// <summary>
    /// 移除控制器里所有会隐藏物体的动画曲线。螺丝的位移与旋转曲线保持原样，
    /// 交互动画反馈不能丢。
    /// </summary>
    private static int StripAnimatorVisibilityCurves(
        RuntimeAnimatorController controller)
    {
        if (controller == null)
        {
            return 0;
        }

        int removed = 0;
        HashSet<AnimationClip> visited = new HashSet<AnimationClip>();

        foreach (AnimationClip clip in controller.animationClips)
        {
            if (clip == null || !visited.Add(clip))
            {
                continue;
            }

            removed += StripVisibilityCurves(clip);
        }

        return removed;
    }

    private static int StripVisibilityCurves(AnimationClip clip)
    {
        if (clip == null)
        {
            return 0;
        }

        int removed = 0;

        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
        {
            if (!IsVisibilityCurve(binding))
            {
                continue;
            }

            AnimationUtility.SetEditorCurve(clip, binding, null);
            removed++;
        }

        foreach (EditorCurveBinding binding in
                 AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            if (!IsVisibilityCurve(binding))
            {
                continue;
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
            removed++;
        }

        if (removed > 0 && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(clip)))
        {
            EditorUtility.SetDirty(clip);
        }

        return removed;
    }

    /// <summary>
    /// 只有写 Renderer.enabled 和 GameObject.active 的曲线算可见性曲线；
    /// 碰撞体、Animator 自身的 m_Enabled 等其它曲线不受影响。
    /// </summary>
    private static bool IsVisibilityCurve(EditorCurveBinding binding)
    {
        if (binding.propertyName == "m_IsActive")
        {
            return true;
        }

        return binding.propertyName == "m_Enabled" &&
            typeof(Renderer).IsAssignableFrom(binding.type);
    }

    /// <summary>
    /// 08 螺丝顺序控制器必须挂在场景级对象上：0809 已经挂在根对象 StepObjs
    /// 上，03 主线同样挂到 StepObjs，保证 M08 未激活时组件依然在运行。
    /// </summary>
    private static void EnsureScrewSequence()
    {
        TexCore_0809M08ScrewSequence[] existing = UnityEngine.Object
            .FindObjectsOfType<TexCore_0809M08ScrewSequence>(true);
        if (existing.Length > 0)
        {
            return;
        }

        Transform host = FindPath("StepObjs");
        TexCore_0809M08ScrewSequence sequence = host.gameObject
            .AddComponent<TexCore_0809M08ScrewSequence>();
        EditorUtility.SetDirty(sequence);
    }

    private static void AddBoxLidRemoval(List<GameObject> topicSteps,
        List<TexCore_InteracterBase> required, Transform container, string name, GameObject boxLid,
        GameObject chargingStation)
    {
        TexCore_InteracterBase step = CreateClick(container, name, boxLid, null, true, false);
        ConfigureM09BoxLidMove(boxLid);
        TexCore_StepObjectState state = step.GetComponent<TexCore_StepObjectState>();
        state.Configure(
            new[] { boxLid, chargingStation },
            null,
            new[] { boxLid, chargingStation },
            null,
            new[] { boxLid, chargingStation },
            null);
        topicSteps.Add(step.gameObject);
        required.Add(step);
    }

    /// <summary>
    /// The 09 box lid moves as one rigid unit. It keeps a gravity-free
    /// kinematic body between grabs so it can neither rotate on its own nor
    /// fall, and the manipulator allows Move only. While
    /// "09_09_移开箱盖" is not active, TexCore_ClickObjectStep keeps its
    /// ObjectManipulator and colliders disabled; once all eight fastener steps
    /// are completed and the step enters 等待触发, the grab becomes available.
    /// VelocityTracking lets the kinematic body follow the hand: MRTK3
    /// temporarily un-kinematics the body during the grab (gravity stays off)
    /// and restores the kinematic state on release, so the lid never drops.
    /// </summary>
    private static void ConfigureM09BoxLidMove(GameObject boxLid)
    {
        if (boxLid == null)
        {
            throw new InvalidOperationException("09 箱盖交互源为空。");
        }

        Rigidbody body = boxLid.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = boxLid.AddComponent<Rigidbody>();
        }
        body.useGravity = false;
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;

        ObjectManipulator manipulator = boxLid.GetComponent<ObjectManipulator>();
        if (manipulator == null)
        {
            throw new InvalidOperationException("09 箱盖缺少 ObjectManipulator。");
        }

        manipulator.AllowedManipulations = TransformFlags.Move;
        manipulator.RigidbodyMovementType = XRBaseInteractable.MovementType.VelocityTracking;
    }

    private static GameObject CreateGate(Transform container, string name, IEnumerable<TexCore_InteracterBase> requirements)
    {
        GameObject reporter = CreateReporter(container, name);
        TexCore_ModuleCompletionGateStep gate = reporter.AddComponent<TexCore_ModuleCompletionGateStep>();
        gate.Configure(requirements, "请先完成本模块前置步骤，再提交完成状态。");
        return reporter;
    }

    private static TexCore_OrderedPlacementStep AddPlacement(List<GameObject> topicSteps,
        List<TexCore_InteracterBase> required,
        Transform container, string name, GameObject source, GameObject target, GameObject visibilityRoot,
        bool keepFinished, bool grabSource, float tolerance, bool hideSourceWhenUnfinished = true,
        bool preserveExistingManipulatorSettings = false)
    {
        TexCore_OrderedPlacementStep step = CreatePlacement(container, name, source, target, visibilityRoot,
            keepFinished, grabSource, tolerance, hideSourceWhenUnfinished,
            preserveExistingManipulatorSettings) as TexCore_OrderedPlacementStep;
        if (step == null)
        {
            throw new InvalidOperationException("放置步骤未能创建有序放置组件。");
        }
        topicSteps.Add(step.gameObject);
        required.Add(step);
        return step;
    }

    private static void AddClick(List<GameObject> topicSteps, List<TexCore_InteracterBase> required,
        Transform container, string name, GameObject source, GameObject visibilityRoot,
        bool grabSource, bool useMouse)
    {
        TexCore_InteracterBase step = CreateClick(container, name, source, visibilityRoot, grabSource, useMouse);
        topicSteps.Add(step.gameObject);
        required.Add(step);
    }

    private static void AddM09NetworkWorkflow(List<GameObject> topicSteps,
        List<TexCore_InteracterBase> required, Transform container, Transform networkRoot)
    {
        List<GameObject> sources = new List<GameObject>();
        List<bool> textInputs = new List<bool>();
        foreach (M09NetworkStepDefinition definition in M09NetworkSteps)
        {
            Transform source = FindM09NetworkSource(networkRoot, definition);
            if (definition.controlKind == M09NetworkControlKind.TextInput)
            {
                if (source.GetComponent<InputField>() == null)
                {
                    throw new InvalidOperationException("09 AGV 配网输入框缺少 InputField：" +
                        definition.stepName);
                }
            }
            else if (source.GetComponent<PressableButton>() == null)
            {
                throw new InvalidOperationException("09 AGV 配网按钮缺少 PressableButton：" +
                    definition.stepName);
            }

            sources.Add(source.gameObject);
            textInputs.Add(definition.controlKind == M09NetworkControlKind.TextInput);
        }

        GameObject reporter = CreateReporter(container, "09_14_AGV配网完成");
        TexCore_PassiveUIEventStep step = reporter.AddComponent<TexCore_PassiveUIEventStep>();
        step.ConfigureSequence(networkRoot.gameObject, sources, textInputs);
        topicSteps.Add(reporter);
        required.Add(step);
    }

    private static Transform FindM09NetworkSource(Transform networkRoot,
        M09NetworkStepDefinition definition)
    {
        Transform current = FindPath(networkRoot, definition.pageName);
        for (int index = 0; index < definition.sourcePath.Length; index++)
        {
            int occurrence = index == definition.sourcePath.Length - 1
                ? definition.finalNameOccurrence
                : 0;
            current = FindDirectChild(current, definition.sourcePath[index], occurrence);
        }

        return current;
    }

    private static void RepairM09NetworkTransitions(Transform networkRoot)
    {
        Transform agv8 = FindPath(networkRoot, "AGV8");
        PressableButton agv8FourthButton = RequirePressableButton(
            FindDirectChild(agv8, "Empty Button (3)", 0), "AGV8 第 4 个按钮");
        PressableButton agv8FifthButton = RequirePressableButton(
            FindDirectChild(agv8, "Empty Button (4)", 0), "AGV8 第 5 个按钮");
        PressableButton agv11Button = RequirePressableButton(
            FindDirectChild(FindPath(networkRoot, "AGV11"), "Empty Button", 0), "AGV11 确认按钮");
        PressableButton agv12Button = RequirePressableButton(
            FindDirectChild(FindPath(networkRoot, "AGV12"), "Empty Button", 0), "AGV12 确认按钮");

        // The original AGV11 callback skips AGV8 button 4, making AGV12
        // unreachable. Restore the authored linear chain without taking over
        // the other page callbacks.
        RegisterBoolPersistentListener(agv11Button, 1, agv8FourthButton.gameObject, true);
        RegisterBoolPersistentListener(agv8FourthButton, 0, agv8FourthButton.gameObject, false);
        RegisterBoolPersistentListener(agv8FourthButton, 1,
            FindPath(networkRoot, "AGV12").gameObject, true);
        RegisterBoolPersistentListener(agv12Button, 1, agv8FifthButton.gameObject, true);
    }

    private static PressableButton RequirePressableButton(Transform value, string description)
    {
        PressableButton button = value != null ? value.GetComponent<PressableButton>() : null;
        if (button == null)
        {
            throw new InvalidOperationException("09 AGV 配网缺少 PressableButton：" + description);
        }

        return button;
    }

    private static void RegisterBoolPersistentListener(PressableButton button, int index,
        GameObject target, bool value)
    {
        if (button == null || target == null || index < 0)
        {
            throw new InvalidOperationException("09 AGV 配网页面跳转回调无效。");
        }

        int count = button.OnClicked.GetPersistentEventCount();
        if (count < index)
        {
            throw new InvalidOperationException("09 AGV 配网页面跳转回调序号缺失：" + button.name);
        }

        if (count == index)
        {
            UnityEventTools.AddBoolPersistentListener(button.OnClicked, target.SetActive, value);
        }
        else
        {
            UnityEventTools.RegisterBoolPersistentListener(button.OnClicked, index,
                target.SetActive, value);
        }

        EditorUtility.SetDirty(button);
    }

    private static TexCore_ClickObjectStep AddInitiallyVisibleClick(List<GameObject> topicSteps,
        List<TexCore_InteracterBase> required, Transform container, string name,
        GameObject source, GameObject visibilityRoot, bool grabSource, bool useMouse,
        IEnumerable<TexCore_InteracterBase> prerequisites = null,
        string prerequisiteMessage = null)
    {
        TexCore_InteracterBase step = CreateClick(container, name, source,
            visibilityRoot, grabSource, useMouse);
        TexCore_ClickObjectStep clickStep = step as TexCore_ClickObjectStep;
        if (clickStep != null && prerequisites != null)
        {
            clickStep.ConfigurePrerequisites(prerequisites, prerequisiteMessage);
        }
        ConfigureInitiallyVisibleClickState(
            step.GetComponent<TexCore_StepObjectState>(), source);
        topicSteps.Add(step.gameObject);
        required.Add(step);
        return clickStep;
    }

    private static void ConfigureInitiallyVisibleClickState(
        TexCore_StepObjectState state, GameObject source)
    {
        state.Configure(
            new[] { source },
            null,
            new[] { source },
            null,
            new[] { source },
            null);
    }

    private static void AddDrillPlacement(List<GameObject> topicSteps,
        List<TexCore_InteracterBase> required, Transform container, string name, GameObject drill,
        GameObject target, float tolerance)
    {
        TexCore_OrderedPlacementStep step = CreatePlacement(container, name, drill, target, null,
            false, true, tolerance) as TexCore_OrderedPlacementStep;
        if (step == null)
        {
            throw new InvalidOperationException("电钻步骤未能创建有序放置组件。");
        }

        ConfigureDrillStep(step, drill, tolerance);
        topicSteps.Add(step.gameObject);
        required.Add(step);
    }

    /// <summary>
    /// The drilled holes under "dakong吸附位置" are only a hint and the
    /// attraction reference for the drill steps. Their colliders must not block
    /// the expansion tubes that are installed into the same holes afterwards,
    /// so every collider below that node becomes a trigger. The holes stay
    /// visible and raycastable, they just stop pushing the tubes away.
    /// </summary>
    private static void ConfigureDrillHoleColliders(Transform drillTargets)
    {
        if (drillTargets == null)
        {
            return;
        }

        foreach (Collider collider in drillTargets.GetComponentsInChildren<Collider>(true))
        {
            if (collider == null || collider.isTrigger)
            {
                continue;
            }

            collider.isTrigger = true;
            EditorUtility.SetDirty(collider);
        }
    }

    private static void AddDrillScrewRemoval(List<GameObject> topicSteps,
        List<TexCore_InteracterBase> required, Transform container, string name, GameObject drill,
        GameObject drillTarget, GameObject screw, float tolerance)
    {
        TexCore_InteracterBase step = CreateDrillScrewRemoval(container, name, drill, drillTarget, screw,
            tolerance);
        topicSteps.Add(step.gameObject);
        required.Add(step);
    }

    private static void ConfigureDrillStep(TexCore_OrderedPlacementStep step, GameObject drill, float tolerance)
    {
        step.ConfigureAutoCompleteOnApproach(tolerance, true);

        ObjectManipulator manipulator = drill.GetComponent<ObjectManipulator>();
        if (manipulator != null)
        {
            manipulator.SmoothingNear = false;
            manipulator.SmoothingFar = false;
        }
    }

    private static GameObject CreateReporter(Transform container, string name)
    {
        GameObject reporter = new GameObject(name);
        reporter.transform.SetParent(container, false);
        reporter.SetActive(true);
        return reporter;
    }

    private static void ConfigureVisibility(GameObject reporter, GameObject source, GameObject target,
        GameObject visibilityRoot, bool keepFinished, bool hideSourceWhenUnfinished = true)
    {
        GameObject visible = visibilityRoot != null ? visibilityRoot : source;
        TexCore_StepObjectState state = reporter.AddComponent<TexCore_StepObjectState>();
        List<GameObject> active = new List<GameObject> { source };
        if (visible != source)
        {
            active.Add(visible);
        }
        if (target != null && target != visible)
        {
            active.Add(target);
        }
        List<GameObject> finished = keepFinished ? new List<GameObject>(active) : new List<GameObject>();
        List<GameObject> unfinishedHides = new List<GameObject>(active);
        if (!hideSourceWhenUnfinished)
        {
            unfinishedHides.Remove(source);
        }
        state.Configure(active, null, finished, null, null, unfinishedHides);
    }

    private static void EnsureClickable(GameObject source, bool grab)
    {
        if (source == null)
        {
            throw new InvalidOperationException("交互源为空。");
        }

        if (source.GetComponentsInChildren<Collider>(true).Length == 0)
        {
            source.AddComponent<BoxCollider>();
        }

        if (grab)
        {
            EnsureMovable(source, true);
            return;
        }

        if (source.GetComponentInChildren<PressableButton>(true) == null)
        {
            source.AddComponent<PressableButton>();
        }
    }

    private static void EnsureMovable(GameObject source, bool addRigidbody)
    {
        if (source == null)
        {
            throw new InvalidOperationException("可移动交互源为空。");
        }

        if (source.GetComponentsInChildren<Collider>(true).Length == 0)
        {
            source.AddComponent<BoxCollider>();
        }
        ObjectManipulator manipulator = source.GetComponent<ObjectManipulator>();
        if (manipulator == null)
        {
            manipulator = source.AddComponent<ObjectManipulator>();
        }
        manipulator.AllowedManipulations = TransformFlags.Move;
        if (addRigidbody && source.GetComponent<Rigidbody>() == null)
        {
            Rigidbody body = source.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = false;
        }
    }

    private static void ConfigureM09RobotControllerPush(GameObject robot)
    {
        if (robot == null)
        {
            throw new InvalidOperationException("09 机器人为空。");
        }

        ObjectManipulator manipulator = robot.GetComponent<ObjectManipulator>();
        Rigidbody body = robot.GetComponent<Rigidbody>();
        Collider robotCollider = robot.GetComponent<Collider>();
        if (manipulator == null || body == null || robotCollider == null)
        {
            throw new InvalidOperationException("09 机器人缺少原始刚体、碰撞体或操控器组件。");
        }

        // Preserve the original ChangJing-8 movement baseline.  The source
        // robot uses an interpolated kinematic body and move-only constraints;
        // changing it to a dynamic, free-rotation object makes it lift away
        // from the work surface when a controller selects it.
        body.useGravity = false;
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        // VelocityTracking 在抓取期间会把刚体切成非运动学（见 MRTK
        // ObjectManipulator.SetupRigidbodyManipulation），此时若旋转不受约束，
        // 与货箱/地面的接触力矩会让机器人一边移动一边打转。锁死三个旋转轴，
        // 只保留平移自由度；完成时的吸附仍由 transform 直接赋值，不受影响。
        body.constraints |= RigidbodyConstraints.FreezeRotation;
        manipulator.AllowedManipulations = TransformFlags.Move;
        manipulator.RigidbodyMovementType = XRBaseInteractable.MovementType.VelocityTracking;

        // PICO can expose its controller as either MRTK's ray interactor or a
        // standard XRI interactor.  This only expands selection compatibility;
        // it does not alter the robot's physical body or movement constraints.
        manipulator.AllowedInteractionTypes |=
            InteractionFlags.Near | InteractionFlags.Ray | InteractionFlags.Generic;
        if (!manipulator.colliders.Contains(robotCollider))
        {
            manipulator.colliders.Add(robotCollider);
        }
    }

    private static void ConfigureTopicPrefabs(List<TexCore_InteracterBase> required08,
        List<TexCore_InteracterBase> required09)
    {
        Configure08TopicPrefabs();
        Configure09TopicPrefabs();
    }

    private static void Configure08TopicPrefabs()
    {
        ConfigureOperate("08充电桩安装/00开箱取出充电桩", 0,
            new[]
            {
                "08_00_使用手电钻钻开包装箱螺丝1",
                "08_00_使用手电钻钻开包装箱螺丝2",
                "08_00_使用手电钻钻开包装箱螺丝3",
                "08_00_使用手电钻钻开包装箱螺丝4",
                "08_00_使用手电钻钻开包装箱螺丝5",
                "08_00_使用手电钻钻开包装箱螺丝6",
                "08_00_使用手电钻钻开包装箱螺丝7",
                "08_00_使用手电钻钻开包装箱螺丝8",
                "08_00_移开包装箱盖",
                "08_00_取出箱内充电桩"
            },
            new[]
            {
                "使用手电钻拆除第 1 颗包装箱固定螺丝。",
                "使用手电钻拆除第 2 颗包装箱固定螺丝。",
                "使用手电钻拆除第 3 颗包装箱固定螺丝。",
                "使用手电钻拆除第 4 颗包装箱固定螺丝。",
                "使用手电钻拆除第 5 颗包装箱固定螺丝。",
                "使用手电钻拆除第 6 颗包装箱固定螺丝。",
                "使用手电钻拆除第 7 颗包装箱固定螺丝。",
                "使用手电钻拆除第 8 颗包装箱固定螺丝。",
                "八颗固定螺丝均已拆除后，抓取并移开包装箱盖。",
                "从包装箱内取出充电桩，准备进行后续安装。"
            });
        ConfigureOperate("08充电桩安装/01粘贴二维码", 30,
            new[] { "08_01_粘贴二维码" },
            new[] { "请将二维码粘贴到充电桩指定位置，使 AGV 可通过上方镜头识别充电桩。" });
        ConfigureOperate("08充电桩安装/02安装膨胀螺丝", 20,
            new[] { "08_02_安装充电桩", "08_03_打开充电桩前盖", "08_04_钻孔1", "08_04_钻孔2", "08_04_钻孔3", "08_04_钻孔4", "08_08_安装膨胀管1", "08_08_安装膨胀管2", "08_08_安装膨胀管3", "08_08_安装膨胀管4", "08_12_锤击固定", "08_13_安装膨胀螺丝1", "08_13_安装膨胀螺丝2", "08_13_安装膨胀螺丝3", "08_13_安装膨胀螺丝4", "08_17_扳手紧固" },
            new[]
            {
                "将充电桩搬运至指定二维码上方，准备使用膨胀螺丝固定。",
                "使用内六角扳手打开充电桩前盖，取出 WiFi 天线和膨胀螺栓。",
                "使用手电钻在第 1 个提示位置钻孔。",
                "使用手电钻在第 2 个提示位置钻孔。",
                "使用手电钻在第 3 个提示位置钻孔。",
                "使用手电钻在第 4 个提示位置钻孔。",
                "从背包取出第 1 根膨胀管并装入钻孔。",
                "从背包取出第 2 根膨胀管并装入钻孔。",
                "从背包取出第 3 根膨胀管并装入钻孔。",
                "从背包取出第 4 根膨胀管并装入钻孔。",
                "从背包取出锤子，敲击膨胀管使其固定。",
                "取出第 1 颗螺丝并装入膨胀管。",
                "取出第 2 颗螺丝并装入膨胀管。",
                "取出第 3 颗螺丝并装入膨胀管。",
                "取出第 4 颗螺丝并装入膨胀管。",
                "从背包取出扳手，将膨胀管中的螺丝拧紧。"
            });
        ConfigureOperate("08充电桩安装/03安装WiFi天线", 10,
            new[] { "08_18_安装WiFi天线1", "08_19_安装WiFi天线2" },
            new[]
            {
                "取出第 1 根 WiFi 天线并安装到充电桩指定位置。",
                "取出第 2 根 WiFi 天线并安装到充电桩指定位置。"
            });
        ConfigureOperate("08充电桩安装/04连接电源网线并开机", 10,
            new[] { "08_20_连接电源", "08_21_连接网线", "08_22_充电桩开机" },
            new[]
            {
                "连接充电桩电源。",
                "连接充电桩网线。",
                "按下充电桩开机开关。"
            });
        ConfigureOperate("08充电桩安装/05充电站配网", 30,
            new[] { "08_23_AGV配网", "08_24_RCS操作" },
            new[]
            {
                "根据界面提示将充电桩加入 RMS 系统，便于统一管理。",
                "在 RCS 界面完成充电桩参数设置。"
            });
        ConfigureOperate("08充电桩安装/06完成整个模块", 100,
            new[] { "完成整个模块_08" }, new[] { "确认 08 充电桩安装的全部步骤已完成。" });
    }

    private static void Configure09TopicPrefabs()
    {
        ConfigureOperate("09机器人入场/02从货箱取出机器人", 10,
            new[] { "09_01_手电钻拆除固定钉1", "09_01_手电钻拆除固定钉2", "09_01_手电钻拆除固定钉3", "09_01_手电钻拆除固定钉4", "09_01_手电钻拆除固定钉5", "09_01_手电钻拆除固定钉6", "09_01_手电钻拆除固定钉7", "09_01_手电钻拆除固定钉8", "09_09_移开箱盖", "09_10_推出机器人" },
            new[] { "依次使用手电钻钻除 8 颗固定钉；全部拆除后移开箱盖，再将机器人推出。" });
        ConfigureOperate("09机器人入场/03检查机器人", 10,
            new[] { "09_11_检查左碰撞检测器", "09_12_检查右碰撞检测器", "09_13_按下机器人开机键" },
            new[] { "完成以下操作后，点击确定：\n检查左右碰撞检测器并按下机器人开机键。" });
        ConfigureOperate("09机器人入场/04AGV配网", 30,
            new[] { "09_14_AGV配网完成" },
            new[] { "按 AGV 配网界面顺序完成全部输入、确认与提交操作。" });
        ConfigureOperate("09机器人入场/05机器人推至二维码", 10, new[] { "09_15_机器人移动至二维码" },
            new[] { "完成以下操作后，点击确定：\n将机器人推至二维码位置。" });
        ConfigureOperate("09机器人入场/06完成整个模块", 100, new[] { "完成整个模块_09" },
            new[] { "确认 09 机器人入场的全部步骤已完成。" });
    }

    private static void Configure08BackpackPanel(Transform legacyRoot)
    {
        Transform backpackRoot = FindPath("背包UI");
        Transform sourcePanel = FindName(backpackRoot, "06货架安装背包面板");
        if (backpackRoot == null || sourcePanel == null)
        {
            throw new InvalidOperationException("找不到现有背包 UI 或 06 背包面板。" );
        }

        Transform existing = FindName(backpackRoot, "08充电桩安装背包面板");
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }

        GameObject panel = UnityEngine.Object.Instantiate(sourcePanel.gameObject, backpackRoot);
        panel.name = "08充电桩安装背包面板";
        panel.SetActive(false);

        Transform buttonTemplate = sourcePanel.GetComponentsInChildren<PressableButton>(true)
            .Select(value => value.transform)
            .FirstOrDefault();
        if (buttonTemplate == null)
        {
            throw new InvalidOperationException("06 背包面板缺少 PressableButton 按钮模板。" );
        }

        List<GameObject> oldChildren = new List<GameObject>();
        for (int i = 0; i < panel.transform.childCount; i++)
        {
            oldChildren.Add(panel.transform.GetChild(i).gameObject);
        }
        foreach (GameObject child in oldChildren)
        {
            UnityEngine.Object.DestroyImmediate(child);
        }

        string[] keys = { "二维码", "充电桩", "手电钻", "膨胀管", "羊角锤", "膨胀螺丝", "扳手", "WiFi天线", "电源插头" };
        string[] labels = { "二维码", "充电桩", "手电钻", "膨胀管", "羊角锤", "膨胀螺丝", "扳手", "WiFi天线", "电源插头" };
        List<GameObject> buttons = new List<GameObject>();
        foreach (string key in keys)
        {
            Transform button = UnityEngine.Object.Instantiate(buttonTemplate, panel.transform);
            button.name = "08背包_" + key;
            button.gameObject.SetActive(true);
            TMP_Text label = button.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault();
            if (label != null)
            {
                label.text = labels[buttons.Count];
            }
            PressableButton pressable = button.GetComponentInChildren<PressableButton>(true);
            if (pressable != null)
            {
                for (int i = pressable.OnClicked.GetPersistentEventCount() - 1; i >= 0; i--)
                {
                    UnityEventTools.RemovePersistentListener(pressable.OnClicked, i);
                }
            }
            Button uiButton = button.GetComponentInChildren<Button>(true);
            if (uiButton != null)
            {
                for (int i = uiButton.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                {
                    UnityEventTools.RemovePersistentListener(uiButton.onClick, i);
                }
            }
            buttons.Add(button.gameObject);
        }

        HorizontalLayoutGroup horizontal = panel.GetComponent<HorizontalLayoutGroup>();
        if (horizontal != null) horizontal.enabled = false;
        VerticalLayoutGroup vertical = panel.GetComponent<VerticalLayoutGroup>();
        if (vertical != null) vertical.enabled = false;
        GameObject gridRoot = new GameObject("08背包按钮网格", typeof(RectTransform), typeof(GridLayoutGroup));
        gridRoot.transform.SetParent(panel.transform, false);
        RectTransform gridRect = gridRoot.GetComponent<RectTransform>();
        gridRect.anchorMin = Vector2.zero;
        gridRect.anchorMax = Vector2.one;
        gridRect.offsetMin = new Vector2(4f, 4f);
        gridRect.offsetMax = new Vector2(-4f, -4f);
        GridLayoutGroup grid = gridRoot.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(62f, 42f);
        grid.spacing = new Vector2(4f, 4f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.MiddleCenter;
        foreach (GameObject button in buttons)
        {
            button.transform.SetParent(gridRoot.transform, false);
        }

        GameObject controllerObject = backpackRoot.gameObject;
        TexCore_08BackpackController controller = controllerObject.GetComponent<TexCore_08BackpackController>();
        if (controller == null)
        {
            controller = controllerObject.AddComponent<TexCore_08BackpackController>();
        }
        controller.SetPanel(panel);
        controller.SetEntries(new List<TexCore_08BackpackController.Entry>
        {
            CreateBackpackEntry("二维码", "二维码", FindPath(legacyRoot, "ErWeiMa").gameObject, buttons[0]),
            CreateBackpackEntry("充电桩", "充电桩", FindPath(legacyRoot, "ChongDianZhuang_fuben").gameObject, buttons[1], true),
            CreateBackpackEntry("手电钻", "手电钻", FindPath(legacyRoot, "ShouDianZuan-CDZ").gameObject, buttons[2]),
            CreateBackpackEntry("膨胀管", "膨胀管", FindPath(legacyRoot, "ChongDianZhuang", "pengzhangluosi", "PengZhangLuoSi (1)").gameObject, buttons[3], true),
            CreateBackpackEntry("羊角锤", "羊角锤", FindPath(legacyRoot, "YangJiaoChui").gameObject, buttons[4]),
            CreateBackpackEntry("膨胀螺丝", "膨胀螺丝", FindPath(legacyRoot, "ChongDianZhuang", "pengzhangluosi", "luosi（1）").gameObject, buttons[5]),
            CreateBackpackEntry("扳手", "扳手", FindPath(legacyRoot, "banshou_2").gameObject, buttons[6]),
            CreateBackpackEntry("WiFi天线", "WiFi天线", FindPath(legacyRoot, "ChongDianZhuang", "tianxian1").gameObject, buttons[7]),
            CreateBackpackEntry("电源插头", "电源插头", FindPath(legacyRoot, "ChongDianZhuang", "Chazuo").gameObject, buttons[8])
        });
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(panel);
        EditorUtility.SetDirty(backpackRoot.gameObject);
    }

    private static TexCore_08BackpackController.Entry CreateBackpackEntry(
        string key, string label, GameObject source, GameObject button,
        bool keepSourceVisible = false)
    {
        return new TexCore_08BackpackController.Entry
        {
            key = key,
            label = label,
            source = source,
            button = button,
            keepSourceVisible = keepSourceVisible
        };
    }

    private static void ConfigureOperate(string relativeDirectory, int score, string[] tasks,
        string[] hints)
    {
        string path = BusinessRoot + "/" + relativeDirectory + ".prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Panel_Operate panel = root.GetComponent<Panel_Operate>();
            if (panel == null)
            {
                throw new InvalidOperationException("操作题缺少 Panel_Operate：" + path);
            }

            panel.topicData.topicType = TopicType.模型操作;
            panel.topicData.score = score;
            panel.topicData.tasks = tasks.ToList();
            panel.topicData.rightTasks = string.Join("_", tasks);
            panel.topicData.hintTexts = hints.Length == 1
                ? Enumerable.Repeat(hints[0], tasks.Length).ToList()
                : hints.ToList();
            panel.topicData.hintAudios = Enumerable.Repeat(string.Empty, hints.Length).ToList();

            TexCore_StepVoiceProfile profile = root.GetComponent<TexCore_StepVoiceProfile>();
            if (profile == null)
            {
                profile = root.AddComponent<TexCore_StepVoiceProfile>();
            }

            profile.stepAudioClips = TexCore_StepVoiceBindings.ResolveStepAudioClips(
                relativeDirectory, tasks.Length);
            EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(panel);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void Ensure08UnpackTopicPrefab()
    {
        string topicPath = Module08Path + "/" + Unpack08TopicName + ".prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(topicPath) == null)
        {
            string templatePath = Module08Path + "/01粘贴二维码.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(templatePath);
            try
            {
                Panel_Operate panel = root.GetComponent<Panel_Operate>();
                if (panel == null)
                {
                    throw new InvalidOperationException("08 操作题模板缺少 Panel_Operate。");
                }

                root.name = Unpack08TopicName;
                panel.datas.name = Unpack08TopicName;
                EditorUtility.SetDirty(panel);
                PrefabUtility.SaveAsPrefabAsset(root, topicPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        string nodePath = BusinessRoot + "/08充电桩安装.prefab";
        GameObject nodeRoot = PrefabUtility.LoadPrefabContents(nodePath);
        try
        {
            Aa_Node node = nodeRoot.GetComponent<Aa_Node>();
            if (node == null || node.datas == null)
            {
                throw new InvalidOperationException("08 节点 Prefab 缺少 Aa_Node 数据。");
            }

            if (node.datas.childData == null)
            {
                node.datas.childData = new List<TexCore_DataClass>();
            }

            if (!node.datas.childData.Any(value => value != null && value.name == Unpack08TopicName))
            {
                int introIndex = node.datas.childData.FindIndex(value => value != null && value.name == "00介绍视频");
                int insertIndex = introIndex >= 0 ? introIndex + 1 : 0;
                node.datas.childData.Insert(insertIndex, new TexCore_DataClass
                {
                    name = Unpack08TopicName,
                    selfDatas = new List<SelfDataBase>(),
                    childData = new List<TexCore_DataClass>()
                });
                EditorUtility.SetDirty(node);
                PrefabUtility.SaveAsPrefabAsset(nodeRoot, nodePath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(nodeRoot);
        }
    }

    private static void Ensure08UnpackTopicInStepTree()
    {
        TexCore_StepObj tree = UnityEngine.Object.FindObjectOfType<TexCore_StepObj>(true);
        if (tree == null)
        {
            throw new InvalidOperationException("场景缺少 TexCore_StepObj。");
        }

        NodeObjs node = tree.interacterObjs.FirstOrDefault(value =>
            value.nodePrefab != null && value.nodePrefab.name == "08充电桩安装");
        if (node == null)
        {
            throw new InvalidOperationException("步骤树缺少节点：08充电桩安装");
        }

        GameObject topicPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            Module08Path + "/" + Unpack08TopicName + ".prefab");
        if (topicPrefab == null)
        {
            throw new InvalidOperationException("找不到 08 开箱取桩 Topic Prefab。");
        }

        int introIndex = node.nodeObjs.FindIndex(value =>
            value.topicPrefab != null &&
            AssetDatabase.GetAssetPath(value.topicPrefab) == Module08Path + "/00介绍视频.prefab");
        int unpackIndex = node.nodeObjs.FindIndex(value => value.topicPrefab == topicPrefab);
        if (unpackIndex >= 0)
        {
            if (introIndex >= 0 && unpackIndex != introIndex + 1)
            {
                TopicObjs unpackTopic = node.nodeObjs[unpackIndex];
                node.nodeObjs.RemoveAt(unpackIndex);
                if (unpackIndex < introIndex)
                {
                    introIndex--;
                }
                node.nodeObjs.Insert(introIndex + 1, unpackTopic);
                EditorUtility.SetDirty(tree);
            }
            return;
        }

        int insertIndex = introIndex >= 0 ? introIndex + 1 : 0;
        node.nodeObjs.Insert(insertIndex, new TopicObjs
        {
            topicPrefab = topicPrefab,
            topicObjs = new List<StepObjs>()
        });
        EditorUtility.SetDirty(tree);
    }

    private static void Remove09KnowledgeTopic()
    {
        string nodePath = BusinessRoot + "/" + Module09Name + ".prefab";
        GameObject nodeRoot = PrefabUtility.LoadPrefabContents(nodePath);
        try
        {
            Aa_Node node = nodeRoot.GetComponent<Aa_Node>();
            if (node == null || node.datas == null || node.datas.childData == null)
            {
                throw new InvalidOperationException("09 节点 Prefab 缺少 Aa_Node 题目数据。");
            }

            if (node.datas.childData.RemoveAll(value =>
                    value != null && value.name == M09KnowledgeTopicName) > 0)
            {
                EditorUtility.SetDirty(node);
                PrefabUtility.SaveAsPrefabAsset(nodeRoot, nodePath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(nodeRoot);
        }

        TexCore_StepObj tree = UnityEngine.Object.FindObjectOfType<TexCore_StepObj>(true);
        if (tree == null)
        {
            throw new InvalidOperationException("场景缺少 TexCore_StepObj。");
        }

        NodeObjs module = tree.interacterObjs.FirstOrDefault(value =>
            value.nodePrefab != null && value.nodePrefab.name == Module09Name);
        if (module == null || module.nodeObjs == null)
        {
            throw new InvalidOperationException("步骤树缺少 09 机器人入场节点。");
        }

        if (module.nodeObjs.RemoveAll(value => value != null && value.topicPrefab != null &&
                value.topicPrefab.name == M09KnowledgeTopicName) > 0)
        {
            EditorUtility.SetDirty(tree);
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(M09KnowledgeTopicPath) != null &&
            !AssetDatabase.DeleteAsset(M09KnowledgeTopicPath))
        {
            throw new InvalidOperationException("无法删除 09 知识点答题 Prefab。");
        }
    }

    private static void ConfigureStepTree()
    {
        TexCore_StepObj stepTree = UnityEngine.Object.FindObjectOfType<TexCore_StepObj>(true);
        if (stepTree == null)
        {
            throw new InvalidOperationException("场景缺少 TexCore_StepObj。");
        }
        EditorUtility.SetDirty(stepTree);
    }

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

    private static void SetTopicSteps(string nodeName, string topicName, List<GameObject> steps)
    {
        TexCore_StepObj tree = UnityEngine.Object.FindObjectOfType<TexCore_StepObj>(true);
        NodeObjs node = tree.interacterObjs.FirstOrDefault(value => value.nodePrefab != null && value.nodePrefab.name == nodeName);
        if (node == null)
        {
            throw new InvalidOperationException("步骤树缺少节点：" + nodeName);
        }
        TopicObjs topic = node.nodeObjs.FirstOrDefault(value => value.topicPrefab != null && value.topicPrefab.name == topicName);
        if (topic == null)
        {
            throw new InvalidOperationException("步骤树缺少题目：" + topicName);
        }

        topic.topicObjs = new List<StepObjs>();
        for (int i = 0; i < steps.Count; i++)
        {
            topic.topicObjs.Add(new StepObjs { stepIndex = i, stepObjs = new List<GameObject> { steps[i] } });
        }
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
        if (existing == null)
        {
            return;
        }
        UnityEngine.Object.DestroyImmediate(existing.gameObject);
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

        // Prefer the expected direct child when duplicate names exist in
        // legacy scenes (for example AGV配网 appears both under UI-指引 and
        // as the actual scene interaction root). Recursive lookup remains a
        // fallback for paths whose legacy hierarchy contains an extra wrapper.
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (NamesEqual(child.name, name))
            {
                return child;
            }
        }

        return parent.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(value => value != parent && NamesEqual(value.name, name));
    }

    private static Transform FindDirectChild(Transform parent, string name, int occurrence)
    {
        if (parent == null || occurrence < 0)
        {
            throw new InvalidOperationException("09 AGV 配网控件路径无效：" + name);
        }

        int matches = 0;
        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (!NamesEqual(child.name, name))
            {
                continue;
            }

            if (matches == occurrence)
            {
                return child;
            }

            matches++;
        }

        throw new InvalidOperationException("找不到 09 AGV 配网子控件：" + name +
            "，序号：" + occurrence);
    }

    private static bool NamesEqual(string left, string right)
    {
        return string.Equals(NormalizeName(left), NormalizeName(right), StringComparison.Ordinal);
    }

    private static string NormalizeName(string value)
    {
        return (value ?? string.Empty).Replace(" ", string.Empty).Replace("　", string.Empty);
    }
}
#endif
