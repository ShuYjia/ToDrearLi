using System;
using MRAdaptiveTraining;

internal static class CoreTests
{
    private static int checks;
    private static void Check(bool value, string message)
    {
        ++checks;
        if (!value) throw new Exception(message);
    }
    private static AttemptRecord Attempt(TrainingSession session, bool complete, int errors, bool tracking, bool issue, double seconds)
    {
        session.BeginAttempt(tracking, DateTime.UtcNow);
        for (int i = 0; i < errors; ++i) session.ReportError("sequence");
        if (issue) session.ReportInteractionIssue("tracking_lost");
        return session.CompleteAttempt(complete, seconds, DateTime.UtcNow);
    }
    private static TrainingSession New() { return new TrainingSession("s1", "t1", new TrainingPolicy(), true); }
    private static void Throws(Action action, string message)
    {
        bool threw = false;
        try { action(); } catch (InvalidOperationException) { threw = true; }
        Check(threw, message);
    }
    public static int Main()
    {
        try
        {
            TrainingSession s = New();
            AttemptRecord a = Attempt(s, true, 0, true, false, 10000);
            Check(a.decision.state == LearningState.Basic && a.decision.action == TrainingAction.Repeat, "First clean attempt retains baseline");
            a = Attempt(s, true, 0, true, false, 10000);
            Check(a.decision.state == LearningState.Proficient && a.decision.action == TrainingAction.Continue && s.CompletedAttempts == 2, "Two clean attempts reduce 3 to 2 even when slow");
            Throws(() => s.BeginAttempt(true, DateTime.UtcNow), "Closed session cannot restart");
            s = New();
            for (int i = 0; i < 3; ++i) a = Attempt(s, true, 1, true, false, 1);
            Check(a.decision.state == LearningState.Basic && a.decision.action == TrainingAction.Continue && s.CompletedAttempts == 3, "Basic preserves 3 attempts");
            s = New();
            for (int i = 0; i < 5; ++i) a = Attempt(s, true, 2, true, false, 1);
            Check(a.decision.state == LearningState.NeedsPractice && a.decision.action == TrainingAction.TeacherReview && s.CompletedAttempts == 5, "Weak extends 3 to 5 then stops");
            s = New();
            for (int i = 0; i < 3; ++i) a = Attempt(s, true, 0, false, false, 1);
            Check(a.decision.state == LearningState.PendingReview && a.decision.action == TrainingAction.TeacherReview, "Missing error instrumentation never means mastery");
            s = New();
            a = Attempt(s, true, 8, true, true, 2);
            Check(a.decision.state == LearningState.PendingReview && a.errorCodes.Count == 8 && a.interactionIssues.Count == 1, "Unreliable data retained but not judged");
            s = New();
            for (int i = 0; i < 5; ++i)
            {
                a = Attempt(s, false, 0, true, false, 1);
                Check(a.decision.action != TrainingAction.Continue, "Required steps can never be skipped");
            }
            Check(a.decision.action == TrainingAction.TeacherReview, "Incomplete task stops at hard cap");
            s = New();
            Attempt(s, true, 0, true, false, 1);
            Attempt(s, true, 0, true, true, 1);
            a = Attempt(s, true, 0, true, false, 1);
            Check(a.decision.state == LearningState.Basic, "Unreliable attempt breaks clean streak");
            s = New();
            a = Attempt(s, true, 0, true, false, double.NaN);
            Check(a.decision.state == LearningState.PendingReview && a.durationSeconds == 0, "Invalid duration sanitized and flagged");
            Throws(() => s.CompleteAttempt(true, 1, DateTime.UtcNow), "Duplicate completion blocked");
            s = New();
            s.BeginAttempt(true, DateTime.UtcNow);
            Throws(() => s.BeginAttempt(true, DateTime.UtcNow), "Overlapping attempts blocked");
            a = s.AbortAttempt("scene changed", 2, DateTime.UtcNow);
            Check(a.interrupted && a.decision.state == LearningState.PendingReview && a.decision.action == TrainingAction.TeacherReview && s.IsClosed, "Abort is not a learning error");
            TrainingPolicy p = new TrainingPolicy();
            s = new TrainingSession("s", "t", p, false);
            p.cleanAttemptsForMastery = 1;
            a = Attempt(s, true, 0, true, false, 2);
            Check(a.decision.state == LearningState.Basic && a.policySnapshot.cleanAttemptsForMastery == 2, "Rule snapshot is stable during session");
            Check(new TrainingPolicy { maximumTotalAttempts = 2 }.Validate() != null, "Invalid cap rejected");
            Check(new TrainingPolicy { minimumOptionalRepeats = 3 }.Validate() != null, "Invalid minimum rejected");
            p = new TrainingPolicy { minimumOptionalRepeats = 2 };
            s = new TrainingSession("s", "t", p, false);
            Attempt(s, true, 0, true, false, 1);
            a = Attempt(s, true, 0, true, false, 1);
            Check(a.decision.state == LearningState.Proficient && a.decision.action == TrainingAction.Repeat, "Course minimum is preserved");
            p = new TrainingPolicy { maximumAdditionalRepeats = 0 };
            s = new TrainingSession("s", "t", p, false);
            for (int i = 0; i < 3; ++i) a = Attempt(s, true, 3, true, false, 1);
            Check(a.decision.action == TrainingAction.TeacherReview, "Zero additional allowance cannot grow");

            // Exhaust all 6^5 possible mixes of clean/error/incomplete/unreliable/missing-data/basic.
            for (int pattern = 0; pattern < 7776; ++pattern)
            {
                s = New();
                int sequence = pattern;
                for (int i = 0; i < 5 && !s.IsClosed; ++i)
                {
                    int kind = sequence % 6; sequence /= 6;
                    a = Attempt(s, kind != 2, kind == 1 ? 3 : (kind == 5 ? 1 : 0), kind != 4, kind == 3, i + 1);
                    Check(a.requiredStepsCompleted || a.decision.action != TrainingAction.Continue, "Exhaustive mandatory gate");
                    Check(a.telemetryReliable && a.errorTrackingAvailable || a.decision.state == LearningState.PendingReview, "Exhaustive unreliable assessment gate");
                    Check(a.decision.plannedTotalAttempts <= 5, "Exhaustive cap");
                }
                Check(s.IsClosed && s.CompletedAttempts <= 5, "Every path terminates within cap");
            }
            Console.WriteLine("PASS: " + checks + " assertions; 7776 mixed paths + targeted cases.");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
