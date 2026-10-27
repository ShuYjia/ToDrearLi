using System;
using System.Collections.Generic;

namespace MRAdaptiveTraining
{
    public enum LearningState { PendingReview, Basic, Proficient, NeedsPractice }
    public enum TrainingAction { Repeat, Continue, TeacherReview }

    // Demonstration defaults only. A teacher must approve values for each real task.
    [Serializable]
    public sealed class TrainingPolicy
    {
        public string policyVersion = "demo-v1";
        public int baselineOptionalRepeats = 2;
        public int minimumOptionalRepeats = 0;
        public int maximumAdditionalRepeats = 2;
        public int maximumTotalAttempts = 5;
        public int cleanAttemptsForMastery = 2;
        public int errorsForNeedsPractice = 2;

        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(policyVersion)) return "规则版本不能为空。";
            if (baselineOptionalRepeats < 0 || minimumOptionalRepeats < 0 || minimumOptionalRepeats > baselineOptionalRepeats)
                return "重复次数必须非负，最低次数不能超过基准次数。";
            if (maximumAdditionalRepeats < 0 || maximumTotalAttempts < 1 || maximumTotalAttempts > 1000)
                return "额外次数必须非负，总尝试上限必须在 1～1000 之间。";
            if ((long)baselineOptionalRepeats + 1 > maximumTotalAttempts) return "总尝试上限不能小于基准训练量。";
            if (cleanAttemptsForMastery < 1 || cleanAttemptsForMastery > maximumTotalAttempts || errorsForNeedsPractice < 1)
                return "熟练所需次数或错误阈值无效。";
            return null;
        }

        public TrainingPolicy Snapshot() { return (TrainingPolicy)MemberwiseClone(); }
    }

    [Serializable]
    public sealed class TrainingDecision
    {
        public LearningState state;
        public TrainingAction action;
        public string reason;
        public int plannedTotalAttempts;
    }

    [Serializable]
    public sealed class AttemptRecord
    {
        public int schemaVersion = 1;
        public string attemptId;
        public string sessionId;
        public string studentId;
        public string taskId;
        public string startedUtc;
        public string endedUtc;
        public string policyVersion;
        public TrainingPolicy policySnapshot;
        public int attemptNumber;
        public double durationSeconds;
        public bool requiredStepsCompleted;
        public bool errorTrackingAvailable;
        public bool telemetryReliable;
        public bool isDemo;
        public bool interrupted;
        public string interruptionReason;
        public List<string> errorCodes = new List<string>();
        public List<string> interactionIssues = new List<string>();
        public TrainingDecision decision;
    }

    public static class TrainingLabels
    {
        public static string State(LearningState state)
        {
            switch (state)
            {
                case LearningState.Proficient: return "掌握较好";
                case LearningState.Basic: return "基本掌握";
                case LearningState.NeedsPractice: return "需要加强";
                default: return "待核实";
            }
        }

        public static string Action(TrainingAction action)
        {
            switch (action)
            {
                case TrainingAction.Repeat: return "继续练习";
                case TrainingAction.Continue: return "进入后续教学";
                default: return "等待教师处理";
            }
        }
    }
}
