using System.Collections.Generic;
using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine;

/// <summary>
/// Locks the original 08 unpacking-box lid until all eight fastener steps
/// have been completed by the user.
/// </summary>
[DisallowMultipleComponent]
public sealed class TexCore_M08BoxLidGate : MonoBehaviour
{
    [SerializeField] private TexCore_ClickObjectStep boxLidStep;
    [SerializeField] private List<TexCore_OrderedPlacementStep> screws =
        new List<TexCore_OrderedPlacementStep>();

    private ObjectManipulator[] manipulators;
    private Collider[] colliders;
    private Rigidbody[] rigidbodies;
    private Transform lockRoot;
    private Vector3 startLocalPosition;
    private Quaternion startLocalRotation;
    private Vector3 startLocalScale;
    private bool[] originalKinematic;
    private bool initialized;
    private bool lidReleased;

    public void Configure(
        TexCore_ClickObjectStep step,
        IEnumerable<TexCore_OrderedPlacementStep> requirements)
    {
        boxLidStep = step;
        screws = new List<TexCore_OrderedPlacementStep>();

        if (requirements != null)
        {
            foreach (TexCore_OrderedPlacementStep screw in requirements)
            {
                if (screw != null && !screws.Contains(screw))
                {
                    screws.Add(screw);
                }
            }
        }

        CacheComponents();
        ApplyLockState();
    }

    private void Awake()
    {
        CacheComponents();
        CaptureStartPose();
    }

    private void OnEnable()
    {
        CacheComponents();
        CaptureStartPose();
        ApplyLockState();
    }

    private void LateUpdate()
    {
        ApplyLockState();
    }

    private void CacheComponents()
    {
        lockRoot = ResolveLockRoot();
        GameObject root = lockRoot != null ? lockRoot.gameObject : gameObject;
        manipulators = root.GetComponentsInChildren<ObjectManipulator>(true);
        colliders = root.GetComponentsInChildren<Collider>(true);
        rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
        TexCore_03NoScalePolicy.Enforce(root);

        originalKinematic = new bool[rigidbodies.Length];
        for (int index = 0; index < rigidbodies.Length; index++)
        {
            if (rigidbodies[index] != null)
            {
                originalKinematic[index] = rigidbodies[index].isKinematic;
            }
        }
    }

    private void CaptureStartPose()
    {
        if (initialized)
        {
            return;
        }

        Transform root = lockRoot != null ? lockRoot : transform;
        startLocalPosition = root.localPosition;
        startLocalRotation = root.localRotation;
        startLocalScale = root.localScale;
        initialized = true;
    }

    private void ApplyLockState()
    {
        ResolveRuntimeReferences();

        // 用户已移开包装箱盖后永久解除锁定：不再复位 transform，
        // 也不再关闭 Collider / ObjectManipulator。
        if (!lidReleased && boxLidStep != null && boxLidStep.WasCompletedByUser)
        {
            lidReleased = true;
        }

        bool allow = lidReleased || CanRemoveBoxLid();

        if (manipulators != null)
        {
            foreach (ObjectManipulator manipulator in manipulators)
            {
                if (manipulator != null)
                {
                    manipulator.enabled = allow;
                }
            }
        }

        if (colliders != null)
        {
            foreach (Collider collider in colliders)
            {
                if (collider != null)
                {
                    collider.enabled = allow;
                }
            }
        }

        if (rigidbodies != null)
        {
            for (int index = 0; index < rigidbodies.Length; index++)
            {
                Rigidbody body = rigidbodies[index];
                if (body == null)
                {
                    continue;
                }

                body.isKinematic = allow
                    ? originalKinematic[index]
                    : true;

                if (!allow)
                {
                    body.velocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
            }
        }

        if (!allow && initialized)
        {
            Transform root = lockRoot != null ? lockRoot : transform;
            root.localPosition = startLocalPosition;
            root.localRotation = startLocalRotation;
            root.localScale = startLocalScale;
        }
    }

    private Transform ResolveLockRoot()
    {
        // 只管理箱盖自身。向上找到 xiangti 会把箱体外壳一起算进来：
        // allow 时会把外壳的 ObjectManipulator 重新打开，与
        // “外壳不再作为独立可抓物”的 0809 处理互相打架。
        return transform;
    }

    private bool CanRemoveBoxLid()
    {
        if (boxLidStep == null ||
            boxLidStep.WasCompletedByUser ||
            boxLidStep.state != FinishState.等待触发)
        {
            return false;
        }

        if (screws == null || screws.Count != 8)
        {
            return false;
        }

        foreach (TexCore_OrderedPlacementStep screw in screws)
        {
            if (screw == null || !screw.WasCompletedByUser)
            {
                return false;
            }
        }

        return true;
    }

    private void ResolveRuntimeReferences()
    {
        if (boxLidStep == null)
        {
            foreach (TexCore_ClickObjectStep step in
                     FindObjectsOfType<TexCore_ClickObjectStep>(true))
            {
                if (step != null && step.name == "08_00_移开包装箱盖")
                {
                    boxLidStep = step;
                    break;
                }
            }
        }

        bool missingScrewReference = screws == null || screws.Count != 8;
        if (!missingScrewReference)
        {
            foreach (TexCore_OrderedPlacementStep screw in screws)
            {
                if (screw == null)
                {
                    missingScrewReference = true;
                    break;
                }
            }
        }

        if (!missingScrewReference)
        {
            return;
        }

        TexCore_OrderedPlacementStep[] discovered =
            FindObjectsOfType<TexCore_OrderedPlacementStep>(true);
        List<TexCore_OrderedPlacementStep> resolved =
            new List<TexCore_OrderedPlacementStep>(8);
        for (int index = 1; index <= 8; index++)
        {
            string expectedName = "08_00_使用手电钻钻开包装箱螺丝" + index;
            foreach (TexCore_OrderedPlacementStep screw in discovered)
            {
                if (screw != null && screw.name == expectedName)
                {
                    resolved.Add(screw);
                    break;
                }
            }
        }

        if (resolved.Count == 8)
        {
            screws = resolved;
        }
    }
}
