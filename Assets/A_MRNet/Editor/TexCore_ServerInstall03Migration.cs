using Bpf.Diagnostics;
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Migrates the current TexCore implementation of M02 server installation
/// from the validated 04 scene into 03. The historical WHY_FuWuQi-6 scene is
/// deliberately not imported, so Mirror, SycObj and PlayerPrefs task logic
/// cannot leak into the current business flow.
/// </summary>
internal static class TexCore_ServerInstall03Migration
{
    private const string SourceScenePath = "Assets/A_MRNet/Sence/04SingleplayerScene.unity";
    private const string DestinationScenePath = "Assets/A_MRNet/Sence/03SingleplayerScene.unity";
    private const string InteractionRootName = "有交互物体";
    private const string SystemName = "01货到人拣选电商仓储实施安装与调试实训系统";
    private const string InstallRootName = "02服务器系统安装";
    private const string StepObjectsName = "StepObjs";
    private const string TopicRootPath =
        "Assets/Resources/BusinessPrefabs/MR混合现实多人协作平台/01货到人拣选电商仓储实施安装与调试实训系统/02服务器系统安装";
    private const string UsbTopicName = "02U盘启动盘插入";
    private const string InstallTopicName = "03系统安装操作";
    private const string IpTopicName = "04设置正确IP";
    private const string CompletionTopicName = "05完成整个模块";
    private const string UsbReporterName = "SYS_USB_01_TakeU";
    private const string IpReporterName = "SYS_IP_01_Configure";
    private const string CompletionReporterName = "M02_完成整个模块";

    [MenuItem("BPF/03 Server Install/Migrate Current M02 From 04")]
    public static void MigrateCurrentM02From04()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene destinationScene = EditorSceneManager.OpenScene(DestinationScenePath, OpenSceneMode.Single);
        Scene sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);

        try
        {
            Transform sourceSystem = FindRequiredPath(sourceScene, InteractionRootName, SystemName);
            Transform destinationSystem = FindRequiredPath(destinationScene, InteractionRootName, SystemName);
            Transform sourceInstall = FindDescendant(sourceSystem, InstallRootName);
            if (sourceInstall == null)
            {
                throw new InvalidOperationException(
                    $"04 does not contain '{InteractionRootName}/{SystemName}/{InstallRootName}'.");
            }

            Transform destinationParent = MapRelativeTransform(
                sourceSystem, destinationSystem, sourceInstall.parent);
            if (destinationParent == null)
            {
                throw new InvalidOperationException(
                    $"03 does not contain the parent hierarchy for '{InstallRootName}'.");
            }

            Transform destinationInstall = FindOrCopyInstallRoot(
                destinationParent, sourceInstall, destinationScene);
            CopyM02StepTree(sourceScene, destinationScene, sourceInstall, destinationInstall);
            ConfigureM02TopicScores();
            RegenerateConfig();

            EditorSceneManager.MarkSceneDirty(destinationScene);
            EditorSceneManager.SaveScene(destinationScene);
            if (!ValidateLoadedDestination(destinationScene, out List<string> errors))
            {
                throw new InvalidOperationException(
                    "03 M02 server-install migration validation failed:\n- " +
                    string.Join("\n- ", errors));
            }

            BpfLog.Info(BpfLogCategory.Diagnostics, "tex_core_server_install03_migration.migrate_current_m02_from04", "03 M02 server-install TexCore flow was migrated from 04.");
        }
        finally
        {
            EditorSceneManager.CloseScene(sourceScene, true);
        }
    }

    [MenuItem("BPF/03 Server Install/Validate Current M02")]
    public static void ValidateCurrentM02()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(DestinationScenePath, OpenSceneMode.Single);
        if (!ValidateLoadedDestination(scene, out List<string> errors))
        {
            BpfLog.Error(BpfLogCategory.Diagnostics, "tex_core_server_install03_migration.validate_current_m02", "03 M02 server-install validation failed:\n- " +
                           string.Join("\n- ", errors));
            return;
        }

        BpfLog.Info(BpfLogCategory.Diagnostics, "tex_core_server_install03_migration.validate_current_m02", "03 M02 server-install TexCore validation passed.");
    }

    private static Transform FindOrCopyInstallRoot(
        Transform destinationSystem,
        Transform sourceInstall,
        Scene destinationScene)
    {
        Transform existing = FindDirectChild(destinationSystem, InstallRootName);
        if (existing != null)
        {
            bool alreadyMigrated = existing.GetComponentInChildren<TexCore_InteracterBase>(true) != null ||
                                    FindDirectChild(existing, "BPF_ServerInstallScreen") != null ||
                                    FindDirectChild(existing, "BPF_ServerInstallScreenActivator") != null;
            if (alreadyMigrated)
            {
                return existing;
            }

            if (existing.childCount != 0 || existing.GetComponents<Component>().Length != 1)
            {
                throw new InvalidOperationException(
                    $"03 already has a non-empty '{InstallRootName}' root; refusing to overwrite it.");
            }

            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        GameObject copied = UnityEngine.Object.Instantiate(sourceInstall.gameObject);
        copied.name = InstallRootName;
        SceneManager.MoveGameObjectToScene(copied, destinationScene);
        copied.transform.SetParent(destinationSystem, false);
        return copied.transform;
    }

    private static void CopyM02StepTree(
        Scene sourceScene,
        Scene destinationScene,
        Transform sourceInstall,
        Transform destinationInstall)
    {
        TexCore_StepObj sourceSteps = FindStepObjects(sourceScene);
        TexCore_StepObj destinationSteps = FindStepObjects(destinationScene);
        NodeObjs sourceNode = sourceSteps.interacterObjs.FirstOrDefault(
            node => node.nodePrefab != null && node.nodePrefab.name == InstallRootName);
        if (sourceNode == null)
        {
            throw new InvalidOperationException($"04 StepObjs has no '{InstallRootName}' node.");
        }

        List<string> topicOrder = GetTopicOrder(sourceNode.nodePrefab);
        NodeObjs copiedNode = CopyNode(sourceNode, sourceInstall, destinationInstall, topicOrder);
        BindSyntheticOperationStep(copiedNode, UsbTopicName,
            EnsureReporter(destinationInstall, UsbReporterName));
        GameObject ipReporter = EnsureReporter(destinationInstall, IpReporterName);
        BindSyntheticOperationStep(copiedNode, IpTopicName, ipReporter);

        List<TexCore_InteracterBase> requirements = CollectTopicInteracters(
            copiedNode, InstallTopicName);
        requirements.Add(copiedNode.nodeObjs
            .First(topic => topic.topicPrefab != null && topic.topicPrefab.name == UsbTopicName)
            .topicObjs[0].stepObjs[0].GetComponent<TexCore_InteracterBase>());
        requirements.Add(ipReporter.GetComponent<TexCore_InteracterBase>());

        GameObject completionReporter = EnsureReporter(destinationInstall, CompletionReporterName);
        TexCore_ModuleCompletionGateStep gate = completionReporter
            .GetComponent<TexCore_ModuleCompletionGateStep>();
        if (gate == null)
        {
            gate = completionReporter.AddComponent<TexCore_ModuleCompletionGateStep>();
        }

        gate.Configure(requirements, "请先完成U盘插入、系统安装和正确IP配置，再提交本模块。");
        BindSyntheticOperationStep(copiedNode, CompletionTopicName, completionReporter);

        int destinationIndex = destinationSteps.interacterObjs.FindIndex(
            node => node.nodePrefab == sourceNode.nodePrefab ||
                   (node.nodePrefab != null && node.nodePrefab.name == InstallRootName));
        if (destinationIndex < 0)
        {
            destinationSteps.interacterObjs.Add(copiedNode);
        }
        else
        {
            destinationSteps.interacterObjs[destinationIndex] = copiedNode;
        }

        destinationSteps.activeInteracter = null;
        EditorUtility.SetDirty(destinationSteps);
        EditorUtility.SetDirty(completionReporter);
        AssetDatabase.SaveAssets();
    }

    private static NodeObjs CopyNode(
        NodeObjs sourceNode,
        Transform sourceRoot,
        Transform destinationRoot,
        IReadOnlyList<string> topicOrder)
    {
        NodeObjs result = new NodeObjs
        {
            nodePrefab = sourceNode.nodePrefab,
            nodeObjs = new List<TopicObjs>()
        };

        for (int topicIndex = 0; topicIndex < topicOrder.Count; topicIndex++)
        {
            string topicName = topicOrder[topicIndex];
            // Historical 04 StepObjs entries can retain their authored step
            // lists while losing the topicPrefab reference. Resolve by name
            // first, then fall back to the node childData position so those
            // serialized steps are not silently discarded during migration.
            TopicObjs sourceTopic = sourceNode.nodeObjs.FirstOrDefault(
                topic => topic != null && topic.topicPrefab != null &&
                         topic.topicPrefab.name == topicName);
            if (sourceTopic == null)
            {
                sourceTopic = ResolveLegacyTopicByShape(sourceNode, topicName, topicIndex);
            }
            TopicObjs topic = new TopicObjs
            {
                topicPrefab = sourceTopic != null && sourceTopic.topicPrefab != null
                    ? sourceTopic.topicPrefab
                    : LoadTopicPrefab(topicName),
                topicObjs = new List<StepObjs>()
            };

            if (sourceTopic != null && sourceTopic.topicObjs != null)
            {
                foreach (StepObjs sourceStep in sourceTopic.topicObjs.OrderBy(step => step.stepIndex))
                {
                    StepObjs step = new StepObjs
                    {
                        stepIndex = topic.topicObjs.Count,
                        stepObjs = sourceStep.stepObjs == null
                            ? new List<GameObject>()
                            : sourceStep.stepObjs.Select(
                                item => FindCopiedObject(item, sourceRoot, destinationRoot)).ToList()
                    };
                    topic.topicObjs.Add(step);
                }
            }

            result.nodeObjs.Add(topic);
        }

        return result;
    }

    private static TopicObjs ResolveLegacyTopicByShape(
        NodeObjs sourceNode,
        string topicName,
        int topicIndex)
    {
        if (sourceNode == null || sourceNode.nodeObjs == null)
        {
            return null;
        }

        // The 04 scene contains an obsolete quiz topic and several topicPrefab
        // references that deserialize as null. The authored step lists remain
        // usable, so identify the two migrated operational topics by their
        // stable step-list shapes instead of assuming the old array index.
        int expectedStepCount = topicName == UsbTopicName
            ? 2
            : topicName == InstallTopicName
                ? 9
                : -1;
        if (expectedStepCount > 0)
        {
            TopicObjs shaped = sourceNode.nodeObjs.FirstOrDefault(topic =>
                topic != null && topic.topicObjs != null &&
                topic.topicObjs.Count == expectedStepCount);
            if (shaped != null)
            {
                return shaped;
            }
        }

        // For synthetic topics (IP/completion) there is no authored source
        // step list to preserve; using the same-position entry is harmless and
        // keeps any future non-null references deterministic.
        return topicIndex < sourceNode.nodeObjs.Count
            ? sourceNode.nodeObjs[topicIndex]
            : null;
    }

    private static List<string> GetTopicOrder(GameObject nodePrefab)
    {
        TexCore_PlaneBase nodePlane = nodePrefab != null
            ? nodePrefab.GetComponent<TexCore_PlaneBase>()
            : null;
        List<string> names = nodePlane != null && nodePlane.datas != null &&
                             nodePlane.datas.childData != null
            ? nodePlane.datas.childData
                .Where(data => data != null && !string.IsNullOrEmpty(data.name))
                .Select(data => data.name)
                .ToList()
            : new List<string>();

        if (names.Count == 0)
        {
            throw new InvalidOperationException(
                $"Node prefab '{nodePrefab?.name}' has no childData topic order.");
        }

        return names;
    }

    private static GameObject LoadTopicPrefab(string topicName)
    {
        GameObject topicPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            $"{TopicRootPath}/{topicName}.prefab");
        if (topicPrefab == null)
        {
            throw new InvalidOperationException($"Missing M02 topic Prefab '{topicName}'.");
        }

        return topicPrefab;
    }

    private static GameObject EnsureReporter(Transform installRoot, string name)
    {
        Transform existing = FindDirectChild(installRoot, name);
        GameObject reporter = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null)
        {
            reporter.transform.SetParent(installRoot, false);
        }

        reporter.SetActive(true);
        if (name == CompletionReporterName)
        {
            if (reporter.GetComponent<TexCore_ModuleCompletionGateStep>() == null)
            {
                reporter.AddComponent<TexCore_ModuleCompletionGateStep>();
            }
        }
        else if (reporter.GetComponent<TexCore_InteracterEvent>() == null)
        {
            reporter.AddComponent<TexCore_InteracterEvent>();
        }

        return reporter;
    }

    private static void BindSyntheticOperationStep(
        NodeObjs node,
        string topicName,
        GameObject stepObject)
    {
        TopicObjs topic = node.nodeObjs.FirstOrDefault(
            item => item.topicPrefab != null && item.topicPrefab.name == topicName);
        if (topic == null)
        {
            throw new InvalidOperationException($"M02 node is missing topic '{topicName}'.");
        }

        topic.topicObjs = new List<StepObjs>
        {
            new StepObjs
            {
                stepIndex = 0,
                stepObjs = new List<GameObject> { stepObject }
            }
        };
    }

    private static List<TexCore_InteracterBase> CollectTopicInteracters(
        NodeObjs node,
        string topicName)
    {
        TopicObjs topic = node.nodeObjs.FirstOrDefault(
            item => item.topicPrefab != null && item.topicPrefab.name == topicName);
        if (topic == null || topic.topicObjs == null)
        {
            return new List<TexCore_InteracterBase>();
        }

        return topic.topicObjs
            .Where(step => step != null && step.stepObjs != null)
            .SelectMany(step => step.stepObjs)
            .Where(stepObject => stepObject != null)
            .SelectMany(stepObject => stepObject.GetComponents<TexCore_InteracterBase>())
            .Distinct()
            .ToList();
    }

    private static GameObject FindCopiedObject(
        GameObject sourceObject,
        Transform sourceRoot,
        Transform destinationRoot)
    {
        if (sourceObject == null)
        {
            throw new InvalidOperationException("04 M02 StepObjs contains a null step-object reference.");
        }

        string relativePath = GetRelativePath(sourceRoot, sourceObject.transform);
        Transform copied = string.IsNullOrEmpty(relativePath)
            ? destinationRoot
            : destinationRoot.Find(relativePath);
        if (copied == null)
        {
            throw new InvalidOperationException(
                $"Could not map M02 step object '{sourceObject.name}' at '{relativePath}'.");
        }

        return copied.gameObject;
    }

    private static bool ValidateLoadedDestination(Scene scene, out List<string> errors)
    {
        errors = new List<string>();
        Transform installRoot = null;
        try
        {
            Transform system = FindRequiredPath(scene, InteractionRootName, SystemName);
            installRoot = FindDescendant(system, InstallRootName);
            if (installRoot == null)
            {
                errors.Add($"Missing '{InstallRootName}' interaction root.");
            }
        }
        catch (InvalidOperationException exception)
        {
            errors.Add(exception.Message);
        }

        if (installRoot != null)
        {
            if (FindDirectChild(installRoot, "BPF_ServerInstallScreen") == null)
            {
                errors.Add("BPF_ServerInstallScreen is missing.");
            }

            if (FindDirectChild(installRoot, "BPF_ServerInstallScreenActivator") == null)
            {
                errors.Add("BPF_ServerInstallScreenActivator is missing.");
            }

            int systemStepCount = installRoot.GetComponentsInChildren<TexCore_InteracterBase>(true).Length;
            if (systemStepCount < 9)
            {
                errors.Add($"Expected at least 9 current TexCore interaction steps, found {systemStepCount}.");
            }
        }

        try
        {
            TexCore_StepObj steps = FindStepObjects(scene);
            NodeObjs node = steps.interacterObjs.FirstOrDefault(
                item => item.nodePrefab != null && item.nodePrefab.name == InstallRootName);
            if (node == null)
            {
                errors.Add($"StepObjs has no '{InstallRootName}' node.");
            }
            else
            {
                List<string> topicOrder = GetTopicOrder(node.nodePrefab);
                if (node.nodeObjs.Count != topicOrder.Count)
                {
                    errors.Add($"M02 node has {node.nodeObjs.Count} topics; expected {topicOrder.Count}.");
                }

                for (int topicIndex = 0; topicIndex < topicOrder.Count; topicIndex++)
                {
                    string expectedTopicName = topicOrder[topicIndex];
                    TopicObjs topic = topicIndex < node.nodeObjs.Count
                        ? node.nodeObjs[topicIndex]
                        : null;
                    if (topic == null || topic.topicPrefab == null ||
                        topic.topicPrefab.name != expectedTopicName)
                    {
                        errors.Add($"M02 topic index {topicIndex} is not '{expectedTopicName}'.");
                        continue;
                    }

                    List<StepObjs> mappedSteps = topic.topicObjs ?? new List<StepObjs>();
                    PanelTopicBase topicBehaviour = topic.topicPrefab != null
                        ? topic.topicPrefab.GetComponent<PanelTopicBase>()
                        : null;
                    int expected = topicBehaviour != null && topicBehaviour.topicData != null &&
                                   topicBehaviour.topicData.tasks != null
                        ? topicBehaviour.topicData.tasks.Count
                        : mappedSteps.Count;
                    List<int> indexes = mappedSteps.Select(step => step.stepIndex).ToList();
                    for (int index = 0; index < indexes.Count; index++)
                    {
                        if (indexes[index] != index)
                        {
                            errors.Add($"Topic '{topic.topicPrefab?.name}' has non-continuous stepIndex.");
                            break;
                        }
                    }

                    int expectedMappedCount = ExpectedMappedStepCount(expectedTopicName);
                    if (mappedSteps.Count != expectedMappedCount)
                    {
                        errors.Add($"Topic '{topic.topicPrefab?.name}' has {mappedSteps.Count} mapped steps; expected {expectedMappedCount}.");
                    }

                    if (expected > 0 && topicBehaviour == null)
                    {
                        errors.Add($"Topic '{topic.topicPrefab?.name}' has no PanelTopicBase component.");
                    }
                }

                ValidateM02TopicScores(errors);

                if (FindDirectChild(installRoot, CompletionReporterName) == null ||
                    FindDirectChild(installRoot, CompletionReporterName)
                        .GetComponent<TexCore_ModuleCompletionGateStep>() == null)
                {
                    errors.Add("M02 completion topic has no TexCore_ModuleCompletionGateStep.");
                }
            }
        }
        catch (InvalidOperationException exception)
        {
            errors.Add(exception.Message);
        }

        return errors.Count == 0;
    }

    private static int ExpectedMappedStepCount(string topicName)
    {
        switch (topicName)
        {
            case UsbTopicName:
            case IpTopicName:
            case CompletionTopicName:
                return 1;
            case InstallTopicName:
                return 9;
            default:
                return 0;
        }
    }

    private static void ConfigureM02TopicScores()
    {
        ConfigureTopicScore(UsbTopicName, 10);
        ConfigureTopicScore(InstallTopicName, 30);
        ConfigureTopicScore(IpTopicName, 30);
        ConfigureTopicScore(CompletionTopicName, 100);
    }

    private static void ConfigureTopicScore(string topicName, int score)
    {
        string path = $"{TopicRootPath}/{topicName}.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        if (root == null)
        {
            throw new InvalidOperationException($"Could not load M02 topic Prefab '{path}'.");
        }

        try
        {
            PanelTopicBase panel = root.GetComponent<PanelTopicBase>();
            if (panel == null || panel.topicData == null)
            {
                throw new InvalidOperationException($"Topic Prefab '{path}' has no topicData.");
            }

            panel.topicData.score = score;
            EditorUtility.SetDirty(panel);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ValidateM02TopicScores(List<string> errors)
    {
        Dictionary<string, int> expected = new Dictionary<string, int>
        {
            { UsbTopicName, 10 },
            { InstallTopicName, 30 },
            { IpTopicName, 30 },
            { CompletionTopicName, 100 }
        };

        foreach (KeyValuePair<string, int> item in expected)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{TopicRootPath}/{item.Key}.prefab");
            PanelTopicBase panel = prefab != null ? prefab.GetComponent<PanelTopicBase>() : null;
            if (panel == null || panel.topicData == null)
            {
                errors.Add($"M02 topic Prefab '{item.Key}' has no topicData.");
            }
            else if (panel.topicData.score != item.Value)
            {
                errors.Add($"M02 topic '{item.Key}' score is {panel.topicData.score}; expected {item.Value}.");
            }
        }
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

        TexCore_DataClass system = config.datas.Find(SystemName);
        TexCore_DataClass module = system != null ? system.Find(InstallRootName) : null;
        if (module != null && module.childData != null)
        {
            module.childData.RemoveAll(topic => topic != null &&
                topic.name == TexCore_M02TopicFlow.KnowledgeQuizTopic);
            module.SetParent();
        }

        manager.datas = config;
        manager.ReadDataFromPrefabSingle(config.datas, manager.prefabsPath);
        XmlSerializer.SaveToXml(configPath, config);
    }

    private static TexCore_StepObj FindStepObjects(Scene scene)
    {
        GameObject root = scene.GetRootGameObjects().FirstOrDefault(item => item.name == StepObjectsName);
        TexCore_StepObj result = root != null
            ? root.GetComponent<TexCore_StepObj>()
            : null;
        if (result == null)
        {
            throw new InvalidOperationException($"Scene '{scene.path}' has no '{StepObjectsName}/TexCore_StepObj'.");
        }

        return result;
    }

    private static Transform FindRequiredPath(Scene scene, params string[] names)
    {
        Transform current = scene.GetRootGameObjects()
            .Select(item => item.transform)
            .FirstOrDefault(item => item.name == names[0]);
        if (current == null)
        {
            throw new InvalidOperationException($"Scene '{scene.path}' has no root '{names[0]}'.");
        }

        for (int index = 1; index < names.Length; index++)
        {
            current = FindDirectChild(current, names[index]);
            if (current == null)
            {
                throw new InvalidOperationException(
                    $"Scene '{scene.path}' has no hierarchy path '{string.Join("/", names)}'.");
            }
        }

        return current;
    }

    private static Transform FindDirectChild(Transform parent, string name)
    {
        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (child.name == name)
            {
                return child;
            }
        }

        return null;
    }

    private static Transform FindDescendant(Transform parent, string name)
    {
        if (parent == null)
        {
            return null;
        }

        if (parent.name == name)
        {
            return parent;
        }

        for (int index = 0; index < parent.childCount; index++)
        {
            Transform result = FindDescendant(parent.GetChild(index), name);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static Transform MapRelativeTransform(
        Transform sourceRoot,
        Transform destinationRoot,
        Transform sourceTransform)
    {
        if (sourceTransform == null)
        {
            return null;
        }

        string relativePath = GetRelativePath(sourceRoot, sourceTransform);
        return string.IsNullOrEmpty(relativePath)
            ? destinationRoot
            : destinationRoot.Find(relativePath);
    }

    private static string GetRelativePath(Transform root, Transform child)
    {
        List<string> parts = new List<string>();
        Transform current = child;
        while (current != null && current != root)
        {
            parts.Add(current.name);
            current = current.parent;
        }

        if (current != root)
        {
            throw new InvalidOperationException(
                $"M02 step object '{child.name}' is outside '{root.name}'.");
        }

        parts.Reverse();
        return string.Join("/", parts);
    }
}
#endif
