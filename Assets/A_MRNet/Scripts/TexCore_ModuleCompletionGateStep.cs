using System.Collections.Generic;
using UnityEngine;

public interface ITexCoreUserCompletionStatus
{
    bool WasCompletedByUser { get; }
}

/// <summary>
/// Optional progress report for gated steps that count their own units, so the
/// completion gate can tell the player how much is still missing.
/// </summary>
public interface ITexCoreProgressStatus
{
    int CompletedCount { get; }

    int RequiredCount { get; }
}

/// <summary>
/// Final-step guard used by module-completion topics. The visible framework
/// step is still completed through TexCore_BusinessManager, but only after all
/// required reporters have already reached their completed state.
/// </summary>
[DisallowMultipleComponent]
public sealed class TexCore_ModuleCompletionGateStep : TexCore_InteracterBase
{
    [SerializeField] private List<TexCore_InteracterBase> requiredSteps = new List<TexCore_InteracterBase>();
    [SerializeField] private string incompleteMessage = "请先完成本模块前置步骤";
    [SerializeField] private string returnTopicEntryName = string.Empty;

    private bool stepActive;
    private bool completed;

    public void Configure(IEnumerable<TexCore_InteracterBase> requirements, string message)
    {
        requiredSteps = new List<TexCore_InteracterBase>();
        if (requirements != null)
        {
            foreach (TexCore_InteracterBase requirement in requirements)
            {
                if (requirement != null && requirement != this && !requiredSteps.Contains(requirement))
                {
                    requiredSteps.Add(requirement);
                }
            }
        }

        if (!string.IsNullOrEmpty(message))
        {
            incompleteMessage = message;
        }
    }

    /// <summary>
    /// Points the failure guidance at the conversation topic the player has to
    /// return to. Empty keeps the previous message-only behavior.
    /// </summary>
    public void SetReturnTopicEntry(string topicName)
    {
        returnTopicEntryName = topicName ?? string.Empty;
    }

    public override void UnFinished()
    {
        completed = false;
        stepActive = false;
        TexCore_FocusHighlight.ClearFor(this);
    }

    public override void UnderWay()
    {
        completed = false;
        stepActive = true;
        if (AreRequirementsComplete())
        {
            CompleteFromGate();
        }
        else
        {
            ShowIncompleteGuidance();
        }
    }

    public override void OnTrigger()
    {
        completed = true;
        stepActive = false;
        TexCore_FocusHighlight.ClearFor(this);
    }

    public override void Finish()
    {
        completed = true;
        stepActive = false;
        TexCore_FocusHighlight.ClearFor(this);
    }

    public void TryComplete()
    {
        if (stepActive && !completed && AreRequirementsComplete())
        {
            CompleteFromGate();
        }
    }

    /// <summary>
    /// Reports how much of the gated work is still missing and highlights the
    /// topic entry the player has to return to. No completion score is given.
    /// </summary>
    private void ShowIncompleteGuidance()
    {
        string message = BuildIncompleteMessage();
        GameObject entry = FindReturnTopicEntry();
        if (entry != null)
        {
            TexCore_FocusHighlight.Show(entry, message, null, null, this);
            return;
        }

        TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, message, 2f);
    }

    private string BuildIncompleteMessage()
    {
        if (string.IsNullOrEmpty(returnTopicEntryName))
        {
            return incompleteMessage;
        }

        ITexCoreProgressStatus progress = FindProgressStatus();
        if (progress == null || progress.RequiredCount <= 0)
        {
            return incompleteMessage;
        }

        return $"当前完成 {progress.CompletedCount}/{progress.RequiredCount}，请返回{returnTopicEntryName}继续搬运。";
    }

    private ITexCoreProgressStatus FindProgressStatus()
    {
        foreach (TexCore_InteracterBase requirement in requiredSteps)
        {
            if (requirement is ITexCoreProgressStatus progress)
            {
                return progress;
            }
        }

        return null;
    }

    private GameObject FindReturnTopicEntry()
    {
        if (string.IsNullOrEmpty(returnTopicEntryName))
        {
            return null;
        }

        // The chapter bar builds its topic entries at runtime as "L2_<topic>".
        GameObject entry = GameObject.Find("L2_" + returnTopicEntryName);
        return entry != null && entry.activeInHierarchy ? entry : null;
    }

    private bool AreRequirementsComplete()
    {
        foreach (TexCore_InteracterBase requirement in requiredSteps)
        {
            if (requirement == null)
            {
                continue;
            }

            ITexCoreUserCompletionStatus completionStatus =
                requirement as ITexCoreUserCompletionStatus;
            if (completionStatus != null)
            {
                if (!completionStatus.WasCompletedByUser)
                {
                    return false;
                }

                continue;
            }

            if (requirement.state != FinishState.触发 && requirement.state != FinishState.已完成)
            {
                return false;
            }
        }

        return true;
    }

    private void CompleteFromGate()
    {
        completed = true;
        stepActive = false;
        TexCore_FocusHighlight.ClearFor(this);
        TexCore_BusinessManager.OnClick(gameObject);
    }
}
