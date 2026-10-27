using MixedReality.Toolkit.UX;
using UnityEngine;
using UnityEngine.Rendering;
using Bpf.Diagnostics;

/// <summary>
/// Connects an MRTK pressable button to the TexCore operation-step flow.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PressableButton))]
public sealed class TexCore_PressableButtonStep : TexCore_InteracterBase
{
    private PressableButton pressableButton;
    [SerializeField] private bool showGuidance = true;
    [SerializeField] private Color guidanceColor = new Color(1f, 0.78f, 0.05f, 1f);
    [SerializeField, Range(0f, 0.5f)] private float guidancePulseAmount = 0.18f;
    [SerializeField, Min(0f)] private float guidancePulseSpeed = 3f;

    private RectTransform buttonRectTransform;
    private GameObject guidanceRoot;
    private LineRenderer guidanceOutline;
    private LineRenderer guidanceArrow;
    private LineRenderer guidanceArrowHead;
    private Material guidanceMaterial;
    private readonly Vector3[] buttonCorners = new Vector3[4];
    private bool stepActive;
    private bool completed;

    private void Awake()
    {
        pressableButton = GetComponent<PressableButton>();
        buttonRectTransform = GetComponent<RectTransform>();
    }

    public new void OnEnable()
    {
        base.OnEnable();
        if (pressableButton == null)
        {
            pressableButton = GetComponent<PressableButton>();
        }

        pressableButton.OnClicked.AddListener(HandleClicked);
        ApplyInteractionState();
    }

    private void OnDisable()
    {
        SetGuidanceVisible(false);
        if (pressableButton != null)
        {
            pressableButton.OnClicked.RemoveListener(HandleClicked);
        }

        base.OnDisabel();
    }

    private void OnDestroy()
    {
        if (guidanceRoot != null)
        {
            if (Application.isPlaying)
            {
                Destroy(guidanceRoot);
            }
            else
            {
                DestroyImmediate(guidanceRoot);
            }
        }

        if (guidanceMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(guidanceMaterial);
            }
            else
            {
                DestroyImmediate(guidanceMaterial);
            }
        }
    }

    private void Update()
    {
        if (stepActive && showGuidance)
        {
            UpdateGuidance();
        }
    }

    public override void UnFinished()
    {
        base.UnFinished();
        completed = false;
        stepActive = false;
        ApplyInteractionState();
        SetGuidanceVisible(false);
    }

    public override void UnderWay()
    {
        base.UnderWay();
        completed = false;
        stepActive = true;
        ApplyInteractionState();
        SetGuidanceVisible(true);
    }

    public override void OnTrigger()
    {
        base.OnTrigger();
        completed = true;
        stepActive = false;
        ApplyInteractionState();
        SetGuidanceVisible(false);
    }

    public override void Finish()
    {
        base.Finish();
        completed = true;
        stepActive = false;
        ApplyInteractionState();
        SetGuidanceVisible(false);
    }

    private void HandleClicked()
    {
        if (!stepActive || completed)
        {
            return;
        }

        completed = true;
        stepActive = false;
        ApplyInteractionState();
        SetGuidanceVisible(false);
        TexCore_BusinessManager.OnClick(gameObject);
    }

    private void ApplyInteractionState()
    {
        if (pressableButton != null)
        {
            pressableButton.enabled = stepActive && !completed;
        }
    }

    private void SetGuidanceVisible(bool visible)
    {
        if (!visible || !showGuidance || buttonRectTransform == null)
        {
            if (guidanceRoot != null)
            {
                guidanceRoot.SetActive(false);
            }
            return;
        }

        EnsureGuidanceCreated();
        if (guidanceRoot == null)
        {
            return;
        }

        guidanceRoot.SetActive(true);
        UpdateGuidance();
    }

    private void EnsureGuidanceCreated()
    {
        if (guidanceRoot != null)
        {
            return;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            BpfLog.Warning(BpfLogCategory.Input, "tex_core_pressable_button_step.ensure_guidance_created", $"{name} cannot create button guidance because Sprites/Default is unavailable.", this);
            showGuidance = false;
            return;
        }

        guidanceRoot = new GameObject($"{name}_ButtonGuidance")
        {
            hideFlags = HideFlags.DontSave,
            layer = gameObject.layer
        };
        guidanceMaterial = new Material(shader)
        {
            name = $"{name}_ButtonGuidanceMaterial",
            hideFlags = HideFlags.DontSave
        };

        guidanceOutline = CreateGuideLine("Outline", true, 4);
        guidanceArrow = CreateGuideLine("Arrow", false, 2);
        guidanceArrowHead = CreateGuideLine("ArrowHead", false, 3);
    }

    private LineRenderer CreateGuideLine(string objectName, bool loop, int positionCount)
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
        line.sortingOrder = 50;
        return line;
    }

    private void UpdateGuidance()
    {
        if (buttonRectTransform == null)
        {
            SetGuidanceVisible(false);
            return;
        }

        if (guidanceRoot == null)
        {
            EnsureGuidanceCreated();
            if (guidanceRoot == null)
            {
                return;
            }
        }

        buttonRectTransform.GetWorldCorners(buttonCorners);
        Vector3 center = (buttonCorners[0] + buttonCorners[1] + buttonCorners[2] + buttonCorners[3]) * 0.25f;
        float width = Vector3.Distance(buttonCorners[0], buttonCorners[3]);
        float height = Vector3.Distance(buttonCorners[0], buttonCorners[1]);
        float size = Mathf.Max(0.01f, Mathf.Min(width, height));
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * guidancePulseSpeed) * guidancePulseAmount;
        float padding = size * 0.22f * pulse;
        float lineWidth = Mathf.Max(0.001f, size * 0.045f);

        Vector3 right = (buttonCorners[3] - buttonCorners[0]).normalized;
        Vector3 up = (buttonCorners[1] - buttonCorners[0]).normalized;
        Vector3 normal = Vector3.Cross(right, up).normalized;
        Camera activeCamera = Camera.main;
        if (activeCamera != null && Vector3.Dot(normal, activeCamera.transform.position - center) < 0f)
        {
            normal = -normal;
        }
        Vector3 surfaceOffset = normal * Mathf.Max(0.002f, size * 0.06f);

        guidanceOutline.startWidth = lineWidth;
        guidanceOutline.endWidth = lineWidth;
        guidanceOutline.startColor = guidanceColor;
        guidanceOutline.endColor = guidanceColor;
        for (int i = 0; i < buttonCorners.Length; i++)
        {
            Vector3 cornerOffset = (buttonCorners[i] - center).normalized * padding;
            guidanceOutline.SetPosition(i, buttonCorners[i] + cornerOffset + surfaceOffset);
        }

        float arrowLength = Mathf.Max(size * 1.1f, 0.03f);
        float arrowWidth = arrowLength * 0.45f;
        Vector3 tip = center + up * (height * 0.5f + padding) + surfaceOffset;
        Vector3 tail = tip + up * arrowLength;

        guidanceArrow.startWidth = lineWidth;
        guidanceArrow.endWidth = lineWidth;
        guidanceArrow.startColor = guidanceColor;
        guidanceArrow.endColor = guidanceColor;
        guidanceArrow.SetPosition(0, tail);
        guidanceArrow.SetPosition(1, tip);

        guidanceArrowHead.startWidth = lineWidth;
        guidanceArrowHead.endWidth = lineWidth;
        guidanceArrowHead.startColor = guidanceColor;
        guidanceArrowHead.endColor = guidanceColor;
        Vector3 headBase = tip + up * (arrowLength * 0.38f);
        guidanceArrowHead.SetPosition(0, headBase + right * arrowWidth);
        guidanceArrowHead.SetPosition(1, tip);
        guidanceArrowHead.SetPosition(2, headBase - right * arrowWidth);
    }
}
