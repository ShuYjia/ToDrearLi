using Bpf.Diagnostics;
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Replaces the 08 and 09 training modules in the mainline 03 scene with the
/// authored module content of the per-module source scenes:
///   08 charging-station installation  &lt;- Assets/A_MRNet/Sence/0809.unity
///   09 robot entry                    &lt;- Assets/A_MRNet/Sence/09.unity
///
/// Only the module subtrees, the 08 backpack chapter panel and the
/// TexCore_08BackpackController bindings are transplanted. The 03 scene
/// framework (XR rig, cameras, canvases, TexCore managers, StepObjs policy and
/// the other modules) stays untouched, and the generated Config.xml is rebuilt
/// from the final scene through the project TexCore_ConfigManager.
/// </summary>
internal static class TexCore_0809SceneMigration
{
    private const string DestinationScenePath = "Assets/A_MRNet/Sence/03SingleplayerScene.unity";
    private const string Module08SourceScenePath = "Assets/A_MRNet/Sence/0809.unity";
    private const string Module09SourceScenePath = "Assets/A_MRNet/Sence/09.unity";

    private const string InteractionRootName = "有交互物体";
    private const string PassiveRootName = "无交互物体";
    private const string SystemName = "01货到人拣选电商仓储实施安装与调试实训系统";
    private const string Module08Name = "08充电桩安装";
    private const string Module09Name = "09机器人入场";
    private const string BackpackRootName = "背包UI";
    private const string BackpackPanel08Name = "08充电桩安装背包面板";
    private const string StepObjectsName = "StepObjs";
    private const string TooltipTopicName = "00开箱取出充电桩";

    private static readonly string[] Module08StepNames =
    {
        "08_00_使用手电钻钻开包装箱螺丝1", "08_00_使用手电钻钻开包装箱螺丝2",
        "08_00_使用手电钻钻开包装箱螺丝3", "08_00_使用手电钻钻开包装箱螺丝4",
        "08_00_使用手电钻钻开包装箱螺丝5", "08_00_使用手电钻钻开包装箱螺丝6",
        "08_00_使用手电钻钻开包装箱螺丝7", "08_00_使用手电钻钻开包装箱螺丝8",
        "08_00_移开包装箱盖", "08_00_取出箱内充电桩",
        "08_01_粘贴二维码", "08_02_安装充电桩", "08_03_打开充电桩前盖",
        "08_04_钻孔1", "08_04_钻孔2", "08_04_钻孔3", "08_04_钻孔4",
        "08_08_安装膨胀管1", "08_08_安装膨胀管2", "08_08_安装膨胀管3", "08_08_安装膨胀管4",
        "08_12_锤击固定",
        "08_13_安装膨胀螺丝1", "08_13_安装膨胀螺丝2", "08_13_安装膨胀螺丝3", "08_13_安装膨胀螺丝4",
        "08_17_扳手紧固",
        "08_18_安装WiFi天线1", "08_19_安装WiFi天线2",
        "08_20_连接电源", "08_21_连接网线", "08_22_充电桩开机",
        "08_23_AGV配网", "08_24_RCS操作", "完成整个模块_08"
    };

    private static readonly string[] Module09StepNames =
    {
        "09_01_手电钻拆除固定钉1", "09_01_手电钻拆除固定钉2",
        "09_01_手电钻拆除固定钉3", "09_01_手电钻拆除固定钉4",
        "09_01_手电钻拆除固定钉5", "09_01_手电钻拆除固定钉6",
        "09_01_手电钻拆除固定钉7", "09_01_手电钻拆除固定钉8",
        "09_09_移开箱盖", "09_10_推出机器人",
        "09_11_检查左碰撞检测器", "09_12_检查右碰撞检测器", "09_13_按下机器人开机键",
        "09_14_AGV配网完成", "09_15_机器人移动至二维码",
        "完成整个模块_09"
    };

    private static readonly string[] Module08BackpackEntries =
    {
        "二维码", "充电桩", "手电钻", "膨胀管", "羊角锤", "膨胀螺丝", "扳手", "WiFi天线", "电源插头"
    };

    /// <summary>Diagnostics of the last migration attempt (menu or scripted).</summary>
    internal static string LastReport = string.Empty;

    [MenuItem("BPF/Migration/Migrate 08 and 09 Modules Into 03")]
    public static void MigrateModulesInto03()
    {
        MigrateInternal(false);
    }

    [MenuItem("BPF/Migration/Validate 03 Module Migration")]
    public static void Validate03ModuleMigration()
    {
        MigrateInternal(true);
    }

    /// <summary>Read-only structural comparison used while proving the migration.</summary>
    internal static string Diagnose()
    {
        Scene destination = EditorSceneManager.OpenScene(DestinationScenePath, OpenSceneMode.Single);
        Scene source08 = EditorSceneManager.OpenScene(Module08SourceScenePath, OpenSceneMode.Additive);
        Scene source09 = EditorSceneManager.OpenScene(Module09SourceScenePath, OpenSceneMode.Additive);
        StringBuilder builder = new StringBuilder();
        try
        {
            DiagnoseRoot(builder, "03<-0809 有交互", RequirePath(destination, InteractionRootName, SystemName, Module08Name),
                RequirePath(source08, InteractionRootName, SystemName, Module08Name));
            DiagnoseRoot(builder, "03<-0809 无交互", RequirePath(destination, PassiveRootName, SystemName, Module08Name),
                RequirePath(source08, PassiveRootName, SystemName, Module08Name));
            DiagnoseRoot(builder, "03<-0809 背包面板", RequirePath(destination, BackpackRootName, BackpackPanel08Name),
                RequirePath(source08, BackpackRootName, BackpackPanel08Name));
            DiagnoseRoot(builder, "03<-09 有交互", RequirePath(destination, InteractionRootName, SystemName, Module09Name),
                RequirePath(source09, InteractionRootName, SystemName, Module09Name));
            DiagnoseRoot(builder, "03<-09 无交互", RequirePath(destination, PassiveRootName, SystemName, Module09Name),
                RequirePath(source09, PassiveRootName, SystemName, Module09Name));
        }
        finally
        {
            EditorSceneManager.CloseScene(source08, true);
            EditorSceneManager.CloseScene(source09, true);
        }

        return builder.ToString();
    }

    private static void DiagnoseRoot(StringBuilder builder, string label, Transform destination, Transform source)
    {
        string destinationSignature = SubtreeSignature(destination);
        string sourceSignature = SubtreeSignature(source);
        builder.AppendLine(string.Format(
            "{0}: dstKids={1} dstNodes={2} srcKids={3} srcNodes={4} equal={5}",
            label, destination.childCount, destination.GetComponentsInChildren<Transform>(true).Length,
            source.childCount, source.GetComponentsInChildren<Transform>(true).Length,
            destinationSignature == sourceSignature));
    }

    private static void MigrateInternal(bool validateOnly)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        List<string> changes = new List<string>();
        List<string> errors = new List<string>();
        bool sceneChanged = false;
        TransplantPlan plan = new TransplantPlan();
        try
        {
            if (validateOnly)
            {
                Scene destinationScene = EditorSceneManager.OpenScene(DestinationScenePath, OpenSceneMode.Single);
                Scene sourceScene08 = EditorSceneManager.OpenScene(Module08SourceScenePath, OpenSceneMode.Additive);
                Scene sourceScene09 = EditorSceneManager.OpenScene(Module09SourceScenePath, OpenSceneMode.Additive);
                try
                {
                    ValidateDestination(destinationScene, sourceScene08, sourceScene09, errors);
                }
                finally
                {
                    EditorSceneManager.CloseScene(sourceScene08, true);
                    EditorSceneManager.CloseScene(sourceScene09, true);
                }
            }
            else
            {
                plan.Collect(Module08SourceScenePath, Module08Name, BackpackPanel08Name, changes);
                plan.Collect(Module09SourceScenePath, Module09Name, null, changes);
                bool planted = plan.PlantInDestination(changes);
                if (planted)
                {
                    // StepObjs and the backpack controller keep their serialized
                    // bindings: they are re-pointed at the transplanted copies
                    // inside PlantInDestination.
                    sceneChanged = true;
                    Scene destinationScene = EditorSceneManager.GetActiveScene();
                    Scene sourceScene08 = EditorSceneManager.OpenScene(Module08SourceScenePath, OpenSceneMode.Additive);
                    Scene sourceScene09 = EditorSceneManager.OpenScene(Module09SourceScenePath, OpenSceneMode.Additive);
                    try
                    {
                        SynchronizeStepObjects(destinationScene, sourceScene08, sourceScene09, changes);
                        ValidateDestination(destinationScene, sourceScene08, sourceScene09, errors);
                    }
                    finally
                    {
                        EditorSceneManager.CloseScene(sourceScene08, true);
                        EditorSceneManager.CloseScene(sourceScene09, true);
                    }
                }

                LastReport = string.Join(" | ", plan.Report);
                changes.AddRange(plan.UnresolvedReferenceNotes);
                if (sceneChanged && errors.Count == 0)
                {
                    // The scene is written only after the migrated content has
                    // been proven, so a failed attempt never persists a broken 03.
                    Scene destinationScene = EditorSceneManager.GetActiveScene();
                    RegenerateConfig();
                    EditorSceneManager.MarkSceneDirty(destinationScene);
                    EditorSceneManager.SaveScene(destinationScene);
                    AssetDatabase.SaveAssets();
                }
            }
        }
        finally
        {
            plan.Cleanup(changes);
        }

        if (errors.Count > 0)
        {
            if (sceneChanged && !validateOnly)
            {
                // Drop the unproven in-memory edits by reloading the saved scene
                // instead of leaving a half-migrated 03 open.
                EditorSceneManager.OpenScene(DestinationScenePath, OpenSceneMode.Single);
            }

            throw new InvalidOperationException(
                "03 08/09 module migration validation failed (" + LastReport + "):\n- " +
                string.Join("\n- ", errors));
        }

        BpfLog.Info(BpfLogCategory.Diagnostics, "tex_core_0809_scene_migration.migrate_modules_into03",
            (validateOnly ? "Validated" : "Migrated") + " 03 08/09 modules from their source scenes. " +
            "changes=" + changes.Count + (changes.Count > 0 ? " [" + string.Join(", ", changes) + "]" : string.Empty));
    }

    // ----------------------------------------------------------------- helpers

    private static Transform FindRoot(Scene scene, string name)
    {
        GameObject root = scene.GetRootGameObjects().FirstOrDefault(item => item != null && item.name == name);
        return root != null ? root.transform : null;
    }

    private static Transform RequireRoot(Scene scene, string name)
    {
        Transform root = FindRoot(scene, name);
        if (root == null)
        {
            throw new InvalidOperationException(
                $"Scene '{scene.path}' has no root object '{name}'.");
        }

        return root;
    }

    private static Transform RequirePath(Scene scene, params string[] names)
    {
        Transform current = RequireRoot(scene, names[0]);
        for (int index = 1; index < names.Length; index++)
        {
            Transform next = FindDirectChild(current, names[index]);
            if (next == null)
            {
                throw new InvalidOperationException(
                    $"Scene '{scene.path}' has no hierarchy path '{string.Join("/", names)}'.");
            }

            current = next;
        }

        return current;
    }

    private static Transform FindDirectChild(Transform parent, string name)
    {
        if (parent == null)
        {
            return null;
        }

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

    private static bool IsDescendantOf(Transform candidate, Transform ancestor)
    {
        Transform current = candidate;
        while (current != null)
        {
            if (current == ancestor)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    /// <summary>
    /// One subtree that has to move from a source scene into 03. Unity nulls
    /// every serialized reference that would cross a scene boundary while the
    /// objects move, so the plan records the old -> new object map first and
    /// rewrites the surviving references afterwards.
/// <summary>
    /// The 08 and 09 module subtrees are rebuilt inside 03 from the authored
    /// content of their source scenes.
    ///
    /// Unity invalidates every serialized reference that would cross a scene
    /// boundary, and it also rewrites the 03 objects that point into a module
    /// (the step tree and the 08 backpack controller). The transplant therefore
    /// goes through a temporary prefab, which keeps the internal references of
    /// each subtree intact, and then re-points the 03 references at the copies
    /// through their recorded transform paths.
    /// </summary>
    private sealed class TransplantPlan
    {
        private const string TempPrefabFolder = "Assets/A_MRNet/Sence/MigrationTemp";

        private sealed class Region
        {
            public string label;
            public string destinationPath;
            public string sourceScenePath;
            public string prefabPath;
            public Transform destinationRoot;
            public GameObject plantedRoot;
        }

        private sealed class PendingReference
        {
            public MonoBehaviour owner;
            public string propertyPath;
            public Transform target;
        }

        private readonly List<Region> regions = new List<Region>();
        private readonly List<PendingReference> references = new List<PendingReference>();
        private readonly Dictionary<string, Transform> plantedByPath =
            new Dictionary<string, Transform>();
        private readonly List<string> unresolved = new List<string>();
        private readonly List<string> report = new List<string>();
        private readonly List<string> tempPrefabs = new List<string>();
        private int applied;

        public IReadOnlyList<string> UnresolvedReferenceNotes => unresolved;

        public IReadOnlyList<string> Report => report;

        /// <summary>
        /// First phase: export every outdated region of 03 out of its source
        /// scene into a temporary prefab asset.
        /// </summary>
        public void Collect(
            string sourceScenePath,
            string moduleName,
            string backpackPanelName,
            List<string> changes)
        {
            Scene source = EditorSceneManager.OpenScene(sourceScenePath, OpenSceneMode.Single);
            try
            {
                AddRegion(source, sourceScenePath, InteractionRootName, moduleName, changes);
                AddRegion(source, sourceScenePath, PassiveRootName, moduleName, changes);
                if (!string.IsNullOrEmpty(backpackPanelName))
                {
                    AddRegion(source, sourceScenePath, BackpackRootName, backpackPanelName, changes);
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(source, true);
            }
        }

        private void AddRegion(
            Scene source,
            string sourceScenePath,
            string parentPath,
            string regionName,
            List<string> changes)
        {
            Transform sourceRoot = RequirePath(source, parentPath, regionName);
            string destinationPath = parentPath + "/" + regionName;
            GameObject prefab = ExportAsTemporaryPrefab(sourceRoot, destinationPath);
            regions.Add(new Region
            {
                label = destinationPath,
                destinationPath = destinationPath,
                sourceScenePath = sourceScenePath,
                prefabPath = prefab != null ? AssetDatabase.GetAssetPath(prefab) : null
            });
            changes.Add(destinationPath);
        }

        private GameObject ExportAsTemporaryPrefab(Transform sourceRoot, string destinationPath)
        {
            if (!AssetDatabase.IsValidFolder(TempPrefabFolder))
            {
                AssetDatabase.CreateFolder("Assets/A_MRNet/Sence", "MigrationTemp");
            }

            GameObject clone = UnityEngine.Object.Instantiate(sourceRoot.gameObject);
            clone.name = sourceRoot.name;
            string fileName = destinationPath.Replace("/", "_") + ".prefab";
            string prefabPath = TempPrefabFolder + "/" + fileName;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(clone, prefabPath);
            UnityEngine.Object.DestroyImmediate(clone);
            AssetDatabase.SaveAssets();
            tempPrefabs.Add(prefabPath);
            return prefab;
        }

        /// <summary>
        /// Second phase: plant every collected region into 03. Returns true when
        /// 03 was changed.
        /// </summary>
        public bool PlantInDestination(List<string> changes)
        {
            Scene destination = EditorSceneManager.OpenScene(DestinationScenePath, OpenSceneMode.Single);
            List<Region> outdated = new List<Region>();
            foreach (Region region in regions)
            {
                region.destinationRoot = FindByPath(destination, region.destinationPath);
                if (region.destinationRoot == null)
                {
                    throw new InvalidOperationException(
                        $"03 has no '{region.destinationPath}' to replace.");
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(region.prefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        $"Temporary migration prefab '{region.prefabPath}' is missing.");
                }

                if (SubtreeSignature(region.destinationRoot) != SubtreeSignature(prefab.transform))
                {
                    outdated.Add(region);
                }
                else
                {
                    // Already current: the region stays exactly as it is, so its
                    // objects must be remapped to themselves.
                    RegisterIdentity(region.destinationRoot);
                    report.Add(region.label + ": already current");
                }
            }

            if (outdated.Count == 0)
            {
                return false;
            }

            CaptureReferences(destination, outdated);

            foreach (Region region in outdated)
            {
                Transform parent = region.destinationRoot.parent;
                int order = region.destinationRoot.GetSiblingIndex();
                Vector3 localPosition = region.destinationRoot.localPosition;
                Quaternion localRotation = region.destinationRoot.localRotation;
                Vector3 localScale = region.destinationRoot.localScale;
                string name = region.destinationRoot.name;

                UnityEngine.Object.DestroyImmediate(region.destinationRoot.gameObject);

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(region.prefabPath);
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab, destination) as GameObject;
                if (instance == null)
                {
                    throw new InvalidOperationException(
                        $"Could not instantiate '{region.prefabPath}' into 03.");
                }

                instance.name = name;
                SceneManager.MoveGameObjectToScene(instance, destination);
                instance.transform.SetParent(parent, false);
                instance.transform.localPosition = localPosition;
                instance.transform.localRotation = localRotation;
                instance.transform.localScale = localScale;
                instance.transform.SetSiblingIndex(order);
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
                region.plantedRoot = instance;
                IndexByPath(instance.transform);
                report.Add(region.label + ": transplanted nodes=" +
                           instance.GetComponentsInChildren<Transform>(true).Length);
            }

            ApplyReferences();
            report.Add("refs=" + references.Count + " applied=" + applied +
                       " unresolved=" + unresolved.Count);
            return true;
        }

        private void RegisterIdentity(Transform root)
        {
            foreach (Transform current in root.GetComponentsInChildren<Transform>(true))
            {
                plantedByPath[HierarchyPath(current)] = current;
            }
        }

        private void IndexByPath(Transform root)
        {
            foreach (Transform current in root.GetComponentsInChildren<Transform>(true))
            {
                plantedByPath[HierarchyPath(current)] = current;
            }
        }

        /// <summary>
        /// Records every 03 reference that points into a replaced region, keyed
        /// by the target's hierarchy path, before the old objects are destroyed.
        /// </summary>
        private void CaptureReferences(Scene destination, List<Region> outdated)
        {
            references.Clear();
            Dictionary<Transform, bool> insideOutdated = new Dictionary<Transform, bool>();
            foreach (Region region in outdated)
            {
                foreach (Transform current in region.destinationRoot.GetComponentsInChildren<Transform>(true))
                {
                    insideOutdated[current] = true;
                }
            }

            int scanned = 0;
            foreach (GameObject root in destination.GetRootGameObjects())
            {
                foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    // Only the project's own gameplay scripts can carry a module
                    // reference; framework and package components cannot.
                    if (behaviour == null ||
                        behaviour.GetType().Assembly.GetName().Name != "Assembly-CSharp")
                    {
                        continue;
                    }

                    scanned++;
                    SerializedObject serialized = new SerializedObject(behaviour);
                    SerializedProperty property = serialized.GetIterator();
                    bool enterChildren = true;
                    while (property.NextVisible(enterChildren))
                    {
                        enterChildren = false;
                        if (property.propertyType != SerializedPropertyType.ObjectReference)
                        {
                            continue;
                        }

                        UnityEngine.Object target = property.objectReferenceValue;
                        if (target == null)
                        {
                            continue;
                        }

                        Transform owner = OwnerTransform(target);
                        if (owner == null || !insideOutdated.ContainsKey(owner))
                        {
                            continue;
                        }

                        references.Add(new PendingReference
                        {
                            owner = behaviour,
                            propertyPath = property.propertyPath,
                            target = owner
                        });
                    }
                }
            }

            report.Add("scanned=" + scanned + " refs=" + references.Count +
                       " regions=" + outdated.Count);
        }

        /// <summary>
        /// Re-points the recorded references at the transplanted copies. A
        /// reference that already resolves to a copy is left untouched.
        /// </summary>
        private void ApplyReferences()
        {
            HashSet<string> unresolvedPaths = new HashSet<string>();
            foreach (PendingReference reference in references)
            {
                if (reference.owner == null)
                {
                    continue;
                }

                Transform replacement;
                if (!plantedByPath.TryGetValue(HierarchyPath(reference.target), out replacement))
                {
                    unresolvedPaths.Add(HierarchyPath(reference.target));
                    continue;
                }

                SerializedObject serialized = new SerializedObject(reference.owner);
                SerializedProperty property = serialized.FindProperty(reference.propertyPath);
                if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
                {
                    unresolvedPaths.Add(reference.owner.GetType().Name + "." + reference.propertyPath);
                    continue;
                }

                UnityEngine.Object current = property.objectReferenceValue;
                if (current != null && OwnerTransform(current) == replacement)
                {
                    continue;
                }

                property.objectReferenceValue = replacement.gameObject;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                applied++;
            }

            foreach (string path in unresolvedPaths)
            {
                unresolved.Add("unresolved transplanted reference: " + path);
            }
        }

        public void Cleanup(List<string> changes)
        {
            foreach (string prefabPath in tempPrefabs)
            {
                AssetDatabase.DeleteAsset(prefabPath);
            }

            tempPrefabs.Clear();
            if (AssetDatabase.IsValidFolder(TempPrefabFolder))
            {
                AssetDatabase.DeleteAsset(TempPrefabFolder);
            }

            AssetDatabase.Refresh();
        }

        private static Transform FindByPath(Scene scene, string path)
        {
            string[] parts = path.Split('/');
            Transform current = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == parts[0])
                {
                    current = root.transform;
                    break;
                }
            }

            for (int index = 1; index < parts.Length && current != null; index++)
            {
                Transform next = null;
                for (int child = 0; child < current.childCount; child++)
                {
                    if (current.GetChild(child).name == parts[index])
                    {
                        next = current.GetChild(child);
                        break;
                    }
                }

                current = next;
            }

            return current;
        }

        private static Transform OwnerTransform(UnityEngine.Object target)
        {
            GameObject gameObject = target as GameObject;
            if (gameObject != null)
            {
                return gameObject.transform;
            }

            Component component = target as Component;
            return component != null ? component.transform : null;
        }
    }


    private static void SynchronizeStepObjects(
        Scene destination,
        Scene source08,
        Scene source09,
        List<string> changes)
    {
        TexCore_StepObj destinationSteps = FindStepObjects(destination);
        TexCore_StepObj sourceSteps08 = FindStepObjects(source08);
        TexCore_StepObj sourceSteps09 = FindStepObjects(source09);

        bool changed = false;
        changed |= SynchronizeNode(destinationSteps, sourceSteps08, Module08Name);
        changed |= SynchronizeNode(destinationSteps, sourceSteps09, Module09Name);
        if (changed)
        {
            destinationSteps.activeInteracter = null;
            EditorUtility.SetDirty(destinationSteps);
            changes.Add(StepObjectsName + "/TexCore_StepObj.interacterObjs");
        }
    }

    private static bool SynchronizeNode(TexCore_StepObj destinationSteps, TexCore_StepObj sourceSteps, string moduleName)
    {
        int sourceIndex = sourceSteps.interacterObjs.FindIndex(
            node => node != null && node.nodePrefab != null && node.nodePrefab.name == moduleName);
        int destinationIndex = destinationSteps.interacterObjs.FindIndex(
            node => node != null && node.nodePrefab != null && node.nodePrefab.name == moduleName);
        if (sourceIndex < 0)
        {
            throw new InvalidOperationException(
                $"Source StepObjs has no '{moduleName}' node.");
        }

        NodeObjs sourceNode = sourceSteps.interacterObjs[sourceIndex];
        if (destinationIndex < 0)
        {
            destinationSteps.interacterObjs.Add(sourceNode);
            return true;
        }

        if (NodeEquals(destinationSteps.interacterObjs[destinationIndex], sourceNode))
        {
            return false;
        }

        destinationSteps.interacterObjs[destinationIndex] = sourceNode;
        return true;
    }

    private static bool NodeEquals(NodeObjs left, NodeObjs right)
    {
        if (left == null || right == null || left.nodePrefab != right.nodePrefab)
        {
            return false;
        }

        if (left.nodeObjs == null || right.nodeObjs == null || left.nodeObjs.Count != right.nodeObjs.Count)
        {
            return false;
        }

        for (int topicIndex = 0; topicIndex < left.nodeObjs.Count; topicIndex++)
        {
            TopicObjs leftTopic = left.nodeObjs[topicIndex];
            TopicObjs rightTopic = right.nodeObjs[topicIndex];
            if (leftTopic == null || rightTopic == null || leftTopic.topicPrefab != rightTopic.topicPrefab)
            {
                return false;
            }

            List<StepObjs> leftSteps = leftTopic.topicObjs ?? new List<StepObjs>();
            List<StepObjs> rightSteps = rightTopic.topicObjs ?? new List<StepObjs>();
            if (leftSteps.Count != rightSteps.Count)
            {
                return false;
            }

            for (int stepIndex = 0; stepIndex < leftSteps.Count; stepIndex++)
            {
                StepObjs leftStep = leftSteps[stepIndex];
                StepObjs rightStep = rightSteps[stepIndex];
                if (leftStep == null || rightStep == null ||
                    leftStep.stepIndex != rightStep.stepIndex ||
                    !SequenceEqual(leftStep.stepObjs, rightStep.stepObjs))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool SequenceEqual(List<GameObject> left, List<GameObject> right)
    {
        int leftCount = left != null ? left.Count : 0;
        int rightCount = right != null ? right.Count : 0;
        if (leftCount != rightCount)
        {
            return false;
        }

        for (int index = 0; index < leftCount; index++)
        {
            if (left[index] != right[index])
            {
                return false;
            }
        }

        return true;
    }

    private static TexCore_StepObj FindStepObjects(Scene scene)
    {
        Transform root = FindRoot(scene, StepObjectsName);
        TexCore_StepObj result = root != null ? root.GetComponent<TexCore_StepObj>() : null;
        if (result == null)
        {
            throw new InvalidOperationException(
                $"Scene '{scene.path}' has no '{StepObjectsName}/TexCore_StepObj'.");
        }

        return result;
    }

    /// <summary>
    /// Structural digest of a subtree. Scene-local object references are
    /// rewritten to the target's relative path so the digest can be compared
    /// across two scenes.
    /// </summary>
    internal static string SubtreeSignature(Transform root)
    {
        if (root == null)
        {
            return "<null>";
        }

        List<string> parts = new List<string>();
        foreach (Transform current in root.GetComponentsInChildren<Transform>(true))
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(RelativePath(root, current)).Append('|');
            builder.Append(current.gameObject.activeSelf ? '1' : '0').Append('|');
            builder.Append(current.gameObject.layer).Append('|');
            builder.Append(current.gameObject.tag).Append('|');
            Component[] components = current.GetComponents<Component>();
            List<string> blocks = new List<string>();
            foreach (Component component in components)
            {
                if (component == null)
                {
                    blocks.Add("<missing>");
                    continue;
                }

                if (component is Transform)
                {
                    continue;
                }

                SerializedObject serialized = new SerializedObject(component);
                StringBuilder block = new StringBuilder();
                block.Append(component.GetType().FullName);
                SerializedProperty property = serialized.GetIterator();
                bool enterChildren = true;
                while (property.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    block.Append('\n').Append(property.propertyPath).Append('=');
                    switch (property.propertyType)
                    {
                        case SerializedPropertyType.ObjectReference:
                            UnityEngine.Object reference = property.objectReferenceValue;
                            block.Append(DescribeReference(reference, root));
                            break;
                        case SerializedPropertyType.Generic:
                        case SerializedPropertyType.ArraySize:
                            break;
                        case SerializedPropertyType.Integer:
                        case SerializedPropertyType.LayerMask:
                        case SerializedPropertyType.Character:
                        case SerializedPropertyType.Enum:
                            block.Append(property.longValue);
                            break;
                        case SerializedPropertyType.Boolean:
                            block.Append(property.boolValue ? '1' : '0');
                            break;
                        case SerializedPropertyType.Float:
                            block.Append(property.doubleValue.ToString("R",
                                System.Globalization.CultureInfo.InvariantCulture));
                            break;
                        case SerializedPropertyType.String:
                            block.Append(property.stringValue);
                            break;
                        case SerializedPropertyType.Color:
                            block.Append(property.colorValue.ToString());
                            break;
                        case SerializedPropertyType.Vector2:
                            block.Append(property.vector2Value.ToString());
                            break;
                        case SerializedPropertyType.Vector3:
                            block.Append(property.vector3Value.ToString());
                            break;
                        case SerializedPropertyType.Vector4:
                            block.Append(property.vector4Value.ToString());
                            break;
                        case SerializedPropertyType.Rect:
                            block.Append(property.rectValue.ToString());
                            break;
                        case SerializedPropertyType.Quaternion:
                            block.Append(property.quaternionValue.ToString());
                            break;
                        case SerializedPropertyType.Bounds:
                            block.Append(property.boundsValue.ToString());
                            break;
                        default:
                            block.Append(property.propertyType);
                            break;
                    }
                }

                blocks.Add(block.ToString());
            }

            blocks.Sort(StringComparer.Ordinal);
            foreach (string block in blocks)
            {
                builder.Append('\n').Append(block);
            }

            parts.Add(builder.ToString());
        }

        parts.Sort(StringComparer.Ordinal);
        StringBuilder digest = new StringBuilder();
        foreach (string part in parts)
        {
            digest.Append(part).Append('\u0000');
        }

        return digest.ToString();
    }

    private static string DescribeReference(UnityEngine.Object reference, Transform root)
    {
        if (reference == null)
        {
            return "<null>";
        }

        GameObject gameObject = reference as GameObject;
        Component component = reference as Component;
        Transform transform = gameObject != null
            ? gameObject.transform
            : (component != null ? component.transform : null);
        if (transform == null)
        {
            return "<asset:" + reference.GetType().Name + ":" + reference.name + ">";
        }

        string suffix = gameObject != null ? string.Empty : "#" + reference.GetType().Name;
        return IsDescendantOf(transform, root)
            ? "in:" + RelativePath(root, transform) + suffix
            : "out:" + HierarchyPath(transform) + suffix;
    }

    private static string RelativePath(Transform root, Transform target)
    {
        if (root == target)
        {
            return string.Empty;
        }

        List<string> parts = new List<string>();
        Transform current = target;
        while (current != null && current != root)
        {
            parts.Add(current.name);
            current = current.parent;
        }

        parts.Reverse();
        return string.Join("/", parts);
    }

    private static string HierarchyPath(Transform target)
    {
        List<string> parts = new List<string>();
        Transform current = target;
        while (current != null)
        {
            parts.Add(current.name);
            current = current.parent;
        }

        parts.Reverse();
        return string.Join("/", parts);
    }

    // -------------------------------------------------------------- validation

    private static void ValidateDestination(
        Scene destination,
        Scene source08,
        Scene source09,
        List<string> errors)
    {
        ValidateModule(destination, source08, Module08Name, InteractionRootName, errors);
        ValidateModule(destination, source08, Module08Name, PassiveRootName, errors);
        ValidateModule(destination, source09, Module09Name, InteractionRootName, errors);
        ValidateModule(destination, source09, Module09Name, PassiveRootName, errors);

        ValidateModuleStepNames(destination, Module08Name, Module08StepNames, errors);
        ValidateModuleStepNames(destination, Module09Name, Module09StepNames, errors);

        ValidateSingleModuleRoot(destination, InteractionRootName, Module08Name, errors);
        ValidateSingleModuleRoot(destination, InteractionRootName, Module09Name, errors);
        ValidateSingleModuleRoot(destination, PassiveRootName, Module08Name, errors);
        ValidateSingleModuleRoot(destination, PassiveRootName, Module09Name, errors);

        ValidateModule08BoxLidGate(destination, errors);
        ValidateModule09NetworkControls(destination, source09, errors);
        ValidateModule09RobotBaseline(destination, errors);
        ValidateModule09QrTarget(destination, errors);
        ValidateBackpack08(destination, source08, errors);
        ValidateStepIndexes(destination, errors);
    }

    private static void ValidateModule(
        Scene destination,
        Scene source,
        string moduleName,
        string interactionRootName,
        List<string> errors)
    {
        Transform sourceRoot = RequirePath(source, interactionRootName, SystemName, moduleName);
        Transform destinationRoot = FindDirectChild(
            RequirePath(destination, interactionRootName, SystemName), moduleName);
        if (destinationRoot == null)
        {
            errors.Add($"'{interactionRootName}/{moduleName}' is missing in 03.");
            return;
        }

        string sourceSignature = SubtreeSignature(sourceRoot);
        string destinationSignature = SubtreeSignature(destinationRoot);
        if (sourceSignature != destinationSignature)
        {
            errors.Add(string.Format(
                "'{0}/{1}' does not match {2} (expected digest {3}, found {4}).",
                interactionRootName, moduleName,
                System.IO.Path.GetFileNameWithoutExtension(source.path),
                ShortHash(sourceSignature), ShortHash(destinationSignature)));
        }
    }

    private static string ShortHash(string value)
    {
        using (System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
            StringBuilder builder = new StringBuilder();
            for (int index = 0; index < 8; index++)
            {
                builder.Append(hash[index].ToString("x2"));
            }

            return builder.ToString();
        }
    }

    private static void ValidateSingleModuleRoot(
        Scene destination,
        string interactionRootName,
        string moduleName,
        List<string> errors)
    {
        Transform system = RequirePath(destination, interactionRootName, SystemName);
        int count = 0;
        for (int index = 0; index < system.childCount; index++)
        {
            if (system.GetChild(index).name == moduleName)
            {
                count++;
            }
        }

        if (count != 1)
        {
            errors.Add($"'{interactionRootName}/{SystemName}' contains {count} '{moduleName}' roots; expected 1.");
        }
    }

    private static void ValidateModuleStepNames(
        Scene destination,
        string moduleName,
        string[] expected,
        List<string> errors)
    {
        Transform steps = RequirePath(destination, InteractionRootName, SystemName, moduleName, "TexCore_" +
            (moduleName.StartsWith("08") ? "08" : "09") + "Steps");
        List<string> names = new List<string>();
        for (int index = 0; index < steps.childCount; index++)
        {
            names.Add(steps.GetChild(index).name);
        }

        if (names.Count != expected.Length)
        {
            errors.Add($"'{moduleName}' has {names.Count} step objects; expected {expected.Length}.");
            return;
        }

        for (int index = 0; index < expected.Length; index++)
        {
            if (names[index] != expected[index])
            {
                errors.Add($"'{moduleName}' step {index} is '{names[index]}'; expected '{expected[index]}'.");
            }
        }
    }

    private static void ValidateModule08BoxLidGate(Scene destination, List<string> errors)
    {
        Transform lid = null;
        foreach (Transform candidate in RequirePath(destination, PassiveRootName, SystemName, Module08Name)
                     .GetComponentsInChildren<Transform>(true))
        {
            if (candidate.name == "DingGai" && candidate.GetComponent<TexCore_M08BoxLidGate>() != null)
            {
                lid = candidate;
                break;
            }
        }

        if (lid == null)
        {
            errors.Add("08 box lid has no TexCore_M08BoxLidGate after the migration.");
        }
    }

    private static void ValidateModule09NetworkControls(Scene destination, Scene source09, List<string> errors)
    {
        Transform sourceNetwork = RequirePath(source09, PassiveRootName, SystemName, Module09Name,
            "ChangJing-8", "AGV配网");
        Transform destinationNetwork = RequirePath(destination, PassiveRootName, SystemName, Module09Name,
            "ChangJing-8", "AGV配网");
        if (SubtreeSignature(sourceNetwork) != SubtreeSignature(destinationNetwork))
        {
            errors.Add("09 'ChangJing-8/AGV配网' does not match 09.unity.");
        }

        TexCore_StepObj steps = FindStepObjects(destination);
        NodeObjs module = steps.interacterObjs.FirstOrDefault(
            node => node != null && node.nodePrefab != null && node.nodePrefab.name == Module09Name);
        TopicObjs networkTopic = module != null && module.nodeObjs != null
            ? module.nodeObjs.FirstOrDefault(
                topic => topic != null && topic.topicPrefab != null && topic.topicPrefab.name == "04AGV配网")
            : null;
        TexCore_PassiveUIEventStep networkStep = networkTopic != null && networkTopic.topicObjs != null &&
                                                 networkTopic.topicObjs.Count > 0 &&
                                                 networkTopic.topicObjs[0].stepObjs != null &&
                                                 networkTopic.topicObjs[0].stepObjs.Count > 0
            ? networkTopic.topicObjs[0].stepObjs[0].GetComponent<TexCore_PassiveUIEventStep>()
            : null;
        if (networkStep == null)
        {
            errors.Add("09 '04AGV配网' has no TexCore_PassiveUIEventStep.");
            return;
        }

        if (networkStep.InteractionSource != destinationNetwork.gameObject)
        {
            errors.Add("09 '04AGV配网' must be bound to '无交互物体/.../09机器人入场/ChangJing-8/AGV配网'.");
        }

        if (networkStep.CheckpointCount != 32)
        {
            errors.Add($"09 AGV network step watches {networkStep.CheckpointCount} controls; expected 32.");
        }

        for (int index = 0; index < networkStep.CheckpointCount; index++)
        {
            GameObject checkpoint = networkStep.GetCheckpointSource(index);
            if (checkpoint == null)
            {
                errors.Add($"09 AGV network checkpoint {index} has no source control.");
                continue;
            }

            if (!IsDescendantOf(checkpoint.transform, destinationNetwork))
            {
                errors.Add($"09 AGV network checkpoint {index} ('{checkpoint.name}') is outside 'ChangJing-8/AGV配网'.");
            }
        }

        Transform hint = FindByHierarchySuffix(destination, "UI-指引/AGV配置网络/AGV配网");
        if (hint != null && networkStep.InteractionSource != null &&
            networkStep.InteractionSource.transform == hint)
        {
            errors.Add("09 '04AGV配网' must not use the 'UI-指引/AGV配置网络/AGV配网' hint image as its source.");
        }
    }

    private static void ValidateModule09RobotBaseline(Scene destination, List<string> errors)
    {
        Transform robot = RequirePath(destination, PassiveRootName, SystemName, Module09Name,
            "ChangJing-8", "AGV", "AGV1");
        Rigidbody body = robot.GetComponent<Rigidbody>();
        if (body == null)
        {
            errors.Add("09 robot 'ChangJing-8/AGV/AGV1' has no Rigidbody.");
            return;
        }

        if (!body.isKinematic)
        {
            errors.Add("09 robot Rigidbody must stay kinematic.");
        }

        if (body.useGravity)
        {
            errors.Add("09 robot Rigidbody must not use gravity.");
        }

        if (body.interpolation != RigidbodyInterpolation.Interpolate)
        {
            errors.Add("09 robot Rigidbody must keep interpolated motion.");
        }

        if (body.collisionDetectionMode != CollisionDetectionMode.Continuous)
        {
            errors.Add("09 robot Rigidbody must keep continuous collision detection.");
        }

        MixedReality.Toolkit.SpatialManipulation.ObjectManipulator manipulator =
            robot.GetComponent<MixedReality.Toolkit.SpatialManipulation.ObjectManipulator>();
        if (manipulator == null)
        {
            errors.Add("09 robot 'ChangJing-8/AGV/AGV1' has no ObjectManipulator.");
            return;
        }

        if (manipulator.AllowedManipulations != MixedReality.Toolkit.TransformFlags.Move)
        {
            errors.Add($"09 robot ObjectManipulator allows '{manipulator.AllowedManipulations}'; expected Move only.");
        }
    }

    private static void ValidateModule09QrTarget(Scene destination, List<string> errors)
    {
        TexCore_StepObj steps = FindStepObjects(destination);
        NodeObjs module = steps.interacterObjs.FirstOrDefault(
            node => node != null && node.nodePrefab != null && node.nodePrefab.name == Module09Name);
        TopicObjs topic = module != null && module.nodeObjs != null
            ? module.nodeObjs.FirstOrDefault(
                value => value != null && value.topicPrefab != null &&
                         value.topicPrefab.name == "05机器人推至二维码")
            : null;
        TexCore_OrderedPlacementStep step = topic != null && topic.topicObjs != null &&
                                            topic.topicObjs.Count > 0 && topic.topicObjs[0].stepObjs != null &&
                                            topic.topicObjs[0].stepObjs.Count > 0
            ? topic.topicObjs[0].stepObjs[0].GetComponent<TexCore_OrderedPlacementStep>()
            : null;
        if (step == null)
        {
            errors.Add("09 '05机器人推至二维码' has no TexCore_OrderedPlacementStep.");
            return;
        }

        if (step.Target == null)
        {
            errors.Add("09 QR step has no target.");
            return;
        }

        string path = HierarchyPath(step.Target);
        if (!path.EndsWith("/ChangJing-8/MR_ChangJing/ErWeiMa/ErWeiMa16", StringComparison.Ordinal))
        {
            errors.Add($"09 QR step targets '{path}'; expected the single placed QR decal '.../ErWeiMa/ErWeiMa16'.");
        }

        if (step.Target.GetComponent<Collider>() == null)
        {
            errors.Add("09 QR step target must keep its collider.");
        }
    }

    private static void ValidateBackpack08(Scene destination, Scene source08, List<string> errors)
    {
        Transform sourcePanel = RequirePath(source08, BackpackRootName, BackpackPanel08Name);
        Transform destinationPanel = FindDirectChild(RequireRoot(destination, BackpackRootName), BackpackPanel08Name);
        if (destinationPanel == null)
        {
            errors.Add($"'{BackpackRootName}/{BackpackPanel08Name}' is missing in 03.");
            return;
        }

        if (SubtreeSignature(sourcePanel) != SubtreeSignature(destinationPanel))
        {
            errors.Add($"'{BackpackRootName}/{BackpackPanel08Name}' does not match 0809.unity.");
        }

        Transform grid = FindDirectChild(destinationPanel, "08背包按钮网格");
        if (grid == null || grid.childCount != Module08BackpackEntries.Length)
        {
            errors.Add($"'{BackpackPanel08Name}/08背包按钮网格' must keep {Module08BackpackEntries.Length} buttons.");
        }

        TexCore_08BackpackController controller =
            RequireRoot(destination, BackpackRootName).GetComponent<TexCore_08BackpackController>();
        if (controller == null)
        {
            errors.Add($"'{BackpackRootName}' has no TexCore_08BackpackController.");
            return;
        }

        if (controller.BackpackPanel != destinationPanel.gameObject)
        {
            errors.Add("TexCore_08BackpackController.backpackPanel must reference the migrated 08 chapter panel.");
        }

        IReadOnlyList<TexCore_08BackpackController.Entry> entries = controller.Entries;
        if (entries.Count != Module08BackpackEntries.Length)
        {
            errors.Add($"TexCore_08BackpackController has {entries.Count} entries; expected {Module08BackpackEntries.Length}.");
            return;
        }

        for (int index = 0; index < entries.Count; index++)
        {
            TexCore_08BackpackController.Entry entry = entries[index];
            if (entry == null)
            {
                errors.Add($"TexCore_08BackpackController entry {index} is null.");
                continue;
            }

            if (entry.key != Module08BackpackEntries[index])
            {
                errors.Add($"TexCore_08BackpackController entry {index} is '{entry.key}'; expected " +
                           $"'{Module08BackpackEntries[index]}'.");
            }

            if (entry.source == null)
            {
                errors.Add($"TexCore_08BackpackController entry '{entry.key}' has no source object.");
            }
            else if (!IsDescendantOf(entry.source.transform,
                         RequirePath(destination, PassiveRootName, SystemName, Module08Name)))
            {
                errors.Add($"TexCore_08BackpackController entry '{entry.key}' source is outside " +
                           $"'{PassiveRootName}/.../{Module08Name}'.");
            }

            if (entry.button == null)
            {
                errors.Add($"TexCore_08BackpackController entry '{entry.key}' has no backpack button.");
            }
            else if (!IsDescendantOf(entry.button.transform, destinationPanel))
            {
                errors.Add($"TexCore_08BackpackController entry '{entry.key}' button is outside the migrated 08 panel.");
            }
        }
    }

    private static void ValidateStepIndexes(Scene destination, List<string> errors)
    {
        TexCore_StepObj steps = FindStepObjects(destination);
        for (int nodeIndex = 0; nodeIndex < steps.interacterObjs.Count; nodeIndex++)
        {
            NodeObjs node = steps.interacterObjs[nodeIndex];
            if (node == null || node.nodeObjs == null)
            {
                continue;
            }

            for (int topicIndex = 0; topicIndex < node.nodeObjs.Count; topicIndex++)
            {
                TopicObjs topic = node.nodeObjs[topicIndex];
                if (topic == null || topic.topicObjs == null)
                {
                    continue;
                }

                for (int stepIndex = 0; stepIndex < topic.topicObjs.Count; stepIndex++)
                {
                    StepObjs step = topic.topicObjs[stepIndex];
                    string label = $"node {nodeIndex} ({NodeName(node)}), topic {topicIndex} " +
                                   $"({(topic.topicPrefab != null ? topic.topicPrefab.name : "null")})";
                    if (step == null)
                    {
                        errors.Add($"{label} has a null step entry at {stepIndex}.");
                        continue;
                    }

                    if (step.stepIndex != stepIndex)
                    {
                        errors.Add($"{label} step {stepIndex} has stepIndex {step.stepIndex}.");
                    }

                    // Reporter ownership is asserted for the migrated modules
                    // only; the other modules are owned by their own migrations
                    // and may be bound late at runtime.
                    if (IsModuleOwnedNodeName(node) &&
                        (step.stepObjs == null || step.stepObjs.Count == 0 ||
                         step.stepObjs.Any(item => item == null)))
                    {
                        errors.Add($"{label} step {stepIndex} has an empty or null step object binding.");
                    }
                }
            }
        }

        // A step object must belong to exactly one step entry.
        Dictionary<GameObject, string> owners = new Dictionary<GameObject, string>();
        for (int nodeIndex = 0; nodeIndex < steps.interacterObjs.Count; nodeIndex++)
        {
            NodeObjs node = steps.interacterObjs[nodeIndex];
            if (node == null || node.nodeObjs == null)
            {
                continue;
            }

            for (int topicIndex = 0; topicIndex < node.nodeObjs.Count; topicIndex++)
            {
                TopicObjs topic = node.nodeObjs[topicIndex];
                if (topic == null || topic.topicObjs == null)
                {
                    continue;
                }

                for (int stepIndex = 0; stepIndex < topic.topicObjs.Count; stepIndex++)
                {
                    StepObjs step = topic.topicObjs[stepIndex];
                    if (step == null || step.stepObjs == null)
                    {
                        continue;
                    }

                    string owner = $"node {nodeIndex}/{NodeName(node)} topic {topicIndex} step {stepIndex}";
                    foreach (GameObject stepObject in step.stepObjs)
                    {
                        if (stepObject == null)
                        {
                            continue;
                        }

                        string existing;
                        if (owners.TryGetValue(stepObject, out existing))
                        {
                            errors.Add($"Step object '{HierarchyPath(stepObject.transform)}' is bound to both " +
                                       $"'{existing}' and '{owner}'.");
                        }
                        else
                        {
                            owners[stepObject] = owner;
                        }
                    }
                }
            }
        }
    }

    private static string NodeName(NodeObjs node)
    {
        return node != null && node.nodePrefab != null ? node.nodePrefab.name : "null";
    }

    private static bool IsModuleOwnedNodeName(NodeObjs node)
    {
        string name = NodeName(node);
        return name == Module08Name || name == Module09Name;
    }

    private static Transform FindByHierarchySuffix(Scene scene, string suffix)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (HierarchyPath(candidate).EndsWith("/" + suffix, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static void RegenerateConfig()
    {
        TexCore_ConfigManager manager = UnityEngine.Object.FindObjectOfType<TexCore_ConfigManager>(true);
        if (manager == null)
        {
            throw new InvalidOperationException("场景缺少 TexCore_ConfigManager，无法生成 Config.xml。");
        }

        string configPath = System.IO.Path.Combine(Application.dataPath, "StreamingAssets", "Config.xml");
        ConfigleData config = XmlSerializer.LoadFromXml(configPath, typeof(ConfigleData)) as ConfigleData;
        if (config == null || config.datas == null)
        {
            throw new InvalidOperationException("无法读取 Config.xml 的 TexCore 配置根节点。");
        }

        manager.datas = config;
        manager.ReadDataFromPrefabSingle(config.datas, manager.prefabsPath);
        XmlSerializer.SaveToXml(configPath, config);
    }
}
#endif
