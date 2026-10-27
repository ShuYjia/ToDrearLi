using Bpf.Diagnostics;
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-time, repeat-safe migration of the current BPF server-network operation
/// objects from 03 into 04. It deliberately copies only the current BPF
/// implementation and never imports the legacy WHY_FuWuQi-6 module.
/// </summary>
internal static class TexCore_ServerNetwork04Migration
{
    private const string SourceScenePath = "Assets/A_MRNet/Sence/03SingleplayerScene.unity";
    private const string DestinationScenePath = "Assets/A_MRNet/Sence/04SingleplayerScene.unity";

    private const string InteractionRootName = "有交互物体";
    private const string SystemName = "01货到人拣选电商仓储实施安装与调试实训系统";
    private const string ServerNodeName = "01服务器网络搭建";
    private const string HardwareOperationName = "04硬件连接操作";
    private const string PowerOnName = "05开启服务器";
    private const string StepObjectsName = "StepObjs";

    [MenuItem("BPF/04 Server Network/Migrate Current BPF Core From 03")]
    public static void MigrateCurrentBpfCoreFrom03()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene destinationScene = EditorSceneManager.OpenScene(DestinationScenePath, OpenSceneMode.Single);
        Scene sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);

        try
        {
            Transform sourceNetwork = FindRequiredPath(sourceScene, InteractionRootName, SystemName, ServerNodeName);
            Transform destinationInteractionRoot = FindRequiredPath(destinationScene, InteractionRootName);
            Transform destinationSystem = FindOrCreateSystemRoot(destinationInteractionRoot, sourceNetwork.parent);

            if (FindDirectChild(destinationSystem, ServerNodeName) != null)
            {
                throw new InvalidOperationException(
                    $"04 already contains '{ServerNodeName}'. The migration will not duplicate an existing server task root.");
            }

            GameObject copiedNetwork = UnityEngine.Object.Instantiate(sourceNetwork.gameObject);
            copiedNetwork.name = ServerNodeName;
            SceneManager.MoveGameObjectToScene(copiedNetwork, destinationScene);
            copiedNetwork.transform.SetParent(destinationSystem, false);

            CopyServerStepTree(sourceScene, destinationScene, sourceNetwork, copiedNetwork.transform);

            EditorSceneManager.MarkSceneDirty(destinationScene);
            EditorSceneManager.SaveScene(destinationScene);

            ValidateLoadedDestination(destinationScene, true);
            BpfLog.Info(BpfLogCategory.Diagnostics, "tex_core_server_network04_migration.migrate_current_bpf_core_from03", "04 server-network BPF core was migrated from 03.");
        }
        finally
        {
            EditorSceneManager.CloseScene(sourceScene, true);
        }
    }

    [MenuItem("BPF/04 Server Network/Validate Current BPF Core")]
    public static void ValidateCurrentBpfCore()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene destinationScene = EditorSceneManager.OpenScene(DestinationScenePath, OpenSceneMode.Single);
        ValidateLoadedDestination(destinationScene, true);
    }

    private static Transform FindOrCreateSystemRoot(Transform destinationInteractionRoot, Transform sourceSystemRoot)
    {
        Transform destinationSystemRoot = FindDirectChild(destinationInteractionRoot, SystemName);
        if (destinationSystemRoot != null)
        {
            return destinationSystemRoot;
        }

        GameObject systemRoot = new GameObject(SystemName);
        Transform transform = systemRoot.transform;
        transform.SetParent(destinationInteractionRoot, false);
        transform.localPosition = sourceSystemRoot.localPosition;
        transform.localRotation = sourceSystemRoot.localRotation;
        transform.localScale = sourceSystemRoot.localScale;
        systemRoot.SetActive(sourceSystemRoot.gameObject.activeSelf);
        return transform;
    }

    private static void CopyServerStepTree(
        Scene sourceScene,
        Scene destinationScene,
        Transform sourceNetwork,
        Transform destinationNetwork)
    {
        TexCore_StepObj sourceStepObjects = FindStepObjects(sourceScene);
        TexCore_StepObj destinationStepObjects = FindStepObjects(destinationScene);
        NodeObjs sourceServerNode = sourceStepObjects.interacterObjs
            .FirstOrDefault(node => node.nodePrefab != null && node.nodePrefab.name == ServerNodeName);

        if (sourceServerNode == null)
        {
            throw new InvalidOperationException($"03 StepObjs does not contain '{ServerNodeName}'.");
        }

        NodeObjs copiedNode = CopyNode(sourceServerNode, sourceNetwork, destinationNetwork);
        int destinationNodeIndex = destinationStepObjects.interacterObjs.FindIndex(
            node => node.nodePrefab == sourceServerNode.nodePrefab ||
                    (node.nodePrefab != null && node.nodePrefab.name == ServerNodeName));

        if (destinationNodeIndex < 0)
        {
            destinationStepObjects.interacterObjs.Insert(0, copiedNode);
        }
        else
        {
            destinationStepObjects.interacterObjs[destinationNodeIndex] = copiedNode;
        }

        destinationStepObjects.activeInteracter = null;
        EditorUtility.SetDirty(destinationStepObjects);
    }

    private static NodeObjs CopyNode(NodeObjs sourceNode, Transform sourceNetwork, Transform destinationNetwork)
    {
        NodeObjs copiedNode = new NodeObjs
        {
            nodePrefab = sourceNode.nodePrefab,
            nodeObjs = new List<TopicObjs>()
        };

        foreach (TopicObjs sourceTopic in sourceNode.nodeObjs)
        {
            TopicObjs copiedTopic = new TopicObjs
            {
                topicPrefab = sourceTopic.topicPrefab,
                topicObjs = new List<StepObjs>()
            };

            foreach (StepObjs sourceStep in sourceTopic.topicObjs)
            {
                StepObjs copiedStep = new StepObjs
                {
                    stepIndex = sourceStep.stepIndex,
                    stepObjs = sourceStep.stepObjs
                        .Select(stepObject => FindCopiedObject(stepObject, sourceNetwork, destinationNetwork))
                        .ToList()
                };
                copiedTopic.topicObjs.Add(copiedStep);
            }

            copiedNode.nodeObjs.Add(copiedTopic);
        }

        return copiedNode;
    }

    private static GameObject FindCopiedObject(
        GameObject sourceObject,
        Transform sourceNetwork,
        Transform destinationNetwork)
    {
        if (sourceObject == null)
        {
            throw new InvalidOperationException("03 StepObjs contains an empty step-object reference.");
        }

        string relativePath = GetRelativePath(sourceNetwork, sourceObject.transform);
        Transform copiedTransform = string.IsNullOrEmpty(relativePath)
            ? destinationNetwork
            : destinationNetwork.Find(relativePath);

        if (copiedTransform == null)
        {
            throw new InvalidOperationException(
                $"Could not map 03 step object '{sourceObject.name}' to its 04 copy at '{relativePath}'.");
        }

        return copiedTransform.gameObject;
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
                $"'{child.name}' is outside the source server-network hierarchy.");
        }

        parts.Reverse();
        return string.Join("/", parts);
    }

    private static TexCore_StepObj FindStepObjects(Scene scene)
    {
        GameObject stepObjects = scene.GetRootGameObjects()
            .FirstOrDefault(gameObject => gameObject.name == StepObjectsName);
        TexCore_StepObj component = stepObjects != null
            ? stepObjects.GetComponent<TexCore_StepObj>()
            : null;

        if (component == null)
        {
            throw new InvalidOperationException($"Scene '{scene.path}' has no '{StepObjectsName}/TexCore_StepObj'.");
        }

        return component;
    }

    private static Transform FindRequiredPath(Scene scene, params string[] names)
    {
        if (names == null || names.Length == 0)
        {
            throw new ArgumentException("A scene path must contain at least one name.", nameof(names));
        }

        Transform current = scene.GetRootGameObjects()
            .Select(gameObject => gameObject.transform)
            .FirstOrDefault(transform => transform.name == names[0]);

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

    private static bool ValidateLoadedDestination(Scene destinationScene, bool logResult)
    {
        List<string> errors = new List<string>();
        Transform serverNetwork = null;
        try
        {
            serverNetwork = FindRequiredPath(destinationScene, InteractionRootName, SystemName, ServerNodeName);
        }
        catch (InvalidOperationException exception)
        {
            errors.Add(exception.Message);
        }

        if (serverNetwork != null)
        {
            int orderedStepCount = serverNetwork.GetComponentsInChildren<TexCore_OrderedPlacementStep>(true).Length;
            if (orderedStepCount != 9)
            {
                errors.Add($"Expected 9 TexCore_OrderedPlacementStep components, found {orderedStepCount}.");
            }

            foreach (TexCore_OrderedPlacementStep step in serverNetwork.GetComponentsInChildren<TexCore_OrderedPlacementStep>(true))
            {
                if (step.Target == null)
                {
                    errors.Add($"'{step.name}' has no ordered-placement target.");
                }
            }

            int buttonStepCount = serverNetwork.GetComponentsInChildren<TexCore_PressableButtonStep>(true).Length;
            if (buttonStepCount != 1)
            {
                errors.Add($"Expected 1 TexCore_PressableButtonStep component, found {buttonStepCount}.");
            }
        }

        try
        {
            TexCore_StepObj stepObjects = FindStepObjects(destinationScene);
            NodeObjs serverNode = stepObjects.interacterObjs
                .FirstOrDefault(node => node.nodePrefab != null && node.nodePrefab.name == ServerNodeName);

            if (serverNode == null)
            {
                errors.Add($"StepObjs has no '{ServerNodeName}' node.");
            }
            else
            {
                TopicObjs hardwareTopic = serverNode.nodeObjs.FirstOrDefault(
                    topic => topic.topicPrefab != null && topic.topicPrefab.name == HardwareOperationName);
                TopicObjs powerOnTopic = serverNode.nodeObjs.FirstOrDefault(
                    topic => topic.topicPrefab != null && topic.topicPrefab.name == PowerOnName);

                if (hardwareTopic == null || hardwareTopic.topicObjs.Count != 9)
                {
                    errors.Add($"'{HardwareOperationName}' must contain 9 mapped steps.");
                }

                if (powerOnTopic == null || powerOnTopic.topicObjs.Count != 1)
                {
                    errors.Add($"'{PowerOnName}' must contain one mapped step.");
                }
            }
        }
        catch (InvalidOperationException exception)
        {
            errors.Add(exception.Message);
        }

        if (errors.Count == 0)
        {
            if (logResult)
            {
                BpfLog.Info(BpfLogCategory.Diagnostics, "tex_core_server_network04_migration.validate_loaded_destination", "04 server-network BPF core validation passed.");
            }

            return true;
        }

        string message = "04 server-network BPF core validation failed:\n- " + string.Join("\n- ", errors);
        if (logResult)
        {
            BpfLog.Error(BpfLogCategory.Diagnostics, "tex_core_server_network04_migration.validate_loaded_destination", null, null, BpfLogField.Of("message", message));
        }

        return false;
    }
}
#endif
