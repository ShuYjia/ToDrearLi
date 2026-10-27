using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Bpf.Diagnostics;

/// <summary>
/// Runtime-only focus guidance shared by the guided TexCore topics.
/// One "current action target" is highlighted at a time; optional secondary
/// targets are outlined without taking over the focus. Geometry is drawn with
/// runtime LineRenderers so that no model material is modified and no material
/// override is ever serialized into a scene or prefab.
/// </summary>
[DisallowMultipleComponent]
public sealed class TexCore_FocusHighlight : MonoBehaviour
{
    private const int RingSegments = 48;
    private const int BoxEdgePositions = 24;
    private const float MinLineWidth = 0.0035f;
    private const float MaxLineWidth = 0.018f;
    private const float HintDuration = 2f;
    private const string GuideObjectName = "TexCore_FocusHighlight";

    public static readonly Color GuideColor = new Color(1f, 0.78f, 0.05f, 1f);

    /// <summary>
    /// Colour used for a target the player can act on right now, as opposed to
    /// the neutral "go here next" yellow.
    /// </summary>
    public static readonly Color ReadyColor = new Color(0.15f, 0.9f, 0.25f, 1f);

    [SerializeField, Range(0f, 0.5f)] private float pulseAmount = 0.18f;
    [SerializeField, Min(0f)] private float pulseSpeed = 3f;

    private static TexCore_FocusHighlight instance;
    private static readonly Vector3[] RectCornerBuffer = new Vector3[4];

    private GameObject guideRoot;
    private Material guideMaterial;
    private LineRenderer primaryOutline;
    private LineRenderer primaryRing;
    private LineRenderer arrowLine;
    private LineRenderer arrowHead;
    private readonly List<LineRenderer> secondaryOutlines = new List<LineRenderer>();
    private readonly List<GameObject> secondaryTargets = new List<GameObject>();
    private readonly Vector3[] uiCorners = new Vector3[4];

    private GameObject primaryTarget;
    private GameObject arrowSource;
    private int ownerId;
    private Color currentColor = GuideColor;

    public static TexCore_FocusHighlight Instance => instance;

    public GameObject PrimaryTarget => primaryTarget;

    public GameObject ArrowSource => arrowSource;

    /// <summary>Instance id of the component that currently owns the focus.</summary>
    public int OwnerId => ownerId;

    public string CurrentHint { get; private set; }

    public bool IsActive => guideRoot != null && guideRoot.activeSelf && primaryTarget != null;

    public int SecondaryCount => secondaryTargets.Count;

    /// <summary>
    /// Highlights one action target and broadcasts the matching hint. Secondary
    /// targets are outlined only and may be null. <paramref name="owner"/> lets
    /// the caller release only its own focus later on.
    /// </summary>
    public static void Show(GameObject target, string hint, GameObject arrowFrom = null,
        IReadOnlyList<GameObject> secondary = null, Object owner = null)
    {
        Show(target, hint, GuideColor, arrowFrom, secondary, owner);
    }

    /// <summary>
    /// Same as <see cref="Show(GameObject, string, GameObject, IReadOnlyList{GameObject}, Object)"/>
    /// but with an explicit guide colour, so a step can distinguish "go here next"
    /// from "you can act on this right now".
    /// </summary>
    public static void Show(GameObject target, string hint, Color guideColor, GameObject arrowFrom = null,
        IReadOnlyList<GameObject> secondary = null, Object owner = null)
    {
        if (target == null)
        {
            Hide();
            return;
        }

        TexCore_FocusHighlight value = EnsureInstance();

        // Buttons Canvas, Single Menu Canvas and 背包UI share one position and
        // are switched with Canvas.enabled, so a target under a disabled Canvas
        // is still activeInHierarchy while it is neither drawn nor pressable.
        // Such a target is refused here: drawing it would paint the guide over
        // whichever Canvas is visible, and the caller's hint would describe an
        // action the player cannot see.
        if (IsUiTarget(target) && !IsUiVisible(target))
        {
            ClearFor(value);
            return;
        }

        value.primaryTarget = target;
        value.arrowSource = arrowFrom;
        value.ownerId = owner != null ? owner.GetInstanceID() : 0;
        value.currentColor = guideColor;
        value.SetSecondary(secondary);
        value.SetHint(hint);
        value.EnsureGuideCreated();
        if (value.guideRoot != null)
        {
            value.guideRoot.SetActive(true);
            value.UpdateGuidance();
        }
    }

    /// <summary>Stops showing the current focus without releasing the guide objects.</summary>
    public static void Hide()
    {
        if (instance == null)
        {
            return;
        }

        instance.ClearState();
    }

    /// <summary>
    /// Stops showing the focus and releases the drawn guides. The guide objects
    /// are pooled and hidden rather than destroyed, because Destroy is deferred
    /// to the end of the frame and repeated clear/show cycles inside one frame
    /// would otherwise pile up orphaned guide roots.
    /// </summary>
    public static void Clear()
    {
        if (instance == null)
        {
            return;
        }

        instance.ClearState();
        instance.ReleaseContent();
    }

    /// <summary>
    /// Clears the focus only when <paramref name="owner"/> is still the one that
    /// set it, so a component that is going away cannot wipe a newer highlight.
    /// </summary>
    public static void ClearFor(Object owner)
    {
        if (instance == null)
        {
            return;
        }

        int id = owner != null ? owner.GetInstanceID() : 0;
        if (instance.ownerId != id)
        {
            return;
        }

        Clear();
    }

    private static TexCore_FocusHighlight EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        GameObject host = new GameObject(GuideObjectName)
        {
            hideFlags = HideFlags.DontSave
        };
        instance = host.AddComponent<TexCore_FocusHighlight>();
        return instance;
    }

    /// <summary>
    /// Whether a highlight target is actually on screen. World objects keep the
    /// existing <c>activeInHierarchy</c> rule; UI targets additionally require
    /// every Canvas and CanvasGroup on their parent chain to be enabled, because
    /// the shared UI roots are hidden by disabling the Canvas rather than by
    /// deactivating the GameObject.
    /// </summary>
    public static bool IsUiVisible(GameObject target)
    {
        if (target == null || !target.activeInHierarchy)
        {
            return false;
        }

        for (Transform current = target.transform; current != null; current = current.parent)
        {
            if (!current.gameObject.activeInHierarchy)
            {
                return false;
            }

            Canvas canvas = current.GetComponent<Canvas>();
            if (canvas != null && !canvas.enabled)
            {
                return false;
            }

            CanvasGroup group = current.GetComponent<CanvasGroup>();
            if (group != null && (!group.enabled || group.alpha <= 0.001f))
            {
                return false;
            }
        }

        return true;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        ReleaseGuide();
    }

    private void LateUpdate()
    {
        Revalidate();
    }

    /// <summary>
    /// Re-checks the current target against the UI visibility rule and redraws
    /// the guide. Driven once per frame by <see cref="LateUpdate"/>; exposed so
    /// an EditMode test can run the same frame step without play mode.
    /// </summary>
    public void Revalidate()
    {
        if (guideRoot == null || !guideRoot.activeSelf)
        {
            return;
        }

        if (primaryTarget == null || !primaryTarget.activeInHierarchy)
        {
            ClearState();
            return;
        }

        // Re-checked every frame so switching Buttons Canvas / Single Menu
        // Canvas / 背包UI drops the stale outline and arrow in the same frame
        // the old Canvas is disabled.
        if (IsUiTarget(primaryTarget) && !IsUiVisible(primaryTarget))
        {
            ClearState();
            return;
        }

        UpdateGuidance();
    }

    private void SetHint(string hint)
    {
        if (string.IsNullOrEmpty(hint))
        {
            CurrentHint = null;
            return;
        }

        if (hint == CurrentHint)
        {
            return;
        }

        CurrentHint = hint;
        TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, hint, HintDuration);
    }

    private void SetSecondary(IReadOnlyList<GameObject> secondary)
    {
        secondaryTargets.Clear();
        if (secondary != null)
        {
            for (int i = 0; i < secondary.Count; i++)
            {
                GameObject value = secondary[i];
                if (value != null && value != primaryTarget && !secondaryTargets.Contains(value))
                {
                    secondaryTargets.Add(value);
                }
            }
        }

        for (int i = secondaryTargets.Count; i < secondaryOutlines.Count; i++)
        {
            if (secondaryOutlines[i] != null)
            {
                secondaryOutlines[i].enabled = false;
            }
        }
    }

    private void ClearState()
    {
        primaryTarget = null;
        arrowSource = null;
        CurrentHint = null;
        currentColor = GuideColor;
        secondaryTargets.Clear();

        // Deactivating the root already stops the drawing, but the pooled
        // LineRenderers keep their enabled flag. Releasing them here means "no
        // leftover outline" holds on its own, independent of the root state.
        ReleaseContent();
        if (guideRoot != null)
        {
            guideRoot.SetActive(false);
        }
    }

    /// <summary>Hides every drawn guide while keeping the pooled objects alive.</summary>
    private void ReleaseContent()
    {
        if (primaryOutline != null)
        {
            primaryOutline.enabled = false;
        }
        if (primaryRing != null)
        {
            primaryRing.enabled = false;
        }
        if (arrowLine != null)
        {
            arrowLine.enabled = false;
        }
        if (arrowHead != null)
        {
            arrowHead.enabled = false;
        }

        for (int i = 0; i < secondaryOutlines.Count; i++)
        {
            if (secondaryOutlines[i] != null)
            {
                secondaryOutlines[i].enabled = false;
            }
        }
    }

    /// <summary>Destroys every runtime guide object. Only the host teardown uses this.</summary>
    private void ReleaseGuide()
    {
        secondaryOutlines.Clear();

        if (guideRoot != null)
        {
            DestroyRuntimeObject(guideRoot);
            guideRoot = null;
        }

        primaryOutline = null;
        primaryRing = null;
        arrowLine = null;
        arrowHead = null;

        if (guideMaterial != null)
        {
            DestroyRuntimeObject(guideMaterial);
            guideMaterial = null;
        }
    }

    private void EnsureGuideCreated()
    {
        if (guideRoot != null)
        {
            return;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            BpfLog.Warning(BpfLogCategory.Scene, "tex_core_focus_highlight.ensure_guide_created",
                "Cannot create focus guidance because Sprites/Default is unavailable.", this);
            return;
        }

        guideRoot = new GameObject($"{GuideObjectName}_Guide")
        {
            hideFlags = HideFlags.DontSave,
            layer = gameObject.layer
        };
        guideRoot.transform.SetParent(transform, false);

        guideMaterial = new Material(shader)
        {
            name = $"{GuideObjectName}_Material",
            hideFlags = HideFlags.DontSave
        };

        primaryOutline = CreateGuideLine("Outline", false, BoxEdgePositions, 56);
        primaryRing = CreateGuideLine("Ring", true, RingSegments, 55);
        arrowLine = CreateGuideLine("Arrow", false, 2, 53);
        arrowHead = CreateGuideLine("ArrowHead", false, 3, 54);
    }

    private LineRenderer CreateGuideLine(string objectName, bool loop, int positionCount, int sortingOrder)
    {
        GameObject lineObject = new GameObject(objectName)
        {
            hideFlags = HideFlags.DontSave,
            layer = gameObject.layer
        };
        lineObject.transform.SetParent(guideRoot.transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.sharedMaterial = guideMaterial;
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
        line.sortingOrder = sortingOrder;
        return line;
    }

    private LineRenderer EnsureSecondaryOutline(int index)
    {
        while (secondaryOutlines.Count <= index)
        {
            secondaryOutlines.Add(CreateGuideLine($"Secondary{secondaryOutlines.Count}", false, BoxEdgePositions, 52));
        }

        return secondaryOutlines[index];
    }

    private void UpdateGuidance()
    {
        if (primaryTarget == null)
        {
            return;
        }

        if (guideRoot == null)
        {
            EnsureGuideCreated();
            if (guideRoot == null)
            {
                return;
            }
        }

        float pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;
        Color color = currentColor;
        color.a = Mathf.Clamp01(Mathf.Lerp(0.62f, 1f,
            pulseAmount > 0f ? (pulse - (1f - pulseAmount)) / (pulseAmount * 2f) : 1f));

        DrawTarget(primaryOutline, primaryRing, primaryTarget, pulse, color, out float lineWidth);
        for (int i = 0; i < secondaryTargets.Count; i++)
        {
            LineRenderer line = EnsureSecondaryOutline(i);
            DrawTarget(line, null, secondaryTargets[i], pulse, color, out _);
        }

        UpdateArrow(lineWidth, color);
    }

    private void DrawTarget(LineRenderer outline, LineRenderer ring, GameObject target,
        float pulse, Color color, out float lineWidth)
    {
        lineWidth = MinLineWidth;
        if (outline == null)
        {
            return;
        }

        if (target == null || !target.activeInHierarchy || !TryGetBounds(target, out Bounds bounds))
        {
            outline.enabled = false;
            if (ring != null)
            {
                ring.enabled = false;
            }

            return;
        }

        // The owner can only re-check its own claim inside LateUpdate, so the
        // UI visibility rule is enforced again here, at the last moment before
        // anything is drawn.
        if (IsUiTarget(target) && !IsUiVisible(target))
        {
            outline.enabled = false;
            if (ring != null)
            {
                ring.enabled = false;
            }

            return;
        }

        lineWidth = GetLineWidth(bounds);
        outline.enabled = true;
        if (IsUiTarget(target))
        {
            outline.loop = true;
            outline.positionCount = 4;
            SetRectangle(outline, target, pulse);
        }
        else
        {
            outline.loop = false;
            outline.positionCount = BoxEdgePositions;
            SetBox(outline, bounds, pulse);
        }

        ApplyLineStyle(outline, lineWidth, color);

        if (ring == null)
        {
            return;
        }

        ring.enabled = !IsUiTarget(target);
        if (ring.enabled)
        {
            SetRing(ring, bounds, pulse);
            ApplyLineStyle(ring, lineWidth, color);
        }
    }

    private void UpdateArrow(float lineWidth, Color color)
    {
        if (arrowLine == null || arrowHead == null)
        {
            return;
        }

        if (arrowSource == null || primaryTarget == null ||
            !TryGetBounds(arrowSource, out Bounds sourceBounds) ||
            !TryGetBounds(primaryTarget, out Bounds targetBounds))
        {
            arrowLine.enabled = false;
            arrowHead.enabled = false;
            return;
        }

        Vector3 start = sourceBounds.center;
        Vector3 end = targetBounds.center;
        Vector3 delta = end - start;
        float distance = delta.magnitude;
        if (distance <= 0.01f)
        {
            arrowLine.enabled = false;
            arrowHead.enabled = false;
            return;
        }

        Vector3 direction = delta / distance;
        Vector3 lineStart = start + direction * sourceBounds.extents.magnitude;
        Vector3 lineEnd = end - direction * targetBounds.extents.magnitude;

        Camera activeCamera = Camera.main;
        Vector3 side = activeCamera != null
            ? Vector3.Cross(direction, activeCamera.transform.forward).normalized
            : Vector3.Cross(direction, Vector3.up).normalized;
        if (side.sqrMagnitude < 0.01f)
        {
            side = Vector3.right;
        }

        arrowLine.enabled = true;
        arrowHead.enabled = true;
        ApplyLineStyle(arrowLine, lineWidth, color);
        ApplyLineStyle(arrowHead, lineWidth, color);
        arrowLine.SetPosition(0, lineStart);
        arrowLine.SetPosition(1, lineEnd);

        float arrowLength = Mathf.Clamp(distance * 0.16f, 0.04f, 0.12f);
        float arrowWidth = arrowLength * 0.55f;
        Vector3 arrowBase = lineEnd - direction * arrowLength;
        arrowHead.SetPosition(0, arrowBase + side * arrowWidth);
        arrowHead.SetPosition(1, lineEnd);
        arrowHead.SetPosition(2, arrowBase - side * arrowWidth);
    }

    private static void ApplyLineStyle(LineRenderer line, float width, Color color)
    {
        line.startWidth = width;
        line.endWidth = width;
        line.startColor = color;
        line.endColor = color;
    }

    private void SetRectangle(LineRenderer line, GameObject value, float pulse)
    {
        RectTransform rect = value.GetComponent<RectTransform>();
        rect.GetWorldCorners(uiCorners);
        Vector3 center = (uiCorners[0] + uiCorners[1] + uiCorners[2] + uiCorners[3]) * 0.25f;
        float size = Mathf.Max(0.01f, Mathf.Min(
            Vector3.Distance(uiCorners[0], uiCorners[3]),
            Vector3.Distance(uiCorners[0], uiCorners[1])));
        float padding = size * 0.16f * pulse;

        Vector3 right = (uiCorners[3] - uiCorners[0]).normalized;
        Vector3 up = (uiCorners[1] - uiCorners[0]).normalized;
        Vector3 normal = Vector3.Cross(right, up).normalized;
        Camera activeCamera = Camera.main;
        if (activeCamera != null && Vector3.Dot(normal, activeCamera.transform.position - center) < 0f)
        {
            normal = -normal;
        }

        Vector3 surfaceOffset = normal * Mathf.Max(0.002f, size * 0.05f);
        for (int i = 0; i < uiCorners.Length; i++)
        {
            Vector3 cornerOffset = (uiCorners[i] - center).normalized * padding;
            line.SetPosition(i, uiCorners[i] + cornerOffset + surfaceOffset);
        }
    }

    private static void SetBox(LineRenderer line, Bounds bounds, float pulse)
    {
        Vector3 extents = bounds.extents * pulse;
        Vector3 center = bounds.center;
        Vector3 min = center - extents;
        Vector3 max = center + extents;
        Vector3[] corners =
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(max.x, max.y, max.z),
            new Vector3(min.x, max.y, max.z)
        };

        int[,] edges =
        {
            { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 },
            { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 },
            { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 }
        };

        for (int i = 0; i < edges.GetLength(0); i++)
        {
            line.SetPosition(i * 2, corners[edges[i, 0]]);
            line.SetPosition(i * 2 + 1, corners[edges[i, 1]]);
        }
    }

    private static void SetRing(LineRenderer line, Bounds bounds, float pulse)
    {
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.15f * pulse;
        Vector3 center = new Vector3(bounds.center.x, bounds.min.y + 0.01f, bounds.center.z);
        for (int i = 0; i < RingSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / RingSegments;
            line.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }
    }

    private static bool IsUiTarget(GameObject value)
    {
        return value != null &&
               value.GetComponent<RectTransform>() != null &&
               value.GetComponentInParent<Canvas>(true) != null;
    }

    private static float GetLineWidth(Bounds bounds)
    {
        return Mathf.Clamp(bounds.extents.magnitude * 0.035f, MinLineWidth, MaxLineWidth);
    }

    private static bool TryGetBounds(GameObject value, out Bounds bounds)
    {
        bounds = default;
        if (value == null)
        {
            return false;
        }

        bool found = false;
        foreach (Collider collider in value.GetComponentsInChildren<Collider>(true))
        {
            if (collider == null)
            {
                continue;
            }

            if (!found)
            {
                bounds = collider.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        if (found)
        {
            return true;
        }

        foreach (Renderer renderer in value.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
            {
                continue;
            }

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (found)
        {
            return true;
        }

        foreach (RectTransform rect in value.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect == null)
            {
                continue;
            }

            rect.GetWorldCorners(RectCornerBuffer);
            for (int i = 0; i < RectCornerBuffer.Length; i++)
            {
                if (!found)
                {
                    bounds = new Bounds(RectCornerBuffer[i], Vector3.zero);
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(RectCornerBuffer[i]);
                }
            }
        }

        if (found && bounds.size.sqrMagnitude < 0.000001f)
        {
            bounds.size = Vector3.one * 0.1f;
        }

        return found;
    }

    private static void DestroyRuntimeObject(Object value)
    {
        if (value == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(value);
        }
        else
        {
            DestroyImmediate(value);
        }
    }
}
