using System.Collections.Generic;
using UnityEngine;
using Bpf.Diagnostics;

/// <summary>
/// 09 拆钉步骤的“最近固定钉”分配器：8 颗固定钉可以任意顺序拆卸，每个拆钉步骤
/// 激活时把它自己的目标换成离钻头最近的、还没拆掉的那一颗。
/// </summary>
/// <remarks>
/// 原场景由 <c>TexCore_0809SceneBuilder.CreateDrillScrewRemoval</c> 把步骤 1..8
/// 固定绑定到 8 个吸附点，第一步会指向离钻头最远的一颗。这里在运行期重排：
/// 位置指引、完成判定和固定钉显隐都跟着“当前步骤实际分配到的那一颗”走。
/// 步骤仍按 1..8 依次激活，只是每一步拆哪一颗由距离决定。
/// </remarks>
[DisallowMultipleComponent]
public sealed class TexCore_M09DrillNearestScrew : MonoBehaviour
{
    [Tooltip("8 个拆钉步骤，按原来的步骤序号排列。")]
    [SerializeField] private TexCore_OrderedPlacementStep[] drillSteps =
        new TexCore_OrderedPlacementStep[0];

    [Tooltip("钻头吸附点根节点：ChangJing-8/ShouDianZuan吸附位置。")]
    [SerializeField] private Transform snapRoot;

    [Tooltip("固定钉可见模型根节点：ChangJing-8/AGV/XiangZi/ZiGongSi。")]
    [SerializeField] private Transform screwRoot;

    private Transform[] authoredSnaps;
    private TexCore_StepObjectState[] stepStates;
    private readonly Dictionary<TexCore_OrderedPlacementStep, Transform> assignment =
        new Dictionary<TexCore_OrderedPlacementStep, Transform>();
    private readonly List<TexCore_OrderedPlacementStep> staleAssignments =
        new List<TexCore_OrderedPlacementStep>();
    private bool configured;

    /// <summary>
    /// 本步骤实际会去拆的固定钉吸附点；未分配或已完成重新进入时返回 null。
    /// </summary>
    public Transform GetAssignedSnap(TexCore_OrderedPlacementStep step)
    {
        return step != null && assignment.TryGetValue(step, out Transform snap) ? snap : null;
    }

    private void Awake()
    {
        Configure();
    }

    private void Configure()
    {
        if (snapRoot == null || screwRoot == null ||
            drillSteps == null || drillSteps.Length == 0)
        {
            BpfLog.Error(BpfLogCategory.Scene, "tex_core_m09_drill_nearest.configure",
                "09 拆钉最近固定钉分配器缺少步骤或吸附点配置。", this);
            return;
        }

        authoredSnaps = new Transform[drillSteps.Length];
        stepStates = new TexCore_StepObjectState[drillSteps.Length];
        for (int index = 0; index < drillSteps.Length; index++)
        {
            TexCore_OrderedPlacementStep step = drillSteps[index];
            authoredSnaps[index] = step != null ? step.Target : null;
            stepStates[index] = step != null
                ? step.GetComponent<TexCore_StepObjectState>()
                : null;
            if (authoredSnaps[index] == null || stepStates[index] == null)
            {
                BpfLog.Error(BpfLogCategory.Scene, "tex_core_m09_drill_nearest.configure",
                    "09 拆钉步骤缺少原始吸附点或状态组件：" +
                    (step != null ? step.name : "<null>"), this);
                return;
            }
        }

        configured = true;
    }

    private void LateUpdate()
    {
        if (!configured)
        {
            return;
        }

        int activeIndex = FindActiveStepIndex();
        bool changed = PruneAssignments(activeIndex);

        if (activeIndex >= 0 && !assignment.ContainsKey(drillSteps[activeIndex]))
        {
            changed |= AssignNearestScrew(activeIndex);
        }

        if (changed)
        {
            ApplyAssignments(activeIndex);
        }
    }

    /// <summary>
    /// 当前正在进行的拆钉步骤。已完成但还没切到下一步的那一帧不算当前步骤，
    /// 否则会把刚拆完的固定钉又分配一次。
    /// </summary>
    private int FindActiveStepIndex()
    {
        for (int index = 0; index < drillSteps.Length; index++)
        {
            TexCore_OrderedPlacementStep step = drillSteps[index];
            if (step != null && !step.WasCompletedByUser &&
                step.state == FinishState.等待触发)
            {
                return index;
            }
        }

        return -1;
    }

    /// <summary>
    /// 只保留“这一轮确实拆掉了”的分配。重进题目时框架会把 userCompleted
    /// 复位，这里跟着自动清空，8 颗固定钉也会重新出现。
    /// </summary>
    private bool PruneAssignments(int activeIndex)
    {
        staleAssignments.Clear();
        foreach (KeyValuePair<TexCore_OrderedPlacementStep, Transform> entry in assignment)
        {
            if (entry.Key == null)
            {
                staleAssignments.Add(entry.Key);
                continue;
            }

            bool isActive = activeIndex >= 0 && entry.Key == drillSteps[activeIndex];
            if (!isActive && !entry.Key.WasCompletedByUser)
            {
                staleAssignments.Add(entry.Key);
            }
        }

        for (int index = 0; index < staleAssignments.Count; index++)
        {
            assignment.Remove(staleAssignments[index]);
        }

        bool changed = staleAssignments.Count > 0;
        staleAssignments.Clear();
        return changed;
    }

    /// <summary>
    /// 把当前步骤分配给离钻头最近的、还没被别的步骤占用的吸附点。
    /// 只在步骤刚激活时算一次，玩家搬动钻头时指引不会乱跳。
    /// </summary>
    private bool AssignNearestScrew(int activeIndex)
    {
        TexCore_OrderedPlacementStep step = drillSteps[activeIndex];
        GameObject source = step.InteractionSource;
        if (source == null)
        {
            return false;
        }

        Vector3 from = source.transform.position;
        Transform best = null;
        float bestDistance = float.MaxValue;
        for (int index = 0; index < authoredSnaps.Length; index++)
        {
            Transform snap = authoredSnaps[index];
            if (snap == null || IsAssigned(snap))
            {
                continue;
            }

            float distance = Vector3.Distance(from, snap.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = snap;
            }
        }

        if (best == null)
        {
            BpfLog.Warning(BpfLogCategory.Scene, "tex_core_m09_drill_nearest.assign",
                "09 拆钉找不到未拆卸的固定钉吸附点。", step.gameObject);
            return false;
        }

        assignment[step] = best;
        BpfLog.Info(BpfLogCategory.Scene, "tex_core_m09_drill_nearest.assign",
            "拆钉步骤指向最近固定钉", step.gameObject,
            BpfLogField.Of("step", step.name),
            BpfLogField.Of("screw", best.name),
            BpfLogField.Of("distance", bestDistance));
        return true;
    }

    /// <summary>
    /// 把分配结果写回每个步骤：目标、位置指引与固定钉显隐都按当前分配重建。
    /// 当前步骤最后再重放一次“等待触发”，保证它的吸附点与固定钉是可见状态。
    /// </summary>
    private void ApplyAssignments(int activeIndex)
    {
        for (int index = 0; index < drillSteps.Length; index++)
        {
            TexCore_OrderedPlacementStep step = drillSteps[index];
            if (step == null)
            {
                continue;
            }

            GameObject drill = step.InteractionSource;
            if (assignment.TryGetValue(step, out Transform snap))
            {
                GameObject screw = FindScrew(snap.name);
                stepStates[index].Configure(
                    new[] { drill, snap.gameObject, screw },
                    null,
                    null,
                    new[] { snap.gameObject, screw },
                    new[] { screw },
                    new[] { snap.gameObject });
                step.RetargetFromRuntime(snap);
                continue;
            }

            // 还没轮到的步骤：清掉目标，自己的固定钉仍按原语义保持可见；
            // 如果原来的吸附点已经被别的步骤拿走，就整对都不再碰。
            Transform authoredSnap = authoredSnaps[index];
            GameObject authoredScrew = FindScrew(authoredSnap.name);
            if (IsAssigned(authoredSnap))
            {
                stepStates[index].Configure(new[] { drill }, null, null, null, null, null);
            }
            else
            {
                stepStates[index].Configure(
                    new[] { drill },
                    null,
                    null,
                    new[] { authoredSnap.gameObject, authoredScrew },
                    new[] { authoredScrew },
                    new[] { authoredSnap.gameObject });
            }

            step.RetargetFromRuntime(null);
        }

        if (activeIndex >= 0)
        {
            stepStates[activeIndex].SetState(FinishState.等待触发);
        }
    }

    private bool IsAssigned(Transform snap)
    {
        foreach (KeyValuePair<TexCore_OrderedPlacementStep, Transform> entry in assignment)
        {
            if (entry.Value == snap)
            {
                return true;
            }
        }

        return false;
    }

    private GameObject FindScrew(string snapName)
    {
        Transform screw = screwRoot != null ? screwRoot.Find(snapName) : null;
        return screw != null ? screw.gameObject : null;
    }
}
