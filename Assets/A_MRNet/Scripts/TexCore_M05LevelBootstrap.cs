using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MixedReality.Toolkit.SpatialManipulation;
using MixedReality.Toolkit.UX;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using Bpf.Diagnostics;

/// <summary>
/// Installs the current TexCore M05 interaction reporters into the existing
/// 03 scene. Legacy HuaXian art remains scene content; progression is owned by
/// these TexCore reporters instead of the archived task scripts.
/// Binding is retried until the complete legacy object hierarchy is present.
/// </summary>
[DisallowMultipleComponent]
public sealed class TexCore_M05LevelBootstrap : MonoBehaviour
{
    private const int ExpectedSegmentCount = 10; // M05 uses five vertical and five horizontal lines.
    private const int ExpectedQrPairCount = 25;

    [SerializeField] private TexCore_StepObj stepObjects;
    [SerializeField] private Transform huaXianRoot;
    [SerializeField] private int m05NodeIndex = 4;
    [SerializeField] private int drawTopicIndex = 4;
    [SerializeField] private int qrTopicIndex = 5;
    [SerializeField] private int completionTopicIndex = 6;

    private TexCore_M05DrawStep drawStep;
    private TexCore_M05QrPasteStep qrStep;
    private TexCore_M05CompletionGateStep completionStep;
    private bool configured;
    private bool warnedMissingBinding;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadHandler()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstallIntoScene(scene);
    }

    private static void InstallIntoScene(Scene scene)
    {
        if (!scene.IsValid() ||
            !(string.Equals(scene.name, "03SingleplayerScene", StringComparison.Ordinal) ||
              string.Equals(scene.name, TexCore_LoadScene.Test09SceneName, StringComparison.Ordinal)))
        {
            return;
        }

        TexCore_StepObj stepObj = Resources.FindObjectsOfTypeAll<TexCore_StepObj>()
            .FirstOrDefault(value => value != null && value.gameObject.scene == scene && value.name == "StepObjs");
        if (stepObj == null || stepObj.GetComponent<TexCore_M05LevelBootstrap>() != null)
        {
            return;
        }

        stepObj.gameObject.AddComponent<TexCore_M05LevelBootstrap>();
    }

    private void Awake()
    {
        Configure();
    }

    private void Update()
    {
        if (!configured || drawStep == null || qrStep == null ||
            drawStep.RequiredSegmentCount != ExpectedSegmentCount ||
            qrStep.PairCount != ExpectedQrPairCount)
        {
            Configure();
        }

        completionStep?.TryComplete();
    }

    private void Configure()
    {
        stepObjects = stepObjects != null ? stepObjects : GetComponent<TexCore_StepObj>();
        huaXianRoot = huaXianRoot != null ? huaXianRoot : FindSceneTransform(gameObject.scene, "HuaXian");
        if (stepObjects == null || huaXianRoot == null)
        {
            if (!warnedMissingBinding)
            {
                warnedMissingBinding = true;
                BpfLog.Warning(BpfLogCategory.Startup, "tex_core_m05_level_bootstrap.configure", "M05 bootstrap requires StepObjs and HuaXian in 03SingleplayerScene.", this);
            }
            return;
        }

        drawStep = GetOrAdd<TexCore_M05DrawStep>(GetOrCreateStepObject("M05_DrawStep"));
        qrStep = GetOrAdd<TexCore_M05QrPasteStep>(GetOrCreateStepObject("M05_QrPasteStep"));
        completionStep = GetOrAdd<TexCore_M05CompletionGateStep>(GetOrCreateStepObject("M05_CompletionStep"));

        Transform qrRoot = FindChild(huaXianRoot, "QRCode");
        Transform targetRoot = FindChild(huaXianRoot, "Pasting");
        drawStep.Configure(
            FindChild(huaXianRoot, "MoDou/DragCube", "DragCube"),
            FindChild(huaXianRoot,
                "MoDou/MoDou/MoDou_03/Plane (2)",
                "MoDou/MoDou_03/Plane (2)"),
            FindChild(huaXianRoot,
                "MoDou/MoDou/MoDou_01/Plane (3)",
                "MoDou/MoDou_01/Plane (3)"), targetRoot);
        drawStep.ConfigurePreparation(
            huaXianRoot,
            FindChild(huaXianRoot, "ShuiPingYi"),
            FindChild(huaXianRoot, "ShuiPingYi/Ray"),
            FindChild(huaXianRoot, "ShuiPingYi/polySurface10"),
            FindChild(huaXianRoot, "JiHaoBi"),
            FindChild(huaXianRoot, "JiHaoBi/Plane (1)"),
            FindChild(huaXianRoot, "MoDou"));
        qrStep.Configure(qrRoot, targetRoot);
        // The QR step owns the black JiZhun copy for its whole phase.
        qrStep.BindDrawStep(drawStep);
        completionStep.Configure(drawStep, qrStep);
        // The panel renders all three counters at once, so it needs a stable
        // view of both workflows instead of only the phase owner.
        TexCore_05BackpackController.RegisterWorkflows(gameObject.scene, drawStep, qrStep);

        if (drawStep.RequiredSegmentCount != ExpectedSegmentCount)
        {
            BpfLog.Warning(BpfLogCategory.Startup, "tex_core_m05_level_bootstrap.configure", $"M05 draw binding found {drawStep.RequiredSegmentCount} segments; expected {ExpectedSegmentCount}.", this);
        }

        if (qrStep.PairCount != ExpectedQrPairCount)
        {
            BpfLog.Warning(BpfLogCategory.Startup, "tex_core_m05_level_bootstrap.configure", $"M05 QR binding found {qrStep.PairCount} pairs; expected {ExpectedQrPairCount}.", this);
        }

        if (drawStep.RequiredSegmentCount == ExpectedSegmentCount && qrStep.PairCount == ExpectedQrPairCount)
        {
            BindStep(drawTopicIndex, drawStep.gameObject, drawStep);
            BindStep(qrTopicIndex, qrStep.gameObject, qrStep);
            BindStep(completionTopicIndex, completionStep.gameObject, completionStep);
            configured = true;
            BpfLog.Info(BpfLogCategory.Startup, "tex_core_m05_level_bootstrap.configure", "M05 ready: laser, marking, draw and QR interactions bound (8 marks, 10 lines, 25 QR pairs).", this);
        }
    }

    private void BindStep(int topicIndex, GameObject stepObject, TexCore_InteracterBase interacter)
    {
        if (m05NodeIndex < 0 || m05NodeIndex >= stepObjects.interacterObjs.Count)
        {
            return;
        }

        NodeObjs node = stepObjects.interacterObjs[m05NodeIndex];
        List<TopicObjs> topics = node.nodeObjs;
        if (topicIndex < 0 || topicIndex >= topics.Count || topics[topicIndex] == null)
        {
            return;
        }

        TopicObjs topic = topics[topicIndex];
        List<StepObjs> steps = topic.topicObjs ?? new List<StepObjs>();
        StepObjs current = steps.FirstOrDefault(value => value != null && value.stepIndex == 0);
        if (current == null)
        {
            current = new StepObjs { stepIndex = 0, stepObjs = new List<GameObject>() };
            steps.Add(current);
        }

        current.stepIndex = 0;
        current.stepObjs = new List<GameObject> { stepObject };
        current.InvalidateStepScripts();
        if (interacter != null)
        {
            interacter.enterTopic = new EnterTopic(node.nodePrefab, topic.topicPrefab, 0)
            {
                nodeIndex = m05NodeIndex,
                topicIndex = topicIndex
            };
        }
        topic.topicObjs = steps;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private GameObject GetOrCreateStepObject(string objectName)
    {
        Transform existing = huaXianRoot.Find(objectName);
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            return existing.gameObject;
        }

        GameObject stepObject = new GameObject(objectName);
        stepObject.transform.SetParent(huaXianRoot, false);
        stepObject.SetActive(true);
        return stepObject;
    }

    internal static Transform FindSceneTransform(Scene scene, string objectName)
    {
        return Resources.FindObjectsOfTypeAll<Transform>()
            .FirstOrDefault(value => value != null && value.gameObject.scene == scene && value.name == objectName);
    }

    private static Transform FindChild(Transform root, params string[] paths)
    {
        foreach (string path in paths)
        {
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            Transform result = root.Find(path.Trim());
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}

/// <summary>
/// Read-only status contract rendered by the 05 backpack panel.  Each M05 step
/// owns its own phase name and hint, so the panel never infers the phase from
/// counters or from the highlighted object.
/// </summary>
public interface ITexCoreM05StepStatus
{
    string PhaseName { get; }
    string PhaseHint { get; }
    string RequiredBackpackAction { get; }
    bool IsCompleted { get; }

    /// <summary>
    /// True while the next action is a backpack press rather than a world
    /// target, so the panel may take the focus for its own button.
    /// </summary>
    bool FocusesBackpackButton { get; }
}

[DisallowMultipleComponent]
public sealed class TexCore_M05DrawStep : TexCore_InteracterBase, ITexCoreUserCompletionStatus, ITexCoreM05StepStatus
{
    private const int ExpectedDatumMarkCount = 8; // M05 marks the eight non-origin datum points.

    [Serializable]
    private sealed class Segment
    {
        public Vector3 start;
        public Vector3 end;
        public bool completed;
    }

    private sealed class DatumMark
    {
        public Vector3 position;
        public GameObject marker;
        public GameObject hint;
    }

    /// <summary>
    /// Cached renderer colours for one ink-line end.  The 03 MoDou ends carry a
    /// MeshRenderer but no Outline component, so the fallback described for the
    /// legacy DrawGridV2 path is used: remember the original colour once and
    /// swap it for the hint colour while that end sits on a valid endpoint.
    /// </summary>
    private sealed class EndpointHighlight
    {
        public Transform root;
        public readonly List<MeshRenderer> renderers = new List<MeshRenderer>();
        public readonly List<Color> originalColors = new List<Color>();
        public readonly List<Color> originalEmissionColors = new List<Color>();
        public readonly List<bool> originalEmissionEnabled = new List<bool>();
        public bool colorsCaptured;
        public bool highlighted;
    }

    private enum PreparationPhase
    {
        Inactive,
        SummonLaserLevel,
        SnapLaserLevel,
        EnableLaserRay,
        AnimatingLaserRay,
        SummonMarkerPen,
        MarkDatumPoints,
        SummonMoDou,
        DrawLines,
        Completed
    }

    [SerializeField] private GameObject interactionSource;
    [SerializeField] private Transform firstMoDouEnd;
    [SerializeField] private Transform secondMoDouEnd;
    [SerializeField, Min(0.001f)] private float endpointTolerance = 0.12f;
    [SerializeField] private Vector3 gridOrigin = Vector3.zero;
    [SerializeField] private float gridUnit = 1f;
    [SerializeField] private Transform gridReference;
    [SerializeField] private LineRenderer previewLine;
    [SerializeField] private LineRenderer completedLineTemplate;
    [SerializeField] private GameObject finishMessage;
    [SerializeField] private GameObject errorMessage;
    [SerializeField] private UnityEvent onCorrectSegment;
    [SerializeField] private UnityEvent onIncorrectSegment;
    [SerializeField] private UnityEvent onAllSegmentsCompleted;
    [Header("M05 前置工具流程")]
    [SerializeField, Min(0.001f)] private float laserSnapTolerance = 0.3f;
    [SerializeField, Min(0.001f)] private float markTolerance = 0.2f;
    [SerializeField] private Transform laserLevel;
    [SerializeField] private Transform laserRay;
    [SerializeField] private Transform laserSwitch;
    [SerializeField] private Transform markerPen;
    [SerializeField] private Transform markerTip;
    [SerializeField] private Transform moDouRoot;
    [SerializeField] private Transform jiZhunIndicator;

    private readonly List<Segment> segments = new List<Segment>();
    private readonly List<ObjectManipulator> manipulators = new List<ObjectManipulator>();
    private Transform visibilityRoot;
    private Vector3 startFirstPosition;
    private Quaternion startFirstRotation;
    private Vector3 startSecondPosition;
    private Quaternion startSecondRotation;
    private bool startPoseCaptured;
    private bool stepActive;
    private bool completed;
    private bool userCompleted;
    private float errorVisibleUntil;
    private readonly List<LineRenderer> completedLines = new List<LineRenderer>();
    // One persistent line per grid segment: a repeated release on the same
    // endpoints can never spawn a second object.
    private readonly HashSet<int> drawnSegmentIndexes = new HashSet<int>();
    // Stable ink-line width independent of whatever width the MoDou scene
    // template happens to carry.
    private const float CompletedLineWidth = 0.012f;
    private static readonly int MainColorId = Shader.PropertyToID("_Color");
    private static readonly int TintColorId = Shader.PropertyToID("_TintColor");
    private GameObject jiZhunClone;
    private readonly List<DatumMark> datumMarks = new List<DatumMark>();
    private readonly List<GameObject> datumHintMarkers = new List<GameObject>();
    private static readonly Color DatumHintColor = new Color(1f, 0.85f, 0.35f, 0.5f);
    [Header("墨斗端点提示")]
    [SerializeField, Min(0.001f)] private float endpointHighlightDistance = 0.1f;
    private static readonly Color EndpointHighlightColor = new Color(0.15f, 0.9f, 0.25f, 1f);
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private const string EmissionKeyword = "_EMISSION";
    private EndpointHighlight firstEndHighlight;
    private EndpointHighlight secondEndHighlight;
    private bool previewLineHighlighted;
    private bool previewLineColorsCaptured;
    private bool previewLineLogged;
    private ObjectManipulator dragCubeManipulator;
    private float endpointHighlightHoldUntil;
    private const float EndpointHighlightHold = 0.4f;
    private Color previewLineStartColor;
    private Color previewLineEndColor;
    private bool hasCandidateEndpoint;
    private Vector3 candidateEndpoint;
    private PreparationPhase preparationPhase;
    private bool jiZhunUnlocked;
    private Vector3 laserStartPosition;
    private Quaternion laserStartRotation;
    private Vector3 penStartPosition;
    private Quaternion penStartRotation;
    private Quaternion laserSwitchStartLocalRotation;
    private bool toolStartPoseCaptured;
    private PressableButton laserSwitchButton;
    private GameObject laserSwitchHitBoxRoot;
    private BoxCollider laserSwitchHitBox;
    [SerializeField] private Vector3 laserSwitchHitBoxSize = new Vector3(0.06f, 0.06f, 0.04f);
    private readonly List<Collider> laserSwitchSuppressedColliders = new List<Collider>();
    private readonly List<ObjectManipulator> laserSwitchSuppressedManipulators = new List<ObjectManipulator>();
    private Transform snapOrigin;
    private bool snapOriginInitialActive;
    private string snapHintCache;
    private float nextLaserSwitchHintTime;
    private Coroutine laserRayAnimation;
    [SerializeField, Min(0.01f)] private float laserSwitchAnimationDuration = 0.5f;

    public bool WasCompletedByUser => userCompleted;
    public int CompletedSegmentCount => segments.Count(value => value.completed);
    public int RequiredSegmentCount => segments.Count;
    public GameObject JiZhunClone => jiZhunClone;
    public int MarkedDatumCount => datumMarks.Count(value => value.marker != null);
    public int RequiredDatumMarkCount => datumMarks.Count;
    public bool HasCandidateEndpoint => hasCandidateEndpoint;
    public Vector3 CandidateEndpoint => candidateEndpoint;
    public string RequiredBackpackAction => RequiredActionFor(preparationPhase);
    public string BackpackHint => HintFor(preparationPhase);
    public string PhaseName => PhaseNameFor(preparationPhase);
    public string PhaseHint => BackpackHint;
    public bool IsCompleted => completed;

    // EnableLaserRay is deliberately excluded: the switch carries the focus
    // there, and two primaries must never compete.
    public bool FocusesBackpackButton =>
        preparationPhase == PreparationPhase.SummonLaserLevel ||
        preparationPhase == PreparationPhase.SummonMarkerPen ||
        preparationPhase == PreparationPhase.SummonMoDou;

    public void Configure(Transform source, Transform firstEnd, Transform secondEnd, Transform reference)
    {
        interactionSource = source != null ? source.gameObject : null;
        firstMoDouEnd = firstEnd;
        secondMoDouEnd = secondEnd;
        firstEndHighlight = CreateEndpointHighlight(firstEnd);
        secondEndHighlight = CreateEndpointHighlight(secondEnd);
        gridReference = reference;
        visibilityRoot = FindAncestor(source != null ? source : firstEnd, "HuaXian");
        previewLine = visibilityRoot != null
            ? FindChild(visibilityRoot, "MoDou/DragLine")?.GetComponent<LineRenderer>()
            : null;
        completedLineTemplate = visibilityRoot != null
            ? FindChild(visibilityRoot, "MoDou/PlaneLine")?.GetComponent<LineRenderer>()
            : null;
        finishMessage = visibilityRoot != null ? FindChild(visibilityRoot, "划线完成")?.gameObject : null;
        errorMessage = visibilityRoot != null ? FindChild(visibilityRoot, "弹线错误")?.gameObject : null;
        CaptureStartPose();
        BuildGrid();
        CacheManipulators();
        ConfigureLineRenderer(previewLine);
        ConfigureLineRenderer(completedLineTemplate);
        ClearCompletedLines();
    }

    /// <summary>
    /// Binds the verified SHIXUN pre-draw workflow to the current 03 scene.
    /// The score topic remains "划线完成"; these actions are its required
    /// physical prerequisites rather than additional score checkpoints.
    /// </summary>
    public void ConfigurePreparation(
        Transform root,
        Transform level,
        Transform ray,
        Transform levelSwitch,
        Transform pen,
        Transform penTip,
        Transform moDou)
    {
        visibilityRoot = root != null ? root : visibilityRoot;
        laserLevel = level;
        laserRay = ray;
        laserSwitch = levelSwitch;
        markerPen = pen;
        markerTip = penTip;
        moDouRoot = moDou;
        snapOrigin = FindChild(root, "基准原点");
        snapOriginInitialActive = snapOrigin != null && snapOrigin.gameObject.activeSelf;
        jiZhunIndicator = ResolveJiZhunIndicator(root);
        jiZhunUnlocked = false;
        SetJiZhunVisible(false);
        BindLaserSwitchButton();
        CaptureToolStartPose();
        BuildDatumMarks();
    }

    public override void UnFinished()
    {
        stepActive = false;
        completed = false;
        userCompleted = false;
        preparationPhase = PreparationPhase.Inactive;
        ResetEndpointPose();
        foreach (Segment segment in segments) segment.completed = false;
        SetManipulation(false);
        ResetPreparation();
        SetMoDouVisible(false);
        SetLineVisible(false);
        ClearCompletedLines();
        // The black datum grid has to outlive this topic: the QR step needs it
        // as its guide, and destroying it here removed it the moment the topic
        // advanced. It is torn down by the QR step and by a fresh UnderWay.
        SetJiZhunVisible(false);
        TexCore_05BackpackController.HideFor(this);
    }

    public override void UnderWay()
    {
        stepActive = true;
        completed = false;
        userCompleted = false;
        foreach (Segment segment in segments) segment.completed = false;
        ResetEndpointPose();
        ActivateRelevantPath(visibilityRoot, null);
        ResetPreparation();
        SetMoDouVisible(false);
        SetManipulation(false);
        ClearCompletedLines();
        DestroyJiZhunClone();
        SetLineVisible(false);
        if (finishMessage != null) finishMessage.SetActive(false);
        if (errorMessage != null) errorMessage.SetActive(false);
        preparationPhase = PreparationPhase.SummonLaserLevel;
        TexCore_05BackpackController.Present(this);
    }

    public override void OnTrigger() => FinishState(false);
    public override void Finish() => FinishState(false);

    private void FinishState(bool byUser)
    {
        completed = true;
        stepActive = false;
        userCompleted |= byUser;
        preparationPhase = PreparationPhase.Completed;
        SetManipulation(false);
        SetMoDouVisible(false);
        SetLineVisible(false);
        SetDatumMarkersVisible(false);
        ClearDatumHintMarkers();
        ClearMoDouEndpointHighlight();
        jiZhunUnlocked = false;
        SetJiZhunVisible(false);
        SetLaserSwitchOccludersSuppressed(false);
        SetLaserSwitchHighlight(false);
        TexCore_05BackpackController.HideFor(this);
    }

    private new void OnEnable()
    {
        base.OnEnable();
        CacheManipulators();
    }

    private void OnDisable()
    {
        TexCore_FocusHighlight.ClearFor(this);
        SetLaserSwitchOccludersSuppressed(false);
        SetDatumMarkersVisible(false);
        ClearDatumHintMarkers();
        ClearMoDouEndpointHighlight();
        foreach (ObjectManipulator manipulator in manipulators)
        {
            if (manipulator != null) manipulator.lastSelectExited.RemoveListener(Released);
        }
        base.OnDisabel();
    }

    private void Released(SelectExitEventArgs args)
    {
        if (!stepActive || completed || preparationPhase != PreparationPhase.DrawLines ||
            firstMoDouEnd == null || secondMoDouEnd == null || args == null || args.interactableObject == null)
        {
            return;
        }

        bool trackedManipulator = manipulators.Any(value => value != null && value.gameObject == args.interactableObject.transform.gameObject);
        if (!trackedManipulator)
        {
            return;
        }

        CompleteInkRelease(firstMoDouEnd.position, secondMoDouEnd.position);
    }

    /// <summary>
    /// Core release decision for one ink-line attempt, kept parameterised so
    /// EditMode tests can drive correct and wrong releases without a live XR
    /// select-exit event.  A wrong release only raises the existing error hint
    /// and never leaves a persistent line behind; a correct release draws one
    /// black line pinned to the grid endpoints and, on the tenth line, creates
    /// the single black JiZhun datum-plane copy.
    /// </summary>
    private void CompleteInkRelease(Vector3 start, Vector3 end)
    {
        int index = segments.FindIndex(value => !value.completed && Matches(value, start, end));
        if (index < 0)
        {
            onIncorrectSegment?.Invoke();
            if (errorMessage != null)
            {
                errorMessage.SetActive(true);
                errorVisibleUntil = Time.time + 1f;
            }
            return;
        }

        CommitSegment(index);
    }

    /// <summary>
    /// Commits one grid line, reports the running total through the tips
    /// channel and, on the tenth line, builds the black JiZhun datum plane.
    /// </summary>
    private void CommitSegment(int index)
    {
        if (index < 0 || index >= segments.Count || segments[index].completed)
        {
            return;
        }

        segments[index].completed = true;
        endpointHighlightHoldUntil = Time.unscaledTime + EndpointHighlightHold;
        DrawCompletedLine(index, segments[index]);
        onCorrectSegment?.Invoke();
        ShowLineProgress();
        TexCore_05BackpackController.RefreshFor(this);
        if (CompletedSegmentCount != RequiredSegmentCount)
        {
            return;
        }

        userCompleted = true;
        FinishState(true);
        if (finishMessage != null) finishMessage.SetActive(true);
        onAllSegmentsCompleted?.Invoke();
        CreateJiZhunClone();
        TexCore_M05Progression.CompleteTopic(gameObject);
    }

    private bool Matches(Segment segment, Vector3 start, Vector3 end)
    {
        return (Vector3.Distance(start, segment.start) <= endpointTolerance &&
                Vector3.Distance(end, segment.end) <= endpointTolerance) ||
               (Vector3.Distance(start, segment.end) <= endpointTolerance &&
                Vector3.Distance(end, segment.start) <= endpointTolerance);
    }

    private static EndpointHighlight CreateEndpointHighlight(Transform endpoint)
    {
        if (endpoint == null)
        {
            return null;
        }

        // The bound endpoint is the MoDou tip plane; its parent is the visible
        // end assembly that the legacy Outline used to mark.
        Transform root = endpoint.parent != null ? endpoint.parent : endpoint;
        return new EndpointHighlight { root = root };
    }

    /// <summary>
    /// Restores the legacy DrawGridV2 hint: the ink-line ends turn green while
    /// they sit on a valid endpoint of a line that is still to be drawn.  This
    /// only reports the candidate; drawing still requires the release check in
    /// <see cref="Released"/> and its 0.12 m tolerance.
    /// </summary>
    private void UpdateMoDouEndpointHighlight()
    {
        if (!stepActive || preparationPhase != PreparationPhase.DrawLines ||
            firstMoDouEnd == null || secondMoDouEnd == null)
        {
            ClearMoDouEndpointHighlight();
            return;
        }

        if (Time.unscaledTime < endpointHighlightHoldUntil)
        {
            // Both ends matched and the line is already committed. Hold the
            // green on head and tail for a moment so the confirmation is
            // actually visible instead of a single-frame flash.
            ApplyEndpointHighlight(firstEndHighlight, true);
            ApplyEndpointHighlight(secondEndHighlight, true);
            ApplyPreviewLineHighlight(true);
            TexCore_FocusHighlight.ClearFor(this);
            return;
        }

        Vector3 firstPosition = firstMoDouEnd.position;
        Vector3 secondPosition = secondMoDouEnd.position;

        bool firstValid = TryFindCandidateEndpoint(firstPosition, out Vector3 firstTarget);
        bool secondValid = firstValid && TryFindPartnerEndpoint(firstTarget, secondPosition);

        hasCandidateEndpoint = firstValid;
        candidateEndpoint = firstTarget;
        ApplyEndpointHighlight(firstEndHighlight, firstValid);
        ApplyEndpointHighlight(secondEndHighlight, secondValid);
        bool ready = firstValid && secondValid;
        // Both ends on the same unfinished line only means the ink line is
        // strung. Plucking it is a separate step, so point the player at the
        // drag block that snaps it.
        ApplyPreviewLineHighlight(ready);
        if (ready && interactionSource != null)
        {
            TexCore_FocusHighlight.Show(
                interactionSource,
                "两端已对准，抓住墨斗中间的拖拽块并松开即可弹线",
                TexCore_FocusHighlight.ReadyColor,
                null,
                null,
                this);
        }
        else
        {
            TexCore_FocusHighlight.ClearFor(this);
        }
    }

    private void ClearMoDouEndpointHighlight()
    {
        hasCandidateEndpoint = false;
        ApplyEndpointHighlight(firstEndHighlight, false);
        ApplyEndpointHighlight(secondEndHighlight, false);
        ApplyPreviewLineHighlight(false);
    }

    private void ApplyPreviewLineHighlight(bool active)
    {
        if (previewLine == null)
        {
            return;
        }

        if (!previewLineColorsCaptured)
        {
            previewLineColorsCaptured = true;
            previewLineStartColor = previewLine.startColor;
            previewLineEndColor = previewLine.endColor;
        }

        if (previewLineHighlighted == active)
        {
            return;
        }

        previewLineHighlighted = active;
        previewLine.startColor = active ? EndpointHighlightColor : previewLineStartColor;
        previewLine.endColor = active ? EndpointHighlightColor : previewLineEndColor;
    }

    /// <summary>
    /// Nearest free endpoint of an unfinished line.  Completed lines, wrong
    /// directions and positions inside the grid all simply fail this test, so
    /// they never highlight.
    /// </summary>
    private bool TryFindCandidateEndpoint(Vector3 position, out Vector3 target)
    {
        target = default;
        float nearest = endpointHighlightDistance;
        bool found = false;
        foreach (Segment segment in segments)
        {
            if (segment.completed)
            {
                continue;
            }

            float startDistance = Vector3.Distance(position, segment.start);
            if (startDistance <= nearest)
            {
                nearest = startDistance;
                target = segment.start;
                found = true;
            }

            float endDistance = Vector3.Distance(position, segment.end);
            if (endDistance <= nearest)
            {
                nearest = endDistance;
                target = segment.end;
                found = true;
            }
        }

        return found;
    }

    /// <summary>
    /// The second end may only highlight on the opposite endpoint of the very
    /// same unfinished line the first end picked.
    /// </summary>
    private bool TryFindPartnerEndpoint(Vector3 candidate, Vector3 position)
    {
        foreach (Segment segment in segments)
        {
            if (segment.completed)
            {
                continue;
            }

            if (candidate == segment.start)
            {
                if (Vector3.Distance(position, segment.end) <= endpointHighlightDistance)
                {
                    return true;
                }
            }
            else if (candidate == segment.end &&
                     Vector3.Distance(position, segment.start) <= endpointHighlightDistance)
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyEndpointHighlight(EndpointHighlight highlight, bool active)
    {
        if (highlight == null)
        {
            return;
        }

        CaptureEndpointColors(highlight);
        if (highlight.highlighted == active)
        {
            return;
        }

        highlight.highlighted = active;
        for (int i = 0; i < highlight.renderers.Count; i++)
        {
            MeshRenderer renderer = highlight.renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material material = renderer.material;
            if (active)
            {
                // The MoDou bodies use a textured Standard material, so a plain
                // _Color tint only multiplies the albedo and barely reads.  The
                // emission channel is what makes the whole end look green.
                material.color = EndpointHighlightColor;
                if (material.HasProperty(EmissionColorId))
                {
                    material.SetColor(EmissionColorId, EndpointHighlightColor);
                    material.EnableKeyword(EmissionKeyword);
                }

                continue;
            }

            material.color = highlight.originalColors[i];
            if (material.HasProperty(EmissionColorId))
            {
                material.SetColor(EmissionColorId, highlight.originalEmissionColors[i]);
                if (highlight.originalEmissionEnabled[i])
                {
                    material.EnableKeyword(EmissionKeyword);
                }
                else
                {
                    material.DisableKeyword(EmissionKeyword);
                }
            }
        }
    }

    private void CaptureEndpointColors(EndpointHighlight highlight)
    {
        if (highlight.colorsCaptured || highlight.root == null)
        {
            return;
        }

        highlight.colorsCaptured = true;
        highlight.renderers.Clear();
        highlight.originalColors.Clear();
        highlight.originalEmissionColors.Clear();
        highlight.originalEmissionEnabled.Clear();
        foreach (MeshRenderer renderer in highlight.root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (renderer == null)
            {
                continue;
            }

            Material material = renderer.material;
            highlight.renderers.Add(renderer);
            highlight.originalColors.Add(material.color);
            highlight.originalEmissionColors.Add(
                material.HasProperty(EmissionColorId) ? material.GetColor(EmissionColorId) : Color.black);
            highlight.originalEmissionEnabled.Add(material.IsKeywordEnabled(EmissionKeyword));
        }
    }

    private void BuildGrid()
    {
        if (segments.Count > 0) return;
        if (gridReference != null)
        {
            Dictionary<int, Vector3> points = new Dictionary<int, Vector3>();
            foreach (Transform point in gridReference.GetComponentsInChildren<Transform>(true))
            {
                int id = Suffix(point.name, "ZhanTieDian");
                if (id > 0 && !points.ContainsKey(id)) points.Add(id, point.position);
            }

            if (points.Count >= 25 && points.ContainsKey(25) && points.ContainsKey(1) && points.ContainsKey(9))
            {
                Vector3 origin = points[25];
                Vector3 xAxis = (points[1] - origin).normalized;
                // The 03-local target numbering snakes after the first row:
                // target 9 is the first point on the positive Z axis, while
                // target 10 is two grid units away.  Using 10 doubled every
                // Z segment and made valid physical lines impossible.
                Vector3 zAxis = (points[9] - origin).normalized;
                float xUnit = Vector3.Distance(origin, points[1]);
                float zUnit = Vector3.Distance(origin, points[9]);
                if (xUnit > 0.001f && zUnit > 0.001f)
                {
                    for (int i = 0; i <= 4; i++)
                    {
                        Vector3 start = origin + xAxis * (i * xUnit);
                        Vector3 end = start + zAxis * (4f * zUnit);
                        segments.Add(new Segment { start = start, end = end });
                    }
                    for (int i = 0; i <= 4; i++)
                    {
                        Vector3 start = origin + zAxis * (i * zUnit);
                        Vector3 end = start + xAxis * (4f * xUnit);
                        segments.Add(new Segment { start = start, end = end });
                    }
                    return;
                }
            }
        }

        for (int i = 0; i <= 4; i++)
        {
            segments.Add(new Segment
            {
                start = gridOrigin + new Vector3(i * gridUnit, 0f, 0f),
                end = gridOrigin + new Vector3(i * gridUnit, 0f, 4f * gridUnit)
            });
        }

        for (int i = 0; i <= 4; i++)
        {
            segments.Add(new Segment
            {
                start = gridOrigin + new Vector3(0f, 0f, i * gridUnit),
                end = gridOrigin + new Vector3(4f * gridUnit, 0f, i * gridUnit)
            });
        }
    }

    private void Update()
    {
        if (errorMessage != null && errorMessage.activeSelf && Time.time >= errorVisibleUntil)
        {
            errorMessage.SetActive(false);
        }

        UpdatePreparation();
        EnforceJiZhunVisibility();
        UpdateDatumHintMarkers();
        UpdateMoDouEndpointHighlight();

        if (!stepActive || preparationPhase != PreparationPhase.DrawLines)
        {
            return;
        }

        LogPreviewLineState();
        if (previewLine == null || firstMoDouEnd == null || secondMoDouEnd == null)
        {
            return;
        }

        previewLine.positionCount = 2;
        previewLine.SetPosition(0, firstMoDouEnd.position);
        previewLine.SetPosition(1, secondMoDouEnd.position);
        // Re-assert visibility while drawing: neither the shared Configure pass
        // nor a scene-default renderer state may leave the ink line switched off.
        if (!previewLine.enabled)
        {
            previewLine.enabled = true;
        }

        UpdateDragCubePosition();
    }

    /// <summary>
    /// One-shot report of the ink line, so a missing binding can be told apart
    /// from a disabled renderer, a degenerate span or an invisible colour.
    /// </summary>
    private void LogPreviewLineState()
    {
        if (previewLineLogged)
        {
            return;
        }

        previewLineLogged = true;
        if (previewLine == null)
        {
            BpfLog.Warning(BpfLogCategory.XR, "tex_core_m05_draw_step.preview_line",
                "previewLine is null: HuaXian/MoDou/DragLine did not resolve.", this);
            return;
        }

        Vector3 first = firstMoDouEnd != null ? firstMoDouEnd.position : Vector3.zero;
        Vector3 second = secondMoDouEnd != null ? secondMoDouEnd.position : Vector3.zero;
        BpfLog.Info(BpfLogCategory.XR, "tex_core_m05_draw_step.preview_line",
            $"enabled={previewLine.enabled} worldSpace={previewLine.useWorldSpace} positions={previewLine.positionCount} width={previewLine.widthMultiplier} start={previewLine.startColor} length={Vector3.Distance(first, second):0.000} first={first} second={second}",
            this);
    }

    /// <summary>
    /// Entry point for the four M05-only backpack buttons.  The button is not
    /// itself a completion event: every physical prerequisite remains checked
    /// here before the 10-line scored operation is enabled.
    /// </summary>
    public void UseBackpackAction(string action)
    {
        if (!stepActive || completed)
        {
            SetLaserSwitchHighlight(false);
            return;
        }

        if (!string.Equals(action, RequiredBackpackAction, StringComparison.Ordinal))
        {
            ShowHint($"当前步骤不能使用{action}");
            return;
        }

        switch (preparationPhase)
        {
            case PreparationPhase.SummonLaserLevel:
                if (!SummonTool(laserLevel))
                {
                    return;
                }
                preparationPhase = PreparationPhase.SnapLaserLevel;
                break;
            case PreparationPhase.EnableLaserRay:
                BeginLaserRayAnimation();
                return;
            case PreparationPhase.SummonMarkerPen:
                if (!SummonTool(markerPen))
                {
                    return;
                }
                EnsureDatumHintMarkers();
                preparationPhase = PreparationPhase.MarkDatumPoints;
                break;
            case PreparationPhase.SummonMoDou:
                StartDrawing();
                return;
        }

        TexCore_05BackpackController.Present(this);
    }

    private void UpdatePreparation()
    {
        if (!stepActive || completed)
        {
            SetLaserSwitchHighlight(false);
            return;
        }

        if (preparationPhase == PreparationPhase.SnapLaserLevel && laserLevel != null)
        {
            Vector3 snapPosition = GetLaserSnapPosition();
            if (Vector3.Distance(laserLevel.position, snapPosition) < laserSnapTolerance)
            {
                laserLevel.SetPositionAndRotation(snapPosition, Quaternion.identity);
                SetToolManipulation(laserLevel, false);
                if (laserSwitch != null)
                {
                    laserSwitch.gameObject.SetActive(true);
                    // The switch is a deliberate press target, not another
                    // freely movable tool after the level has snapped.
                    SetToolManipulation(laserSwitch, false);
                }
                // The level body and the switch mesh both stay solid for the
                // rest of the scene; only the dedicated hit box may answer the
                // ray while this phase is active.
                SetLaserSwitchOccludersSuppressed(true);
                if (laserSwitchButton != null)
                {
                    laserSwitchButton.enabled = true;
                }
                preparationPhase = PreparationPhase.EnableLaserRay;
                nextLaserSwitchHintTime = 0f;
                ShowHint("请按黄色高亮的 polySurface10 开关，开启激光射线。");
                SetLaserSwitchHighlight(true);
                TexCore_05BackpackController.Present(this);
            }
        }

        UpdateLaserSnapGuidance();
        if (preparationPhase == PreparationPhase.EnableLaserRay &&
            Time.unscaledTime >= nextLaserSwitchHintTime)
        {
            ShowHint("请按黄色高亮的 polySurface10 开关，开启激光射线。");
            nextLaserSwitchHintTime = Time.unscaledTime + 3f;
        }

        if (preparationPhase == PreparationPhase.MarkDatumPoints)
        {
            MarkNearbyDatumPoints();
        }
    }

    private void BeginLaserRayAnimation()
    {
        // Single-fire latch: firstSelectEntered and OnClicked both route here,
        // but the first call moves the phase on, so the second one returns
        // above and the rotation can never play twice for one press.
        if (preparationPhase != PreparationPhase.EnableLaserRay)
        {
            return;
        }

        preparationPhase = PreparationPhase.AnimatingLaserRay;
        SetLaserSwitchHighlight(false);
        SetToolManipulation(laserSwitch, false);
        if (laserSwitchButton != null)
        {
            laserSwitchButton.enabled = false;
        }
        if (laserRayAnimation != null)
        {
            StopCoroutine(laserRayAnimation);
        }

        laserRayAnimation = StartCoroutine(PlayLaserSwitchAnimation());
    }

    private IEnumerator PlayLaserSwitchAnimation()
    {
        if (laserSwitch == null)
        {
            yield break;
        }

        Quaternion startRotation = laserSwitch.localRotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(0f, 0f, 90f);
        float elapsed = 0f;
        while (elapsed < laserSwitchAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / laserSwitchAnimationDuration);
            t = t * t * (3f - 2f * t);
            laserSwitch.localRotation = Quaternion.Slerp(startRotation, endRotation, t);
            yield return null;
        }

        laserSwitch.localRotation = endRotation;
        laserRayAnimation = null;
        if (!stepActive || completed || preparationPhase != PreparationPhase.AnimatingLaserRay)
        {
            yield break;
        }

        if (laserRay != null)
        {
            laserRay.gameObject.SetActive(true);
        }
        preparationPhase = PreparationPhase.SummonMarkerPen;
        TexCore_05BackpackController.Present(this);
    }

    private void MarkNearbyDatumPoints()
    {
        if (markerTip == null || laserRay == null || !laserRay.gameObject.activeInHierarchy)
        {
            return;
        }

        bool markedAny = false;
        for (int i = 0; i < datumMarks.Count; i++)
        {
            DatumMark datum = datumMarks[i];
            if (datum.marker != null || Vector3.Distance(markerTip.position, datum.position) >= markTolerance)
            {
                continue;
            }

            datum.marker = CreateDatumMarker(i, datum.position);
            markedAny = datum.marker != null;
        }

        if (markedAny)
        {
            TexCore_05BackpackController.RefreshFor(this);
        }

        if (datumMarks.Count != ExpectedDatumMarkCount || datumMarks.Any(value => value.marker == null))
        {
            return;
        }

        // This matches DrawGridV2: once all eight non-origin points are
        // marked, the laser level is removed and the ink line becomes usable.
        ClearDatumHintMarkers();
        SetDatumMarkersVisible(false);
        SetSnapOriginVisible(false);
        SetToolManipulation(markerPen, false);
        if (markerPen != null) markerPen.gameObject.SetActive(false);
        SetToolManipulation(laserLevel, false);
        if (laserLevel != null) laserLevel.gameObject.SetActive(false);
        jiZhunUnlocked = true;
        preparationPhase = PreparationPhase.SummonMoDou;
        SetJiZhunVisible(true);
        ShowHint("已完成 8 个点，请召唤墨斗。");
        TexCore_05BackpackController.Present(this);
    }

    /// <summary>
    /// Hides the runtime datum markers without dropping their references, so
    /// <see cref="MarkedDatumCount"/> keeps reporting the finished eight points
    /// while the markers stop cluttering the ink-line phase.
    /// </summary>
    private void SetDatumMarkersVisible(bool visible)
    {
        foreach (DatumMark datum in datumMarks)
        {
            if (datum.marker != null)
            {
                datum.marker.SetActive(visible);
            }
        }
    }

    /// <summary>
    /// The origin marker belongs to the marking stage only. It is the point the
    /// laser level snaps to, so it has to be back for a fresh attempt, and it
    /// leaves together with the eight marks when the grid is complete.
    /// </summary>
    private void SetSnapOriginVisible(bool visible)
    {
        if (snapOrigin == null)
        {
            return;
        }

        bool shouldShow = visible && snapOriginInitialActive;
        if (snapOrigin.gameObject.activeSelf != shouldShow)
        {
            snapOrigin.gameObject.SetActive(shouldShow);
        }
    }

    /// <summary>
    /// Creates one faint target hint per non-origin datum point. The hints are
    /// what tells the player where the eight marks go before any of them exist.
    /// </summary>
    private void EnsureDatumHintMarkers()
    {
        if (datumHintMarkers.Count > 0 || visibilityRoot == null)
        {
            return;
        }

        for (int i = 0; i < datumMarks.Count; i++)
        {
            DatumMark datum = datumMarks[i];
            GameObject hint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hint.name = $"M05_基准点提示_{i + 1}";
            hint.transform.SetParent(visibilityRoot, true);
            hint.transform.position = datum.position + Vector3.up * 0.012f;
            hint.transform.localScale = Vector3.one * 0.07f;
            foreach (Collider collider in hint.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }

            SetDatumHintColor(hint, DatumHintColor);
            datum.hint = hint;
            datumHintMarkers.Add(hint);
        }
    }

    /// <summary>
    /// Keeps every pending point faint and turns the one under the pen tip
    /// green, then hands that single point to the shared focus so the player also
    /// gets the outline and the arrow from the pen. A marked point loses its hint
    /// at once and stops being a target.
    /// </summary>
    private void UpdateDatumHintMarkers()
    {
        if (preparationPhase != PreparationPhase.MarkDatumPoints)
        {
            return;
        }

        DatumMark ready = null;
        float readyDistance = markTolerance;
        foreach (DatumMark datum in datumMarks)
        {
            if (datum.hint == null)
            {
                continue;
            }

            if (datum.marker != null)
            {
                datum.hint.SetActive(false);
                continue;
            }

            float distance = markerTip != null
                ? Vector3.Distance(markerTip.position, datum.position)
                : float.MaxValue;
            SetDatumHintColor(datum.hint, distance <= markTolerance ? ReadyColor : DatumHintColor);
            if (distance <= readyDistance)
            {
                readyDistance = distance;
                ready = datum;
            }
        }

        if (ready != null)
        {
            TexCore_FocusHighlight.Show(
                ready.hint,
                "可标记",
                ReadyColor,
                markerTip != null ? markerTip.gameObject : null,
                null,
                this);
            return;
        }

        TexCore_FocusHighlight.ClearFor(this);
    }

    private static void SetDatumHintColor(GameObject hint, Color color)
    {
        Renderer renderer = hint != null ? hint.GetComponent<Renderer>() : null;
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }

    private void ClearDatumHintMarkers()
    {
        TexCore_FocusHighlight.ClearFor(this);
        foreach (GameObject hint in datumHintMarkers)
        {
            if (hint == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(hint);
            }
            else
            {
                DestroyImmediate(hint);
            }
        }

        datumHintMarkers.Clear();
        foreach (DatumMark datum in datumMarks)
        {
            datum.hint = null;
        }
    }

    private static Color ReadyColor => TexCore_FocusHighlight.ReadyColor;

    /// <summary>
    /// Testing shortcut: puts the step straight into the ink-line phase so the
    /// drawing can be exercised without redoing the laser, ray and marking.
    /// </summary>
    public void JumpToInkDrawing()
    {
        if (!stepActive || completed || preparationPhase == PreparationPhase.DrawLines)
        {
            return;
        }

        if (laserRayAnimation != null)
        {
            StopCoroutine(laserRayAnimation);
            laserRayAnimation = null;
        }

        if (laserRay != null) laserRay.gameObject.SetActive(false);
        if (laserLevel != null) laserLevel.gameObject.SetActive(false);
        if (markerPen != null) markerPen.gameObject.SetActive(false);
        if (laserSwitch != null) laserSwitch.gameObject.SetActive(false);
        ClearDatumHintMarkers();
        SetDatumMarkersVisible(false);
        SetSnapOriginVisible(false);
        SetLaserSwitchOccludersSuppressed(false);
        SetLaserSwitchHighlight(false);
        jiZhunUnlocked = true;
        SetJiZhunVisible(true);
        preparationPhase = PreparationPhase.SummonMoDou;
        StartDrawing();
    }

    private void StartDrawing()
    {
        if (preparationPhase != PreparationPhase.SummonMoDou)
        {
            return;
        }

        SetMoDouVisible(true);
        if (moDouRoot != null)
        {
            TexCore_BackpackPhysics.Stabilize(moDouRoot.gameObject);
        }
        ActivateRelevantPath(interactionSource != null ? interactionSource.transform : null, visibilityRoot);
        ActivateRelevantPath(firstMoDouEnd, visibilityRoot);
        ActivateRelevantPath(secondMoDouEnd, visibilityRoot);
        SetManipulation(true);
        SetLineVisible(true);
        preparationPhase = PreparationPhase.DrawLines;
        ShowLineProgress();
        TexCore_05BackpackController.Present(this);
    }

    private void ResetPreparation()
    {
        if (laserRayAnimation != null)
        {
            StopCoroutine(laserRayAnimation);
            laserRayAnimation = null;
        }

        // Restore the level body and switch colliders before the tool poses are
        // reset, so the next attempt starts from the scene's original state.
        SetLaserSwitchOccludersSuppressed(false);
        ClearDatumHintMarkers();
        SetSnapOriginVisible(true);
        ClearMoDouEndpointHighlight();

        if (laserSwitchButton != null)
        {
            laserSwitchButton.enabled = false;
        }

        jiZhunUnlocked = false;
        SetJiZhunVisible(false);

        if (laserRay != null) laserRay.gameObject.SetActive(false);
        if (laserSwitch != null)
        {
            SetToolManipulation(laserSwitch, false);
            laserSwitch.gameObject.SetActive(false);
            if (toolStartPoseCaptured)
            {
                laserSwitch.localRotation = laserSwitchStartLocalRotation;
            }
        }

        SetToolManipulation(laserLevel, false);
        SetToolManipulation(markerPen, false);
        if (toolStartPoseCaptured)
        {
            if (laserLevel != null) laserLevel.SetPositionAndRotation(laserStartPosition, laserStartRotation);
            if (markerPen != null) markerPen.SetPositionAndRotation(penStartPosition, penStartRotation);
        }
        if (laserLevel != null) laserLevel.gameObject.SetActive(false);
        if (markerPen != null) markerPen.gameObject.SetActive(false);

        foreach (DatumMark datum in datumMarks)
        {
            if (datum.marker == null)
            {
                continue;
            }

            if (Application.isPlaying) Destroy(datum.marker);
            else DestroyImmediate(datum.marker);
            datum.marker = null;
        }
    }

    private bool SummonTool(Transform tool)
    {
        if (tool == null)
        {
            ShowHint("当前工具对象未绑定");
            return false;
        }

        ActivateRelevantPath(tool, visibilityRoot);
        tool.gameObject.SetActive(true);
        TexCore_BackpackPhysics.Stabilize(tool.gameObject);
        SetToolManipulation(tool, true);
        return true;
    }

    private void SetMoDouVisible(bool visible)
    {
        if (moDouRoot != null)
        {
            moDouRoot.gameObject.SetActive(visible);
        }
        else
        {
            SetVisible(visible);
        }
    }

    private Vector3 GetLaserSnapPosition()
    {
        // Verified from the SHIXUN DrawGridV2 source.  Treat it as a local
        // point so the migrated HuaXian root can be placed anywhere in 03.
        return visibilityRoot != null
            ? visibilityRoot.TransformPoint(new Vector3(0f, 0f, 0.066f))
            : new Vector3(0f, 0f, 0.066f);
    }

    /// <summary>
    /// Creates the runtime press target for the laser switch.  polySurface10
    /// owns a BoxCollider and its parent ShuiPingYi owns a much larger one, so
    /// a ray aimed at the switch is resolved against the parent body first and
    /// the switch itself stays hard to press.  A small dedicated child collider
    /// gives the button its own reachable hit area without touching the scene.
    /// </summary>
    private void EnsureLaserSwitchHitBox()
    {
        if (laserSwitchHitBox != null || laserSwitch == null)
        {
            return;
        }

        laserSwitchHitBoxRoot = new GameObject("M05_polySurface10_按压命中体");
        laserSwitchHitBoxRoot.layer = laserSwitch.gameObject.layer;
        laserSwitchHitBoxRoot.transform.SetParent(laserSwitch, false);
        laserSwitchHitBoxRoot.transform.localPosition = Vector3.zero;
        laserSwitchHitBoxRoot.transform.localRotation = Quaternion.identity;
        laserSwitchHitBoxRoot.transform.localScale = Vector3.one;

        laserSwitchHitBox = laserSwitchHitBoxRoot.AddComponent<BoxCollider>();
        laserSwitchHitBox.isTrigger = false;
        laserSwitchHitBox.size = laserSwitchHitBoxSize;
    }

    private void BindLaserSwitchButton()
    {
        if (laserSwitchButton != null)
        {
            laserSwitchButton.OnClicked.RemoveListener(HandleLaserSwitchClicked);
            laserSwitchButton.firstSelectEntered.RemoveListener(HandleLaserSwitchSelected);
            laserSwitchButton = null;
        }

        EnsureLaserSwitchHitBox();
        if (laserSwitchHitBoxRoot == null)
        {
            return;
        }

        laserSwitchButton = laserSwitchHitBoxRoot.GetComponent<PressableButton>() ??
                            laserSwitchHitBoxRoot.AddComponent<PressableButton>();
        if (laserSwitchButton == null)
        {
            return;
        }

        // The button must own exactly the dedicated hit box.  XRBaseInteractable
        // only fills its collider list from Awake, so it has to be re-pointed
        // whenever the list was populated by an earlier bind.
        laserSwitchButton.TriggerOnRelease = true;
        laserSwitchButton.colliders.Clear();
        laserSwitchButton.colliders.Add(laserSwitchHitBox);

        laserSwitchButton.OnClicked.RemoveListener(HandleLaserSwitchClicked);
        laserSwitchButton.OnClicked.AddListener(HandleLaserSwitchClicked);
        laserSwitchButton.firstSelectEntered.RemoveListener(HandleLaserSwitchSelected);
        laserSwitchButton.firstSelectEntered.AddListener(HandleLaserSwitchSelected);
        laserSwitchButton.enabled = false;

        BpfLog.Info(BpfLogCategory.XR, "tex_core_m05_draw_step.laser_switch_bind",
            $"hitBox={laserSwitchHitBoxRoot.name} size={laserSwitchHitBoxSize} colliders={laserSwitchButton.colliders.Count} triggerOnRelease={laserSwitchButton.TriggerOnRelease} layer={LayerMask.LayerToName(laserSwitchButton.gameObject.layer)} enabled={laserSwitchButton.enabled}",
            this);
    }

    /// <summary>
    /// Silences the colliders and manipulators that would otherwise win ray
    /// selection against the dedicated hit box.  Only that hit box stays
    /// interactable while the switch phase is active.  Recorded components are
    /// restored on reset, step exit and disable.
    /// </summary>
    private void SetLaserSwitchOccludersSuppressed(bool suppressed)
    {
        if (!suppressed)
        {
            foreach (Collider collider in laserSwitchSuppressedColliders)
            {
                if (collider != null)
                {
                    collider.enabled = true;
                }
            }

            foreach (ObjectManipulator manipulator in laserSwitchSuppressedManipulators)
            {
                if (manipulator != null)
                {
                    manipulator.enabled = true;
                }
            }

            laserSwitchSuppressedColliders.Clear();
            laserSwitchSuppressedManipulators.Clear();
            return;
        }

        if (laserSwitchSuppressedColliders.Count > 0 || laserSwitchSuppressedManipulators.Count > 0)
        {
            return;
        }

        SuppressOccludersOn(laserSwitch);
        SuppressOccludersOn(laserLevel);

        BpfLog.Info(BpfLogCategory.XR, "tex_core_m05_draw_step.laser_switch_occluders",
            $"suppressed colliders={laserSwitchSuppressedColliders.Count} manipulators={laserSwitchSuppressedManipulators.Count} hitBoxEnabled={(laserSwitchHitBox != null && laserSwitchHitBox.enabled)} buttonEnabled={(laserSwitchButton != null && laserSwitchButton.enabled)}",
            this);
    }

    private void SuppressOccludersOn(Transform target)
    {
        if (target == null)
        {
            return;
        }

        // Own components only: the hit box is a child and must stay enabled.
        foreach (Collider collider in target.GetComponents<Collider>())
        {
            if (collider == null || !collider.enabled)
            {
                continue;
            }

            collider.enabled = false;
            laserSwitchSuppressedColliders.Add(collider);
        }

        foreach (ObjectManipulator manipulator in target.GetComponents<ObjectManipulator>())
        {
            if (manipulator == null || !manipulator.enabled)
            {
                continue;
            }

            manipulator.enabled = false;
            laserSwitchSuppressedManipulators.Add(manipulator);
        }
    }

    /// <summary>
    /// Prefers the 03-local HuaXian/JiZhun indicator.  The scene-wide lookup is
    /// kept as a controlled fallback: it only accepts the same name inside the
    /// same scene and inside this HuaXian root, so M05 can never bind an
    /// indicator that belongs to another scene or to a prefab asset.
    /// </summary>
    private static Transform ResolveJiZhunIndicator(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        Transform local = FindChild(root, "JiZhun", "LuoSi/JiZhun");
        if (local != null)
        {
            return local;
        }

        Transform candidate = TexCore_M05LevelBootstrap.FindSceneTransform(root.gameObject.scene, "JiZhun");
        return candidate != null && candidate.IsChildOf(root) ? candidate : null;
    }

    private void SetJiZhunVisible(bool visible)
    {
        if (jiZhunIndicator == null)
        {
            return;
        }

        // JiZhun is the reward for finishing the eight datum marks, so any
        // request made before the unlock stays hidden.
        bool shouldShow = visible && jiZhunUnlocked;
        if (jiZhunIndicator.gameObject.activeSelf != shouldShow)
        {
            jiZhunIndicator.gameObject.SetActive(shouldShow);
        }
    }

    /// <summary>
    /// The legacy 05 hierarchy activates JiZhun outside this step: ShuiPingYi
    /// carries an ObjectManipulator whose runtime OnDisabled event calls
    /// JiZhun.SetActive(true), and it fires as soon as the laser level snaps
    /// and its manipulation is switched off.  Re-assert the phase gate every
    /// frame so the indicator cannot appear before the eight datum marks.
    /// </summary>
    private void EnforceJiZhunVisibility()
    {
        if (jiZhunIndicator == null || !jiZhunIndicator.gameObject.activeSelf)
        {
            return;
        }

        bool unlockedPhase = jiZhunUnlocked && stepActive && !completed &&
            (preparationPhase == PreparationPhase.SummonMoDou ||
             preparationPhase == PreparationPhase.DrawLines);
        if (!unlockedPhase)
        {
            SetJiZhunVisible(false);
        }
    }

    private void HandleLaserSwitchClicked()
    {
        if (!stepActive || completed || preparationPhase != PreparationPhase.EnableLaserRay)
        {
            return;
        }

        BeginLaserRayAnimation();
    }

    private void HandleLaserSwitchSelected(SelectEnterEventArgs args)
    {
        if (!stepActive || completed || preparationPhase != PreparationPhase.EnableLaserRay)
        {
            return;
        }

        BeginLaserRayAnimation();
    }

    /// <summary>
    /// Focus guidance for the laser switch is delegated to the shared
    /// <see cref="TexCore_FocusHighlight"/> so M05 matches the other guided
    /// TexCore topics instead of drawing its own outline and arrow.
    /// </summary>
    private void SetLaserSwitchHighlight(bool visible)
    {
        if (!visible || laserSwitch == null)
        {
            TexCore_FocusHighlight.ClearFor(this);
            return;
        }

        TexCore_FocusHighlight.Show(laserSwitch.gameObject, null, null, null, this);
    }

    /// <summary>
    /// Points the shared focus at the snap origin while the level still has to
    /// be carried there.  The hint is constant on purpose: the shared focus
    /// rebroadcasts a hint whenever it changes, so a per-frame distance would
    /// spam the hint channel.
    /// </summary>
    private void UpdateLaserSnapGuidance()
    {
        if (preparationPhase != PreparationPhase.SnapLaserLevel || snapOrigin == null)
        {
            return;
        }

        TexCore_FocusHighlight.Show(
            snapOrigin.gameObject,
            SnapHint,
            laserLevel != null ? laserLevel.gameObject : null,
            null,
            this);
    }

    private string SnapHint
    {
        get
        {
            if (snapHintCache == null)
            {
                snapHintCache = $"请将水平仪靠近基准位（<{laserSnapTolerance:0.00}m）";
            }

            return snapHintCache;
        }
    }

    private void CaptureToolStartPose()
    {
        if (toolStartPoseCaptured || laserLevel == null || markerPen == null)
        {
            return;
        }

        laserStartPosition = laserLevel.position;
        laserStartRotation = laserLevel.rotation;
        penStartPosition = markerPen.position;
        penStartRotation = markerPen.rotation;
        laserSwitchStartLocalRotation = laserSwitch != null ? laserSwitch.localRotation : Quaternion.identity;
        toolStartPoseCaptured = true;
    }

    private void BuildDatumMarks()
    {
        if (datumMarks.Count > 0)
        {
            return;
        }

        Dictionary<int, Vector3> points = new Dictionary<int, Vector3>();
        if (gridReference != null)
        {
            foreach (Transform point in gridReference.GetComponentsInChildren<Transform>(true))
            {
                int id = Suffix(point.name, "ZhanTieDian");
                if (id > 0 && !points.ContainsKey(id))
                {
                    points.Add(id, point.position);
                }
            }
        }

        if (points.TryGetValue(25, out Vector3 origin) && points.TryGetValue(1, out Vector3 xPoint) &&
            points.TryGetValue(9, out Vector3 zPoint))
        {
            Vector3 xAxis = (xPoint - origin).normalized;
            Vector3 zAxis = (zPoint - origin).normalized;
            float xUnit = Vector3.Distance(origin, xPoint);
            float zUnit = Vector3.Distance(origin, zPoint);
            if (xUnit > 0.001f && zUnit > 0.001f)
            {
                for (int i = 1; i <= 4; i++)
                {
                    datumMarks.Add(new DatumMark { position = origin + xAxis * (xUnit * i) });
                    datumMarks.Add(new DatumMark { position = origin + zAxis * (zUnit * i) });
                }
                return;
            }
        }

        Vector3 fallbackOrigin = visibilityRoot != null ? visibilityRoot.position : Vector3.zero;
        for (int i = 1; i <= 4; i++)
        {
            datumMarks.Add(new DatumMark { position = fallbackOrigin + new Vector3(i, 0f, 0f) });
            datumMarks.Add(new DatumMark { position = fallbackOrigin + new Vector3(0f, 0f, i) });
        }
    }

    private GameObject CreateDatumMarker(int index, Vector3 position)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = $"M05_基准标点_{index + 1}";
        marker.transform.SetParent(visibilityRoot, true);
        marker.transform.position = position + Vector3.up * 0.012f;
        marker.transform.localScale = Vector3.one * 0.045f;
        foreach (Collider collider in marker.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        Renderer renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(1f, 0.74f, 0.05f, 1f);
        }
        return marker;
    }

    private static void SetToolManipulation(Transform tool, bool enabled)
    {
        if (tool == null)
        {
            return;
        }

        foreach (ObjectManipulator manipulator in tool.GetComponentsInChildren<ObjectManipulator>(true))
        {
            manipulator.enabled = enabled;
        }
    }

    private static string PhaseNameFor(PreparationPhase phase)
    {
        switch (phase)
        {
            case PreparationPhase.SummonLaserLevel: return "召唤激光水平仪";
            case PreparationPhase.SnapLaserLevel: return "放置激光水平仪";
            case PreparationPhase.EnableLaserRay:
            case PreparationPhase.AnimatingLaserRay: return "开启激光射线";
            case PreparationPhase.SummonMarkerPen: return "召唤记号笔";
            case PreparationPhase.MarkDatumPoints: return "标记基准点";
            case PreparationPhase.SummonMoDou: return "召唤墨斗";
            case PreparationPhase.DrawLines: return "弹线";
            case PreparationPhase.Completed: return "划线完成";
            default: return "未开始";
        }
    }

    private static string RequiredActionFor(PreparationPhase phase)
    {
        switch (phase)
        {
            case PreparationPhase.SummonLaserLevel: return "激光水平仪";
            case PreparationPhase.EnableLaserRay: return "开启射线";
            case PreparationPhase.SummonMarkerPen: return "记号笔";
            case PreparationPhase.SummonMoDou: return "墨斗";
            default: return string.Empty;
        }
    }

    private string HintFor(PreparationPhase phase)
    {
        switch (phase)
        {
            case PreparationPhase.SummonLaserLevel:
                return "从 05 专用背包召唤激光水平仪。";
            case PreparationPhase.SnapLaserLevel:
                return "将激光水平仪靠近基准位；距离小于 0.3m 自动吸附。";
            case PreparationPhase.EnableLaserRay:
                return "旋转水平仪开关开启射线，或点击背包中的“开启射线”。";
            case PreparationPhase.SummonMarkerPen:
                return "从 05 专用背包召唤记号笔。";
            case PreparationPhase.MarkDatumPoints:
                return $"用笔尖标记 8 个非原点基准位（{MarkedDatumCount}/{RequiredDatumMarkCount}）。";
            case PreparationPhase.SummonMoDou:
                return "已完成 8 个点，请召唤墨斗。";
            case PreparationPhase.DrawLines:
                return $"墨斗已召唤，可划 10 条基准线（{CompletedSegmentCount}/{RequiredSegmentCount}）。";
            case PreparationPhase.Completed:
                return "划线完成。";
            default:
                return string.Empty;
        }
    }

    /// <summary>
    /// Writes the line count into the persistent Tips Canvas title. The
    /// 显示提示 channel is a modal dialog in practice mode, so it must never
    /// carry a running counter that changes once per line.
    /// </summary>
    private void ShowLineProgress()
    {
        string progress = $"划线：{CompletedSegmentCount}/{RequiredSegmentCount}";
        TexCore_EventCenter.Broadcast<string>(
            TexCore_EventType.显示标题,
            progress + "\n请把墨斗两端分别贴到同一条网格线的两个端点上");
        // The running total also goes out as a popup, because the title line is
        // easy to miss while looking down at the floor.
        TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, progress, 1.5f);
    }

    private static void ShowHint(string text)
    {
        TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, text, 1.5f);
    }

    private void ConfigureLineRenderer(LineRenderer line)
    {
        if (line == null) return;
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.enabled = false;
    }

    private void SetLineVisible(bool enabled)
    {
        if (previewLine != null) previewLine.enabled = enabled;
        // PlaneLine is a clone template, never a visible pre-drawn line.
        // Keeping it hidden prevents the scene-default renderer from leaking
        // all line visuals before the eight datum marks are complete.
        if (completedLineTemplate != null) completedLineTemplate.enabled = false;
    }

    private void ClearCompletedLines()
    {
        foreach (LineRenderer line in completedLines)
        {
            if (line == null) continue;
            if (Application.isPlaying) Destroy(line.gameObject);
            else DestroyImmediate(line.gameObject);
        }
        completedLines.Clear();
        drawnSegmentIndexes.Clear();
    }

    private void DrawCompletedLine(int index, Segment segment)
    {
        if (completedLineTemplate == null || index < 0) return;
        if (!drawnSegmentIndexes.Add(index)) return;
        // Parent to the HuaXian root instead of the MoDou template: the line
        // positions are world-space grid endpoints and the line must stay
        // visible after the MoDou is hidden and during the QR step.
        Transform parent = visibilityRoot != null
            ? visibilityRoot
            : completedLineTemplate.transform.parent;
        LineRenderer line = Instantiate(completedLineTemplate, parent);
        line.gameObject.name = "M05_CompletedLine_" + index;
        line.gameObject.SetActive(true);
        line.enabled = true;
        line.useWorldSpace = true;
        line.positionCount = 2;
        ForceBlackLineStyle(line);
        line.SetPosition(0, segment.start);
        line.SetPosition(1, segment.end);
        completedLines.Add(line);
    }

    /// <summary>
    /// Forces the runtime completed-line style: a plain black gradient with
    /// black endpoint colours and a stable width, no matter what colour or
    /// width the scene's PlaneLine template carries.
    /// </summary>
    private static void ForceBlackLineStyle(LineRenderer line)
    {
        line.startColor = Color.black;
        line.endColor = Color.black;
        line.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));
        line.widthMultiplier = CompletedLineWidth;
        Renderer renderer = line.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        // Vertex colours multiply with the material, so blackening the known
        // tint channels keeps the line black even on textured templates.
        Material material = renderer.material;
        if (material.HasProperty(MainColorId)) material.color = Color.black;
        if (material.HasProperty(TintColorId)) material.SetColor(TintColorId, Color.black);

        // The template material is lit, which is why a black ink line stayed
        // invisible. Draw the completed lines with the same unlit shader the
        // runtime guides use, so only the vertex colour decides how they read.
        Material unlit = CompletedLineMaterial;
        if (unlit != null)
        {
            line.sharedMaterial = unlit;
        }
    }

    private static Material completedLineMaterial;

    private static Material CompletedLineMaterial
    {
        get
        {
            if (completedLineMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    completedLineMaterial = new Material(shader)
                    {
                        name = "M05_划线黑色材质",
                        hideFlags = HideFlags.DontSave
                    };
                }
            }

            return completedLineMaterial;
        }
    }

    /// <summary>
    /// Creates the single black JiZhun datum-plane copy after the tenth line.
    /// It keeps the original's world position, rotation, scale and meshes, is
    /// repainted black, and drops every collider and manipulator so it can
    /// never block the QR paste interactions.
    /// </summary>
    private void CreateJiZhunClone()
    {
        if (jiZhunClone != null || jiZhunIndicator == null)
        {
            return;
        }

        GameObject clone = Instantiate(
            jiZhunIndicator.gameObject,
            visibilityRoot != null ? visibilityRoot : jiZhunIndicator.parent);
        clone.name = "M05_JiZhun_黑色复制体";
        clone.transform.SetPositionAndRotation(jiZhunIndicator.position, jiZhunIndicator.rotation);
        clone.transform.localScale = jiZhunIndicator.lossyScale;
        foreach (Collider collider in clone.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        foreach (ObjectManipulator manipulator in clone.GetComponentsInChildren<ObjectManipulator>(true))
        {
            manipulator.enabled = false;
        }

        foreach (MeshRenderer renderer in clone.GetComponentsInChildren<MeshRenderer>(true))
        {
            Material[] materials = renderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i].HasProperty(MainColorId)) materials[i].color = Color.black;
                if (materials[i].HasProperty(EmissionColorId))
                {
                    materials[i].SetColor(EmissionColorId, Color.black);
                    materials[i].DisableKeyword(EmissionKeyword);
                }
            }

            renderer.materials = materials;
        }

        clone.SetActive(true);
        jiZhunClone = clone;
    }

    /// <summary>
    /// Keeps the black copy visible during the QR step without touching the
    /// phase-gated original JiZhun indicator.
    /// </summary>
    public void EnsureJiZhunCloneVisible()
    {
        if (jiZhunClone != null) jiZhunClone.SetActive(true);
    }

    public void DestroyJiZhunClone()
    {
        if (jiZhunClone == null) return;
        if (Application.isPlaying) Destroy(jiZhunClone);
        else DestroyImmediate(jiZhunClone);
        jiZhunClone = null;
    }

    private void CacheManipulators()
    {
        foreach (ObjectManipulator manipulator in manipulators)
        {
            manipulator.lastSelectExited.RemoveListener(Released);
        }

        manipulators.Clear();
        AddManipulator(interactionSource != null ? interactionSource.transform : null);
        AddManipulator(firstMoDouEnd);
        AddManipulator(secondMoDouEnd);
        dragCubeManipulator = interactionSource != null
            ? interactionSource.GetComponent<ObjectManipulator>() ??
              interactionSource.GetComponentInChildren<ObjectManipulator>(true)
            : null;
    }

    /// <summary>
    /// Keeps the drag block on the midpoint of the strung ink line, the way the
    /// legacy DrawGridV2 did, so the block the player has to pluck always sits
    /// between the two ends. While the block itself is held it stays free.
    /// </summary>
    private void UpdateDragCubePosition()
    {
        if (interactionSource == null || firstMoDouEnd == null || secondMoDouEnd == null)
        {
            return;
        }

        if (dragCubeManipulator != null && dragCubeManipulator.isSelected)
        {
            return;
        }

        interactionSource.transform.position =
            (firstMoDouEnd.position + secondMoDouEnd.position) * 0.5f;
    }

    private void AddManipulator(Transform source)
    {
        if (source == null)
        {
            return;
        }

        ObjectManipulator manipulator = source.GetComponent<ObjectManipulator>() ??
                                        source.GetComponentInParent<ObjectManipulator>() ??
                                        source.GetComponentInChildren<ObjectManipulator>(true);
        if (manipulator == null || manipulators.Contains(manipulator))
        {
            return;
        }

        manipulators.Add(manipulator);
        TexCore_03NoScalePolicy.Enforce(manipulator);
        manipulator.lastSelectExited.AddListener(Released);
    }

    private static int Suffix(string name, string prefix)
    {
        if (string.IsNullOrEmpty(name) || !name.StartsWith(prefix, StringComparison.Ordinal)) return -1;
        Match match = Regex.Match(name.Substring(prefix.Length), "^(\\d+)$");
        return match.Success && int.TryParse(match.Value, out int value) ? value : -1;
    }

    private static Transform FindChild(Transform root, params string[] paths)
    {
        // Keep path lookup local to each runtime step so the bootstrap can
        // rebuild bindings after the legacy hierarchy finishes loading.
        if (root == null) return null;
        foreach (string path in paths)
        {
            if (!string.IsNullOrEmpty(path))
            {
                Transform result = root.Find(path.Trim());
                if (result != null) return result;
            }
        }
        return null;
    }

    private void SetManipulation(bool enabled)
    {
        foreach (ObjectManipulator manipulator in manipulators)
        {
            if (manipulator != null) manipulator.enabled = enabled && !completed;
        }
    }

    private void SetVisible(bool enabled)
    {
        if (interactionSource != null) interactionSource.SetActive(enabled);
    }

    private void CaptureStartPose()
    {
        if (firstMoDouEnd == null || secondMoDouEnd == null)
        {
            startPoseCaptured = false;
            return;
        }

        startFirstPosition = firstMoDouEnd.position;
        startFirstRotation = firstMoDouEnd.rotation;
        startSecondPosition = secondMoDouEnd.position;
        startSecondRotation = secondMoDouEnd.rotation;
        startPoseCaptured = true;
    }

    private void ResetEndpointPose()
    {
        if (!startPoseCaptured || firstMoDouEnd == null || secondMoDouEnd == null)
        {
            return;
        }

        firstMoDouEnd.SetPositionAndRotation(startFirstPosition, startFirstRotation);
        secondMoDouEnd.SetPositionAndRotation(startSecondPosition, startSecondRotation);
    }

    private static Transform FindAncestor(Transform source, string name)
    {
        for (Transform current = source; current != null; current = current.parent)
        {
            if (string.Equals(current.name, name, StringComparison.Ordinal))
            {
                return current;
            }
        }

        return null;
    }

    private static void ActivateRelevantPath(Transform target, Transform stopAt)
    {
        for (Transform current = target; current != null; current = current.parent)
        {
            current.gameObject.SetActive(true);
            if (stopAt != null && current == stopAt)
            {
                // Keep walking to the scene root.  The legacy 05 hierarchy is
                // itself disabled, so stopping at HuaXian leaves every child
                // inactiveInHierarchy and therefore non-interactable.
            }
        }
    }
}

[DisallowMultipleComponent]
public sealed class TexCore_M05QrPasteStep : TexCore_InteracterBase, ITexCoreUserCompletionStatus, ITexCoreM05StepStatus
{
    private const string SummonQrAction = "二维码";
    private const int ExpectedPairCount = 25;
    private const int TargetGuideSegments = 32;
    private const float TargetGuideRadius = 0.12f;

    private sealed class Pair
    {
        public int id;
        public GameObject interactiveQr;
        public GameObject placedQr;
        public Transform target;
        public ObjectManipulator manipulator;
        public Collider[] interactiveColliders;
        public Rigidbody interactiveBody;
        public bool placed;
        public bool triggered;
        public LineRenderer guide;
        public Renderer[] targetRenderers;
        public bool[] targetRendererStates;
        public bool warnedTargetRenderer;
    }

    private const float PressSeconds = 1.5f;

    [SerializeField, Min(0.001f)] private float positionTolerance = 0.25f;
    [SerializeField, Range(0f, 180f)] private float rotationTolerance = 35f;
    [SerializeField] private UnityEvent onPairPlaced;
    [SerializeField] private UnityEvent onIncorrectPlacement;
    [SerializeField] private UnityEvent onAllPairsCompleted;

    private readonly List<Pair> pairs = new List<Pair>();
    private Transform qrContainer;
    private Transform targetContainer;
    private Transform visibilityRoot;
    private TexCore_M05DrawStep drawStep;
    private bool stepActive;
    private bool completed;
    private bool userCompleted;
    private bool qrSummoned;
    private bool pairsValid;
    private int lastCompletedId;
    private float lastCompletedTime = -1f;
    private Pair pressing;
    private float pressTimer;
    private const float CompletionHintDuration = 2.5f;
    private Material targetGuideMaterial;
    private bool targetGuidesCreated;
    private static readonly Color TargetGuideColor = new Color(1f, 0.85f, 0.35f, 0.4f);
    private static readonly Color TargetReadyColor = new Color(0.15f, 0.9f, 0.25f, 1f);

    /// <summary>Index of the pair the player is currently on, or -1 when done.</summary>
    private int CurrentIndex
    {
        get
        {
            for (int i = 0; i < pairs.Count; i++)
            {
                if (!pairs[i].placed)
                {
                    return i;
                }
            }

            return -1;
        }
    }

    public bool WasCompletedByUser => userCompleted;
    public int PlacedCount => pairs.Count(value => value.placed);
    public int PairCount => pairs.Count;
    public string RequiredBackpackAction => qrSummoned ? string.Empty : SummonQrAction;
    public string BackpackHint => PhaseHint;
    public string PhaseName => !qrSummoned ? "召唤二维码" : completed ? "贴码完成" : "粘贴二维码";
    public bool IsCompleted => completed;
    public bool FocusesBackpackButton => !qrSummoned;

    /// <summary>
    /// Full QR status for the panel: which code is current, what has to happen
    /// to it, and a short confirmation of the pair that was just swapped in.
    /// </summary>
    public string PhaseHint
    {
        get
        {
            if (!pairsValid)
            {
                return "二维码配对不完整：需要 25 组 ErWeiMaN / 预放二维码 / ZhanTieDianN。";
            }

            if (!qrSummoned)
            {
                return $"点击背包中的“二维码”召唤第 1 张二维码（共 {pairs.Count} 张）。";
            }

            if (lastCompletedTime > 0f && Time.unscaledTime - lastCompletedTime < CompletionHintDuration)
            {
                return $"已贴附：{PlacedCount}/{pairs.Count}\n已激活：预放二维码 {lastCompletedId}\n请操作下一个二维码";
            }

            int index = CurrentIndex;
            if (index < 0)
            {
                return $"已贴附：{PlacedCount}/{pairs.Count}，全部完成。";
            }

            return $"当前二维码：第 {pairs[index].id}/{pairs.Count} 个\n目标状态：请扶正 Y 角度并靠近高亮目标点\n已贴附：{PlacedCount}/{pairs.Count}";
        }
    }

    public void Configure(Transform qrRoot, Transform targetRoot)
    {
        qrContainer = qrRoot;
        targetContainer = targetRoot;
        visibilityRoot = FindAncestor(qrRoot, "HuaXian");
        BuildPairs();
    }

    /// <summary>
    /// Owns the black JiZhun copy during the QR phase: it stays visible while
    /// the QR pairs are being pasted and disappears on module reset or when
    /// every pair is placed.
    /// </summary>
    public void BindDrawStep(TexCore_M05DrawStep step)
    {
        drawStep = step;
    }

    public override void UnFinished()
    {
        stepActive = false;
        completed = false;
        userCompleted = false;
        qrSummoned = false;
        ResetPairs();
        SetManipulation(false);
        SetPairsVisible(false);
        RestoreTargetRenderers();
        ClearTargetGuides();
        if (drawStep != null) drawStep.DestroyJiZhunClone();
        TexCore_05BackpackController.HideFor(this);
    }

    public override void UnderWay()
    {
        stepActive = true;
        completed = false;
        userCompleted = false;
        qrSummoned = false;
        ResetPairs();
        ActivateRelevantPath(qrContainer, visibilityRoot);
        ActivateRelevantPath(targetContainer, visibilityRoot);
        SetManipulation(false);
        SetPairsVisible(false);
        // The ZhanTieDian meshes are never the marker: the runtime rings read
        // their transform instead, and the meshes stay off for the whole phase.
        EnsureTargetGuides();
        SuppressTargetRenderers(false);
        // The black JiZhun copy from the draw step keeps assisting the player
        // through the whole QR phase.
        if (drawStep != null) drawStep.EnsureJiZhunCloneVisible();
        TexCore_05BackpackController.Present(this);
    }

    public override void OnTrigger() => FinishState(false);
    public override void Finish() => FinishState(false);

    private void FinishState(bool byUser)
    {
        completed = true;
        stepActive = false;
        userCompleted |= byUser;
        SetManipulation(false);
        RestoreTargetRenderers();
        ClearTargetGuides();
        // All 25 pairs are placed (or the topic was force-finished), so the
        // black datum-plane copy is no longer needed and must not linger
        // over the finished grid.
        if (drawStep != null) drawStep.DestroyJiZhunClone();
        TexCore_05BackpackController.HideFor(this);
    }

    /// <summary>
    /// Entry point for the final M05 backpack button.  It exposes the existing
    /// 25 physical QR pairs; completion still requires every pair to pass the
    /// position and rotation tolerances in <see cref="Released"/>.
    /// </summary>
    public void UseBackpackAction(string action)
    {
        if (!stepActive || completed || qrSummoned ||
            !string.Equals(action, SummonQrAction, StringComparison.Ordinal))
        {
            return;
        }

        qrSummoned = true;
        SetPairsVisible(true);
        SetManipulation(true);
        SuppressTargetRenderers(true);
        TexCore_05BackpackController.RefreshFor(this);
    }

    private new void OnEnable()
    {
        base.OnEnable();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        RestoreTargetRenderers();
        ClearTargetGuides();
        base.OnDisabel();
    }

    /// <summary>
    /// Builds the 25 explicit triples the QR phase works on: the pick-up code
    /// that carries a collider and a manipulator, the pre-placed code for the
    /// same number that carries neither, and the ZhanTieDian that only supplies
    /// a position, a rotation and the number. Anything less than a complete set
    /// is reported and blocks the interaction instead of being substituted.
    /// </summary>
    private void BuildPairs()
    {
        Unsubscribe();
        pairs.Clear();
        pairsValid = false;
        if (qrContainer == null || targetContainer == null) return;

        Dictionary<int, Transform> targets = new Dictionary<int, Transform>();
        foreach (Transform target in targetContainer.GetComponentsInChildren<Transform>(true))
        {
            int id = Suffix(target.name, "ZhanTieDian");
            if (id > 0 && !targets.ContainsKey(id)) targets.Add(id, target);
        }

        Dictionary<int, GameObject> interactive = new Dictionary<int, GameObject>();
        Dictionary<int, GameObject> placed = new Dictionary<int, GameObject>();
        foreach (Transform qr in qrContainer.GetComponentsInChildren<Transform>(true))
        {
            int id = Suffix(qr.name, "ErWeiMa");
            if (id <= 0) continue;
            ObjectManipulator manipulator = qr.GetComponent<ObjectManipulator>() ??
                                            qr.GetComponentInChildren<ObjectManipulator>(true);
            bool grabbable = qr.GetComponentInChildren<Collider>(true) != null && manipulator != null;
            Dictionary<int, GameObject> bucket = grabbable ? interactive : placed;
            if (!bucket.ContainsKey(id) || !bucket[id].activeSelf) bucket[id] = qr.gameObject;
        }

        for (int id = 1; id <= ExpectedPairCount; id++)
        {
            if (!targets.TryGetValue(id, out Transform target) ||
                !interactive.TryGetValue(id, out GameObject interactiveQr) ||
                !placed.TryGetValue(id, out GameObject placedQr))
            {
                BpfLog.Error(BpfLogCategory.XR, "tex_core_m05_qr_paste.build_pairs",
                    $"QR pair {id} is incomplete: target={targets.ContainsKey(id)} interactive={interactive.ContainsKey(id)} placed={placed.ContainsKey(id)}. The QR interaction stays disabled.",
                    this);
                return;
            }

            ObjectManipulator manipulator = interactiveQr.GetComponent<ObjectManipulator>() ??
                                            interactiveQr.GetComponentInChildren<ObjectManipulator>(true);
            TexCore_03NoScalePolicy.Enforce(interactiveQr);
            Pair entry = new Pair
            {
                id = id,
                interactiveQr = interactiveQr,
                placedQr = placedQr,
                target = target,
                manipulator = manipulator,
                interactiveColliders = interactiveQr.GetComponentsInChildren<Collider>(true),
                interactiveBody = interactiveQr.GetComponent<Rigidbody>() ??
                                  interactiveQr.GetComponentInChildren<Rigidbody>(true)
            };
            CacheTargetRenderers(entry);
            pairs.Add(entry);
        }

        pairsValid = true;
        Subscribe();
    }

    private void Subscribe()
    {
        foreach (Pair pair in pairs)
        {
            if (pair.manipulator == null) continue;
            pair.manipulator.lastSelectExited.RemoveListener(Released);
            pair.manipulator.lastSelectExited.AddListener(Released);
        }
    }

    private void Unsubscribe()
    {
        foreach (Pair pair in pairs)
        {
            if (pair.manipulator != null) pair.manipulator.lastSelectExited.RemoveListener(Released);
        }
    }

    private void Released(SelectExitEventArgs args)
    {
        // Completion is proximity driven, so a release only records a miss and
        // clears the selection; it can never be the success entry point.
        Pair pair = pairs.FirstOrDefault(value =>
            value.manipulator != null && value.manipulator.gameObject == args.interactableObject.transform.gameObject);
        if (!stepActive || completed || pair == null || pair.placed || pair.triggered)
        {
            return;
        }

        if (!IsAligned(pair))
        {
            onIncorrectPlacement?.Invoke();
        }

        SuppressTargetRenderers(true);
    }

    private void ResetPairs()
    {
        TexCore_FocusHighlight.ClearFor(this);
        foreach (Pair pair in pairs)
        {
            pair.placed = false;
            pair.triggered = false;
            if (pair.placedQr != null) pair.placedQr.SetActive(false);
            if (pair.interactiveQr != null) pair.interactiveQr.SetActive(false);
            if (pair.guide != null) pair.guide.enabled = false;

            foreach (Collider collider in pair.interactiveColliders)
            {
                if (collider != null) collider.enabled = true;
            }

            if (pair.manipulator != null) pair.manipulator.enabled = false;

            // Keep the pick-up code exactly where the scene put it: a dynamic
            // body with a solid grab box depenetrates against the floor and
            // shoots away, which is why held props stay kinematic here.
            TexCore_BackpackPhysics.Stabilize(pair.interactiveQr);
        }
    }

    private void SetManipulation(bool enabled)
    {
        Pair current = CurrentPair;
        foreach (Pair pair in pairs)
        {
            bool isCurrent = enabled && !pair.placed && !pair.triggered && pair == current;
            if (pair.manipulator != null) pair.manipulator.enabled = isCurrent && pairsValid;
            if (pair.interactiveQr != null) pair.interactiveQr.SetActive(isCurrent && pairsValid);
            if (isCurrent && pairsValid && pair.interactiveQr != null)
            {
                // A freshly exposed code must never be simulated: gravity and
                // depenetration are what made it drift away on its own.
                TexCore_BackpackPhysics.Stabilize(pair.interactiveQr);
            }
        }

        // The ZhanTieDian objects are logical targets only; their own meshes must
        // never become the visual again.
        SuppressTargetRenderers(false);
    }

    private void SetPairsVisible(bool visible)
    {
        // Only the current pick-up code is ever exposed; the other 24 and every
        // pre-placed code stay hidden until their turn.
        foreach (Pair pair in pairs)
        {
            if (pair.interactiveQr != null)
            {
                bool isCurrent = visible && !pair.placed && !pair.triggered && pair == CurrentPair;
                pair.interactiveQr.SetActive(isCurrent && pairsValid);
                if (isCurrent)
                {
                    TexCore_BackpackPhysics.Stabilize(pair.interactiveQr);
                }
            }

            if (pair.placedQr != null)
            {
                pair.placedQr.SetActive(false);
            }
        }

        SetTargetGuidesVisible(visible);
        SuppressTargetRenderers(false);
    }

    private Pair CurrentPair
    {
        get
        {
            int index = CurrentIndex;
            return index >= 0 ? pairs[index] : null;
        }
    }

    /// <summary>
    /// Completes the current pair as soon as the held code is close enough and
    /// square to the target, without waiting for the release. The pick-up code
    /// is only hidden, never destroyed, so a reset restores it with no rebuild.
    /// </summary>
    private void CompletePair(Pair pair)
    {
        if (pair == null || pair.placed || pair.triggered)
        {
            return;
        }

        pair.triggered = true;
        if (pair.manipulator != null) pair.manipulator.enabled = false;
        foreach (Collider collider in pair.interactiveColliders)
        {
            if (collider != null) collider.enabled = false;
        }

        if (pair.interactiveBody != null)
        {
            pair.interactiveBody.velocity = Vector3.zero;
            pair.interactiveBody.angularVelocity = Vector3.zero;
            pair.interactiveBody.isKinematic = true;
        }

        if (pair.interactiveQr != null) pair.interactiveQr.SetActive(false);
        if (pair.placedQr != null)
        {
            pair.placedQr.transform.SetPositionAndRotation(pair.target.position, pair.target.rotation);
            pair.placedQr.SetActive(true);
            DisableInteraction(pair.placedQr);
        }

        if (pair.guide != null) pair.guide.enabled = false;
        pair.placed = true;
        pair.triggered = false;
        lastCompletedId = pair.id;
        lastCompletedTime = Time.unscaledTime;
        SuppressTargetRenderers(true);
        TexCore_05BackpackController.RefreshFor(this);

        if (PlacedCount == pairs.Count && pairs.Count > 0)
        {
            userCompleted = true;
            FinishState(true);
            onAllPairsCompleted?.Invoke();
            TexCore_M05Progression.CompleteTopic(gameObject);
            return;
        }

        // Expose the next pick-up code right away.
        SetManipulation(true);
        onPairPlaced?.Invoke();
    }

    /// <summary>A pre-placed code is scenery: it must never be grabbable again.</summary>
    private static void DisableInteraction(GameObject value)
    {
        foreach (Collider collider in value.GetComponentsInChildren<Collider>(true))
        {
            if (collider != null) collider.enabled = false;
        }

        foreach (ObjectManipulator manipulator in value.GetComponentsInChildren<ObjectManipulator>(true))
        {
            if (manipulator != null) manipulator.enabled = false;
        }

        foreach (Rigidbody body in value.GetComponentsInChildren<Rigidbody>(true))
        {
            if (body == null) continue;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
        }
    }

    /// <summary>
    /// Angle between the two headings around the vertical axis only, so tilting
    /// the code in X or Z never counts as aligned.
    /// </summary>
    private static float YawDelta(Quaternion a, Quaternion b)
    {
        Vector3 forwardA = Vector3.ProjectOnPlane(a * Vector3.forward, Vector3.up);
        Vector3 forwardB = Vector3.ProjectOnPlane(b * Vector3.forward, Vector3.up);
        if (forwardA.sqrMagnitude < 0.0001f || forwardB.sqrMagnitude < 0.0001f)
        {
            return 0f;
        }

        return Vector3.Angle(forwardA, forwardB);
    }

    private bool IsAligned(Pair pair)
    {
        if (pair.interactiveQr == null || pair.target == null)
        {
            return false;
        }

        if (Vector3.Distance(pair.interactiveQr.transform.position, pair.target.position) > positionTolerance)
        {
            return false;
        }

        return YawDelta(pair.interactiveQr.transform.rotation, pair.target.rotation) <= rotationTolerance;
    }

    /// <summary>
    /// Records the renderer state each target starts with, so the QR phase can
    /// keep the meshes off and hand the scene back untouched afterwards.
    /// </summary>
    private static void CacheTargetRenderers(Pair pair)
    {
        Renderer[] renderers = pair.target.GetComponentsInChildren<Renderer>(true);
        pair.targetRenderers = renderers;
        pair.targetRendererStates = new bool[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            pair.targetRendererStates[i] = renderers[i] != null && renderers[i].enabled;
        }
    }

    /// <summary>
    /// Switches every target mesh off. The target objects stay in the hierarchy
    /// because the QR snapping reads their transform; only the renderers go.
    /// When <paramref name="reportUnexpected"/> is set, a renderer that some
    /// other script switched back on is reported once per target instead of
    /// every frame.
    /// </summary>
    private void SuppressTargetRenderers(bool reportUnexpected)
    {
        foreach (Pair pair in pairs)
        {
            if (pair.targetRenderers == null)
            {
                continue;
            }

            foreach (Renderer renderer in pair.targetRenderers)
            {
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                renderer.enabled = false;
                if (!reportUnexpected || pair.warnedTargetRenderer)
                {
                    continue;
                }

                pair.warnedTargetRenderer = true;
                BpfLog.Warning(BpfLogCategory.XR, "tex_core_m05_qr_paste.target_renderer",
                    $"Target '{pair.target.name}' renderer '{renderer.name}' was switched on during the QR phase and has been switched off again.",
                    renderer);
            }
        }
    }

    private void RestoreTargetRenderers()
    {
        foreach (Pair pair in pairs)
        {
            if (pair.targetRenderers == null || pair.targetRendererStates == null)
            {
                continue;
            }

            for (int i = 0; i < pair.targetRenderers.Length && i < pair.targetRendererStates.Length; i++)
            {
                if (pair.targetRenderers[i] != null)
                {
                    pair.targetRenderers[i].enabled = pair.targetRendererStates[i];
                }
            }

            pair.warnedTargetRenderer = false;
        }
    }

    /// <summary>
    /// Builds one flat ring per target from the target's own position and
    /// rotation. The rings carry no collider and no manipulator, so they can
    /// never be grabbed and never show the original target mesh.
    /// </summary>
    private void EnsureTargetGuides()
    {
        if (targetGuidesCreated || visibilityRoot == null)
        {
            return;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            BpfLog.Warning(BpfLogCategory.XR, "tex_core_m05_qr_paste.target_guide",
                "Cannot build the QR target guides because Sprites/Default is unavailable.", this);
            return;
        }

        targetGuidesCreated = true;
        targetGuideMaterial = new Material(shader)
        {
            name = "M05_ZhanTieDian_指引材质",
            hideFlags = HideFlags.DontSave
        };

        foreach (Pair pair in pairs)
        {
            GameObject guideObject = new GameObject($"M05_贴附指引_{pair.target.name}")
            {
                hideFlags = HideFlags.DontSave,
                layer = pair.target.gameObject.layer
            };
            guideObject.transform.SetParent(visibilityRoot, true);
            guideObject.transform.SetPositionAndRotation(pair.target.position, pair.target.rotation);

            LineRenderer line = guideObject.AddComponent<LineRenderer>();
            line.sharedMaterial = targetGuideMaterial;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = TargetGuideSegments;
            line.widthMultiplier = 0.006f;
            line.alignment = LineAlignment.View;
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            for (int i = 0; i < TargetGuideSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / TargetGuideSegments;
                line.SetPosition(i, new Vector3(
                    Mathf.Cos(angle) * TargetGuideRadius, 0f, Mathf.Sin(angle) * TargetGuideRadius));
            }

            SetTargetGuideColor(line, TargetGuideColor);
            line.enabled = false;
            pair.guide = line;
        }
    }

    private static void SetTargetGuideColor(LineRenderer line, Color color)
    {
        if (line == null)
        {
            return;
        }

        line.startColor = color;
        line.endColor = color;
    }

    private void SetTargetGuidesVisible(bool visible)
    {
        EnsureTargetGuides();
        Pair current = CurrentPair;
        foreach (Pair pair in pairs)
        {
            if (pair.guide != null)
            {
                pair.guide.enabled = visible && pairsValid && pair == current;
            }
        }
    }

    /// <summary>
    /// Only the current target is drawn. It turns green while the held code is
    /// square to it, and the shared focus points at it with an arrow from the
    /// held code so the player can see the correction still needed.
    /// </summary>
    private void UpdateTargetGuides()
    {
        if (!stepActive || completed || !qrSummoned || !pairsValid)
        {
            return;
        }

        Pair current = CurrentPair;
        if (current == null || current.guide == null)
        {
            TexCore_FocusHighlight.ClearFor(this);
            return;
        }

        bool aligned = current.manipulator != null && current.manipulator.isSelected && IsAligned(current);
        current.guide.enabled = true;
        SetTargetGuideColor(current.guide, aligned ? TargetReadyColor : TargetGuideColor);
        TexCore_FocusHighlight.Show(
            current.guide.gameObject,
            aligned ? "可贴附" : "请扶正 Y 角度并靠近目标点",
            aligned ? TargetReadyColor : TexCore_FocusHighlight.GuideColor,
            aligned ? current.interactiveQr : null,
            null,
            this);
    }

    private void ClearTargetGuides()
    {
        TexCore_FocusHighlight.ClearFor(this);
        foreach (Pair pair in pairs)
        {
            if (pair.guide != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(pair.guide.gameObject);
                }
                else
                {
                    DestroyImmediate(pair.guide.gameObject);
                }

                pair.guide = null;
            }
        }

        if (targetGuideMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(targetGuideMaterial);
            }
            else
            {
                DestroyImmediate(targetGuideMaterial);
            }

            targetGuideMaterial = null;
        }

        targetGuidesCreated = false;
    }

    private void Update()
    {
        if (!stepActive || completed)
        {
            return;
        }

        SuppressTargetRenderers(true);
        UpdateTargetGuides();
        UpdatePressState();
    }

    /// <summary>
    /// Pasting has two beats, matching the legacy rub-flat step: the code has to
    /// be held square to its target, and the hand then has to stay put while it
    /// is pressed flat. Only a finished press swaps the pair.
    /// </summary>
    private void UpdatePressState()
    {
        if (!qrSummoned || !pairsValid)
        {
            return;
        }

        Pair current = CurrentPair;
        bool aligned = current != null && current.manipulator != null &&
                       current.manipulator.isSelected && IsAligned(current);
        if (!aligned)
        {
            // A hand-held code wobbles. Decay the press instead of wiping it, so
            // a brief slip does not throw away the whole hold.
            if (pressing != null)
            {
                pressTimer -= Time.deltaTime * 2f;
                if (pressTimer <= 0f)
                {
                    pressing = null;
                    pressTimer = 0f;
                }
            }

            return;
        }

        if (pressing != current)
        {
            pressing = current;
            pressTimer = 0f;
            ShowPopup($"已对准，请把手停在这里压平 {PressSeconds:0.0} 秒。");
        }

        pressTimer += Time.deltaTime;
        TexCore_EventCenter.Broadcast<string>(
            TexCore_EventType.显示标题,
            $"正在压平二维码：{Mathf.RoundToInt(Mathf.Clamp01(pressTimer / PressSeconds) * 100f)}%");
        if (pressTimer < PressSeconds)
        {
            return;
        }

        pressing = null;
        pressTimer = 0f;
        CompletePair(current);
        ShowPopup($"已贴附：{PlacedCount}/{pairs.Count}");
    }

    private static void ShowPopup(string text)
    {
        TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, text, 1.5f);
    }

    private static int Suffix(string name, string prefix)
    {
        if (string.IsNullOrEmpty(name) || !name.StartsWith(prefix, StringComparison.Ordinal)) return -1;
        Match match = Regex.Match(name.Substring(prefix.Length), "^(\\d+)$");
        return match.Success && int.TryParse(match.Value, out int value) ? value : -1;
    }

    private static Transform FindAncestor(Transform source, string name)
    {
        for (Transform current = source; current != null; current = current.parent)
        {
            if (string.Equals(current.name, name, StringComparison.Ordinal))
            {
                return current;
            }
        }

        return null;
    }

    private static void ActivateRelevantPath(Transform target, Transform stopAt)
    {
        for (Transform current = target; current != null; current = current.parent)
        {
            current.gameObject.SetActive(true);
        }
    }
}

[DisallowMultipleComponent]
public sealed class TexCore_M05CompletionGateStep : TexCore_InteracterBase, ITexCoreUserCompletionStatus
{
    private TexCore_InteracterBase draw;
    private TexCore_InteracterBase qr;
    private bool stepActive;
    private bool completed;
    private bool userCompleted;

    public bool WasCompletedByUser => userCompleted;

    public void Configure(TexCore_InteracterBase draw, TexCore_InteracterBase qr)
    {
        this.draw = draw;
        this.qr = qr;
    }

    public override void UnFinished()
    {
        stepActive = false;
        completed = false;
        userCompleted = false;
    }

    public override void UnderWay()
    {
        stepActive = true;
        completed = false;
        userCompleted = false;
        TryComplete();
    }

    public override void OnTrigger()
    {
        completed = true;
        stepActive = false;
    }

    public override void Finish()
    {
        completed = true;
        stepActive = false;
    }

    public void TryComplete()
    {
        if (!stepActive || completed || draw == null || qr == null)
        {
            return;
        }

        bool drawComplete = draw is ITexCoreUserCompletionStatus drawStatus
            ? drawStatus.WasCompletedByUser
            : draw.state == FinishState.触发 || draw.state == FinishState.已完成;
        bool qrComplete = qr is ITexCoreUserCompletionStatus qrStatus
            ? qrStatus.WasCompletedByUser
            : qr.state == FinishState.触发 || qr.state == FinishState.已完成;
        if (!drawComplete || !qrComplete)
        {
            return;
        }

        completed = true;
        userCompleted = true;
        stepActive = false;
        TexCore_M05Progression.CompleteTopic(gameObject);
    }
}

/// <summary>
/// Bridges a physical M05 completion into the existing Panel_Operate flow.
/// The generated topic still exposes the legacy task "确定"; invoking the
/// active panel keeps scoring, topic advancement and ExamRecord handling in
/// the framework instead of bypassing it with a synthetic click on HuaXian.
/// </summary>
internal static class TexCore_M05Progression
{
    public static void CompleteTopic(GameObject source)
    {
        Panel_Operate[] panels = UnityEngine.Object.FindObjectsOfType<Panel_Operate>(true);
        foreach (Panel_Operate panel in panels)
        {
            if (panel == null || !panel.isActiveAndEnabled || panel.topicData == null ||
                panel.topicData.tasks == null || panel.stepIndex >= panel.topicData.tasks.Count ||
                !panel.topicData.tasks[panel.stepIndex].Equals("确定", StringComparison.Ordinal))
            {
                continue;
            }

            GameObject confirm = new GameObject("确定");
            panel.OnClick(confirm);
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(confirm);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(confirm);
            }
            return;
        }

        TexCore_InteracterBase interacter = source != null
            ? source.GetComponent<TexCore_InteracterBase>()
            : null;
        if (interacter != null && interacter.enterTopic != null)
        {
            TexCore_EventCenter.Broadcast(
                TexCore_EventType.触发物体,
                interacter.enterTopic.nodeIndex,
                interacter.enterTopic.topicIndex,
                interacter.enterTopic.enterStep);
        }
    }
}
