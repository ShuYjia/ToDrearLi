using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using Bpf.Diagnostics;

/// <summary>
/// TexCore adapter for the QR-code pasting stage of HuaXian.
///
/// QR objects and target points are paired by their numeric suffix
/// (ErWeiMa12 -> ZhanTieDian12), never by Hierarchy order. A QR is accepted
/// only when released inside the configured position/rotation tolerance. The
/// step reports completion once every configured pair has been placed.
/// </summary>
[DisallowMultipleComponent]
public sealed class TexCore_HuaXianQrPasteStep : TexCore_InteracterBase, ITexCoreUserCompletionStatus
{
    [Serializable]
    public sealed class Pair
    {
        public string id;
        public GameObject qrCode;
        public Transform target;

        [NonSerialized] internal bool placed;
        [NonSerialized] internal Vector3 startPosition;
        [NonSerialized] internal Quaternion startRotation;
        [NonSerialized] internal Vector3 startScale;
        [NonSerialized] internal bool startActiveSelf;
        [NonSerialized] internal Rigidbody body;
        [NonSerialized] internal ObjectManipulator manipulator;
        [NonSerialized] internal bool startIsKinematic;
        [NonSerialized] internal bool startUseGravity;
        [NonSerialized] internal RigidbodyConstraints startConstraints;
        [NonSerialized] internal bool[] colliderStates;
        [NonSerialized] internal Collider[] colliders;
        [NonSerialized] internal UnityEngine.Events.UnityAction<SelectExitEventArgs> releaseHandler;
    }

    [Header("二维码与目标点")]
    [SerializeField] private List<Pair> pairs = new List<Pair>();
    [SerializeField] private bool autoBuildPairsWhenEmpty = true;
    [SerializeField] private Transform qrContainer;
    [SerializeField] private Transform targetContainer;
    [SerializeField] private bool includeInactiveObjects = true;
    [SerializeField] private string qrNamePrefix = "ErWeiMa";
    [SerializeField] private string targetNamePrefix = "ZhanTieDian";

    [Header("放置判定")]
    [SerializeField, Min(0.001f)] private float positionTolerance = 0.12f;
    [SerializeField, Range(0f, 180f)] private float rotationTolerance = 12f;
    [SerializeField] private bool ignoreVerticalPosition = true;
    [SerializeField] private bool compareYawOnly = true;
    [SerializeField] private Transform rotationReference;
    [SerializeField] private bool lockPlacedObjects = true;
    [SerializeField] private bool resetWhenInactive = true;

    [Header("事件")]
    [SerializeField] private UnityEvent onPairPlaced;
    [SerializeField] private UnityEvent onIncorrectPlacement;
    [SerializeField] private UnityEvent onAllPairsCompleted;

    private bool stepActive;
    private bool completed;
    private bool userCompleted;
    private bool pairsBuilt;
    private bool warningShown;
    private readonly List<Pair> runtimePairs = new List<Pair>();

    public bool WasCompletedByUser => userCompleted;
    public int PlacedCount
    {
        get
        {
            int count = 0;
            foreach (Pair pair in runtimePairs)
            {
                if (pair != null && pair.placed)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public int PairCount => runtimePairs.Count;

    public void ConfigureContainers(Transform qrRoot, Transform targetRoot)
    {
        qrContainer = qrRoot;
        targetContainer = targetRoot;
        pairsBuilt = false;
        BuildPairsIfNeeded(true);
    }

    public void RebuildPairs()
    {
        UnsubscribeAll();
        pairsBuilt = false;
        runtimePairs.Clear();
        BuildPairsIfNeeded(true);
        if (stepActive)
        {
            SetManipulationEnabled(true);
        }
    }

    private void Awake()
    {
        BuildPairsIfNeeded(false);
    }

    public new void OnEnable()
    {
        base.OnEnable();
        BuildPairsIfNeeded(false);
        SubscribeAll();
    }

    private void OnDisable()
    {
        UnsubscribeAll();
        SetManipulationEnabled(false);
        base.OnDisabel();
    }

    public override void UnFinished()
    {
        base.UnFinished();
        stepActive = false;
        completed = false;
        userCompleted = false;
        if (resetWhenInactive)
        {
            ResetPairs();
        }
        SetPairObjectsActive(false);
        SetManipulationEnabled(false);
    }

    public override void UnderWay()
    {
        base.UnderWay();
        BuildPairsIfNeeded(false);
        stepActive = true;
        completed = false;
        userCompleted = false;
        if (resetWhenInactive)
        {
            ResetPairs();
        }
        SetPairObjectsActive(true);
        SetManipulationEnabled(true);
    }

    public override void OnTrigger()
    {
        base.OnTrigger();
        completed = true;
        stepActive = false;
        SetManipulationEnabled(false);
    }

    public override void Finish()
    {
        base.Finish();
        completed = true;
        stepActive = false;
        SetManipulationEnabled(false);
    }

    private void HandleReleased(Pair pair, SelectExitEventArgs args)
    {
        if (!stepActive || completed || pair == null || pair.placed || pair.qrCode == null || pair.target == null)
        {
            return;
        }

        if (!IsWithinTolerance(pair))
        {
            onIncorrectPlacement?.Invoke();
            return;
        }

        SnapAndLock(pair);
        pair.placed = true;
        onPairPlaced?.Invoke();

        if (runtimePairs.Count > 0 && PlacedCount >= runtimePairs.Count)
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
        onAllPairsCompleted?.Invoke();
        TexCore_BusinessManager.OnClick(gameObject);
    }

    private bool IsWithinTolerance(Pair pair)
    {
        Vector3 sourcePosition = pair.qrCode.transform.position;
        Vector3 targetPosition = pair.target.position;
        if (ignoreVerticalPosition)
        {
            sourcePosition.y = 0f;
            targetPosition.y = 0f;
        }

        if (Vector3.Distance(sourcePosition, targetPosition) > Mathf.Max(0.001f, positionTolerance))
        {
            return false;
        }

        if (rotationTolerance >= 180f)
        {
            return true;
        }

        float angle = compareYawOnly
            ? GetYawDifference(pair.qrCode.transform, pair.target)
            : Quaternion.Angle(pair.qrCode.transform.rotation, pair.target.rotation);
        return angle <= rotationTolerance;
    }

    private float GetYawDifference(Transform source, Transform target)
    {
        Vector3 axis = rotationReference != null ? rotationReference.up : Vector3.up;
        Vector3 sourceForward = Vector3.ProjectOnPlane(source.forward, axis);
        Vector3 targetForward = Vector3.ProjectOnPlane(target.forward, axis);
        if (sourceForward.sqrMagnitude < 0.0001f || targetForward.sqrMagnitude < 0.0001f)
        {
            return Quaternion.Angle(source.rotation, target.rotation);
        }

        return Mathf.Abs(Vector3.SignedAngle(sourceForward, targetForward, axis));
    }

    private void SnapAndLock(Pair pair)
    {
        Transform source = pair.qrCode.transform;
        source.SetPositionAndRotation(pair.target.position, pair.target.rotation);

        if (pair.body != null)
        {
            pair.body.velocity = Vector3.zero;
            pair.body.angularVelocity = Vector3.zero;
            if (lockPlacedObjects)
            {
                pair.body.isKinematic = true;
                pair.body.constraints = RigidbodyConstraints.FreezeAll;
            }
        }

        if (lockPlacedObjects && pair.manipulator != null)
        {
            pair.manipulator.enabled = false;
        }
    }

    private void BuildPairsIfNeeded(bool force)
    {
        if (pairsBuilt && !force)
        {
            return;
        }

        pairsBuilt = true;
        bool rebuildFromScene = autoBuildPairsWhenEmpty &&
                                (pairs.Count == 0 || force);
        if (rebuildFromScene)
        {
            pairs.Clear();
            BuildPairsFromContainers();
        }

        runtimePairs.Clear();
        foreach (Pair pair in pairs)
        {
            if (pair == null || pair.qrCode == null || pair.target == null)
            {
                continue;
            }

            CachePair(pair);
            runtimePairs.Add(pair);
        }

        if (runtimePairs.Count == 0)
        {
            WarnOnce("未找到有效的二维码/目标点配对；请配置 Pairs，或指定 QR Container 与 Target Container。", this);
        }
    }

    private void BuildPairsFromContainers()
    {
        Transform qrRoot = qrContainer != null ? qrContainer : transform;
        Transform targetRoot = targetContainer != null ? targetContainer : transform;
        Dictionary<int, GameObject> qrById = new Dictionary<int, GameObject>();
        Dictionary<int, Transform> targetById = new Dictionary<int, Transform>();

        Transform[] qrObjects = qrRoot.GetComponentsInChildren<Transform>(includeInactiveObjects);
        foreach (Transform item in qrObjects)
        {
            int id = ParseSuffix(item.name, qrNamePrefix);
            if (id > 0 &&
                (!qrById.TryGetValue(id, out GameObject existing) ||
                 (!HasObjectManipulator(existing) && HasObjectManipulator(item.gameObject))))
            {
                // The scene keeps a non-interactive display copy and an
                // interactive copy for each QR code. Prefer the latter when
                // duplicate numeric suffixes are encountered.
                qrById[id] = item.gameObject;
            }
        }

        Transform[] targetObjects = targetRoot.GetComponentsInChildren<Transform>(includeInactiveObjects);
        foreach (Transform item in targetObjects)
        {
            int id = ParseSuffix(item.name, targetNamePrefix);
            if (id > 0 && !targetById.ContainsKey(id))
            {
                targetById.Add(id, item);
            }
        }

        List<int> ids = new List<int>(qrById.Keys);
        ids.Sort();
        foreach (int id in ids)
        {
            if (!targetById.TryGetValue(id, out Transform target))
            {
                WarnOnce($"二维码 {qrNamePrefix}{id} 没有匹配的目标点。", this);
                continue;
            }

            pairs.Add(new Pair
            {
                id = id.ToString(),
                qrCode = qrById[id],
                target = target
            });
        }
    }

    private static int ParseSuffix(string objectName, string prefix)
    {
        if (string.IsNullOrEmpty(objectName) || string.IsNullOrEmpty(prefix) ||
            !objectName.StartsWith(prefix, StringComparison.Ordinal))
        {
            return -1;
        }

        Match match = Regex.Match(objectName.Substring(prefix.Length), "^(\\d+)$");
        return match.Success && int.TryParse(match.Value, out int value) ? value : -1;
    }

    private void CachePair(Pair pair)
    {
        pair.placed = false;
        pair.startPosition = pair.qrCode.transform.position;
        pair.startRotation = pair.qrCode.transform.rotation;
        pair.startScale = pair.qrCode.transform.localScale;
        pair.startActiveSelf = pair.qrCode.activeSelf;
        pair.manipulator = pair.qrCode.GetComponent<ObjectManipulator>() ??
                           pair.qrCode.GetComponentInChildren<ObjectManipulator>(true);
        TexCore_03NoScalePolicy.Enforce(pair.qrCode);
        pair.body = pair.qrCode.GetComponent<Rigidbody>() ??
                    pair.qrCode.GetComponentInChildren<Rigidbody>(true);
        pair.colliders = pair.qrCode.GetComponentsInChildren<Collider>(true);
        pair.colliderStates = new bool[pair.colliders.Length];
        for (int i = 0; i < pair.colliders.Length; i++)
        {
            pair.colliderStates[i] = pair.colliders[i] != null && pair.colliders[i].enabled;
        }

        if (pair.body != null)
        {
            pair.startIsKinematic = pair.body.isKinematic;
            pair.startUseGravity = pair.body.useGravity;
            pair.startConstraints = pair.body.constraints;
        }

        if (pair.manipulator == null)
        {
            WarnOnce($"二维码 {pair.qrCode.name} 没有 ObjectManipulator；该二维码无法通过 MRTK 拖放完成。", pair.qrCode);
        }
    }

    private static bool HasObjectManipulator(GameObject candidate)
    {
        return candidate != null &&
               (candidate.GetComponent<ObjectManipulator>() != null ||
                candidate.GetComponentInChildren<ObjectManipulator>(true) != null);
    }

    private void SubscribeAll()
    {
        foreach (Pair pair in runtimePairs)
        {
            if (pair == null || pair.manipulator == null)
            {
                continue;
            }

            if (pair.releaseHandler != null)
            {
            pair.manipulator.lastSelectExited.RemoveListener(pair.releaseHandler);
            }

            pair.releaseHandler = args => HandleReleased(pair, args);
            pair.manipulator.lastSelectExited.AddListener(pair.releaseHandler);
        }
    }

    private void UnsubscribeAll()
    {
        foreach (Pair pair in runtimePairs)
        {
            if (pair != null && pair.manipulator != null && pair.releaseHandler != null)
            {
                pair.manipulator.lastSelectExited.RemoveListener(pair.releaseHandler);
                pair.releaseHandler = null;
            }
        }
    }

    private void ResetPairs()
    {
        foreach (Pair pair in runtimePairs)
        {
            if (pair == null || pair.qrCode == null)
            {
                continue;
            }

            pair.qrCode.transform.SetPositionAndRotation(pair.startPosition, pair.startRotation);
            pair.qrCode.transform.localScale = pair.startScale;
            pair.placed = false;

            if (pair.body != null)
            {
                pair.body.velocity = Vector3.zero;
                pair.body.angularVelocity = Vector3.zero;
                pair.body.isKinematic = pair.startIsKinematic;
                pair.body.useGravity = pair.startUseGravity;
                pair.body.constraints = pair.startConstraints;
            }

            if (pair.colliders != null)
            {
                for (int i = 0; i < pair.colliders.Length; i++)
                {
                    if (pair.colliders[i] != null && i < pair.colliderStates.Length)
                    {
                        pair.colliders[i].enabled = pair.colliderStates[i];
                    }
                }
            }
        }
    }

    private void SetManipulationEnabled(bool enabled)
    {
        foreach (Pair pair in runtimePairs)
        {
            if (pair == null || pair.manipulator == null)
            {
                continue;
            }

            pair.manipulator.enabled = enabled && !pair.placed && !completed;
        }
    }

    private void SetPairObjectsActive(bool active)
    {
        foreach (Pair pair in runtimePairs)
        {
            if (pair == null || pair.qrCode == null)
            {
                continue;
            }

            pair.qrCode.SetActive(active || pair.startActiveSelf);
        }
    }

    private void WarnOnce(string message, UnityEngine.Object context)
    {
        if (warningShown)
        {
            return;
        }

        warningShown = true;
        BpfLog.Warning(BpfLogCategory.Scene, "tex_core_hua_xian_qr_paste_step.warn_once", $"{name}: {message}", context);
    }
}

