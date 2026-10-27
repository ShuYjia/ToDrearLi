using System;
using System.Collections.Generic;

namespace MRAdaptiveTraining
{
    // Pure C#: no Unity, device SDK, networking or model dependencies.
    public sealed class TrainingSession
    {
        private readonly TrainingPolicy policy;
        private readonly List<AttemptRecord> records = new List<AttemptRecord>();
        private readonly string studentId;
        private readonly string taskId;
        private readonly bool isDemo;
        private int plannedTotal;
        private int additionalRepeats;
        private bool closed;
        public readonly string SessionId = Guid.NewGuid().ToString("N");
        public AttemptRecord ActiveAttempt { get; private set; }
        public TrainingDecision LastDecision { get; private set; }
        public bool IsClosed { get { return closed; } }
        public int CompletedAttempts { get { return records.Count; } }
        public int PlannedTotal { get { return plannedTotal; } }

        public TrainingSession(string student, string task, TrainingPolicy rules, bool demo)
        {
            if (string.IsNullOrWhiteSpace(student) || string.IsNullOrWhiteSpace(task))
                throw new ArgumentException("学生标识和任务标识不能为空。");
            if (rules == null) throw new ArgumentNullException("rules");
            string error = rules.Validate();
            if (error != null) throw new ArgumentException(error);
            studentId = student.Trim();
            taskId = task.Trim();
            policy = rules.Snapshot();
            isDemo = demo;
            plannedTotal = 1 + policy.baselineOptionalRepeats;
        }

        public void BeginAttempt(bool errorTrackingAvailable, DateTime utcNow)
        {
            if (closed) throw new InvalidOperationException("本次训练已结束，需要由教学流程明确开始新训练。");
            if (ActiveAttempt != null) throw new InvalidOperationException("当前尝试尚未结束。");
            ActiveAttempt = new AttemptRecord
            {
                attemptId = Guid.NewGuid().ToString("N"), sessionId = SessionId,
                studentId = studentId, taskId = taskId, startedUtc = utcNow.ToUniversalTime().ToString("o"),
                policyVersion = policy.policyVersion, policySnapshot = policy.Snapshot(),
                attemptNumber = records.Count + 1, errorTrackingAvailable = errorTrackingAvailable,
                telemetryReliable = true, isDemo = isDemo
            };
        }

        public void ReportError(string code)
        {
            RequireActive();
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("错误代码不能为空。");
            ActiveAttempt.errorCodes.Add(code.Trim());
        }

        public void ReportInteractionIssue(string code)
        {
            RequireActive();
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("交互问题代码不能为空。");
            ActiveAttempt.interactionIssues.Add(code.Trim());
            ActiveAttempt.telemetryReliable = false;
        }

        public AttemptRecord CompleteAttempt(bool requiredStepsCompleted, double seconds, DateTime utcNow)
        {
            RequireActive();
            AttemptRecord record = ActiveAttempt;
            record.requiredStepsCompleted = requiredStepsCompleted;
            record.durationSeconds = seconds;
            record.endedUtc = utcNow.ToUniversalTime().ToString("o");
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
            {
                record.durationSeconds = 0;
                record.telemetryReliable = false;
                record.interactionIssues.Add("invalid_duration");
            }
            LearningState state = Evaluate(record);
            string reason = Explain(record, state);
            int count = records.Count + 1;
            TrainingAction action;

            if (!requiredStepsCompleted)
            {
                action = count < policy.maximumTotalAttempts ? TrainingAction.Repeat : TrainingAction.TeacherReview;
                plannedTotal = Math.Min(policy.maximumTotalAttempts, Math.Max(plannedTotal, count + 1));
                reason += " 必要步骤未全部完成，禁止跳过；" +
                    (action == TrainingAction.Repeat ? "重置后重新操作。" : "已到尝试上限，交由教师处理。");
            }
            else if (state == LearningState.PendingReview)
            {
                action = count < plannedTotal ? TrainingAction.Repeat : TrainingAction.TeacherReview;
                reason += " 保留基准训练安排；完成后仍需核实数据，不自动认定熟练。";
            }
            else if (state == LearningState.Proficient)
            {
                plannedTotal = Math.Max(count, 1 + policy.minimumOptionalRepeats);
                action = count < plannedTotal ? TrainingAction.Repeat : TrainingAction.Continue;
                reason += " 只减少可省略的重复练习。";
            }
            else if (state == LearningState.NeedsPractice)
            {
                if (count >= policy.maximumTotalAttempts)
                {
                    action = TrainingAction.TeacherReview;
                    reason += " 已到尝试上限，等待教师处理。";
                }
                else if (count < plannedTotal)
                {
                    action = TrainingAction.Repeat;
                    reason += " 先完成已经安排的练习。";
                }
                else if (additionalRepeats < policy.maximumAdditionalRepeats)
                {
                    ++additionalRepeats;
                    plannedTotal = count + 1;
                    action = TrainingAction.Repeat;
                    reason += " 增加一次同类练习。";
                }
                else
                {
                    action = TrainingAction.TeacherReview;
                    reason += " 补练次数已用完，等待教师处理。";
                }
            }
            else
            {
                action = count < plannedTotal ? TrainingAction.Repeat : TrainingAction.Continue;
                reason += " 按当前训练安排继续。";
            }

            LastDecision = new TrainingDecision { state = state, action = action, reason = reason, plannedTotalAttempts = plannedTotal };
            record.decision = LastDecision;
            records.Add(record);
            ActiveAttempt = null;
            closed = action != TrainingAction.Repeat;
            return record;
        }

        public AttemptRecord AbortAttempt(string reason, double seconds, DateTime utcNow)
        {
            RequireActive();
            ActiveAttempt.interrupted = true;
            ActiveAttempt.interruptionReason = reason;
            ActiveAttempt.telemetryReliable = false;
            AttemptRecord record = CompleteAttempt(false, seconds, utcNow);
            record.decision.state = LearningState.PendingReview;
            record.decision.action = TrainingAction.TeacherReview;
            record.decision.reason = "训练中断：" + reason + "。不将中断计为学习错误；需要重新安排训练。";
            closed = true;
            return record;
        }

        private LearningState Evaluate(AttemptRecord current)
        {
            if (!current.telemetryReliable || !current.errorTrackingAvailable) return LearningState.PendingReview;
            if (!current.requiredStepsCompleted || current.errorCodes.Count >= policy.errorsForNeedsPractice)
                return LearningState.NeedsPractice;
            int clean = IsClean(current) ? 1 : 0;
            for (int i = records.Count - 1; clean > 0 && i >= 0; --i)
            {
                if (!IsClean(records[i])) break;
                ++clean;
            }
            return clean >= policy.cleanAttemptsForMastery ? LearningState.Proficient : LearningState.Basic;
        }

        private static bool IsClean(AttemptRecord record)
        {
            return record.requiredStepsCompleted && record.telemetryReliable && record.errorTrackingAvailable && !record.interrupted && record.errorCodes.Count == 0;
        }

        private string Explain(AttemptRecord record, LearningState state)
        {
            if (state == LearningState.PendingReview)
                return !record.errorTrackingAvailable ? "未接入完整的错误采集，不能把没有记录当作没有错误。" : "存在交互或数据异常，本次不用于能力判断。";
            if (state == LearningState.Proficient) return "本轮训练中连续 " + policy.cleanAttemptsForMastery + " 次可靠完成且无明确错误。";
            if (!record.requiredStepsCompleted) return "必要操作尚未完成。";
            if (state == LearningState.NeedsPractice) return "本次明确错误 " + record.errorCodes.Count + " 次，达到本任务规则阈值。";
            return "已完成必要操作，但尚未达到连续稳定完成条件。时间仅记录，不单独决定学习状态。";
        }

        private void RequireActive()
        {
            if (ActiveAttempt == null) throw new InvalidOperationException("尚未开始尝试，或本次尝试已经结束。");
        }
    }
}
