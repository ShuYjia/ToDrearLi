using System;
using System.Collections.Generic;
using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using Bpf.Diagnostics;

public enum RackAssemblyStepKind
{
    Instruction,
    PlacePart,
    UseTool,
    MoveAssembly
}

[Serializable]
public class RackAssemblyStepDefinition
{
    public string name;
    public RackAssemblyStepKind kind;
    public string partKey;
    public List<Transform> blueTargets = new List<Transform>();
    public List<Transform> greenTargets = new List<Transform>();
    public List<Transform> resultObjects = new List<Transform>();
    public List<Transform> toolTargets = new List<Transform>();
    public List<string> targetPartKeys = new List<string>();
    public List<string> autoSpawnPartKeys = new List<string>();
    public Transform requiredTool;
    public Transform moveTarget;
    public bool waitForConfirmation;
    [Min(0.01f)] public float positionTolerance = 0.12f;
    [Range(0f, 180f)] public float rotationTolerance = 180f;
    /// <summary>
    /// Pose applied to the part taken out of the backpack for this step. The 06
    /// 底座 step tips the base over so its underside (the 二维码 slot) faces up.
    /// </summary>
    public Vector3 spawnEulerOffset = Vector3.zero;
    /// <summary>
    /// Targets have to be completed in list order. The 06 底座 step uses this so
    /// the 二维码 must be stuck onto the base before the base may be placed.
    /// </summary>
    public bool requireOrderedTargets;
    /// <summary>
    /// Optional per-target transform that sizes and places the guidance frame.
    /// The 07 电脑主机 target carries a stray part 1.94m below the chassis, so
    /// its frame is anchored to the chassis panel instead of the full bounds.
    /// </summary>
    public List<Transform> guidanceAnchors = new List<Transform>();
}

[Serializable]
public class RackAssemblyTemplateDefinition
{
    public string key;
    public Transform template;
}

/// <summary>
/// Runtime state for the 06/07 assembly topics. It keeps the existing
/// TexCore topic/step flow, while adding repeated backpack spawning and
/// deterministic blue/green/result state restoration.
/// </summary>
[DisallowMultipleComponent]
public sealed class TexCore_RackAssemblyController : MonoBehaviour
{
    private static readonly Color DisplayWallPlacementGuidanceColor = new Color(0.05f, 0.95f, 1f, 1f);
    private static readonly Color DisplayWallPreviewGuidanceColor = new Color(0.18f, 1f, 0f, 1f);
    private static readonly Color DisplayWallToolGuidanceColor = new Color(1f, 0.78f, 0.05f, 1f);
    private static readonly Color DisplayWallMoveGuidanceColor = new Color(1f, 0.78f, 0.05f, 1f);

    [SerializeField] private Transform temporaryArea;
    [SerializeField] private Transform completeAssembly;
    [SerializeField] private GameObject backpackPanel;
    [SerializeField] private GameObject defaultBackpackContent;
    [SerializeField] private Transform drillTemplate;
    [SerializeField] private string spawnedObjectPrefix = "06";
    [SerializeField] private List<RackAssemblyTemplateDefinition> configuredTemplates = new List<RackAssemblyTemplateDefinition>();
    [SerializeField] private List<RackAssemblyStepDefinition> steps = new List<RackAssemblyStepDefinition>();

    private readonly Dictionary<string, Transform> partTemplates = new Dictionary<string, Transform>();
    private readonly List<GameObject> spawnedObjects = new List<GameObject>();
    private readonly HashSet<int> completedTargets = new HashSet<int>();
    private readonly Dictionary<TexCore_RackAssemblyInteractable, int> previewTargets = new Dictionary<TexCore_RackAssemblyInteractable, int>();
    private int activeStepIndex = -1;
    private bool active;
    private GameObject activeReporter;
    private Vector3 completeAssemblyStartPosition;
    private Quaternion completeAssemblyStartRotation;
    private bool completeAssemblyPoseCaptured;
    private Material displayWallGuidanceMaterial;
    private readonly List<DisplayWallTargetGuidance> displayWallTargetGuides = new List<DisplayWallTargetGuidance>();

    public int ActiveStepIndex => activeStepIndex;
    public RackAssemblyStepDefinition ActiveStep => IsValidStep(activeStepIndex) ? steps[activeStepIndex] : null;

    private void Awake()
    {
        CacheTemplates();
        CaptureCompleteAssemblyPose();
        SetAllControlledObjects(false);
        TexCore_BackpackChapterTabs.EnsureInitialized();
        SetAssemblyManipulationEnabled(false);
    }

    private void OnDisable()
    {
        DeactivateStep();
    }

    private void OnDestroy()
    {
        ClearDisplayWallTargetGuidance();
        if (displayWallGuidanceMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(displayWallGuidanceMaterial);
            }
            else
            {
                DestroyImmediate(displayWallGuidanceMaterial);
            }
        }
    }

    private void LateUpdate()
    {
        if (!IsDisplayWallGuidanceActive())
        {
            SetDisplayWallTargetGuidanceVisible(false);
            return;
        }

        EnforceMoveAssemblyHeight();
        UpdateMoveAssemblyGuidance();

        foreach (DisplayWallTargetGuidance guide in displayWallTargetGuides)
        {
            guide.Update();
        }
    }

    /// <summary>
    /// The finished 货架 may only be slid across the floor. ObjectManipulator
    /// moves the transform directly in kinematic mode, so Rigidbody constraints
    /// do not limit it and the height has to be pinned every frame instead.
    /// </summary>
    private void EnforceMoveAssemblyHeight()
    {
        RackAssemblyStepDefinition step = ActiveStep;
        if (step == null || step.kind != RackAssemblyStepKind.MoveAssembly || completeAssembly == null)
        {
            return;
        }

        // Stay on the plane the rack was picked up from. If the authored drop
        // position sits on another height, use that one so the step can still be
        // completed instead of forcing a height the target cannot reach.
        float height = completeAssemblyStartPosition.y;
        if (step.moveTarget != null &&
            Mathf.Abs(step.moveTarget.position.y - height) > step.positionTolerance)
        {
            height = step.moveTarget.position.y;
        }

        Vector3 position = completeAssembly.position;
        if (!Mathf.Approximately(position.y, height))
        {
            completeAssembly.position = new Vector3(position.x, height, position.z);
        }
    }

    /// <summary>
    /// Turns the drop position frame green as soon as the rack covers it, which
    /// is the same distance test that releases (completes) the move step.
    /// </summary>
    private void UpdateMoveAssemblyGuidance()
    {
        RackAssemblyStepDefinition step = ActiveStep;
        if (step == null || step.kind != RackAssemblyStepKind.MoveAssembly ||
            step.moveTarget == null || completeAssembly == null)
        {
            return;
        }

        bool aligned = Vector3.Distance(completeAssembly.position, step.moveTarget.position) <= step.positionTolerance;
        AddActiveDisplayWallTargetGuidance(
            step.moveTarget,
            aligned ? DisplayWallPreviewGuidanceColor : DisplayWallMoveGuidanceColor);
    }

    public void SetStepDefinitions(List<RackAssemblyStepDefinition> definitions)
    {
        steps = definitions ?? new List<RackAssemblyStepDefinition>();
    }

    public void SetTemporaryArea(Transform value)
    {
        temporaryArea = value;
        CacheTemplates();
    }

    public void SetCompleteAssembly(Transform value)
    {
        completeAssembly = value;
        CaptureCompleteAssemblyPose();
    }

    public void SetBackpackPanel(GameObject value)
    {
        backpackPanel = value;
    }

    public void SetDefaultBackpackContent(GameObject value)
    {
        defaultBackpackContent = value;
    }

    public void SetDrillTemplate(Transform value)
    {
        drillTemplate = value;
        CacheTemplates();
    }

    public void SetSpawnedObjectPrefix(string value)
    {
        spawnedObjectPrefix = string.IsNullOrWhiteSpace(value) ? "Assembly" : value;
    }

    public void SetConfiguredTemplates(List<RackAssemblyTemplateDefinition> definitions)
    {
        configuredTemplates = definitions ?? new List<RackAssemblyTemplateDefinition>();
        CacheTemplates();
    }

    public void ActivateStep(int stepIndex, GameObject reporter)
    {
        if (!IsValidStep(stepIndex))
        {
            DeactivateStep();
            return;
        }

        active = true;
        activeStepIndex = stepIndex;
        activeReporter = reporter;
        completedTargets.Clear();
        ClearSpawnedObjects();
        ApplyCompletedState(stepIndex);
        ShowCurrentTargets();

        RackAssemblyStepDefinition step = steps[stepIndex];
        // 07 opens with an Instruction step whose "确认步骤" button lives inside
        // the 07 backpack chapter panel. Keep that panel visible for such steps,
        // otherwise starting the step immediately hides the only button that can
        // confirm it.
        SetBackpackVisible(step.kind == RackAssemblyStepKind.PlacePart ||
                           step.kind == RackAssemblyStepKind.UseTool ||
                           NeedsBackpackConfirmButton(step));

        if ((step.kind == RackAssemblyStepKind.Instruction && !step.waitForConfirmation) ||
            (step.kind != RackAssemblyStepKind.Instruction && GetTargetCount(step) == 0))
        {
            CompleteCurrentStep();
        }
        else if (step.kind == RackAssemblyStepKind.MoveAssembly)
        {
            EnsureMoveInteractable();
        }
    }

    public void DeactivateStep()
    {
        active = false;
        activeStepIndex = -1;
        activeReporter = null;
        completedTargets.Clear();
        ClearSpawnedObjects();
        SetDisplayWallTargetGuidanceVisible(false);
        SetAllControlledObjects(false);
        SetAssemblyManipulationEnabled(false);
        SetBackpackVisible(false);
    }

    public void FinishStep(int stepIndex)
    {
        if (!IsValidStep(stepIndex))
        {
            return;
        }

        ApplyCompletedState(stepIndex + 1);
        if (activeStepIndex == stepIndex)
        {
            DeactivateStep();
        }
    }

    public void ResetBeforeStep(int stepIndex)
    {
        ClearSpawnedObjects();
        completedTargets.Clear();
        ApplyCompletedState(Mathf.Clamp(stepIndex, 0, steps.Count));
        RefreshDisplayWallTargetGuidance();
    }

    public void SpawnBottomBase() => SpawnPart("底座");
    public void SpawnUpright() => SpawnPart("立柱");
    public void SpawnScrew() => SpawnPart("螺丝");
    public void SpawnShelf() => SpawnPart("层板");
    public void SpawnBeam() => SpawnPart("横梁");
    public void SpawnBin() => SpawnPart("库位盒");
    public void SpawnGlue() => SpawnTool("胶水");
    public void SpawnWrench() => SpawnTool("扳手");
    public void SpawnDrill() => SpawnTool("电钻");
    public void SpawnHammer() => SpawnTool("橡胶锤");

    public void ConfirmInstruction()
    {
        if (!active || ActiveStep == null ||
            ActiveStep.kind != RackAssemblyStepKind.Instruction ||
            !ActiveStep.waitForConfirmation)
        {
            ShowHint("当前步骤无需确认");
            return;
        }

        CompleteCurrentStep();
    }

    public void SpawnPart(string partKey)
    {
        SpawnPart(partKey, true);
    }

    private void SpawnPart(string partKey, bool spawnAccompanyingParts)
    {
        if (!active || ActiveStep == null ||
            ActiveStep.kind != RackAssemblyStepKind.PlacePart ||
            !StepAcceptsPart(ActiveStep, partKey))
        {
            ShowHint($"当前步骤不需要{partKey}");
            return;
        }

        if (!partTemplates.TryGetValue(partKey, out Transform template) || template == null)
        {
            ShowHint($"未配置{partKey}取件模板");
            return;
        }

        bool isPrimaryPart = partKey == ActiveStep.partKey;
        SpawnInteractable(
            template,
            partKey,
            false,
            isPrimaryPart ? ActiveStep.spawnEulerOffset : Vector3.zero);

        if (!spawnAccompanyingParts || ActiveStep.autoSpawnPartKeys == null)
        {
            return;
        }

        foreach (string accompanyingKey in ActiveStep.autoSpawnPartKeys)
        {
            if (!string.IsNullOrEmpty(accompanyingKey) && accompanyingKey != partKey)
            {
                SpawnPart(accompanyingKey, false);
            }
        }
    }

    public void SpawnTool(string toolKey)
    {
        if (!active || ActiveStep == null ||
            ActiveStep.kind != RackAssemblyStepKind.UseTool ||
            !StepAcceptsPart(ActiveStep, toolKey))
        {
            ShowHint($"当前步骤不需要{toolKey}");
            return;
        }

        if (!partTemplates.TryGetValue(toolKey, out Transform template) || template == null)
        {
            ShowHint($"未配置{toolKey}工具");
            return;
        }

        SpawnInteractable(template, toolKey, true, Vector3.zero);
    }

    public bool TryCompleteTarget(TexCore_RackAssemblyInteractable interactable)
    {
        if (!active || interactable == null || ActiveStep == null || interactable.StepIndex != activeStepIndex)
        {
            return false;
        }

        RackAssemblyStepDefinition step = ActiveStep;
        if (step.kind == RackAssemblyStepKind.MoveAssembly)
        {
            if (completeAssembly == null || step.moveTarget == null ||
                Vector3.Distance(completeAssembly.position, step.moveTarget.position) > step.positionTolerance)
            {
                return false;
            }

            completeAssembly.SetPositionAndRotation(step.moveTarget.position, step.moveTarget.rotation);
            FreezeAssemblyPhysics();
            CompleteCurrentStep();
            return true;
        }

        int targetIndex = FindNearestTargetIndex(interactable.transform, step, interactable.ItemKey);
        if (targetIndex < 0 || completedTargets.Contains(targetIndex))
        {
            return false;
        }

        previewTargets.Remove(interactable);
        completedTargets.Add(targetIndex);
        ApplyTargetCompletion(step, targetIndex, interactable.transform);
        DestroySpawned(interactable.gameObject);
        UpdateProgress(step);

        if (completedTargets.Count >= GetTargetCount(step))
        {
            CompleteCurrentStep();
        }
        return true;
    }

    public void UpdateTargetPreview(TexCore_RackAssemblyInteractable interactable)
    {
        if (!active || interactable == null || ActiveStep == null ||
            interactable.StepIndex != activeStepIndex ||
            (ActiveStep.kind != RackAssemblyStepKind.PlacePart &&
             ActiveStep.kind != RackAssemblyStepKind.UseTool))
        {
            ClearTargetPreview(interactable);
            return;
        }

        int targetIndex = FindNearestTargetIndex(interactable.transform, ActiveStep, interactable.ItemKey);
        if (previewTargets.TryGetValue(interactable, out int previousTarget) && previousTarget == targetIndex)
        {
            return;
        }

        if (targetIndex < 0)
        {
            previewTargets.Remove(interactable);
        }
        else
        {
            previewTargets[interactable] = targetIndex;
        }
        RefreshTargetPreviews();
    }

    public void ClearTargetPreview(TexCore_RackAssemblyInteractable interactable)
    {
        if (interactable != null && previewTargets.Remove(interactable))
        {
            RefreshTargetPreviews();
        }
    }

    public Transform GetGuidanceTarget(TexCore_RackAssemblyInteractable interactable)
    {
        if (!active || interactable == null || ActiveStep == null ||
            interactable.StepIndex != activeStepIndex)
        {
            return null;
        }

        RackAssemblyStepDefinition step = ActiveStep;
        if (step.kind == RackAssemblyStepKind.MoveAssembly)
        {
            return step.moveTarget;
        }

        if (step.kind != RackAssemblyStepKind.PlacePart && step.kind != RackAssemblyStepKind.UseTool)
        {
            return null;
        }

        List<Transform> targets = step.kind == RackAssemblyStepKind.UseTool ? step.toolTargets : step.blueTargets;
        int targetIndex = FindNearestGuidanceTargetIndex(
            interactable.transform,
            targets,
            step,
            interactable.ItemKey);
        return targetIndex >= 0 ? targets[targetIndex] : null;
    }

    private void CacheTemplates()
    {
        partTemplates.Clear();
        if (configuredTemplates != null && configuredTemplates.Count > 0)
        {
            foreach (RackAssemblyTemplateDefinition definition in configuredTemplates)
            {
                if (definition != null)
                {
                    AddTemplate(definition.key, definition.template);
                }
            }
            return;
        }

        if (temporaryArea != null)
        {
            AddTemplate("底座", temporaryArea.Find("HuoJia1"));
            AddTemplate("立柱", temporaryArea.Find("HuoJia9"));
            AddTemplate("横梁", temporaryArea.Find("pCube154"));
            AddTemplate("库位盒", temporaryArea.Find("ZhiXiang19"));
            AddTemplate("层板", temporaryArea.Find("pCube162"));
            Transform screwContainer = temporaryArea.Find("LuoSi");
            AddTemplate("螺丝", screwContainer != null && screwContainer.childCount > 0 ? screwContainer.GetChild(0) : screwContainer);
        }

        AddTemplate("胶水", transform.Find("JiaoShui"));
        AddTemplate("扳手", transform.Find("BanShou"));
        AddTemplate("橡胶锤", transform.Find("橡胶锤-11"));
        AddTemplate("电钻", drillTemplate);
        if (temporaryArea != null)
        {
            AddTemplate("货架二维码", temporaryArea.Find("ErWeiMa"));
        }
    }

    private void AddTemplate(string key, Transform value)
    {
        if (value != null)
        {
            partTemplates[key] = value;
            value.gameObject.SetActive(false);
        }
    }

    private void SpawnInteractable(
        Transform template,
        string itemKey,
        bool tool,
        Vector3 spawnEulerOffset)
    {
        Transform destinationParent = temporaryArea != null ? temporaryArea.parent : transform;
        GameObject spawned = InstantiatePreservingWorldScale(template, destinationParent);
        spawned.name = $"{spawnedObjectPrefix}_{itemKey}_取出";
        spawned.SetActive(true);
        ApplySpawnEulerOffset(spawned, spawnEulerOffset);
        RemoveLegacyIdentity(spawned);

        ObjectManipulator manipulator = spawned.GetComponent<ObjectManipulator>();
        if (manipulator == null)
        {
            manipulator = spawned.AddComponent<ObjectManipulator>();
        }
        manipulator.AllowedManipulations = MixedReality.Toolkit.TransformFlags.Move;
        TexCore_03NoScalePolicy.Enforce(spawned);

        EnsureCollider(spawned);

        if (spawned.GetComponent<Rigidbody>() == null)
        {
            spawned.AddComponent<Rigidbody>();
        }
        TexCore_BackpackPhysics.Stabilize(spawned);

        TexCore_RackAssemblyInteractable item = spawned.AddComponent<TexCore_RackAssemblyInteractable>();
        item.Initialize(this, activeStepIndex, itemKey, tool);
        AddNameLabel(spawned, itemKey);
        spawnedObjects.Add(spawned);
    }

    /// <summary>
    /// The authored item templates only carry a name label on the drill, so
    /// every other backpack-spawned part clones that label to show its own
    /// name while a controller ray or hand hovers it.
    /// </summary>
    private void AddNameLabel(GameObject spawned, string displayName)
    {
        if (spawned == null || string.IsNullOrEmpty(displayName) ||
            spawned.GetComponentInChildren<Canvas>(true) != null)
        {
            return;
        }

        GameObject labelTemplate = FindLabelTemplate();
        if (labelTemplate == null)
        {
            return;
        }

        spawned.AddComponent<TexCore_ObjectNameLabel>().Initialize(labelTemplate, displayName);
    }

    private GameObject FindLabelTemplate()
    {
        if (drillTemplate == null)
        {
            return null;
        }

        Transform label = drillTemplate.Find("BQ");
        if (label != null)
        {
            return label.gameObject;
        }

        Canvas canvas = drillTemplate.GetComponentInChildren<Canvas>(true);
        return canvas != null ? canvas.gameObject : null;
    }

    private static GameObject InstantiatePreservingWorldScale(
        Transform template,
        Transform destinationParent)
    {
        GameObject spawned = Instantiate(template.gameObject, template.parent);
        spawned.transform.SetParent(destinationParent, true);
        return spawned;
    }

    /// <summary>
    /// Applies the pose the step spawns its part with (06 tips the 底座 over so
    /// the 二维码 slot on its underside faces up) and keeps the part resting on
    /// the surface it was taken from instead of sinking into it.
    /// </summary>
    private static void ApplySpawnEulerOffset(GameObject spawned, Vector3 eulerOffset)
    {
        if (spawned == null)
        {
            return;
        }

        bool hasBounds = TryGetRendererBounds(spawned, out Bounds before);
        if (eulerOffset == Vector3.zero)
        {
            return;
        }

        spawned.transform.rotation = Quaternion.Euler(eulerOffset) * spawned.transform.rotation;
        if (hasBounds && TryGetRendererBounds(spawned, out Bounds after))
        {
            spawned.transform.position += Vector3.up * (before.min.y - after.min.y);
        }
    }

    private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
    {
        bounds = new Bounds();
        bool hasBounds = false;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (hasBounds)
            {
                bounds.Encapsulate(renderer.bounds);
                continue;
            }

            bounds = renderer.bounds;
            hasBounds = true;
        }

        return hasBounds;
    }

    private static void EnsureCollider(GameObject spawned)
    {
        if (spawned.GetComponentInChildren<Collider>(true) != null)
        {
            return;
        }

        Renderer[] renderers = spawned.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            spawned.AddComponent<BoxCollider>();
            return;
        }

        Bounds localBounds = new Bounds(
            spawned.transform.InverseTransformPoint(renderers[0].bounds.center),
            Vector3.zero);
        foreach (Renderer renderer in renderers)
        {
            Bounds bounds = renderer.bounds;
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 corner = new Vector3(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z);
                        localBounds.Encapsulate(spawned.transform.InverseTransformPoint(corner));
                    }
                }
            }
        }

        BoxCollider collider = spawned.AddComponent<BoxCollider>();
        collider.center = localBounds.center;
        collider.size = localBounds.size;
    }

    private static void RemoveLegacyIdentity(GameObject spawned)
    {
        foreach (TexCore_InteracterBase legacy in spawned.GetComponents<TexCore_InteracterBase>())
        {
            Destroy(legacy);
        }
    }

    private int FindNearestTargetIndex(Transform item, RackAssemblyStepDefinition step, string itemKey)
    {
        List<Transform> targets = step.kind == RackAssemblyStepKind.UseTool ? step.toolTargets : step.blueTargets;
        float bestDistance = float.MaxValue;
        int bestIndex = -1;
        for (int i = 0; i < targets.Count; i++)
        {
            Transform target = targets[i];
            if (target == null || completedTargets.Contains(i) ||
                !IsTargetUnlocked(step, i) || !TargetAcceptsPart(step, i, itemKey))
            {
                continue;
            }

            float distance = Vector3.Distance(item.position, target.position);
            bool rotationMatches = step.rotationTolerance >= 180f || Quaternion.Angle(item.rotation, target.rotation) <= step.rotationTolerance;
            if (distance <= step.positionTolerance && rotationMatches && distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }
        return bestIndex;
    }

    private int FindNearestGuidanceTargetIndex(
        Transform item,
        IReadOnlyList<Transform> targets,
        RackAssemblyStepDefinition step,
        string itemKey)
    {
        if (item == null || targets == null)
        {
            return -1;
        }

        float bestDistance = float.MaxValue;
        int bestIndex = -1;
        for (int i = 0; i < targets.Count; i++)
        {
            Transform target = targets[i];
            if (target == null || completedTargets.Contains(i) ||
                !IsTargetUnlocked(step, i) || !TargetAcceptsPart(step, i, itemKey))
            {
                continue;
            }

            float distance = (item.position - target.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }
        return bestIndex;
    }

    /// <summary>
    /// Ordered steps only unlock their targets one after another, so the 06 底座
    /// step cannot be finished by placing the base before its 二维码 is stuck on.
    /// </summary>
    private bool IsTargetUnlocked(RackAssemblyStepDefinition step, int index)
    {
        if (step == null || !step.requireOrderedTargets)
        {
            return true;
        }

        for (int i = 0; i < index; i++)
        {
            if (!completedTargets.Contains(i))
            {
                return false;
            }
        }

        return true;
    }

    private void ApplyTargetCompletion(
        RackAssemblyStepDefinition step,
        int index,
        Transform materialSource = null)
    {
        SetAt(step.blueTargets, index, false);
        SetAt(step.greenTargets, index, false);
        SetAt(step.toolTargets, index, false);
        if (step.kind == RackAssemblyStepKind.PlacePart)
        {
            Transform resultObject = GetAt(step.resultObjects, index);
            RestoreResultMaterials(
                materialSource != null ? materialSource : GetTemplateForTarget(step, index),
                resultObject);
        }
        SetAt(step.resultObjects, index, true);
        // Re-evaluate the targets: an ordered step unlocks its next target only
        // now, and the preview/availability state has to follow.
        RefreshTargetPreviews();
    }

    private Transform GetTemplateForTarget(RackAssemblyStepDefinition step, int index)
    {
        string key = step.partKey;
        if (step.targetPartKeys != null && index >= 0 && index < step.targetPartKeys.Count)
        {
            key = step.targetPartKeys[index];
        }

        return !string.IsNullOrEmpty(key) && partTemplates.TryGetValue(key, out Transform template)
            ? template
            : null;
    }

    private void RestoreResultMaterials(Transform sourceRoot, Transform resultRoot)
    {
        if (sourceRoot == null || resultRoot == null)
        {
            return;
        }

        Dictionary<string, Renderer> sourceRenderers = new Dictionary<string, Renderer>();
        Dictionary<Mesh, Material[]> materialsByMesh = new Dictionary<Mesh, Material[]>();
        CacheSourceMaterials(sourceRoot, sourceRenderers, materialsByMesh, true);
        foreach (Transform template in partTemplates.Values)
        {
            CacheSourceMaterials(template, sourceRenderers, materialsByMesh, false);
        }

        foreach (Renderer resultRenderer in resultRoot.GetComponentsInChildren<Renderer>(true))
        {
            string path = GetRendererPath(resultRoot, resultRenderer.transform);
            if (path != null && sourceRenderers.TryGetValue(path, out Renderer sourceRenderer) &&
                TryAssignResultMaterials(resultRenderer, sourceRenderer.sharedMaterials))
            {
                continue;
            }

            Mesh resultMesh = GetRendererMesh(resultRenderer);
            if (resultMesh != null && materialsByMesh.TryGetValue(resultMesh, out Material[] sourceMaterials))
            {
                TryAssignResultMaterials(resultRenderer, sourceMaterials);
            }
        }
    }

    private static void CacheSourceMaterials(
        Transform sourceRoot,
        IDictionary<string, Renderer> sourceRenderers,
        IDictionary<Mesh, Material[]> materialsByMesh,
        bool cachePaths)
    {
        if (sourceRoot == null)
        {
            return;
        }

        foreach (Renderer renderer in sourceRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (cachePaths)
            {
                string path = GetRendererPath(sourceRoot, renderer.transform);
                if (path != null && !sourceRenderers.ContainsKey(path))
                {
                    sourceRenderers.Add(path, renderer);
                }
            }

            Mesh mesh = GetRendererMesh(renderer);
            Material[] materials = renderer.sharedMaterials;
            if (mesh != null && !materialsByMesh.ContainsKey(mesh) && HasUsableMaterials(materials))
            {
                materialsByMesh.Add(mesh, materials);
            }
        }
    }

    private static bool TryAssignResultMaterials(Renderer resultRenderer, Material[] sourceMaterials)
    {
        if (sourceMaterials.Length != resultRenderer.sharedMaterials.Length ||
            !HasUsableMaterials(sourceMaterials))
        {
            return false;
        }

        resultRenderer.sharedMaterials = sourceMaterials;
        return true;
    }

    private static Mesh GetRendererMesh(Renderer renderer)
    {
        if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
        {
            return skinnedMeshRenderer.sharedMesh;
        }

        MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
        return meshFilter != null ? meshFilter.sharedMesh : null;
    }

    private static string GetRendererPath(Transform root, Transform item)
    {
        List<string> path = new List<string>();
        Transform current = item;
        while (current != root)
        {
            if (current == null || current.parent == null)
            {
                return null;
            }

            int sameNameIndex = 0;
            for (int siblingIndex = 0; siblingIndex < current.GetSiblingIndex(); siblingIndex++)
            {
                if (current.parent.GetChild(siblingIndex).name == current.name)
                {
                    sameNameIndex++;
                }
            }
            path.Add($"{current.name}[{sameNameIndex}]");
            current = current.parent;
        }

        path.Reverse();
        return path.Count == 0 ? "<root>" : string.Join("/", path);
    }

    private static bool HasUsableMaterials(IEnumerable<Material> materials)
    {
        bool hasMaterial = false;
        foreach (Material material in materials)
        {
            Shader shader = material != null ? material.shader : null;
            if (shader == null || !shader.isSupported || shader.name == "Hidden/InternalErrorShader")
            {
                return false;
            }
            hasMaterial = true;
        }
        return hasMaterial;
    }

    private void ApplyCompletedState(int completedStepCount)
    {
        SetAllControlledObjects(false);
        RestoreCompleteAssemblyPose();
        for (int i = 0; i < completedStepCount && i < steps.Count; i++)
        {
            RackAssemblyStepDefinition step = steps[i];
            if (step.kind == RackAssemblyStepKind.MoveAssembly)
            {
                if (completeAssembly != null && step.moveTarget != null)
                {
                    completeAssembly.SetPositionAndRotation(step.moveTarget.position, step.moveTarget.rotation);
                }
                continue;
            }
            for (int targetIndex = 0; targetIndex < GetTargetCount(step); targetIndex++)
            {
                ApplyTargetCompletion(step, targetIndex);
            }
        }
    }

    private void ShowCurrentTargets()
    {
        RackAssemblyStepDefinition step = ActiveStep;
        if (step == null)
        {
            return;
        }

        if (step.kind == RackAssemblyStepKind.PlacePart)
        {
            SetListActive(step.blueTargets, true);
        }
        else if (step.kind == RackAssemblyStepKind.UseTool)
        {
            SetListActive(step.resultObjects, false);
            SetListActive(step.toolTargets, true);
        }
        else if (step.kind == RackAssemblyStepKind.MoveAssembly && completeAssembly != null)
        {
            completeAssembly.gameObject.SetActive(true);
        }

        // Availability is part of the preview state: ordered steps hide the
        // targets that are still locked behind an earlier one.
        RefreshTargetPreviews();
    }

    private void RefreshTargetPreviews()
    {
        RackAssemblyStepDefinition step = ActiveStep;
        if (!active || step == null)
        {
            return;
        }

        if (step.kind == RackAssemblyStepKind.PlacePart)
        {
            for (int i = 0; i < GetTargetCount(step); i++)
            {
                if (completedTargets.Contains(i))
                {
                    continue;
                }

                // Ordered steps only advertise the target the player may reach
                // next: the 06 底座 step keeps the base target hidden until the
                // 二维码 has been stuck onto the base.
                bool isAvailable = IsTargetUnlocked(step, i);
                bool isPreviewed = isAvailable && previewTargets.ContainsValue(i);
                SetAt(step.blueTargets, i, isAvailable && !isPreviewed);
                SetAt(step.greenTargets, i, isPreviewed);
            }
        }

        RefreshDisplayWallTargetGuidance();
    }

    private void SetAllControlledObjects(bool value)
    {
        foreach (RackAssemblyStepDefinition step in steps)
        {
            SetListActive(step.blueTargets, value);
            SetListActive(step.greenTargets, value);
            SetListActive(step.resultObjects, value);
            SetListActive(step.toolTargets, value);
        }

        if (!value)
        {
            SetDisplayWallTargetGuidanceVisible(false);
        }
    }

    private bool IsDisplayWallGuidanceActive()
    {
        string prefix = spawnedObjectPrefix?.Trim();
        return active && ActiveStep != null &&
               (string.Equals(prefix, "06", StringComparison.Ordinal) ||
                string.Equals(prefix, "07", StringComparison.Ordinal));
    }

    private void RefreshDisplayWallTargetGuidance()
    {
        if (!IsDisplayWallGuidanceActive())
        {
            SetDisplayWallTargetGuidanceVisible(false);
            return;
        }

        RackAssemblyStepDefinition step = ActiveStep;
        RemoveInactiveDisplayWallTargetGuidance(step);
        AddActiveDisplayWallTargetGuidance(step.blueTargets, step.guidanceAnchors, DisplayWallPlacementGuidanceColor, DisplayWallPlacementGuidanceColor);
        AddActiveDisplayWallTargetGuidance(step.greenTargets, step.guidanceAnchors, DisplayWallPreviewGuidanceColor, DisplayWallPreviewGuidanceColor);
        AddActiveDisplayWallTargetGuidance(step.toolTargets, null, DisplayWallToolGuidanceColor, DisplayWallPreviewGuidanceColor);
        if (step.kind == RackAssemblyStepKind.MoveAssembly)
        {
            AddActiveDisplayWallTargetGuidance(step.moveTarget, DisplayWallMoveGuidanceColor);
        }
    }

    private void AddActiveDisplayWallTargetGuidance(IReadOnlyList<Transform> targets, Color color)
    {
        AddActiveDisplayWallTargetGuidance(targets, null, color, color);
    }

    private void AddActiveDisplayWallTargetGuidance(
        IReadOnlyList<Transform> targets,
        IReadOnlyList<Transform> anchors,
        Color color,
        Color previewColor)
    {
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            // UseTool steps can reuse the completed object as their result
            // object (glue/wrench keep the installed screw). Such a target must
            // stop advertising itself as a pending position even though the
            // object stays active.
            if (completedTargets.Contains(i))
            {
                continue;
            }

            // The position the player is aiming at is drawn with the preview
            // colour so it stands out from the remaining pending targets.
            Transform anchor = anchors != null && i < anchors.Count ? anchors[i] : null;
            AddActiveDisplayWallTargetGuidance(
                targets[i],
                anchor,
                previewTargets.ContainsValue(i) ? previewColor : color);
        }
    }

    private void AddActiveDisplayWallTargetGuidance(Transform target, Color color)
    {
        AddActiveDisplayWallTargetGuidance(target, null, color);
    }

    private void AddActiveDisplayWallTargetGuidance(Transform target, Transform boundsSource, Color color)
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            return;
        }

        DisplayWallTargetGuidance guide = FindDisplayWallTargetGuidance(target);
        if (guide != null && guide.BoundsSource != boundsSource)
        {
            // The step changed this target's frame anchor, so rebuild the guide.
            guide.Dispose();
            displayWallTargetGuides.Remove(guide);
            guide = null;
        }

        if (guide == null)
        {
            guide = CreateDisplayWallTargetGuidance(target, boundsSource);
            if (guide == null)
            {
                return;
            }
            displayWallTargetGuides.Add(guide);
        }

        guide.SetColor(color);
        guide.Update();
    }

    private void RemoveInactiveDisplayWallTargetGuidance(RackAssemblyStepDefinition step)
    {
        for (int i = displayWallTargetGuides.Count - 1; i >= 0; i--)
        {
            DisplayWallTargetGuidance guide = displayWallTargetGuides[i];
            if (!IsActiveDisplayWallGuidanceTarget(step, guide.Target))
            {
                guide.Dispose();
                displayWallTargetGuides.RemoveAt(i);
            }
        }
    }

    private bool IsActiveDisplayWallGuidanceTarget(RackAssemblyStepDefinition step, Transform target)
    {
        return (target != null && target.gameObject.activeInHierarchy && target == step.moveTarget) ||
               IsActiveDisplayWallGuidanceTarget(step.blueTargets, target) ||
               IsActiveDisplayWallGuidanceTarget(step.greenTargets, target) ||
               IsActiveDisplayWallGuidanceTarget(step.toolTargets, target);
    }

    private bool IsActiveDisplayWallGuidanceTarget(IReadOnlyList<Transform> targets, Transform target)
    {
        if (targets == null || target == null || !target.gameObject.activeInHierarchy)
        {
            return false;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == target)
            {
                return !completedTargets.Contains(i);
            }
        }
        return false;
    }

    private DisplayWallTargetGuidance FindDisplayWallTargetGuidance(Transform target)
    {
        foreach (DisplayWallTargetGuidance guide in displayWallTargetGuides)
        {
            if (guide.Target == target)
            {
                return guide;
            }
        }
        return null;
    }

    private DisplayWallTargetGuidance CreateDisplayWallTargetGuidance(Transform target, Transform boundsSource)
    {
        if (!EnsureDisplayWallGuidanceMaterial())
        {
            return null;
        }

        return new DisplayWallTargetGuidance(target, boundsSource, displayWallGuidanceMaterial);
    }

    private bool EnsureDisplayWallGuidanceMaterial()
    {
        if (displayWallGuidanceMaterial != null)
        {
            return true;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            BpfLog.Warning(BpfLogCategory.Scene, "tex_core_rack_assembly_controller.ensure_display_wall_guidance_material", $"{name} 无法创建 06/07 装配目标指引：找不到 Sprites/Default Shader。", this);
            return false;
        }

        displayWallGuidanceMaterial = new Material(shader)
        {
            name = "07DisplayWallTargetGuidanceMaterial",
            hideFlags = HideFlags.DontSave
        };
        return true;
    }

    private void SetDisplayWallTargetGuidanceVisible(bool value)
    {
        foreach (DisplayWallTargetGuidance guide in displayWallTargetGuides)
        {
            guide.SetVisible(value);
        }
    }

    private void ClearDisplayWallTargetGuidance()
    {
        foreach (DisplayWallTargetGuidance guide in displayWallTargetGuides)
        {
            guide.Dispose();
        }
        displayWallTargetGuides.Clear();
    }

    private sealed class DisplayWallTargetGuidance
    {
        private const float GuidancePadding = 0.025f;
        private const float GuidancePulseAmount = 0.12f;
        private const float GuidancePulseSpeed = 3f;
        private const float GuidanceFallbackSize = 0.1f;

        private readonly Transform target;
        private readonly Transform boundsSource;
        private readonly GameObject root;
        private readonly LineRenderer outline;
        private readonly LineRenderer arrow;
        private readonly LineRenderer arrowHead;
        private readonly Vector3[] rectCorners = new Vector3[4];
        private Color color = Color.white;

        public Transform Target => target;
        public Transform BoundsSource => boundsSource;

        public DisplayWallTargetGuidance(Transform target, Transform boundsSource, Material material)
        {
            this.target = target;
            this.boundsSource = boundsSource;
            root = new GameObject($"{target.name}_07TargetGuidance")
            {
                hideFlags = HideFlags.DontSave,
                layer = target.gameObject.layer
            };
            outline = CreateGuideLine("Outline", material, true, 4, 54);
            arrow = CreateGuideLine("Arrow", material, false, 2, 53);
            arrowHead = CreateGuideLine("ArrowHead", material, false, 3, 53);
        }

        public void SetColor(Color value)
        {
            color = value;
        }

        public void SetVisible(bool visible)
        {
            if (root != null)
            {
                root.SetActive(visible);
            }
        }

        public void Update()
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                SetVisible(false);
                return;
            }

            Bounds bounds;
            if (!TryGetBounds(out bounds))
            {
                SetVisible(false);
                return;
            }

            root.SetActive(true);

            Camera activeCamera = Camera.main;
            Vector3 center = bounds.center;
            Vector3 right = activeCamera != null ? activeCamera.transform.right : Vector3.right;
            Vector3 up = activeCamera != null ? activeCamera.transform.up : Vector3.up;
            Vector3 normal = activeCamera != null
                ? (activeCamera.transform.position - center).normalized
                : Vector3.forward;
            if (normal.sqrMagnitude < 0.001f)
            {
                normal = Vector3.forward;
            }

            float pulse = 1f + Mathf.Sin(Time.unscaledTime * GuidancePulseSpeed) * GuidancePulseAmount;
            float halfWidth = Mathf.Max(0.015f, bounds.extents.magnitude * 0.55f) + GuidancePadding;
            float halfHeight = Mathf.Max(0.015f, bounds.extents.magnitude * 0.35f) + GuidancePadding;
            halfWidth *= pulse;
            halfHeight *= pulse;
            Vector3 offset = normal * Mathf.Max(0.002f, bounds.extents.magnitude * 0.04f);

            rectCorners[0] = center - right * halfWidth - up * halfHeight + offset;
            rectCorners[1] = center - right * halfWidth + up * halfHeight + offset;
            rectCorners[2] = center + right * halfWidth + up * halfHeight + offset;
            rectCorners[3] = center + right * halfWidth - up * halfHeight + offset;

            float lineWidth = Mathf.Max(0.001f, Mathf.Min(halfWidth, halfHeight) * 0.07f);
            Color pulsingColor = color;
            pulsingColor.a *= Mathf.Lerp(0.65f, 1f,
                (pulse - (1f - GuidancePulseAmount)) / (GuidancePulseAmount * 2f));
            SetLineColorAndWidth(outline, pulsingColor, lineWidth);
            for (int i = 0; i < rectCorners.Length; i++)
            {
                outline.SetPosition(i, rectCorners[i]);
            }

            Vector3 tip = center + up * (halfHeight + GuidancePadding) + offset;
            Vector3 tail = tip + up * Mathf.Max(halfHeight * 0.9f, 0.04f);
            float arrowWidth = Mathf.Max(0.02f, halfWidth * 0.25f);
            SetLineColorAndWidth(arrow, pulsingColor, lineWidth);
            arrow.SetPosition(0, tail);
            arrow.SetPosition(1, tip);
            SetLineColorAndWidth(arrowHead, pulsingColor, lineWidth);
            Vector3 headBase = tip + up * Mathf.Max(halfHeight * 0.3f, 0.02f);
            arrowHead.SetPosition(0, headBase + right * arrowWidth);
            arrowHead.SetPosition(1, tip);
            arrowHead.SetPosition(2, headBase - right * arrowWidth);
        }

        public void Dispose()
        {
            if (root == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(root);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private LineRenderer CreateGuideLine(
            string objectName,
            Material material,
            bool loop,
            int positionCount,
            int sortingOrder)
        {
            GameObject lineObject = new GameObject(objectName)
            {
                hideFlags = HideFlags.DontSave,
                layer = root.layer
            };
            lineObject.transform.SetParent(root.transform, false);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.loop = loop;
            line.positionCount = positionCount;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.numCapVertices = 4;
            line.numCornerVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.lightProbeUsage = LightProbeUsage.Off;
            line.reflectionProbeUsage = ReflectionProbeUsage.Off;
            line.sortingOrder = sortingOrder;
            return line;
        }

        private bool TryGetBounds(out Bounds bounds)
        {
            bool found = false;
            bounds = default;

            // A target may point its frame at one of its children: the 07
            // 电脑主机 target carries a stray part far below the chassis.
            Transform source = boundsSource != null ? boundsSource : target;

            foreach (Collider collider in source.GetComponentsInChildren<Collider>(true))
            {
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
                {
                    continue;
                }

                AddBounds(ref bounds, ref found, collider.bounds);
            }

            foreach (Renderer renderer in source.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                AddBounds(ref bounds, ref found, renderer.bounds);
            }

            if (found)
            {
                return true;
            }

            bounds = new Bounds(source.position, Vector3.one * GuidanceFallbackSize);
            return true;
        }

        private static void AddBounds(ref Bounds bounds, ref bool found, Bounds value)
        {
            if (!found)
            {
                bounds = value;
                found = true;
            }
            else
            {
                bounds.Encapsulate(value);
            }
        }

        private static void SetLineColorAndWidth(LineRenderer line, Color value, float width)
        {
            line.startWidth = width;
            line.endWidth = width;
            line.startColor = value;
            line.endColor = value;
        }
    }

    private void CaptureCompleteAssemblyPose()
    {
        if (completeAssembly == null)
        {
            return;
        }
        completeAssemblyStartPosition = completeAssembly.position;
        completeAssemblyStartRotation = completeAssembly.rotation;
        completeAssemblyPoseCaptured = true;
    }

    private void RestoreCompleteAssemblyPose()
    {
        if (completeAssembly != null && completeAssemblyPoseCaptured)
        {
            completeAssembly.SetPositionAndRotation(completeAssemblyStartPosition, completeAssemblyStartRotation);
        }
    }

    private void EnsureMoveInteractable()
    {
        if (completeAssembly == null)
        {
            return;
        }

        ObjectManipulator manipulator = completeAssembly.GetComponent<ObjectManipulator>();
        if (manipulator == null)
        {
            manipulator = completeAssembly.gameObject.AddComponent<ObjectManipulator>();
        }
        manipulator.AllowedManipulations = MixedReality.Toolkit.TransformFlags.Move;
        TexCore_03NoScalePolicy.Enforce(completeAssembly.gameObject);
        // Kinematic movement keeps the player's translation authoritative and
        // stops PhysX from pushing or spinning the rack.
        manipulator.RigidbodyMovementType = XRBaseInteractable.MovementType.Kinematic;
        manipulator.enabled = true;

        Rigidbody body = completeAssembly.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = completeAssembly.gameObject.AddComponent<Rigidbody>();
        }
        body.useGravity = false;
        // The finished rack is a scene prop: it must only translate across the
        // floor. Keeping the body kinematic and rotation/height locked prevents
        // the enabled box collider from being ejected out of the floor as a
        // spinning body and keeps physics-driven movement on the same plane.
        body.isKinematic = true;
        body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        TexCore_RackAssemblyInteractable item = completeAssembly.GetComponent<TexCore_RackAssemblyInteractable>();
        if (item == null)
        {
            item = completeAssembly.gameObject.AddComponent<TexCore_RackAssemblyInteractable>();
        }
        item.Initialize(this, activeStepIndex, string.Empty, false);
        SetAssemblyCollidersEnabled(true);
    }

    private void SetAssemblyManipulationEnabled(bool value)
    {
        if (completeAssembly == null)
        {
            return;
        }
        ObjectManipulator manipulator = completeAssembly.GetComponent<ObjectManipulator>();
        if (manipulator != null)
        {
            manipulator.enabled = value;
        }
        if (!value)
        {
            TexCore_RackAssemblyInteractable item = completeAssembly.GetComponent<TexCore_RackAssemblyInteractable>();
            item?.HideGuidance();
            FreezeAssemblyPhysics();
        }
        SetAssemblyCollidersEnabled(value);
    }

    private void FreezeAssemblyPhysics()
    {
        if (completeAssembly == null)
        {
            return;
        }

        Rigidbody body = completeAssembly.GetComponent<Rigidbody>();
        if (body == null)
        {
            return;
        }

        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.useGravity = false;
        body.isKinematic = true;
        body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
    }

    private void SetAssemblyCollidersEnabled(bool value)
    {
        if (completeAssembly == null)
        {
            return;
        }
        foreach (Collider item in completeAssembly.GetComponents<Collider>())
        {
            item.enabled = value;
        }
    }

    private static void SetListActive(IEnumerable<Transform> items, bool value)
    {
        if (items == null)
        {
            return;
        }
        foreach (Transform item in items)
        {
            SetControlledActive(item, value);
        }
    }

    private static void SetAt(IReadOnlyList<Transform> items, int index, bool value)
    {
        if (items != null && index >= 0 && index < items.Count)
        {
            SetControlledActive(items[index], value);
        }
    }

    private static void SetControlledActive(Transform item, bool value)
    {
        if (item == null)
        {
            return;
        }

        item.gameObject.SetActive(value);
        if (value)
        {
            EnsureResultVisible(item);
        }
    }

    /// <summary>
    /// The authored LuoSi animation disables the screw MeshRenderer at the end
    /// of its non-looping 1.55s clip, so every installed screw vanished about
    /// 1.5s after the controller showed it. Stop that animator and force the
    /// renderers back on so installed parts stay visible.
    /// </summary>
    private static void EnsureResultVisible(Transform item)
    {
        foreach (Animator animator in item.GetComponentsInChildren<Animator>(true))
        {
            if (animator != null && animator.enabled)
            {
                animator.enabled = false;
            }
        }

        foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer != null && !renderer.enabled)
            {
                renderer.enabled = true;
            }
        }
    }

    private static int GetTargetCount(RackAssemblyStepDefinition step)
    {
        if (step == null)
        {
            return 0;
        }
        if (step.kind == RackAssemblyStepKind.UseTool)
        {
            return step.toolTargets?.Count ?? 0;
        }
        if (step.kind == RackAssemblyStepKind.MoveAssembly)
        {
            return step.moveTarget == null ? 0 : 1;
        }
        return step.blueTargets?.Count ?? 0;
    }

    private static bool StepAcceptsPart(RackAssemblyStepDefinition step, string partKey)
    {
        if (step == null || string.IsNullOrEmpty(partKey))
        {
            return false;
        }
        if (step.partKey == partKey)
        {
            return true;
        }
        return step.targetPartKeys != null && step.targetPartKeys.Contains(partKey);
    }

    private static bool TargetAcceptsPart(RackAssemblyStepDefinition step, int index, string partKey)
    {
        if (step == null || step.targetPartKeys == null || step.targetPartKeys.Count == 0)
        {
            return true;
        }
        if (index < 0 || index >= step.targetPartKeys.Count)
        {
            return false;
        }
        return step.targetPartKeys[index] == partKey;
    }

    private void CompleteCurrentStep()
    {
        if (!active)
        {
            return;
        }
        active = false;
        SetDisplayWallTargetGuidanceVisible(false);
        SetBackpackVisible(false);
        if (activeReporter != null)
        {
            TexCore_BusinessManager.OnClick(activeReporter);
        }
    }

    private void UpdateProgress(RackAssemblyStepDefinition step)
    {
        int total = GetTargetCount(step);
        TexCore_EventCenter.Broadcast<string>(
            TexCore_EventType.显示标题,
            $"{GetProgressTitle(step)}（{completedTargets.Count}/{total}）");
    }

    /// <summary>
    /// Step names already carry an authored "（0/16）" suffix. Strip that stale
    /// suffix so the live progress title replaces it instead of appending a
    /// second counter.
    /// </summary>
    private static string GetProgressTitle(RackAssemblyStepDefinition step)
    {
        string name = step != null ? step.name : string.Empty;
        if (string.IsNullOrEmpty(name) || name[name.Length - 1] != '）')
        {
            return name;
        }

        int start = name.LastIndexOf('（');
        if (start < 0)
        {
            return name;
        }

        string inner = name.Substring(start + 1, name.Length - start - 2);
        int slash = inner.IndexOf('/');
        if (slash <= 0 || slash >= inner.Length - 1)
        {
            return name;
        }

        for (int i = 0; i < inner.Length; i++)
        {
            if (i != slash && !char.IsDigit(inner[i]))
            {
                return name;
            }
        }

        return name.Substring(0, start).TrimEnd();
    }

    private static void ShowHint(string text)
    {
        TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, text, 1.5f);
    }

    private void SetBackpackVisible(bool value)
    {
        TexCore_BackpackChapterTabs.EnsureInitialized();
        if (value)
        {
            TexCore_BackpackChapterTabs.ShowPanel(backpackPanel);
        }
        else if (backpackPanel != null && backpackPanel.activeSelf)
        {
            TexCore_BackpackChapterTabs.ShowDefault();
        }
    }

    private static bool NeedsBackpackConfirmButton(RackAssemblyStepDefinition step)
    {
        return step != null &&
               step.kind == RackAssemblyStepKind.Instruction &&
               step.waitForConfirmation;
    }

    private static Transform GetAt(IReadOnlyList<Transform> items, int index)
    {
        return items != null && index >= 0 && index < items.Count ? items[index] : null;
    }

    public void ShowDefaultBackpackChapter()
    {
        TexCore_BackpackChapterTabs.ShowDefault();
    }

    public void ShowRackBackpackChapter()
    {
        TexCore_BackpackChapterTabs.ShowPanel(backpackPanel);
    }

    private void DestroySpawned(GameObject spawned)
    {
        TexCore_RackAssemblyInteractable item = spawned != null
            ? spawned.GetComponent<TexCore_RackAssemblyInteractable>()
            : null;
        item?.HideGuidance();
        spawnedObjects.Remove(spawned);
        Destroy(spawned);
    }

    private void ClearSpawnedObjects()
    {
        previewTargets.Clear();
        foreach (GameObject spawned in spawnedObjects)
        {
            if (spawned != null)
            {
                TexCore_RackAssemblyInteractable item = spawned.GetComponent<TexCore_RackAssemblyInteractable>();
                item?.HideGuidance();
                Destroy(spawned);
            }
        }
        spawnedObjects.Clear();
    }

    private bool IsValidStep(int index)
    {
        return steps != null && index >= 0 && index < steps.Count;
    }
}

[DisallowMultipleComponent]
public sealed class TexCore_RackAssemblyInteractable : MonoBehaviour
{
    private const int GuideCircleSegments = 48;
    private const float SourceMarkerRadius = 0.06f;
    private const float TargetMarkerRadius = 0.07f;
    private const float GuideLineWidth = 0.008f;
    private const float GuideVerticalOffset = 0.015f;
    private const float PulseAmount = 0.18f;
    private const float PulseSpeed = 3f;
    private static readonly Color SourceGuideColor = new Color(1f, 0.78f, 0.05f, 1f);
    private static readonly Color TargetGuideColor = new Color(0.05f, 0.95f, 1f, 1f);

    private TexCore_RackAssemblyController controller;
    private ObjectManipulator manipulator;
    private GameObject guideRoot;
    private LineRenderer sourceMarker;
    private LineRenderer targetMarker;
    private LineRenderer directionLine;
    private LineRenderer arrowHead;
    private Material guideMaterial;
    private bool completed;

    public int StepIndex { get; private set; }
    public string ItemKey { get; private set; }

    public void Initialize(TexCore_RackAssemblyController owner, int stepIndex, string itemKey, bool tool)
    {
        if (manipulator != null)
        {
            manipulator.lastSelectExited.RemoveListener(HandleReleased);
        }
        controller = owner;
        StepIndex = stepIndex;
        ItemKey = itemKey;
        completed = false;
        manipulator = GetComponent<ObjectManipulator>();
        if (manipulator != null)
        {
            manipulator.lastSelectExited.AddListener(HandleReleased);
        }
        UpdateGuidance(controller != null ? controller.GetGuidanceTarget(this) : null);
    }

    private void OnDisable()
    {
        SetGuidanceVisible(false);
    }

    private void OnDestroy()
    {
        SetGuidanceVisible(false);
        controller?.ClearTargetPreview(this);
        if (manipulator != null)
        {
            manipulator.lastSelectExited.RemoveListener(HandleReleased);
        }

        if (guideRoot != null)
        {
            if (Application.isPlaying)
            {
                Destroy(guideRoot);
            }
            else
            {
                DestroyImmediate(guideRoot);
            }
        }

        if (guideMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(guideMaterial);
            }
            else
            {
                DestroyImmediate(guideMaterial);
            }
        }
    }

    private void Update()
    {
        if (completed)
        {
            SetGuidanceVisible(false);
            return;
        }

        controller?.UpdateTargetPreview(this);
        UpdateGuidance(controller != null ? controller.GetGuidanceTarget(this) : null);
    }

    private void HandleReleased(SelectExitEventArgs args)
    {
        if (!completed && controller != null && controller.TryCompleteTarget(this))
        {
            completed = true;
            SetGuidanceVisible(false);
        }
    }

    public void HideGuidance()
    {
        SetGuidanceVisible(false);
    }

    private void SetGuidanceVisible(bool visible)
    {
        if (guideRoot != null)
        {
            guideRoot.SetActive(visible);
        }
    }

    private void EnsureGuidanceCreated()
    {
        if (guideRoot != null)
        {
            return;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            BpfLog.Warning(BpfLogCategory.Scene, "tex_core_rack_assembly_controller.ensure_guidance_created", $"{name} cannot create rack guidance because Sprites/Default shader was not found.", this);
            return;
        }

        guideRoot = new GameObject($"{name}_RackGuidance");
        guideRoot.hideFlags = HideFlags.DontSave;
        guideRoot.layer = gameObject.layer;

        guideMaterial = new Material(shader)
        {
            name = $"{name}_RackGuidanceMaterial",
            hideFlags = HideFlags.DontSave
        };

        sourceMarker = CreateGuideLine("CurrentObjectMarker", true, GuideCircleSegments);
        targetMarker = CreateGuideLine("TargetMarker", true, GuideCircleSegments);
        directionLine = CreateGuideLine("DirectionLine", false, 2);
        arrowHead = CreateGuideLine("ArrowHead", false, 3);

        directionLine.startColor = SourceGuideColor;
        directionLine.endColor = TargetGuideColor;
        arrowHead.startColor = TargetGuideColor;
        arrowHead.endColor = TargetGuideColor;
    }

    private LineRenderer CreateGuideLine(string objectName, bool loop, int positionCount)
    {
        GameObject lineObject = new GameObject(objectName);
        lineObject.hideFlags = HideFlags.DontSave;
        lineObject.layer = gameObject.layer;
        lineObject.transform.SetParent(guideRoot.transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.sharedMaterial = guideMaterial;
        line.useWorldSpace = true;
        line.loop = loop;
        line.positionCount = positionCount;
        line.startWidth = GuideLineWidth;
        line.endWidth = GuideLineWidth;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.numCapVertices = 4;
        line.numCornerVertices = 2;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.lightProbeUsage = LightProbeUsage.Off;
        line.reflectionProbeUsage = ReflectionProbeUsage.Off;
        line.sortingOrder = 50;
        return line;
    }

    private void UpdateGuidance(Transform target)
    {
        if (target == null)
        {
            SetGuidanceVisible(false);
            return;
        }

        EnsureGuidanceCreated();
        if (guideRoot == null)
        {
            return;
        }
        guideRoot.SetActive(true);

        Camera activeCamera = Camera.main;
        Vector3 sourceRight = activeCamera != null ? activeCamera.transform.right : Vector3.right;
        Vector3 sourceUp = activeCamera != null ? activeCamera.transform.up : Vector3.up;
        Vector3 sourceFacing = activeCamera != null
            ? (activeCamera.transform.position - transform.position).normalized
            : Vector3.up;
        Vector3 targetFacing = activeCamera != null
            ? (activeCamera.transform.position - target.position).normalized
            : Vector3.up;

        float pulse = 1f + Mathf.Sin(Time.unscaledTime * PulseSpeed) * PulseAmount;
        Vector3 sourceCenter = transform.position + sourceFacing * GuideVerticalOffset;
        Vector3 targetCenter = target.position + targetFacing * GuideVerticalOffset;
        SetCircle(sourceMarker, sourceCenter, sourceRight, sourceUp, SourceMarkerRadius * pulse);
        SetCircle(targetMarker, targetCenter, sourceRight, sourceUp, TargetMarkerRadius * pulse);

        Color pulsingSourceColor = SourceGuideColor;
        float pulsePhase = (pulse - (1f - PulseAmount)) / (PulseAmount * 2f);
        pulsingSourceColor.a *= Mathf.Lerp(0.6f, 1f, pulsePhase);
        sourceMarker.startColor = pulsingSourceColor;
        sourceMarker.endColor = pulsingSourceColor;
        targetMarker.startColor = TargetGuideColor;
        targetMarker.endColor = TargetGuideColor;

        Vector3 sourceToTarget = targetCenter - sourceCenter;
        float distance = sourceToTarget.magnitude;
        if (distance <= 0.001f)
        {
            directionLine.enabled = false;
            arrowHead.enabled = false;
            return;
        }

        directionLine.enabled = true;
        arrowHead.enabled = true;
        Vector3 direction = sourceToTarget / distance;
        Vector3 lineStart = sourceCenter + direction * SourceMarkerRadius;
        Vector3 lineEnd = targetCenter - direction * TargetMarkerRadius;
        directionLine.SetPosition(0, lineStart);
        directionLine.SetPosition(1, lineEnd);

        Vector3 cameraForward = activeCamera != null ? activeCamera.transform.forward : Vector3.forward;
        Vector3 arrowSide = Vector3.Cross(direction, cameraForward).normalized;
        if (arrowSide.sqrMagnitude < 0.01f)
        {
            arrowSide = Vector3.Cross(direction, Vector3.up).normalized;
        }
        if (arrowSide.sqrMagnitude < 0.01f)
        {
            arrowSide = Vector3.right;
        }

        float arrowLength = Mathf.Clamp(distance * 0.18f, 0.035f, 0.09f);
        float arrowWidth = arrowLength * 0.55f;
        Vector3 arrowBase = lineEnd - direction * arrowLength;
        arrowHead.SetPosition(0, arrowBase + arrowSide * arrowWidth);
        arrowHead.SetPosition(1, lineEnd);
        arrowHead.SetPosition(2, arrowBase - arrowSide * arrowWidth);
    }

    private static void SetCircle(
        LineRenderer line,
        Vector3 center,
        Vector3 axisX,
        Vector3 axisY,
        float radius)
    {
        axisX.Normalize();
        axisY.Normalize();
        for (int i = 0; i < GuideCircleSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / GuideCircleSegments;
            Vector3 offset = axisX * Mathf.Cos(angle) + axisY * Mathf.Sin(angle);
            line.SetPosition(i, center + offset * radius);
        }
    }
}
