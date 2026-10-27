using System.Collections.Generic;
using MixedReality.Toolkit.SpatialManipulation;
using MixedReality.Toolkit.UX;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using Bpf.Diagnostics;

/// <summary>
/// Small TexCore step adapter for fixed scene objects that should complete
/// when the user clicks or grabs/releases the object while its step is active.
/// </summary>
[DisallowMultipleComponent]
public sealed class TexCore_ClickObjectStep : TexCore_InteracterBase, ITexCoreUserCompletionStatus
{
    [SerializeField] private GameObject interactionSource;
    [SerializeField] private bool completeOnMouseClick = true;
    [SerializeField] private bool completeOnPressableButton = true;
    [SerializeField] private bool completeOnGrabRelease = false;
    [SerializeField] private bool disableColliderUntilActive = true;
    [SerializeField] private List<TexCore_InteracterBase> prerequisites = new List<TexCore_InteracterBase>();
    [SerializeField] private string prerequisiteMessage = "请先完成前置步骤。";
    [Header("当前步骤指引")]
    [SerializeField] private bool showGuidance = true;
    [SerializeField] private Color guidanceColor = new Color(1f, 0.78f, 0.05f, 1f);
    [SerializeField, Range(0f, 0.5f)] private float guidancePulseAmount = 0.18f;
    [SerializeField, Min(0f)] private float guidancePulseSpeed = 3f;
    [SerializeField, Min(0f)] private float guidancePadding = 0.02f;
    [SerializeField, Min(0.01f)] private float guidanceFallbackSize = 0.08f;
    [SerializeField] private UnityEvent onStepActive;
    [SerializeField] private UnityEvent onStepCompleted;

    private PressableButton pressableButton;
    private ObjectManipulator[] objectManipulators;
    private Rigidbody[] rigidbodies;
    private Collider[] colliders;
    private bool stepActive;
    private bool completed;
    private bool userCompleted;
    private GameObject guidanceRoot;
    private LineRenderer guidanceOutline;
    private LineRenderer guidanceArrow;
    private LineRenderer guidanceArrowHead;
    private Material guidanceMaterial;
    private readonly Vector3[] rectCorners = new Vector3[4];

    public bool WasCompletedByUser => userCompleted;

    public GameObject InteractionSource => interactionSource != null ? interactionSource : gameObject;

    /// <summary>
    /// False when this reporter is guidance only and must never complete a step.
    /// </summary>
    public bool CompletesStep =>
        completeOnMouseClick || completeOnPressableButton || completeOnGrabRelease;

    public bool ShowGuidance => showGuidance;

    public void Configure(
        bool mouseClick,
        bool pressableButton,
        bool grabRelease,
        bool disableColliders)
    {
        completeOnMouseClick = mouseClick;
        completeOnPressableButton = pressableButton;
        completeOnGrabRelease = grabRelease;
        disableColliderUntilActive = disableColliders;
        // AddComponent registers the pressable callback in OnEnable before the
        // caller can configure this reporter. Keep the serialized callback in
        // sync so a guidance-only reporter can never complete a step twice.
        CacheSourceComponents();
        SyncPressableListener();
    }

    public void ConfigureSource(GameObject source)
    {
        interactionSource = source;
        // Keep the cached components and the pressable callback on the object
        // that actually reports this step.
        CacheSourceComponents();
        SyncPressableListener();
    }

    public void ConfigurePrerequisites(IEnumerable<TexCore_InteracterBase> requirements,
        string message)
    {
        prerequisites = new List<TexCore_InteracterBase>();
        if (requirements != null)
        {
            foreach (TexCore_InteracterBase requirement in requirements)
            {
                if (requirement != null && requirement != this &&
                    !prerequisites.Contains(requirement))
                {
                    prerequisites.Add(requirement);
                }
            }
        }

        if (!string.IsNullOrEmpty(message))
        {
            prerequisiteMessage = message;
        }
    }

    private void Awake()
    {
        CacheSourceComponents();
    }

    public new void OnEnable()
    {
        base.OnEnable();
        CacheSourceComponents();
        SyncPressableListener();

        if (objectManipulators != null && completeOnGrabRelease)
        {
            foreach (ObjectManipulator manipulator in objectManipulators)
            {
                if (manipulator != null)
                {
                    manipulator.lastSelectExited.AddListener(HandleReleased);
                }
            }
        }

        ApplyInteractionState();
        SetGuidanceVisible(false);
    }

    private void OnDisable()
    {
        if (pressableButton != null)
        {
            pressableButton.OnClicked.RemoveListener(CompleteFromUser);
        }

        if (objectManipulators != null)
        {
            foreach (ObjectManipulator manipulator in objectManipulators)
            {
                if (manipulator != null)
                {
                    manipulator.lastSelectExited.RemoveListener(HandleReleased);
                }
            }
        }

        SetGuidanceVisible(false);
        base.OnDisabel();
    }

    private void OnDestroy()
    {
        if (guidanceRoot != null)
        {
            if (Application.isPlaying) Destroy(guidanceRoot);
            else DestroyImmediate(guidanceRoot);
        }
        if (guidanceMaterial != null)
        {
            if (Application.isPlaying) Destroy(guidanceMaterial);
            else DestroyImmediate(guidanceMaterial);
        }
    }

    private void Update()
    {
        if (stepActive && !completed && showGuidance)
        {
            UpdateGuidance();
        }
    }

    private void LateUpdate()
    {
        if (HasIncompletePrerequisites() &&
            !TexCore_OrderedPlacementStep.IsSourceRequiredByActiveStep(
                InteractionSource))
        {
            ForcePrerequisiteLock();
        }
    }

    private void OnMouseUpAsButton()
    {
        if (completeOnMouseClick && (interactionSource == null || interactionSource == gameObject))
        {
            CompleteFromUser();
        }
    }

    public override void UnFinished()
    {
        completed = false;
        userCompleted = false;
        stepActive = false;
        ApplyInteractionState();
        SetGuidanceVisible(false);
        TexCore_08BackpackController.NotifyStepEnded(gameObject);
    }

    public override void UnderWay()
    {
        completed = false;
        stepActive = true;
        ApplyInteractionState();
        bool prerequisitesComplete = ArePrerequisitesComplete();
        SetGuidanceVisible(prerequisitesComplete);
        if (!prerequisitesComplete)
        {
            TexCore_EventCenter.Broadcast<string, float>(
                TexCore_EventType.显示提示, prerequisiteMessage, 1.5f);
        }
        TexCore_08BackpackController.NotifyStepStarted(gameObject, InteractionSource);
        onStepActive?.Invoke();
    }

    public override void OnTrigger()
    {
        completed = true;
        stepActive = false;
        ApplyInteractionState();
        SetGuidanceVisible(false);
        TexCore_08BackpackController.NotifyStepEnded(gameObject);
        onStepCompleted?.Invoke();
    }

    public override void Finish()
    {
        completed = true;
        stepActive = false;
        ApplyInteractionState();
        SetGuidanceVisible(false);
        TexCore_08BackpackController.NotifyStepEnded(gameObject);
        onStepCompleted?.Invoke();
    }

    private void HandleReleased(SelectExitEventArgs args)
    {
        CompleteFromUser();
    }

    private void CompleteFromUser()
    {
        if (!stepActive || completed || !ArePrerequisitesComplete())
        {
            if (stepActive && !completed)
            {
                TexCore_EventCenter.Broadcast<string, float>(
                    TexCore_EventType.显示提示, prerequisiteMessage, 1.5f);
            }
            return;
        }

        completed = true;
        userCompleted = true;
        stepActive = false;
        ApplyInteractionState();
        SetGuidanceVisible(false);
        TexCore_08BackpackController.NotifyStepEnded(gameObject);
        TexCore_BusinessManager.OnClick(gameObject);
    }

    private void CacheSourceComponents()
    {
        GameObject source = interactionSource != null ? interactionSource : gameObject;
        pressableButton = source.GetComponentInChildren<PressableButton>(true);
        objectManipulators = source.GetComponentsInChildren<ObjectManipulator>(true);
        rigidbodies = source.GetComponentsInChildren<Rigidbody>(true);
        colliders = source.GetComponentsInChildren<Collider>(true);
        TexCore_03NoScalePolicy.Enforce(source);
    }

    /// <summary>
    /// Registers the pressable callback only while this reporter is allowed to
    /// complete its step. Guidance-only reporters leave the button to the step
    /// script that owns the completion.
    /// </summary>
    private void SyncPressableListener()
    {
        if (pressableButton == null) return;
        pressableButton.OnClicked.RemoveListener(CompleteFromUser);
        if (completeOnPressableButton)
        {
            pressableButton.OnClicked.AddListener(CompleteFromUser);
        }
    }

    private void ApplyInteractionState()
    {
        bool allow = stepActive && !completed && ArePrerequisitesComplete();
        bool preserveSharedSource = !allow &&
            TexCore_OrderedPlacementStep.IsSourceRequiredByActiveStep(InteractionSource);
        if (pressableButton != null)
        {
            pressableButton.enabled = allow;
        }

        if (objectManipulators != null && completeOnGrabRelease && !preserveSharedSource)
        {
            foreach (ObjectManipulator manipulator in objectManipulators)
            {
                if (manipulator != null)
                {
                    manipulator.enabled = allow;
                }
            }
        }

        if (prerequisites != null && prerequisites.Count > 0 && !preserveSharedSource)
        {
            SetRigidbodiesLocked(!allow);
        }

        if (!disableColliderUntilActive || colliders == null || preserveSharedSource)
        {
            return;
        }

        foreach (Collider value in colliders)
        {
            if (value != null)
            {
                value.enabled = allow;
            }
        }
    }

    private bool HasIncompletePrerequisites()
    {
        return prerequisites != null && prerequisites.Count > 0 &&
               !ArePrerequisitesComplete();
    }

    private void ForcePrerequisiteLock()
    {
        if (pressableButton != null)
        {
            pressableButton.enabled = false;
        }

        if (objectManipulators != null)
        {
            foreach (ObjectManipulator manipulator in objectManipulators)
            {
                if (manipulator != null)
                {
                    manipulator.enabled = false;
                }
            }
        }

        if (colliders != null)
        {
            foreach (Collider value in colliders)
            {
                if (value != null)
                {
                    value.enabled = false;
                }
            }
        }

        SetRigidbodiesLocked(true);
    }

    private void SetRigidbodiesLocked(bool locked)
    {
        if (rigidbodies == null)
        {
            return;
        }

        foreach (Rigidbody body in rigidbodies)
        {
            if (body == null)
            {
                continue;
            }

            body.isKinematic = locked;
            if (locked)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }
    }

    private bool ArePrerequisitesComplete()
    {
        if (prerequisites == null)
        {
            return true;
        }

        foreach (TexCore_InteracterBase prerequisite in prerequisites)
        {
            if (prerequisite == null)
            {
                continue;
            }

            ITexCoreUserCompletionStatus completionStatus =
                prerequisite as ITexCoreUserCompletionStatus;
            if (completionStatus != null)
            {
                if (!completionStatus.WasCompletedByUser)
                {
                    return false;
                }

                continue;
            }

            if (prerequisite.state != FinishState.触发 &&
                prerequisite.state != FinishState.已完成)
            {
                return false;
            }
        }

        return true;
    }

    private void SetGuidanceVisible(bool visible)
    {
        if (!visible || !showGuidance)
        {
            if (guidanceRoot != null) guidanceRoot.SetActive(false);
            return;
        }

        ActivateParentChain(InteractionSource.transform);
        EnsureGuidanceCreated();
        if (guidanceRoot == null) return;
        guidanceRoot.SetActive(true);
        UpdateGuidance();
    }

    private void EnsureGuidanceCreated()
    {
        if (guidanceRoot != null) return;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            BpfLog.Warning(BpfLogCategory.Input, "tex_core_click_object_step.ensure_guidance_created", $"{name} cannot create click guidance because Sprites/Default is unavailable.", this);
            showGuidance = false;
            return;
        }

        guidanceRoot = new GameObject($"{name}_ClickGuidance")
        {
            hideFlags = HideFlags.DontSave,
            layer = gameObject.layer
        };
        guidanceMaterial = new Material(shader)
        {
            name = $"{name}_ClickGuidanceMaterial",
            hideFlags = HideFlags.DontSave
        };
        guidanceOutline = CreateGuideLine("Outline", true, 4, 4);
        guidanceArrow = CreateGuideLine("Arrow", false, 2, 2);
        guidanceArrowHead = CreateGuideLine("ArrowHead", false, 3, 3);
    }

    private LineRenderer CreateGuideLine(string objectName, bool loop, int positionCount, int sortingOrder)
    {
        GameObject lineObject = new GameObject(objectName)
        {
            hideFlags = HideFlags.DontSave,
            layer = gameObject.layer
        };
        lineObject.transform.SetParent(guidanceRoot.transform, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.sharedMaterial = guidanceMaterial;
        line.useWorldSpace = true;
        line.loop = loop;
        line.positionCount = positionCount;
        line.alignment = LineAlignment.View;
        line.numCapVertices = 4;
        line.numCornerVertices = 2;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.lightProbeUsage = LightProbeUsage.Off;
        line.reflectionProbeUsage = ReflectionProbeUsage.Off;
        line.sortingOrder = sortingOrder + 50;
        return line;
    }

    private void UpdateGuidance()
    {
        if (guidanceRoot == null) EnsureGuidanceCreated();
        Bounds bounds;
        if (!TryGetGuidanceBounds(out bounds))
        {
            SetGuidanceVisible(false);
            return;
        }

        Camera activeCamera = Camera.main;
        Vector3 center = bounds.center;
        Vector3 right = activeCamera != null ? activeCamera.transform.right : Vector3.right;
        Vector3 up = activeCamera != null ? activeCamera.transform.up : Vector3.up;
        Vector3 normal = activeCamera != null ? (activeCamera.transform.position - center).normalized : Vector3.forward;
        float halfWidth = Mathf.Max(0.015f, bounds.extents.magnitude * 0.55f) + guidancePadding;
        float halfHeight = Mathf.Max(0.015f, bounds.extents.magnitude * 0.35f) + guidancePadding;
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * guidancePulseSpeed) * guidancePulseAmount;
        halfWidth *= pulse;
        halfHeight *= pulse;
        Vector3 offset = normal * Mathf.Max(0.002f, bounds.extents.magnitude * 0.04f);
        Vector3[] corners =
        {
            center - right * halfWidth - up * halfHeight + offset,
            center - right * halfWidth + up * halfHeight + offset,
            center + right * halfWidth + up * halfHeight + offset,
            center + right * halfWidth - up * halfHeight + offset
        };
        float width = Mathf.Max(0.001f, Mathf.Min(halfWidth, halfHeight) * 0.07f);
        guidanceOutline.startWidth = width;
        guidanceOutline.endWidth = width;
        guidanceOutline.startColor = guidanceColor;
        guidanceOutline.endColor = guidanceColor;
        for (int i = 0; i < corners.Length; i++) guidanceOutline.SetPosition(i, corners[i]);

        Vector3 tip = center + up * (halfHeight + guidancePadding) + offset;
        Vector3 tail = tip + up * Mathf.Max(halfHeight * 0.9f, 0.04f);
        float arrowWidth = Mathf.Max(0.02f, halfWidth * 0.25f);
        guidanceArrow.startWidth = width;
        guidanceArrow.endWidth = width;
        guidanceArrow.startColor = guidanceColor;
        guidanceArrow.endColor = guidanceColor;
        guidanceArrow.SetPosition(0, tail);
        guidanceArrow.SetPosition(1, tip);
        Vector3 headBase = tip + up * (halfHeight * 0.3f);
        guidanceArrowHead.startWidth = width;
        guidanceArrowHead.endWidth = width;
        guidanceArrowHead.startColor = guidanceColor;
        guidanceArrowHead.endColor = guidanceColor;
        guidanceArrowHead.SetPosition(0, headBase + right * arrowWidth);
        guidanceArrowHead.SetPosition(1, tip);
        guidanceArrowHead.SetPosition(2, headBase - right * arrowWidth);
    }

    private bool TryGetGuidanceBounds(out Bounds bounds)
    {
        GameObject source = interactionSource != null ? interactionSource : gameObject;
        Collider[] sourceColliders = source.GetComponentsInChildren<Collider>(true);
        bool found = false;
        bounds = default;
        foreach (Collider value in sourceColliders)
        {
            if (value == null || !value.enabled) continue;
            if (!found) { bounds = value.bounds; found = true; }
            else bounds.Encapsulate(value.bounds);
        }
        if (found) return true;

        Renderer[] renderers = source.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer value in renderers)
        {
            if (value == null) continue;
            if (!found) { bounds = value.bounds; found = true; }
            else bounds.Encapsulate(value.bounds);
        }
        if (found) return true;

        RectTransform[] rectTransforms = source.GetComponentsInChildren<RectTransform>(true);
        foreach (RectTransform value in rectTransforms)
        {
            if (value == null || value.rect.width <= 0.0001f || value.rect.height <= 0.0001f)
            {
                continue;
            }

            value.GetWorldCorners(rectCorners);
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
        if (found) return true;

        bounds = new Bounds(source.transform.position, Vector3.one * guidanceFallbackSize);
        return true;
    }

    private static void ActivateParentChain(Transform value)
    {
        for (Transform current = value; current != null; current = current.parent)
        {
            current.gameObject.SetActive(true);
        }
    }
}
