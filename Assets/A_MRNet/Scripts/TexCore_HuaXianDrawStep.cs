using System;
using System.Collections.Generic;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using Bpf.Diagnostics;

/// <summary>
/// A TexCore step adapter for the HuaXian (line drawing) operation.
///
/// The scene supplies the visual model and the MRTK manipulator. This
/// component only owns the operation state: it checks the two MoDou ends on
/// release, records each correct segment, and reports the whole operation to
/// the normal TexCore topic flow when every required segment is complete.
/// </summary>
[DisallowMultipleComponent]
public sealed class TexCore_HuaXianDrawStep : TexCore_InteracterBase, ITexCoreUserCompletionStatus
{
    [Serializable]
    public sealed class Segment
    {
        [Tooltip("Optional label shown in the Inspector/logs.")]
        public string name;

        [Tooltip("Explicit world-space start and end references. Leave both empty when using the generated grid.")]
        public Transform start;
        public Transform end;

        [NonSerialized] internal Vector3 runtimeStart;
        [NonSerialized] internal Vector3 runtimeEnd;
        [NonSerialized] internal bool completed;
    }

    [Header("MRTK 交互对象")]
    [SerializeField] private GameObject interactionSource;
    [SerializeField] private Transform firstMoDouEnd;
    [SerializeField] private Transform secondMoDouEnd;

    [Header("划线显示")]
    [SerializeField] private LineRenderer previewLine;
    [SerializeField] private LineRenderer drawnLinePrefab;
    [SerializeField, Min(0f)] private float errorLineLifetime = 0.5f;
    [SerializeField] private bool hideLineTemplate = true;
    [SerializeField] private bool keepPreviewWhileActive = true;

    [Header("判定")]
    [SerializeField, Min(0.001f)] private float endpointTolerance = 0.12f;
    [SerializeField] private bool requireAllSegments = true;
    [SerializeField, Min(1)] private int requiredCorrectSegments = 1;
    [SerializeField] private bool projectEndpointsToPlane = true;
    [SerializeField] private Transform projectionPlane;

    [Header("线段配置")]
    [SerializeField] private List<Segment> segments = new List<Segment>();
    [SerializeField] private bool generateGridWhenSegmentsAreEmpty = true;
    [SerializeField] private Transform gridReference;
    [SerializeField, Min(1)] private int gridWidth = 4;
    [SerializeField, Min(1)] private int gridHeight = 4;
    [SerializeField, Min(0.001f)] private float gridUnit = 1f;

    [Header("事件")]
    [SerializeField] private UnityEvent onCorrectSegment;
    [SerializeField] private UnityEvent onIncorrectSegment;
    [SerializeField] private UnityEvent onAllSegmentsCompleted;

    private readonly List<LineRenderer> spawnedLines = new List<LineRenderer>();
    private readonly List<LineRenderer> errorLines = new List<LineRenderer>();
    private ObjectManipulator manipulator;
    private bool stepActive;
    private bool completed;
    private bool userCompleted;
    private bool segmentsBuilt;
    private bool templateInitialActive;
    private bool warningShown;

    public bool WasCompletedByUser => userCompleted;
    public int CompletedSegmentCount
    {
        get
        {
            int count = 0;
            foreach (Segment segment in segments)
            {
                if (segment != null && segment.completed)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public int RequiredSegmentCount => segments.Count;

    public void ConfigureSource(GameObject source, Transform firstEnd, Transform secondEnd)
    {
        interactionSource = source;
        firstMoDouEnd = firstEnd;
        secondMoDouEnd = secondEnd;
        CacheInteractionSource();
    }

    public void RebuildSegments()
    {
        segmentsBuilt = false;
        BuildSegmentsIfNeeded();
    }

    private void Awake()
    {
        CacheInteractionSource();
        if (interactionSource == null && firstMoDouEnd == null && secondMoDouEnd == null)
        {
            WarnOnce("请在 Inspector 配置 interactionSource、firstMoDouEnd 和 secondMoDouEnd。", this);
        }

        if (drawnLinePrefab != null)
        {
            templateInitialActive = drawnLinePrefab.gameObject.activeSelf;
        }
    }

    public new void OnEnable()
    {
        base.OnEnable();
        CacheInteractionSource();
        BuildSegmentsIfNeeded();

        if (manipulator != null)
        {
            manipulator.lastSelectExited.AddListener(HandleReleased);
        }
    }

    private void OnDisable()
    {
        if (manipulator != null)
        {
            manipulator.lastSelectExited.RemoveListener(HandleReleased);
        }

        SetManipulationEnabled(false);
        SetPreviewVisible(false);
        base.OnDisabel();
    }

    private void Update()
    {
        if (!stepActive || !keepPreviewWhileActive)
        {
            return;
        }

        UpdatePreviewLine();
    }

    public override void UnFinished()
    {
        base.UnFinished();
        stepActive = false;
        completed = false;
        userCompleted = false;
        ResetProgress();
        SetManipulationEnabled(false);
        SetPreviewVisible(false);
    }

    public override void UnderWay()
    {
        base.UnderWay();
        BuildSegmentsIfNeeded();
        stepActive = true;
        completed = false;
        userCompleted = false;
        ResetSegmentStates();
        SetManipulationEnabled(true);
        SetPreviewVisible(true);
        UpdatePreviewLine();
    }

    public override void OnTrigger()
    {
        base.OnTrigger();
        completed = true;
        stepActive = false;
        SetManipulationEnabled(false);
        SetPreviewVisible(false);
    }

    public override void Finish()
    {
        base.Finish();
        completed = true;
        stepActive = false;
        SetManipulationEnabled(false);
        SetPreviewVisible(false);
    }

    private void HandleReleased(SelectExitEventArgs args)
    {
        if (!stepActive || completed)
        {
            return;
        }

        if (!TryGetCurrentLine(out Vector3 currentStart, out Vector3 currentEnd))
        {
            return;
        }

        int segmentIndex = FindMatchingSegment(currentStart, currentEnd);
        if (segmentIndex < 0)
        {
            SpawnLine(currentStart, currentEnd, true);
            onIncorrectSegment?.Invoke();
            return;
        }

        Segment segment = segments[segmentIndex];
        segment.completed = true;
        SpawnLine(segment.runtimeStart, segment.runtimeEnd, false);
        onCorrectSegment?.Invoke();

        int required = requireAllSegments
            ? segments.Count
            : Mathf.Clamp(requiredCorrectSegments, 1, Mathf.Max(1, segments.Count));
        if (segments.Count > 0 && CompletedSegmentCount >= required)
        {
            CompleteFromUser();
        }
    }

    private void CompleteFromUser()
    {
        if (completed)
        {
            return;
        }

        completed = true;
        stepActive = false;
        userCompleted = true;
        SetManipulationEnabled(false);
        SetPreviewVisible(false);
        onAllSegmentsCompleted?.Invoke();
        TexCore_BusinessManager.OnClick(gameObject);
    }

    private void BuildSegmentsIfNeeded()
    {
        if (segmentsBuilt)
        {
            return;
        }

        segmentsBuilt = true;
        if (segments.Count > 0)
        {
            int validSegmentCount = 0;
            foreach (Segment segment in segments)
            {
                if (segment == null || segment.start == null || segment.end == null)
                {
                    continue;
                }

                segment.runtimeStart = ProjectPoint(segment.start.position);
                segment.runtimeEnd = ProjectPoint(segment.end.position);
                segment.completed = false;
                validSegmentCount++;
            }

            if (validSegmentCount == 0)
            {
                WarnOnce("Segments 已添加，但没有任何一项同时配置 start 和 end。", this);
            }

            return;
        }

        if (!generateGridWhenSegmentsAreEmpty)
        {
            WarnOnce("未配置任何划线线段，也未启用自动网格生成。", this);
            return;
        }

        Transform reference = gridReference != null ? gridReference : projectionPlane;
        if (reference == null)
        {
            reference = transform;
        }

        for (int x = 0; x <= gridWidth; x++)
        {
            AddGeneratedSegment(
                reference.TransformPoint(new Vector3(x * gridUnit, 0f, 0f)),
                reference.TransformPoint(new Vector3(x * gridUnit, 0f, gridHeight * gridUnit)),
                $"GridX{x}");
        }

        for (int z = 0; z <= gridHeight; z++)
        {
            AddGeneratedSegment(
                reference.TransformPoint(new Vector3(0f, 0f, z * gridUnit)),
                reference.TransformPoint(new Vector3(gridWidth * gridUnit, 0f, z * gridUnit)),
                $"GridZ{z}");
        }
    }

    private void AddGeneratedSegment(Vector3 start, Vector3 end, string label)
    {
        Segment segment = new Segment
        {
            name = label,
            runtimeStart = ProjectPoint(start),
            runtimeEnd = ProjectPoint(end),
            completed = false
        };
        segments.Add(segment);
    }

    private int FindMatchingSegment(Vector3 start, Vector3 end)
    {
        float tolerance = Mathf.Max(0.001f, endpointTolerance);
        for (int i = 0; i < segments.Count; i++)
        {
            Segment segment = segments[i];
            if (segment == null || segment.completed)
            {
                continue;
            }

            bool forward = Vector3.Distance(start, segment.runtimeStart) <= tolerance &&
                           Vector3.Distance(end, segment.runtimeEnd) <= tolerance;
            bool reverse = Vector3.Distance(start, segment.runtimeEnd) <= tolerance &&
                           Vector3.Distance(end, segment.runtimeStart) <= tolerance;
            if (forward || reverse)
            {
                return i;
            }
        }

        return -1;
    }

    private bool TryGetCurrentLine(out Vector3 start, out Vector3 end)
    {
        Transform first = firstMoDouEnd;
        Transform second = secondMoDouEnd;
        if (first == null || second == null)
        {
            start = default;
            end = default;
            WarnOnce("未配置墨斗两端；请将墨斗的两个端点拖入 firstMoDouEnd 和 secondMoDouEnd。", this);
            return false;
        }

        start = ProjectPoint(first.position);
        end = ProjectPoint(second.position);
        if (Vector3.Distance(start, end) <= 0.001f)
        {
            WarnOnce("墨斗两端位置重合，无法判定线段。", this);
            return false;
        }

        return true;
    }

    private Vector3 ProjectPoint(Vector3 point)
    {
        if (!projectEndpointsToPlane || projectionPlane == null)
        {
            return point;
        }

        Vector3 local = projectionPlane.InverseTransformPoint(point);
        local.y = 0f;
        return projectionPlane.TransformPoint(local);
    }

    private void UpdatePreviewLine()
    {
        if (previewLine == null || !TryGetPreviewPoints(out Vector3 start, out Vector3 middle, out Vector3 end))
        {
            return;
        }

        previewLine.useWorldSpace = true;
        previewLine.positionCount = middle == start || middle == end ? 2 : 3;
        previewLine.SetPosition(0, start);
        if (previewLine.positionCount == 3)
        {
            previewLine.SetPosition(1, middle);
            previewLine.SetPosition(2, end);
        }
        else
        {
            previewLine.SetPosition(1, end);
        }
    }

    private bool TryGetPreviewPoints(out Vector3 start, out Vector3 middle, out Vector3 end)
    {
        start = default;
        middle = default;
        end = default;
        Transform first = firstMoDouEnd;
        Transform second = secondMoDouEnd;
        if (first == null || second == null)
        {
            return false;
        }

        start = ProjectPoint(first.position);
        end = ProjectPoint(second.position);
        middle = SourceGameObject != null ? SourceGameObject.transform.position : (start + end) * 0.5f;
        return true;
    }

    private void SpawnLine(Vector3 start, Vector3 end, bool error)
    {
        if (drawnLinePrefab == null)
        {
            return;
        }

        LineRenderer line = Instantiate(drawnLinePrefab, drawnLinePrefab.transform.parent);
        line.gameObject.name = error ? "HuaXian_ErrorLine" : "HuaXian_Line";
        line.gameObject.SetActive(true);
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);

        if (error)
        {
            errorLines.Add(line);
            if (errorLineLifetime > 0f)
            {
                Destroy(line.gameObject, errorLineLifetime);
            }
        }
        else
        {
            spawnedLines.Add(line);
        }
    }

    private void ResetProgress()
    {
        ResetSegmentStates();
        foreach (LineRenderer line in spawnedLines)
        {
            if (line != null)
            {
                Destroy(line.gameObject);
            }
        }

        foreach (LineRenderer line in errorLines)
        {
            if (line != null)
            {
                Destroy(line.gameObject);
            }
        }

        spawnedLines.Clear();
        errorLines.Clear();
    }

    private void ResetSegmentStates()
    {
        foreach (Segment segment in segments)
        {
            if (segment != null)
            {
                segment.completed = false;
            }
        }
    }

    private void SetManipulationEnabled(bool enabled)
    {
        if (manipulator != null)
        {
            manipulator.AllowedManipulations = TransformFlags.Move;
            TexCore_03NoScalePolicy.Enforce(manipulator);
            manipulator.enabled = enabled && !completed;
        }
    }

    private void SetPreviewVisible(bool visible)
    {
        if (previewLine != null)
        {
            previewLine.enabled = visible && keepPreviewWhileActive;
        }

        if (drawnLinePrefab != null && hideLineTemplate)
        {
            drawnLinePrefab.gameObject.SetActive(visible ? templateInitialActive : false);
        }
    }

    private GameObject SourceGameObject => interactionSource != null ? interactionSource : gameObject;

    private void CacheInteractionSource()
    {
        GameObject source = SourceGameObject;
        manipulator = source != null
            ? source.GetComponent<ObjectManipulator>() ?? source.GetComponentInChildren<ObjectManipulator>(true)
            : null;

        if (manipulator == null)
        {
            WarnOnce("未找到 ObjectManipulator；划线步骤不会收到释放事件。", source);
        }
    }

    private void WarnOnce(string message, UnityEngine.Object context)
    {
        if (warningShown)
        {
            return;
        }

        warningShown = true;
        BpfLog.Warning(BpfLogCategory.UI, "tex_core_hua_xian_draw_step.warn_once", $"{name}: {message}", context);
    }
}
