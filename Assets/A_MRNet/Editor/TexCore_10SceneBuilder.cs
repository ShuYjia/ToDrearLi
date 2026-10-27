using Bpf.Diagnostics;
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Builds the current-project M10 rack-entry interaction in 03SingleplayerScene.
/// Legacy changjing-4 objects remain visual/reference content; legacy workflow
/// scripts are not copied into the current runtime.
/// </summary>
internal static class TexCore_10SceneBuilder
{
    private const string ScenePath = "Assets/A_MRNet/Sence/03SingleplayerScene.unity";
    private const string BusinessRoot =
        "Assets/Resources/BusinessPrefabs/MR混合现实多人协作平台/01货到人拣选电商仓储实施安装与调试实训系统";
    private const string SystemName = "01货到人拣选电商仓储实施安装与调试实训系统";
    private const string LegacyRackPath =
        "Assets/APP/OldMRTK/changjing-4/Support/GameMain/Entity/HuoJia-4.prefab";
    private const string CurrentRackPath = "Assets/A_MRNet/Models/TexCore_M10RackCarrier.prefab";
    private const string OperationTask = "10_机器人搬运到指定货架点";
    private const string OperationTopic = "02机器人搬运至货架点";
    private const string EntryQrName = "10_货架入场二维码";

    [MenuItem("BPF/TexCore/构建 10 货架入场")]
    private static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            throw new InvalidOperationException("找不到 03SingleplayerScene.unity。");
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform interactiveRoot = FindPath("有交互物体", SystemName, "10货架入场");
        Transform legacyRoot = FindPath("无交互物体", SystemName, "10货架入场", "changjing-4");
        Transform legacyModule = legacyRoot.parent;

        ClearGenerated(interactiveRoot, "TexCore_10Steps");
        GameObject robot = FindPath(legacyRoot, "MR_ChangJing", "AGV1").gameObject;
        List<GameObject> targets = legacyRoot.GetComponentsInChildren<Transform>(true)
            .Where(value => value != legacyRoot && value.CompareTag("huojia"))
            .Select(value => value.gameObject)
            .OrderBy(value => NumericName(value.name))
            .ThenBy(value => value.name, StringComparer.Ordinal)
            .ToList();

        if (targets.Count == 0)
        {
            throw new InvalidOperationException("M10 静态场景缺少 huojia 目标点。");
        }

        EnsureMovable(robot, true);
        ConfigureM10Robot(robot);
        Transform spawnMarker = FindName(legacyRoot, "colliderxs");
        if (spawnMarker == null)
        {
            throw new InvalidOperationException("M10 缺少货架生成点 colliderxs。");
        }

        Transform container = CreateContainer(interactiveRoot, "TexCore_10Steps");
        Transform spawnPoint = CreateSpawnPoint(container, spawnMarker);
        GameObject rack = CreateCurrentRack(container, spawnPoint);
        TexCore_RackEntryWorkflowStep workflow = rack.AddComponent<TexCore_RackEntryWorkflowStep>();
        workflow.Configure(robot, targets, spawnPoint, 0.24f);
        ConfigureEntryQr(container, legacyRoot, workflow);

        GameObject gateObject = CreateReporter(container, "完成整个模块_10");
        TexCore_ModuleCompletionGateStep gate = gateObject.AddComponent<TexCore_ModuleCompletionGateStep>();
        gate.Configure(new[] { workflow }, "请先完成 50 个货架目标点的搬运，再提交完成状态。");
        gate.SetReturnTopicEntry(OperationTopic);

        ConfigureM10TopicPrefabs();
        // Intro and knowledge topics intentionally keep their empty step
        // lists; only physical operation topics need scene-object bindings.
        SetTopicSteps("10货架入场", "02机器人搬运至货架点", new List<GameObject> { rack });
        SetTopicSteps("10货架入场", "03完成整个模块", new List<GameObject> { gateObject });

        // Keep the historical no-interaction hierarchy state unchanged. The
        // business flow activates the selected module at runtime, matching 08/09.
        EditorUtility.SetDirty(interactiveRoot);
        EditorUtility.SetDirty(legacyModule);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        BpfLog.Info(BpfLogCategory.Diagnostics, "tex_core_10_scene_builder.build", $"TexCore M10 构建完成：{targets.Count} 个目标点，操作 10 分，完成 10 分。");
    }

    private static void ConfigureM10TopicPrefabs()
    {
        ConfigureSelect(
            "10货架入场/01服务器设计主要目的",
            30,
            "货架入场时，正确的搬运顺序是什么？",
            new[]
            {
                "先将货架放到二维码上方，再让机器人接驳并搬运到指定货架点",
                "直接关闭所有目标点，再启动机器人",
                "只移动货架，不需要机器人接驳",
                "先拆除场景中的所有目标点，再开始搬运"
            },
            "先将货架放到二维码上方，再让机器人接驳并搬运到指定货架点");

        ConfigureOperate(
            "10货架入场/02机器人搬运至货架点",
            10,
            new[] { OperationTask },
            new[]
            {
                "将货架放到二维码上方并接驳机器人；控制机器人依次到达高亮货架点，完成全部 50 个目标点。"
            });

        ConfigureOperate(
            "10货架入场/03完成整个模块",
            10,
            new[] { "完成整个模块_10" },
            new[] { "确认全部货架目标点均已完成，再提交本模块。" });

        ConfigureTopicGuidance("10货架入场/00介绍视频", TexCore_M10TopicGuidance.GuidanceMode.Video);
        ConfigureTopicGuidance("10货架入场/01服务器设计主要目的", TexCore_M10TopicGuidance.GuidanceMode.Select);
    }

    /// <summary>
    /// Attaches the topic-level focus guide. The video topic only has a skip
    /// button, and the select topic highlights the option area, then 确定, then
    /// 下一步 without ever pointing at a specific answer.
    /// </summary>
    private static void ConfigureTopicGuidance(string relativeDirectory,
        TexCore_M10TopicGuidance.GuidanceMode mode)
    {
        string path = BusinessRoot + "/" + relativeDirectory + ".prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            TexCore_M10TopicGuidance guidance = root.GetComponent<TexCore_M10TopicGuidance>();
            if (guidance == null)
            {
                guidance = root.AddComponent<TexCore_M10TopicGuidance>();
            }

            guidance.Configure(mode);
            EditorUtility.SetDirty(guidance);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureSelect(string relativeDirectory, int score, string hint,
        string[] options, string correct)
    {
        string path = BusinessRoot + "/" + relativeDirectory + ".prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Panel_Select panel = root.GetComponent<Panel_Select>();
            if (panel == null)
            {
                throw new InvalidOperationException("选择题缺少 Panel_Select：" + path);
            }

            panel.topicData.topicType = TopicType.文本选择;
            panel.topicData.score = score;
            panel.topicData.tasks = options.ToList();
            panel.topicData.rightTasks = correct;
            panel.topicData.hintTexts = new List<string> { hint };
            panel.topicData.hintAudios = new List<string> { string.Empty };
            panel.selfData.selecionCount = SelecionCount.单选;

            TexCore_StepVoiceProfile profile = root.GetComponent<TexCore_StepVoiceProfile>();
            if (profile == null)
            {
                profile = root.AddComponent<TexCore_StepVoiceProfile>();
            }

            // A selection topic has options, not steps, so it owns no slots.
            profile.Resize(0);
            EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(panel);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
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

    private static GameObject CreateCurrentRack(Transform container, Transform spawnPoint)
    {
        GameObject currentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CurrentRackPath);
        if (currentPrefab == null)
        {
            GameObject legacyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LegacyRackPath);
            if (legacyPrefab == null)
            {
                throw new InvalidOperationException("找不到旧货架模型 Prefab：" + LegacyRackPath);
            }

            EnsureAssetFolder("Assets/A_MRNet/Models");
            GameObject assetRoot = PrefabUtility.InstantiatePrefab(legacyPrefab) as GameObject;
            if (assetRoot == null)
            {
                throw new InvalidOperationException("无法实例化旧货架模型。");
            }

            PrefabUtility.UnpackPrefabInstance(assetRoot, PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
            StripLegacyComponents(assetRoot);
            assetRoot.name = "TexCore_M10RackCarrier";
            EnsureMovable(assetRoot, true);
            currentPrefab = PrefabUtility.SaveAsPrefabAsset(assetRoot, CurrentRackPath);
            UnityEngine.Object.DestroyImmediate(assetRoot);
            AssetDatabase.SaveAssets();
        }

        GameObject rack = PrefabUtility.InstantiatePrefab(currentPrefab, container) as GameObject;
        if (rack == null)
        {
            throw new InvalidOperationException("无法实例化当前货架模型。");
        }

        rack.name = OperationTask;
        rack.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        rack.SetActive(true);
        return rack;
    }

    private static void StripLegacyComponents(GameObject root)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
            foreach (MonoBehaviour component in child.GetComponents<MonoBehaviour>())
            {
                if (component == null)
                {
                    continue;
                }

                string typeName = component.GetType().FullName ?? string.Empty;
                if (typeName.StartsWith("Changjing_4.", StringComparison.Ordinal))
                {
                    UnityEngine.Object.DestroyImmediate(component, true);
                }
            }
        }
    }

    /// <summary>
    /// Offset applied to the legacy colliderxs marker when placing the reused
    /// rack. The marker sits inside AGV1's collider, so a rack released there
    /// docked to the robot in the same frame and the player never got the
    /// "drag the rack into the robot" step. The marker is also 0.31 m too high:
    /// its Y made the rack float, while the target points rest at Y 1.15.
    /// </summary>
    private static readonly Vector3 SpawnPointOffset = new Vector3(0f, -0.31f, 0.59f);

    private static Transform CreateSpawnPoint(Transform container, Transform source)
    {
        GameObject point = new GameObject("10_货架生成点");
        point.transform.SetParent(container, true);
        point.transform.SetPositionAndRotation(source.position + SpawnPointOffset, source.rotation);
        point.SetActive(true);
        return point.transform;
    }

    /// <summary>
    /// Builds the single M10 entry QR code from the module's own legacy code
    /// marker. The player has to place the rack on it before the AGV may dock, so
    /// the workflow needs both the anchor to align the rack to and the area
    /// collider that bounds where the code counts.
    /// </summary>
    private static void ConfigureEntryQr(Transform container, Transform legacyRoot,
        TexCore_RackEntryWorkflowStep workflow)
    {
        Transform source = FindName(legacyRoot, "ErWeiMa7");
        if (source == null)
        {
            throw new InvalidOperationException("M10 缺少入场二维码 ErWeiMa7。");
        }

        Transform existing = FindName(container, EntryQrName);
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }

        GameObject root = new GameObject(EntryQrName);
        root.transform.SetParent(container, true);
        root.transform.SetPositionAndRotation(source.position, source.rotation);
        root.layer = source.gameObject.layer;

        GameObject areaObject = new GameObject("Area");
        areaObject.transform.SetParent(root.transform, false);
        areaObject.layer = root.layer;
        BoxCollider area = areaObject.AddComponent<BoxCollider>();
        area.isTrigger = true;
        area.size = new Vector3(0.8f, 0.2f, 0.8f);
        area.center = Vector3.zero;

        // Copy the mesh and material instead of the legacy object, so no legacy
        // component travels with the marker.
        GameObject mark = new GameObject("Mark");
        mark.transform.SetParent(root.transform, false);
        mark.layer = root.layer;
        MeshFilter filter = mark.AddComponent<MeshFilter>();
        MeshRenderer renderer = mark.AddComponent<MeshRenderer>();
        MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
        MeshRenderer sourceRenderer = source.GetComponent<MeshRenderer>();
        filter.sharedMesh = sourceFilter != null ? sourceFilter.sharedMesh : null;
        renderer.sharedMaterial = sourceRenderer != null ? sourceRenderer.sharedMaterial : null;

        workflow.SetEntryQr(root.transform, area);
    }

    private static void EnsureMovable(GameObject source, bool addRigidbody)
    {
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

    private static void ConfigureM10Robot(GameObject robot)
    {
        if (robot == null)
        {
            throw new InvalidOperationException("M10 机器人为空。");
        }

        Rigidbody body = robot.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = robot.AddComponent<Rigidbody>();
        }

        // AGV1 is a floor vehicle. Keep player-driven motion authoritative and
        // prevent physics/controller impulses from lifting or tipping it.
        body.useGravity = false;
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        body.constraints = RigidbodyConstraints.FreezePositionY |
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;
        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        ObjectManipulator manipulator = robot.GetComponent<ObjectManipulator>();
        if (manipulator == null)
        {
            manipulator = robot.AddComponent<ObjectManipulator>();
        }

        manipulator.AllowedManipulations = TransformFlags.Move | TransformFlags.Rotate;
        manipulator.RigidbodyMovementType = XRBaseInteractable.MovementType.VelocityTracking;
        manipulator.AllowedInteractionTypes |=
            InteractionFlags.Near | InteractionFlags.Ray | InteractionFlags.Generic;

        Collider robotCollider = robot.GetComponent<Collider>();
        if (robotCollider != null && !manipulator.colliders.Contains(robotCollider))
        {
            manipulator.colliders.Add(robotCollider);
        }
    }

    private static void SetTopicSteps(string nodeName, string topicName, List<GameObject> steps)
    {
        TexCore_StepObj tree = UnityEngine.Object.FindObjectOfType<TexCore_StepObj>(true);
        if (tree == null)
        {
            throw new InvalidOperationException("场景缺少 TexCore_StepObj。");
        }

        NodeObjs node = tree.interacterObjs.FirstOrDefault(value =>
            value.nodePrefab != null && value.nodePrefab.name == nodeName);
        if (node == null)
        {
            throw new InvalidOperationException("步骤树缺少节点：" + nodeName);
        }

        TopicObjs topic = node.nodeObjs.FirstOrDefault(value =>
            value.topicPrefab != null && value.topicPrefab.name == topicName);
        if (topic == null)
        {
            throw new InvalidOperationException("步骤树缺少题目：" + topicName);
        }

        topic.topicObjs = new List<StepObjs>();
        for (int i = 0; i < steps.Count; i++)
        {
            topic.topicObjs.Add(new StepObjs
            {
                stepIndex = i,
                stepObjs = new List<GameObject> { steps[i] }
            });
        }
        EditorUtility.SetDirty(tree);
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

    private static GameObject CreateReporter(Transform container, string name)
    {
        GameObject reporter = new GameObject(name);
        reporter.transform.SetParent(container, false);
        reporter.SetActive(true);
        return reporter;
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

    private static bool NamesEqual(string left, string right)
    {
        return string.Equals(left, right, StringComparison.Ordinal) ||
            string.Equals(left.Trim(), right.Trim(), StringComparison.Ordinal);
    }

    private static int NumericName(string value)
    {
        string digits = new string(value.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out int result) ? result : int.MaxValue;
    }

    private static void EnsureAssetFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        int slash = path.LastIndexOf('/');
        string parent = slash > 0 ? path.Substring(0, slash) : "Assets";
        string name = path.Substring(slash + 1);
        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
