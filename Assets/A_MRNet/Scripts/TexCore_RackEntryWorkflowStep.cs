using System.Collections.Generic;
using System.Linq;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine;

/// <summary>
/// Single-step guidance state of the M10 rack-entry operation. The state only
/// drives highlighting and hint text; it never changes the underlying rule that
/// one rack is reused for all target points.
/// </summary>
public enum RackEntryGuidanceState
{
    None,
    SelectRack,
    MoveRackToEntryQr,
    AttachRack,
    MoveToTarget,
    Complete
}

/// <summary>
/// Current-project replacement for the legacy M10 rack-entry workflow.
/// A single movable rack is reused for each target point: the player first puts
/// it on the M10 entry QR code, then drives the AGV underneath it, then moves
/// the AGV/rack pair to the next target. The operation is reported to TexCore
/// only after every target point has been completed.
/// </summary>
[DisallowMultipleComponent]
public sealed class TexCore_RackEntryWorkflowStep : TexCore_InteracterBase,
    ITexCoreUserCompletionStatus,
    ITexCoreProgressStatus
{
    public const string GuideSelectRack = "请点击背包中的“货架”取出待搬运货架。";
    public const string GuideMoveRackToQr = "请将货架移动到二维码上方。";
    public const string GuideMoveCloser = "请再靠近高亮货架点一点。";

    private const string GrabColliderName = "GrabCollider";

    [SerializeField] private GameObject robot;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform entryQrAnchor;
    [SerializeField] private Collider entryQrArea;
    [SerializeField] private List<GameObject> targetPoints = new List<GameObject>();
    [SerializeField, Min(0f)] private float targetTolerance = 0.24f;
    [SerializeField, Min(0f)] private float entryQrTolerance = 0.24f;
    [SerializeField, Min(0f)] private float entryQrHeightTolerance = 0.08f;
    [SerializeField, Min(0f)] private float entryQrAngleTolerance = 15f;
    [SerializeField, Min(0f)] private float dockTolerance = 0.2f;
    [SerializeField, Min(0f)] private float robotMoveSpeed = 0.8f;
    [SerializeField] private bool showGuidance = true;
    [SerializeField] private RackEntryGuidanceState guidance;

    private readonly List<GameObject> guidanceSecondary = new List<GameObject>();
    private readonly List<GameObject> placedRacks = new List<GameObject>();
    private readonly List<Vector3> route = new List<Vector3>();
    private int routeIndex;
    private Vector3 routeDestination;
    private bool routeReady;

    private Rigidbody body;
    private ObjectManipulator manipulator;
    private Collider grabCollider;
    private RotationAxisConstraint rotationConstraint;
    private Transform originalParent;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 startScale;
    private bool startPoseCaptured;
    private float rackBaseOffset;
    private bool rackBaseOffsetCached;
    private float rackCarryHeight;
    private RigidbodyConstraints originalConstraints;
    private bool constraintsCaptured;
    private bool rackOnEntryQr;
    private int targetIndex;
    private bool stepActive;
    private bool completed;
    private bool userCompleted;
    private bool attachedToRobot;

    public bool WasCompletedByUser => userCompleted;

    public GameObject Robot => robot;

    public IReadOnlyList<GameObject> TargetPoints => targetPoints;

    public int CompletedTargetCount => targetIndex;

    public bool IsAttachedToRobot => attachedToRobot;

    public RackEntryGuidanceState GuidanceState => guidance;

    /// <summary>
    /// Binds the single entry QR code the rack has to be placed on before the
    /// AGV is allowed to dock under it.
    /// </summary>
    public void SetEntryQr(Transform anchor, Collider area)
    {
        entryQrAnchor = anchor;
        entryQrArea = area;
    }

    public bool IsRackOnEntryQrNow => rackOnEntryQr;

    public int CompletedCount => targetIndex;

    public int RequiredCount => targetPoints.Count;

    public GameObject CurrentTarget =>
        targetIndex >= 0 && targetIndex < targetPoints.Count ? targetPoints[targetIndex] : null;

    public void Configure(GameObject robotObject, IEnumerable<GameObject> targets,
        Transform spawn, float tolerance)
    {
        robot = robotObject;
        spawnPoint = spawn;
        targetPoints = targets == null
            ? new List<GameObject>()
            : targets.Where(value => value != null).Distinct().ToList();
        targetTolerance = Mathf.Max(0f, tolerance);
        CacheSource();
        CaptureStartPose();
    }

    private void Awake()
    {
        CacheSource();
        CaptureStartPose();
    }

    public new void OnEnable()
    {
        base.OnEnable();
        CacheSource();
        // OnEnable also fires every time the rack is taken out of the backpack,
        // so the pose is only captured while it is still trusted.
        if (!startPoseCaptured)
        {
            CaptureStartPose();
        }
        SetManipulationEnabled(false);
    }

    private void OnDisable()
    {
        SetManipulationEnabled(false);
        // The rack is also deactivated while it is withheld in the backpack, so
        // only the visuals are released here; the guidance state is kept.
        TexCore_FocusHighlight.ClearFor(this);
        base.OnDisabel();
    }

    public override void UnFinished()
    {
        stepActive = false;
        completed = false;
        userCompleted = false;
        targetIndex = 0;
        attachedToRobot = false;
        rackOnEntryQr = false;
        DetachFromRobot();
        ClearPlacedRacks();
        ResetTargetPoints();
        ResetRackToSpawn();
        SetManipulationEnabled(false);
        TexCore_10BackpackController.HideFor(this);
        SetGuidanceState(RackEntryGuidanceState.None);
    }

    public override void UnderWay()
    {
        stepActive = true;
        completed = false;
        userCompleted = false;
        targetIndex = Mathf.Clamp(targetIndex, 0, targetPoints.Count);
        SetTargetPointsActive();
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        SetManipulationEnabled(true);
        TexCore_10BackpackController.Present(this);
        SetGuidanceState(RackEntryGuidanceState.SelectRack);
    }

    public override void OnTrigger()
    {
        completed = true;
        stepActive = false;
        SetManipulationEnabled(false);
        TexCore_10BackpackController.HideFor(this);
        SetGuidanceState(RackEntryGuidanceState.Complete);
    }

    public override void Finish()
    {
        completed = true;
        stepActive = false;
        SetManipulationEnabled(false);
        TexCore_10BackpackController.HideFor(this);
        SetGuidanceState(RackEntryGuidanceState.Complete);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!stepActive || completed || other == null)
        {
            return;
        }

        Transform otherTransform = other.transform;
        if (!attachedToRobot)
        {
            TryDockToRobot(otherTransform);
            return;
        }

        if (IsCurrentTarget(otherTransform))
        {
            CompleteCurrentTarget(otherTransform.gameObject);
        }
    }

    /// <summary>
    /// Docking needs a continuous check: the robot first touches the rack bounds
    /// off centre and then drives the rest of the way in, which raises no second
    /// enter event.
    /// </summary>
    private void OnTriggerStay(Collider other)
    {
        if (!stepActive || completed || attachedToRobot || other == null)
        {
            return;
        }

        TryDockToRobot(other.transform);
    }

    private void TryDockToRobot(Transform otherTransform)
    {
        // The rack has to be sitting on the entry QR first. Without this gate the
        // player could skip the placement step entirely and dock wherever the
        // rack happened to be released.
        if (!rackOnEntryQr || !IsRobot(otherTransform) || !IsRobotUnderRack())
        {
            return;
        }

        AttachToRobot(otherTransform);
    }

    /// <summary>
    /// The robot docks by parking under the rack, not by touching it anywhere.
    /// </summary>
    private bool IsRobotUnderRack()
    {
        if (robot == null)
        {
            return false;
        }

        Vector3 rackPosition = transform.position;
        Vector3 robotPosition = robot.transform.position;
        float deltaX = robotPosition.x - rackPosition.x;
        float deltaZ = robotPosition.z - rackPosition.z;
        return deltaX * deltaX + deltaZ * deltaZ <= dockTolerance * dockTolerance;
    }

    /// <summary>
    /// The entry QR code is validated every frame instead of on a trigger: the
    /// player drags the rack by hand and may set it down anywhere.
    /// </summary>
    private void Update()
    {
        if (!stepActive || completed)
        {
            return;
        }

        if (guidance == RackEntryGuidanceState.MoveRackToEntryQr)
        {
            if (TrySettleRackOnEntryQr())
            {
                // Once the rack sits on the entry code it stays there: the rest of
                // the round is the robot's job.
                SetManipulationEnabled(false);
                SetGuidanceState(RackEntryGuidanceState.AttachRack);
            }

            return;
        }

        if (guidance == RackEntryGuidanceState.AttachRack && rackOnEntryQr && !IsRackOnEntryQr())
        {
            // Taking the rack back off the code has to revoke the placement, or
            // the gate would only ever be tested once.
            rackOnEntryQr = false;
            SetManipulationEnabled(true);
            SetGuidanceState(RackEntryGuidanceState.MoveRackToEntryQr);
            return;
        }

        if (guidance == RackEntryGuidanceState.MoveToTarget)
        {
            // The robot drives itself here, and a kinematic body moved by writing
            // the transform does not reliably raise trigger callbacks, so the
            // settlement is judged on the ground plane directly.
            GameObject target = CurrentTarget;
            if (target != null && IsRackWithinTarget(target))
            {
                CompleteCurrentTarget(target);
                return;
            }
        }

        UpdateAutoRobot();
    }

    /// <summary>
    /// A shelf is carried across the floor, never lifted. The rigidbody freeze is
    /// the primary lock, but the manipulator drives a kinematic body through
    /// MovePosition and that target is only applied at the next physics step, so
    /// the height is also corrected here before the frame is drawn.
    /// </summary>
    private void LateUpdate()
    {
        if (!stepActive || completed || guidance != RackEntryGuidanceState.MoveRackToEntryQr)
        {
            return;
        }

        Vector3 position = transform.position;
        if (Mathf.Abs(position.y - rackCarryHeight) > 0.0001f)
        {
            position.y = rackCarryHeight;
            transform.position = position;
        }
    }

    private bool IsRackWithinTarget(GameObject target)
    {
        if (target == null)
        {
            return false;
        }

        Vector3 delta = transform.position - target.transform.position;
        return delta.x * delta.x + delta.z * delta.z <= targetTolerance * targetTolerance;
    }

    /// <summary>
    /// The robot drives itself: under the rack once the rack waits on the entry
    /// code, then on to the highlighted target point. The player only ever moves
    /// the rack, never the robot.
    /// </summary>
    private void UpdateAutoRobot()
    {
        if (robot == null)
        {
            return;
        }

        Vector3 destination;
        if (guidance == RackEntryGuidanceState.AttachRack && rackOnEntryQr)
        {
            destination = transform.position;
        }
        else if (guidance == RackEntryGuidanceState.MoveToTarget)
        {
            GameObject target = CurrentTarget;
            if (target == null)
            {
                return;
            }

            destination = target.transform.position;
        }
        else
        {
            return;
        }

        EnsureRoute(destination);
        if (routeIndex >= route.Count)
        {
            return;
        }

        // Follow the lane waypoints one axis at a time, so the robot never cuts
        // across the rack grid and never enters a cell that already holds a rack.
        Vector3 waypoint = route[routeIndex];
        Vector3 robotPosition = robot.transform.position;
        robot.transform.position = GetAxisAlignedStep(robotPosition, waypoint,
            robotMoveSpeed * Time.deltaTime);

        Vector3 moved = robot.transform.position;
        if (new Vector2(moved.x - waypoint.x, moved.z - waypoint.z).sqrMagnitude < 0.000001f)
        {
            routeIndex++;
        }
    }

    private void EnsureRoute(Vector3 destination)
    {
        if (routeReady && (routeDestination - destination).sqrMagnitude <= 0.0001f)
        {
            return;
        }

        route.Clear();
        routeIndex = 0;
        routeDestination = destination;
        routeReady = true;
        BuildRoute(robot.transform.position, destination, route);
    }

    /// <summary>
    /// Lays a route over the rack grid: the columns and rows of the rack points are
    /// the lanes, and a cell that already holds a delivered rack cannot be driven
    /// through. A straight or single-corner drive would plough through the racks
    /// placed in earlier rounds.
    /// </summary>
    public List<Vector3> BuildRoute(Vector3 from, Vector3 to)
    {
        List<Vector3> result = new List<Vector3>();
        BuildRoute(from, to, result);
        return result;
    }

    private void BuildRoute(Vector3 from, Vector3 to, List<Vector3> result)
    {
        List<float> columns = new List<float>();
        List<float> rows = new List<float>();
        CollectAxis(targetPoints, true, columns);
        CollectAxis(targetPoints, false, rows);
        if (columns.Count == 0 || rows.Count == 0)
        {
            result.Add(to);
            return;
        }

        // The entry code sits in the front corridor, and that corridor is the lane
        // the robot drives in on, so it takes part in the grid as its own row.
        float corridor = entryQrAnchor != null ? entryQrAnchor.position.z : from.z;
        if (!Contains(rows, corridor))
        {
            rows.Add(corridor);
            rows.Sort();
        }

        int rowCount = rows.Count;
        bool[] blocked = new bool[columns.Count * rowCount];
        foreach (GameObject placed in placedRacks)
        {
            if (placed == null)
            {
                continue;
            }

            Vector3 position = placed.transform.position;
            blocked[NearestIndex(columns, position.x) * rowCount
                + NearestIndex(rows, position.z)] = true;
        }

        int startColumn = NearestIndex(columns, from.x);
        int startRow = NearestIndex(rows, from.z);
        int goalColumn = NearestIndex(columns, to.x);
        int goalRow = NearestIndex(rows, to.z);
        int start = startColumn * rowCount + startRow;
        int goal = goalColumn * rowCount + goalRow;
        int total = columns.Count * rowCount;

        int[] previous = new int[total];
        for (int i = 0; i < total; i++)
        {
            previous[i] = -2;
        }

        previous[start] = -1;
        Queue<int> pending = new Queue<int>();
        pending.Enqueue(start);
        while (pending.Count > 0 && previous[goal] == -2)
        {
            int current = pending.Dequeue();
            int column = current / rowCount;
            int row = current % rowCount;
            for (int direction = 0; direction < 4; direction++)
            {
                // Rows are sorted towards the entry corridor, and that corridor is
                // the main aisle: an equally short route steps out of the rack rows
                // first, and from the corridor it travels along it before turning in.
                int nextColumn = column + (direction == 1 ? -1 : direction == 2 ? 1 : 0);
                int nextRow = row + (direction == 0 ? 1 : direction == 3 ? -1 : 0);
                if (nextColumn < 0 || nextColumn >= columns.Count
                    || nextRow < 0 || nextRow >= rowCount)
                {
                    continue;
                }

                int next = nextColumn * rowCount + nextRow;
                if (previous[next] != -2 || blocked[next])
                {
                    continue;
                }

                previous[next] = current;
                pending.Enqueue(next);
            }
        }

        if (previous[goal] == -2)
        {
            // Every lane is taken: fall back to the direct approach rather than
            // leaving the robot stranded.
            result.Add(to);
            return;
        }

        List<int> nodes = new List<int>();
        for (int node = goal; node != -1; node = previous[node])
        {
            nodes.Add(node);
        }

        nodes.Reverse();
        foreach (int node in nodes)
        {
            result.Add(new Vector3(columns[node / rowCount], from.y, rows[node % rowCount]));
        }

        if (result.Count == 0
            || new Vector2(result[result.Count - 1].x - to.x, result[result.Count - 1].z - to.z)
                .sqrMagnitude > 0.000001f)
        {
            result.Add(to);
        }
    }

    private static void CollectAxis(List<GameObject> points, bool useX, List<float> result)
    {
        foreach (GameObject point in points)
        {
            if (point == null)
            {
                continue;
            }

            Vector3 position = point.transform.position;
            float value = useX ? position.x : position.z;
            if (!Contains(result, value))
            {
                result.Add(value);
            }
        }

        result.Sort();
    }

    private static bool Contains(List<float> values, float value)
    {
        foreach (float candidate in values)
        {
            if (Mathf.Abs(candidate - value) < 0.25f)
            {
                return true;
            }
        }

        return false;
    }

    private static int NearestIndex(List<float> values, float value)
    {
        int best = 0;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < values.Count; i++)
        {
            float distance = Mathf.Abs(values[i] - value);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        return best;
    }

    /// <summary>
    /// The robot drives on the rack-grid axes, so it closes the X gap first and
    /// the Z gap only afterwards. A straight line towards the goal would cut the
    /// corner between the racks, which is not a path an AGV can take.
    /// </summary>
    public static Vector3 GetAxisAlignedStep(Vector3 from, Vector3 destination, float maxDistance)
    {
        float deltaX = destination.x - from.x;
        float deltaZ = destination.z - from.z;
        if (Mathf.Abs(deltaX) > 0.001f)
        {
            return new Vector3(from.x + Mathf.Clamp(deltaX, -maxDistance, maxDistance),
                from.y, from.z);
        }

        return new Vector3(from.x, from.y,
            from.z + Mathf.Clamp(deltaZ, -maxDistance, maxDistance));
    }

    private bool TrySettleRackOnEntryQr()
    {
        if (rackOnEntryQr || !IsRackOnEntryQr())
        {
            return false;
        }

        transform.SetPositionAndRotation(GetEntryQrRackPose(), entryQrAnchor.rotation);
        if (body != null)
        {
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        rackOnEntryQr = true;
        return true;
    }

    /// <summary>
    /// A rack counts as placed on the entry QR only when it is centred on the
    /// code, resting at the code height and facing the same way, and only while
    /// it stays inside the dedicated area collider.
    /// </summary>
    private bool IsRackOnEntryQr()
    {
        if (entryQrAnchor == null || !IsInsideEntryQrArea(transform.position))
        {
            return false;
        }

        Vector3 anchor = entryQrAnchor.position;
        Vector3 rack = transform.position;
        float deltaX = rack.x - anchor.x;
        float deltaZ = rack.z - anchor.z;
        if (deltaX * deltaX + deltaZ * deltaZ > entryQrTolerance * entryQrTolerance)
        {
            return false;
        }

        float rackBottom = rack.y + GetRackBaseOffset();
        if (Mathf.Abs(rackBottom - anchor.y) > entryQrHeightTolerance)
        {
            return false;
        }

        Vector3 rackForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        Vector3 anchorForward = Vector3.ProjectOnPlane(entryQrAnchor.forward, Vector3.up);
        return rackForward.sqrMagnitude < 0.0001f || anchorForward.sqrMagnitude < 0.0001f ||
               Vector3.Angle(rackForward, anchorForward) <= entryQrAngleTolerance;
    }

    private bool IsInsideEntryQrArea(Vector3 rackPosition)
    {
        if (entryQrArea == null)
        {
            return true;
        }

        Bounds bounds = entryQrArea.bounds;
        return rackPosition.x >= bounds.min.x && rackPosition.x <= bounds.max.x &&
               rackPosition.z >= bounds.min.z && rackPosition.z <= bounds.max.z;
    }

    private Vector3 GetEntryQrRackPose()
    {
        Vector3 pose = entryQrAnchor.position;
        pose.y -= GetRackBaseOffset();
        return pose;
    }

    private bool IsRobot(Transform value)
    {
        if (robot == null)
        {
            return false;
        }

        for (Transform current = value; current != null; current = current.parent)
        {
            if (current == robot.transform || current.CompareTag("jiqiren"))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsCurrentTarget(Transform value)
    {
        if (targetIndex < 0 || targetIndex >= targetPoints.Count)
        {
            return false;
        }

        GameObject target = targetPoints[targetIndex];
        if (target == null || !target.activeInHierarchy)
        {
            return false;
        }

        Transform targetTransform = target.transform;
        return value == targetTransform || value.IsChildOf(targetTransform) ||
            targetTransform.IsChildOf(value);
    }

    private void AttachToRobot(Transform robotTransform)
    {
        if (robot == null)
        {
            return;
        }

        originalParent = transform.parent;
        transform.SetParent(robot.transform, true);
        attachedToRobot = true;
        PlaceRackOnRobot();
        if (body != null)
        {
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        if (showGuidance)
        {
            TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示,
                "货架已接驳机器人，请将机器人移动到高亮货架点。", 1.5f);
        }

        SetGuidanceState(RackEntryGuidanceState.MoveToTarget);
    }

    /// <summary>
    /// Centres the rack over the robot without lifting it: the rack keeps the
    /// floor height it was carried at, so the robot passes underneath while the
    /// docked pair drives across the floor.
    /// </summary>
    private void PlaceRackOnRobot()
    {
        if (robot == null)
        {
            return;
        }

        Vector3 robotPosition = robot.transform.position;
        Vector3 position = transform.position;
        position.x = robotPosition.x;
        position.z = robotPosition.z;
        position.y = rackCarryHeight;
        transform.position = position;
    }

    /// <summary>
    /// Lowest point of the rack in its own local space. Cached because the rack
    /// is a fixed model and walking its renderers is not cheap.
    /// </summary>
    private float GetRackBaseOffset()
    {
        if (rackBaseOffsetCached)
        {
            return rackBaseOffset;
        }

        bool found = false;
        float min = 0f;
        Matrix4x4 toRack = transform.worldToLocalMatrix;
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
            {
                continue;
            }

            Bounds local = renderer.localBounds;
            Matrix4x4 matrix = toRack * renderer.transform.localToWorldMatrix;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = new Vector3(
                    (corner & 1) == 0 ? local.min.x : local.max.x,
                    (corner & 2) == 0 ? local.min.y : local.max.y,
                    (corner & 4) == 0 ? local.min.z : local.max.z);
                float y = matrix.MultiplyPoint3x4(point).y;
                min = found ? Mathf.Min(min, y) : y;
                found = true;
            }
        }

        rackBaseOffset = found ? min : 0f;
        rackBaseOffsetCached = true;
        return rackBaseOffset;
    }

    private void CompleteCurrentTarget(GameObject target)
    {
        if (target == null || targetIndex >= targetPoints.Count)
        {
            return;
        }

        // Planar check on purpose. The docked rack rides on top of the robot, so
        // its Y sits about half a metre above the target points and a full 3D
        // distance would never fit inside the tolerance. What has to line up is
        // the robot parked over the target.
        if (!IsRackWithinTarget(target))
        {
            if (showGuidance)
            {
                TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示,
                    GuideMoveCloser, 1.5f);
            }

            return;
        }

        transform.SetParent(null, true);
        transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
        attachedToRobot = false;
        // The business flow ends with 所有货架入场, so a finished point keeps a
        // rack standing on it instead of showing an empty slot.
        PlaceRackCopyAt(target.transform.position, target.transform.rotation, target.name);
        target.SetActive(false);
        targetIndex++;

        if (targetIndex < targetPoints.Count)
        {
            ResetRackToSpawn();
            SetManipulationEnabled(true);
            // Every round restarts from the entry QR code, so the placement gate
            // has to be reopened for the next rack.
            rackOnEntryQr = false;
            SetGuidanceState(RackEntryGuidanceState.MoveRackToEntryQr);
            if (showGuidance)
            {
                TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示,
                    $"已完成 {targetIndex}/{targetPoints.Count} 个货架点，请将下一货架先放到二维码上方。", 1.5f);
            }
            return;
        }

        userCompleted = true;
        completed = true;
        stepActive = false;
        SetManipulationEnabled(false);
        TexCore_10BackpackController.HideFor(this);
        SetGuidanceState(RackEntryGuidanceState.Complete);
        TexCore_BusinessManager.OnClick(gameObject);
    }

    /// <summary>
    /// Leaves an inert copy of the rack standing on a finished target point. The
    /// copy keeps only the visual hierarchy: no workflow step, no manipulator, no
    /// rigidbody and no live colliders, so it can never take part in the run.
    /// </summary>
    private void PlaceRackCopyAt(Vector3 position, Quaternion rotation, string targetName)
    {
        Transform parent = originalParent != null ? originalParent : transform.parent;
        GameObject placed = Instantiate(gameObject, parent);
        placed.name = "10_已入场货架_" + targetName;
        placed.SetActive(false);
        StripPlacedRackBehaviour(placed);
        placed.transform.SetPositionAndRotation(position, rotation);
        placed.transform.localScale = startScale;
        placed.SetActive(true);
        placedRacks.Add(placed);
    }

    private static void StripPlacedRackBehaviour(GameObject placed)
    {
        foreach (TexCore_InteracterBase interacter in placed.GetComponentsInChildren<TexCore_InteracterBase>(true))
        {
            if (interacter == null)
            {
                continue;
            }

            // Disable first: Destroy is deferred, and a second live workflow step
            // must not run for the rest of the frame.
            interacter.enabled = false;
            DestroyPlacedObject(interacter);
        }

        foreach (ObjectManipulator manipulator in placed.GetComponentsInChildren<ObjectManipulator>(true))
        {
            if (manipulator != null)
            {
                DestroyPlacedObject(manipulator);
            }
        }

        foreach (ConstraintManager constraints in placed.GetComponentsInChildren<ConstraintManager>(true))
        {
            if (constraints != null)
            {
                DestroyPlacedObject(constraints);
            }
        }

        foreach (Rigidbody placedBody in placed.GetComponentsInChildren<Rigidbody>(true))
        {
            if (placedBody != null)
            {
                DestroyPlacedObject(placedBody);
            }
        }

        foreach (Collider placedCollider in placed.GetComponentsInChildren<Collider>(true))
        {
            if (placedCollider != null)
            {
                placedCollider.enabled = false;
            }
        }
    }

    private void ClearPlacedRacks()
    {
        for (int i = 0; i < placedRacks.Count; i++)
        {
            if (placedRacks[i] != null)
            {
                DestroyPlacedObject(placedRacks[i]);
            }
        }

        placedRacks.Clear();
    }

    /// <summary>Destroy works at runtime; edit-mode validation needs DestroyImmediate.</summary>
    private static void DestroyPlacedObject(Object value)
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

    private void CacheSource()
    {
        body = GetComponent<Rigidbody>();
        // Only captured once: CacheSource also runs on every OnEnable, and by then
        // the hand-carry freeze may already be applied.
        if (body != null && !constraintsCaptured)
        {
            originalConstraints = body.constraints;
            constraintsCaptured = true;
        }

        manipulator = GetComponent<ObjectManipulator>();
        if (manipulator == null)
        {
            manipulator = gameObject.GetComponentInChildren<ObjectManipulator>(true);
        }
        TexCore_03NoScalePolicy.Enforce(gameObject);
        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }

        EnsureGrabCollider();
        LockRackRotation();
    }

    /// <summary>
    /// MRTK can only select an interactable through the colliders listed on its
    /// ObjectManipulator, and it needs a solid one. The rack's own box is a
    /// trigger because that is what drives docking, so the player had nothing to
    /// grab: the list was empty and the only collider was a trigger. Mirror what
    /// ConfigureM10Robot does for the AGV and register a dedicated solid collider.
    /// </summary>
    private void EnsureGrabCollider()
    {
        if (manipulator == null)
        {
            return;
        }

        if (grabCollider == null)
        {
            Transform existing = transform.Find(GrabColliderName);
            if (existing != null)
            {
                grabCollider = existing.GetComponent<Collider>();
            }
        }

        if (grabCollider == null)
        {
            GameObject grab = new GameObject(GrabColliderName)
            {
                layer = gameObject.layer
            };
            grab.transform.SetParent(transform, false);
            BoxCollider box = grab.AddComponent<BoxCollider>();
            if (GetComponent<Collider>() is BoxCollider source)
            {
                box.center = source.center;
                box.size = source.size;
            }

            grabCollider = box;
        }

        // The rack is kept kinematic and the AGV is too, so the solid box adds no
        // collision response; it only gives the interactors something to hit.
        grabCollider.enabled = true;
        if (!manipulator.colliders.Contains(grabCollider))
        {
            manipulator.colliders.Add(grabCollider);
        }
    }

    /// <summary>
    /// Records the pose the reused rack returns to. Configure only runs while
    /// the scene is built, and these fields are not serialized, so a runtime
    /// capture is required too: otherwise startScale stays zero and the first
    /// ResetRackToSpawn scales the rack out of existence.
    /// </summary>
    private void CaptureStartPose()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        startScale = transform.localScale;
        rackCarryHeight = startPosition.y;
        if (startScale.sqrMagnitude < 0.000001f)
        {
            // A previously saved zero scale must never be treated as the pose.
            startScale = Vector3.one;
        }

        startPoseCaptured = true;
    }

    private void ResetRackToSpawn()
    {
        DetachFromRobot();
        if (transform.parent == null && originalParent != null)
        {
            transform.SetParent(originalParent, true);
        }
        Transform destination = spawnPoint != null ? spawnPoint : transform;
        transform.SetPositionAndRotation(destination.position, destination.rotation);
        // The rack is a floor-standing shelf. It keeps this height for the whole
        // hand-carry, so remember the height it was reset to.
        rackCarryHeight = transform.position.y;
        transform.localScale = startScale;
        if (body != null)
        {
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        gameObject.SetActive(true);
    }

    private void DetachFromRobot()
    {
        if (transform.parent == (robot != null ? robot.transform : null))
        {
            transform.SetParent(originalParent, true);
        }
    }

    private void ResetTargetPoints()
    {
        foreach (GameObject target in targetPoints)
        {
            if (target != null)
            {
                target.SetActive(true);
            }
        }
    }

    private void SetTargetPointsActive()
    {
        for (int i = targetIndex; i < targetPoints.Count; i++)
        {
            if (targetPoints[i] != null)
            {
                targetPoints[i].SetActive(true);
            }
        }
    }

    private void SetManipulationEnabled(bool enabled)
    {
        if (manipulator == null)
        {
            return;
        }

        manipulator.enabled = enabled;
        LockRackRotation();
        LockRackHeight(enabled);
    }

    /// <summary>
    /// A shelf stands on the floor, so carrying it by hand is a slide across the
    /// ground plane: the player may not lift it into the air. The kinematic body
    /// is driven through MovePosition, which the physics engine still filters
    /// through the rigidbody constraints, hence the frozen Y axis.
    /// </summary>
    private void LockRackHeight(bool locked)
    {
        if (body == null)
        {
            return;
        }

        body.constraints = locked
            ? originalConstraints | RigidbodyConstraints.FreezePositionY
            : originalConstraints;
    }

    /// <summary>
    /// The rack is a tall shelf the player has to stand on the entry QR code, so
    /// it may only translate. AllowedManipulations alone is not enough: the
    /// manipulator drives the constraint manager independently of that flag, so a
    /// lock on all three rotation axes is what actually stops the rack from being
    /// turned or tipped over while it is carried by hand.
    /// </summary>
    private void LockRackRotation()
    {
        if (manipulator == null)
        {
            return;
        }

        manipulator.AllowedManipulations = TransformFlags.Move;
        manipulator.ApplyTorque = false;
        // VelocityTracking makes the manipulator force the body out of kinematic
        // mode, and a dynamic body with the solid grab box depenetrates against
        // the floor and shoots the rack away. Kinematic movement keeps the body
        // driven by us alone.
        manipulator.RigidbodyMovementType =
            UnityEngine.XR.Interaction.Toolkit.XRBaseInteractable.MovementType.Kinematic;

        if (body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
        }

        ConstraintManager constraints = GetComponent<ConstraintManager>();
        if (constraints == null)
        {
            return;
        }

        if (rotationConstraint == null)
        {
            rotationConstraint = constraints.GetComponent<RotationAxisConstraint>();
            if (rotationConstraint == null)
            {
                rotationConstraint = constraints.gameObject.AddComponent<RotationAxisConstraint>();
            }
        }

        rotationConstraint.ConstraintOnRotation =
            AxisFlags.XAxis | AxisFlags.YAxis | AxisFlags.ZAxis;
        rotationConstraint.UseLocalSpaceForConstraint = false;
        rotationConstraint.enabled = true;
    }

    /// <summary>
    /// Moves the single-step guidance to <paramref name="value"/> and redraws the
    /// matching highlight. Only one action target is highlighted at a time.
    /// </summary>
    public void SetGuidanceState(RackEntryGuidanceState value)
    {
        guidance = value;
        // Every state change picks a new destination, and the rack grid may have
        // grown since the last route was laid.
        routeReady = false;
        RefreshGuidance();
    }

    private void RefreshGuidance()
    {
        if (!showGuidance || !stepActive)
        {
            TexCore_FocusHighlight.ClearFor(this);
            return;
        }

        switch (guidance)
        {
            case RackEntryGuidanceState.SelectRack:
                // The backpack controller owns this step because the rack itself
                // is withheld and must not be highlighted while it is hidden.
                TexCore_10BackpackController.HighlightRackEntry();
                break;
            case RackEntryGuidanceState.MoveRackToEntryQr:
                // The player drags the rack onto the entry code, so the code is
                // the destination and the arrow runs from the rack to it.
                guidanceSecondary.Clear();
                if (gameObject.activeInHierarchy)
                {
                    guidanceSecondary.Add(gameObject);
                }

                TexCore_FocusHighlight.Show(
                    entryQrAnchor != null ? entryQrAnchor.gameObject : gameObject,
                    GuideMoveRackToQr, gameObject, guidanceSecondary, this);
                break;
            case RackEntryGuidanceState.AttachRack:
            case RackEntryGuidanceState.MoveToTarget:
                // Both of these are the robot driving itself: it runs under the
                // rack and then on to the rack point without the player doing
                // anything, so neither leg needs an operation highlight.
                TexCore_FocusHighlight.ClearFor(this);
                break;
            default:
                // None and Complete both show no operation highlight. The
                // Complete state is preserved for the completion text.
                TexCore_FocusHighlight.ClearFor(this);
                break;
        }
    }

    /// <summary>
    /// Holds the current carrier in the M10 backpack until the player takes it
    /// out. This affects only the first carrier of the operation; the existing
    /// workflow continues to reset that carrier for later target points.
    /// </summary>
    public void WithholdUntilSummoned()
    {
        if (!stepActive || completed)
        {
            return;
        }

        SetManipulationEnabled(false);
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Releases the carrier selected in the M10 backpack at its configured
    /// spawn point and restores the normal movable-rack interaction.
    /// </summary>
    public bool ReleaseFromBackpack()
    {
        if (!stepActive || completed)
        {
            return false;
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        ResetRackToSpawn();
        TexCore_BackpackPhysics.Stabilize(gameObject);
        SetManipulationEnabled(true);
        rackOnEntryQr = false;
        SetGuidanceState(RackEntryGuidanceState.MoveRackToEntryQr);
        return true;
    }
}
