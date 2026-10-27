using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using Bpf.Diagnostics;

/// <summary>
/// Connects an MRTK grab/release interaction to the existing BPF ordered
/// operation topic and step-state system.
/// </summary>
[DisallowMultipleComponent]
public sealed class TexCore_OrderedPlacementStep : TexCore_InteracterBase, ITexCoreUserCompletionStatus
{
    private const int GuideCircleSegments = 48;

    [SerializeField] private GameObject interactionSource;
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float positionTolerance = 0.12f;
    [SerializeField, Range(0f, 180f)] private float rotationTolerance = 180f;
    [SerializeField] private bool resetWhenInactive = true;
    [SerializeField] private bool snapOnFrameworkCompletion = true;
    [SerializeField] private bool restrictManipulationsToMove = true;
    [SerializeField] private bool completeWhenWithinTolerance;
    [SerializeField] private bool requireExitBeforeAutoComplete = true;
    [SerializeField] private bool ignoreVerticalForTolerance;

    [Header("当前步骤指引")]
    [SerializeField] private bool showGuidance = true;
    [SerializeField] private Color sourceGuideColor = new Color(1f, 0.78f, 0.05f, 1f);
    [SerializeField] private Color targetGuideColor = new Color(0.05f, 0.95f, 1f, 1f);
    [SerializeField, Min(0.01f)] private float sourceMarkerRadius = 0.06f;
    [SerializeField, Min(0.01f)] private float targetMarkerRadius = 0.07f;
    [SerializeField, Min(0.001f)] private float guideLineWidth = 0.008f;
    [SerializeField, Min(0f)] private float guideVerticalOffset = 0.015f;
    [SerializeField, Min(0f)] private float pulseAmount = 0.18f;
    [SerializeField, Min(0f)] private float pulseSpeed = 3f;
    [SerializeField, Min(0.01f)] private float maximumMarkerRadius = 1.2f;

    private ObjectManipulator manipulator;
    private Rigidbody body;
    private GameObject guideRoot;
    private LineRenderer sourceMarker;
    private LineRenderer targetMarker;
    private LineRenderer directionLine;
    private LineRenderer arrowHead;
    private Material guideMaterial;
    private readonly Vector3[] rectCorners = new Vector3[4];
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 startScale;
    private bool initialized;
    private bool stepActive;
    private bool completed;
    private bool userCompleted;
    private bool autoCompleteApproachArmed;

    public bool WasCompletedByUser => userCompleted;

    public GameObject InteractionSource => interactionSource != null ? interactionSource : gameObject;

    public static bool IsSourceRequiredByActiveStep(GameObject source)
    {
        if (source == null)
        {
            return false;
        }

        foreach (TexCore_OrderedPlacementStep other in
                 FindObjectsOfType<TexCore_OrderedPlacementStep>(true))
        {
            if (other.stepActive && !other.completed &&
                other.InteractionSource == source)
            {
                return true;
            }
        }

        return false;
    }

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    public float PositionTolerance
    {
        get => positionTolerance;
        set => positionTolerance = Mathf.Max(0f, value);
    }

    public float RotationTolerance
    {
        get => rotationTolerance;
        set => rotationTolerance = Mathf.Clamp(value, 0f, 180f);
    }

    /// <summary>
    /// Compares only the horizontal distance to the target. Use it for sources
    /// whose vertical axis is locked by a constraint, where the target origin
    /// cannot be reached on Y.
    /// </summary>
    public bool IgnoreVerticalForTolerance
    {
        get => ignoreVerticalForTolerance;
        set => ignoreVerticalForTolerance = value;
    }

    public void ConfigureSource(GameObject source)
    {
        interactionSource = source;
        CacheInteractionSource();
        CaptureStartPose();
    }

    /// <summary>
    /// Makes a placement complete as soon as the player is actively holding
    /// the source within the supplied target tolerance. This is used for
    /// drill operations, where forcing an exact release is unnecessarily hard.
    /// </summary>
    public void ConfigureAutoCompleteOnApproach(float tolerance,
        bool requireExitBeforeApproach = true)
    {
        completeWhenWithinTolerance = true;
        requireExitBeforeAutoComplete = requireExitBeforeApproach;
        PositionTolerance = tolerance;
    }

    /// <summary>
    /// Keeps a shared scene object where the player left it when TexCore
    /// changes steps programmatically. User-completed placement still snaps
    /// to its intended target.
    /// </summary>
    public void ConfigurePreserveSourcePoseAcrossStateChanges()
    {
        resetWhenInactive = false;
        snapOnFrameworkCompletion = false;
        restrictManipulationsToMove = false;
    }

    /// <summary>
    /// 运行期把本步骤的目标换成重新分配的物体（09 拆钉按“离钻头最近”重排），
    /// 并按新目标重算“必须先接近”的待命标记：新目标本来就在容差内时不能立刻
    /// 判定完成，否则玩家一抓起钻头就白拿一步。
    /// </summary>
    public void RetargetFromRuntime(Transform newTarget)
    {
        target = newTarget;
        autoCompleteApproachArmed = !IsWithinTargetTolerance();
    }

    private void Awake()
    {
        CacheInteractionSource();
        CaptureStartPose();
    }

    private void Start()
    {
        // Cable/plug tasks should move as one rigid object. Rotation and scale
        // are resolved by the configured target pose after a successful drop.
        if (manipulator != null && restrictManipulationsToMove)
        {
            manipulator.AllowedManipulations = TransformFlags.Move;
        }
    }

    public new void OnEnable()
    {
        base.OnEnable();
        CacheInteractionSource();
        if (manipulator != null)
        {
            manipulator.lastSelectExited.AddListener(HandleReleased);
        }
    }

    private void OnDisable()
    {
        SetGuidanceVisible(false);
        if (manipulator != null)
        {
            manipulator.lastSelectExited.RemoveListener(HandleReleased);
        }

        base.OnDisabel();
    }

    private void OnDestroy()
    {
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
        if (stepActive && completeWhenWithinTolerance && manipulator != null &&
            manipulator.isSelected)
        {
            UpdateAutoCompleteApproachArm();
            if (CanAutoCompleteOnApproach())
            {
                CompleteFromUser();
                return;
            }
        }

        if (stepActive && showGuidance)
        {
            UpdateGuidance();
        }
    }

    public override void UnFinished()
    {
        base.UnFinished();
        bool wasActive = stepActive;
        completed = false;
        userCompleted = false;
        stepActive = false;
        autoCompleteApproachArmed = false;
        SetManipulationEnabled(false);
        if (resetWhenInactive && (!completeWhenWithinTolerance || wasActive) &&
            ShouldResetToStart())
        {
            ResetToStart();
        }
        SetGuidanceVisible(false);
        if (!completeWhenWithinTolerance || wasActive)
        {
            TexCore_08BackpackController.NotifyStepEnded(gameObject);
        }
    }

    public override void UnderWay()
    {
        base.UnderWay();
        bool needsActivation = !stepActive || completed;
        completed = false;
        stepActive = true;
        SetManipulationEnabled(true);
        if (needsActivation)
        {
            autoCompleteApproachArmed = !requireExitBeforeAutoComplete ||
                                      !IsWithinTargetTolerance();
        }
        if (resetWhenInactive && (!completeWhenWithinTolerance || needsActivation) &&
            ShouldResetToStart())
        {
            ResetToStart();
        }
        SetGuidanceVisible(true);
        if (!completeWhenWithinTolerance || needsActivation)
        {
            TexCore_08BackpackController.NotifyStepStarted(gameObject, InteractionSource);
        }
    }

    public override void OnTrigger()
    {
        if (completed)
        {
            return;
        }

        base.OnTrigger();
        CompleteAtTarget(snapOnFrameworkCompletion);
    }

    public override void Finish()
    {
        if (completed)
        {
            return;
        }

        base.Finish();
        CompleteAtTarget(snapOnFrameworkCompletion);
    }

    public void CaptureStartPose()
    {
        startPosition = SourceTransform.position;
        startRotation = SourceTransform.rotation;
        startScale = SourceTransform.localScale;
        initialized = true;
    }

    public bool IsWithinTargetTolerance()
    {
        if (target == null)
        {
            return false;
        }

        bool positionMatches = DistanceToTarget() <= positionTolerance;
        bool rotationMatches = rotationTolerance >= 180f ||
                               Quaternion.Angle(SourceTransform.rotation, target.rotation) <= rotationTolerance;
        return positionMatches && rotationMatches;
    }

    private float DistanceToTarget()
    {
        Vector3 delta = SourceTransform.position - target.position;
        if (!ignoreVerticalForTolerance)
        {
            return delta.magnitude;
        }

        // The 09 robot keeps its vertical axis locked while its QR target origin
        // sits lower than the robot origin, so the vertical delta must stay out
        // of the placement check.
        return new Vector2(delta.x, delta.z).magnitude;
    }

    private void HandleReleased(SelectExitEventArgs args)
    {
        if (!stepActive || completed || target == null)
        {
            return;
        }

        if (completeWhenWithinTolerance)
        {
            UpdateAutoCompleteApproachArm();
            if (!CanAutoCompleteOnApproach())
            {
                return;
            }
        }
        else if (!IsWithinTargetTolerance())
        {
            return;
        }

        CompleteFromUser();
    }

    private void CompleteFromUser()
    {
        if (!stepActive || completed || target == null)
        {
            return;
        }

        SnapToTarget();
        completed = true;
        stepActive = false;
        userCompleted = true;
        SetGuidanceVisible(false);
        if (!completeWhenWithinTolerance)
        {
            SetManipulationEnabled(false);
        }
        TexCore_08BackpackController.NotifyStepEnded(gameObject);
        TexCore_BusinessManager.OnClick(gameObject);
    }

    private void CompleteAtTarget(bool snapToTarget)
    {
        if (completed || (completeWhenWithinTolerance && !stepActive))
        {
            return;
        }

        completed = true;
        stepActive = false;
        SetGuidanceVisible(false);
        if (snapToTarget)
        {
            SnapToTarget();
        }
        if (!completeWhenWithinTolerance)
        {
            SetManipulationEnabled(false);
        }
        TexCore_08BackpackController.NotifyStepEnded(gameObject);
    }

    private void UpdateAutoCompleteApproachArm()
    {
        if (completeWhenWithinTolerance && requireExitBeforeAutoComplete &&
            !autoCompleteApproachArmed && !IsWithinTargetTolerance())
        {
            autoCompleteApproachArmed = true;
        }
    }

    private bool CanAutoCompleteOnApproach()
    {
        return IsWithinTargetTolerance() &&
               (!requireExitBeforeAutoComplete || autoCompleteApproachArmed);
    }

    private bool ShouldResetToStart()
    {
        foreach (TexCore_OrderedPlacementStep other in
                 FindObjectsOfType<TexCore_OrderedPlacementStep>(true))
        {
            if (other != this && other.completed &&
                other.InteractionSource == InteractionSource)
            {
                return false;
            }
        }

        return true;
    }

    private void SetGuidanceVisible(bool visible)
    {
        if (!visible || !showGuidance || target == null)
        {
            if (guideRoot != null)
            {
                guideRoot.SetActive(false);
            }
            return;
        }

        ActivateParentChain(SourceTransform);
        ActivateParentChain(target);
        EnsureGuidanceCreated();
        if (guideRoot == null)
        {
            return;
        }
        guideRoot.SetActive(true);
        UpdateGuidance();
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
            BpfLog.Warning(BpfLogCategory.Scene, "tex_core_ordered_placement_step.ensure_guidance_created", $"{name} 无法创建步骤指引：找不到 Sprites/Default Shader。", this);
            showGuidance = false;
            return;
        }

        guideRoot = new GameObject($"{name}_StepGuidance");
        guideRoot.hideFlags = HideFlags.DontSave;
        guideRoot.layer = SourceGameObject.layer;

        guideMaterial = new Material(shader)
        {
            name = $"{name}_StepGuidanceMaterial",
            hideFlags = HideFlags.DontSave
        };

        sourceMarker = CreateGuideLine("CurrentObjectMarker", true, GuideCircleSegments);
        targetMarker = CreateGuideLine("TargetMarker", true, GuideCircleSegments);
        directionLine = CreateGuideLine("DirectionLine", false, 2);
        arrowHead = CreateGuideLine("ArrowHead", false, 3);

        directionLine.startColor = sourceGuideColor;
        directionLine.endColor = targetGuideColor;
        arrowHead.startColor = targetGuideColor;
        arrowHead.endColor = targetGuideColor;
    }

    private LineRenderer CreateGuideLine(string objectName, bool loop, int positionCount)
    {
        GameObject lineObject = new GameObject(objectName);
        lineObject.hideFlags = HideFlags.DontSave;
        lineObject.layer = SourceGameObject.layer;
        lineObject.transform.SetParent(guideRoot.transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.sharedMaterial = guideMaterial;
        line.useWorldSpace = true;
        line.loop = loop;
        line.positionCount = positionCount;
        line.startWidth = guideLineWidth;
        line.endWidth = guideLineWidth;
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

    private void UpdateGuidance()
    {
        if (target == null)
        {
            SetGuidanceVisible(false);
            return;
        }

        if (guideRoot == null)
        {
            EnsureGuidanceCreated();
            if (guideRoot == null)
            {
                return;
            }
        }

        Camera activeCamera = Camera.main;
        Vector3 sourceRight = activeCamera != null ? activeCamera.transform.right : Vector3.right;
        Vector3 sourceUp = activeCamera != null ? activeCamera.transform.up : Vector3.up;
        Vector3 sourceFacing = activeCamera != null
            ? (activeCamera.transform.position - SourceTransform.position).normalized
            : Vector3.up;
        Vector3 targetFacing = activeCamera != null
            ? (activeCamera.transform.position - target.position).normalized
            : Vector3.up;

        float pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;
        float sourceRadius = GetMarkerRadius(SourceTransform, sourceMarkerRadius);
        float targetRadius = GetMarkerRadius(target, targetMarkerRadius);
        Vector3 sourceCenter = GetGuideCenter(SourceTransform) + sourceFacing * guideVerticalOffset;
        Vector3 targetCenter = GetGuideCenter(target) + targetFacing * guideVerticalOffset;
        SetCircle(sourceMarker, sourceCenter, sourceRight, sourceUp, sourceRadius * pulse);
        SetCircle(targetMarker, targetCenter, sourceRight, sourceUp, targetRadius * pulse);

        Color pulsingSourceColor = sourceGuideColor;
        float pulsePhase = pulseAmount > 0f
            ? (pulse - (1f - pulseAmount)) / (pulseAmount * 2f)
            : 1f;
        pulsingSourceColor.a *= Mathf.Lerp(0.6f, 1f, pulsePhase);
        sourceMarker.startColor = pulsingSourceColor;
        sourceMarker.endColor = pulsingSourceColor;
        targetMarker.startColor = targetGuideColor;
        targetMarker.endColor = targetGuideColor;

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
        Vector3 lineStart = sourceCenter + direction * sourceRadius;
        Vector3 lineEnd = targetCenter - direction * targetRadius;
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

    private Vector3 GetGuideCenter(Transform value)
    {
        Bounds bounds;
        return TryGetGuidanceBounds(value, out bounds) ? bounds.center : value.position;
    }

    private float GetMarkerRadius(Transform value, float minimumRadius)
    {
        Bounds bounds;
        if (!TryGetGuidanceBounds(value, out bounds))
        {
            return minimumRadius;
        }

        return Mathf.Clamp(bounds.extents.magnitude * 0.75f + minimumRadius,
            minimumRadius, maximumMarkerRadius);
    }

    private bool TryGetGuidanceBounds(Transform value, out Bounds bounds)
    {
        bool found = false;
        bounds = default;
        foreach (Collider collider in value.GetComponentsInChildren<Collider>(true))
        {
            if (collider == null) continue;
            if (!found) { bounds = collider.bounds; found = true; }
            else bounds.Encapsulate(collider.bounds);
        }
        if (found) return true;

        foreach (Renderer renderer in value.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null) continue;
            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        if (found) return true;

        foreach (RectTransform rectTransform in value.GetComponentsInChildren<RectTransform>(true))
        {
            if (rectTransform == null || rectTransform.rect.width <= 0.0001f ||
                rectTransform.rect.height <= 0.0001f)
            {
                continue;
            }

            rectTransform.GetWorldCorners(rectCorners);
            for (int i = 0; i < rectCorners.Length; i++)
            {
                if (!found)
                {
                    bounds = new Bounds(rectCorners[i], Vector3.zero);
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(rectCorners[i]);
                }
            }
        }
        return found;
    }

    private static void ActivateParentChain(Transform value)
    {
        for (Transform current = value; current != null; current = current.parent)
        {
            current.gameObject.SetActive(true);
        }
    }

    private void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        Vector3 position = target.position;
        if (ignoreVerticalForTolerance)
        {
            // The target is a floor marker whose origin sits below the movable
            // object (the 09 robot QR code), so only the horizontal pose may be
            // forced; the object keeps the height the player released it at.
            position.y = SourceTransform.position.y;
        }

        SourceTransform.SetPositionAndRotation(position, target.rotation);
        StopBodyMotion();
    }

    private void ResetToStart()
    {
        if (!initialized)
        {
            CaptureStartPose();
        }

        SourceTransform.SetPositionAndRotation(startPosition, startRotation);
        SourceTransform.localScale = startScale;
        StopBodyMotion();
    }

    private void StopBodyMotion()
    {
        if (body == null)
        {
            return;
        }

        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    private void SetManipulationEnabled(bool enabled)
    {
        if (!enabled && IsInteractionSourceRequiredByAnotherActiveStep())
        {
            return;
        }

        if (manipulator != null)
        {
            manipulator.enabled = enabled;
        }
    }

    private bool IsInteractionSourceRequiredByAnotherActiveStep()
    {
        return IsSourceRequiredByActiveStep(InteractionSource);
    }

    private GameObject SourceGameObject => interactionSource != null ? interactionSource : gameObject;

    private Transform SourceTransform => SourceGameObject.transform;

    private void CacheInteractionSource()
    {
        GameObject source = SourceGameObject;
        manipulator = source.GetComponent<ObjectManipulator>();
        body = source.GetComponent<Rigidbody>();
        TexCore_03NoScalePolicy.Enforce(source);
    }
}
