using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Events;

namespace MRAdaptiveTraining
{
    [DisallowMultipleComponent]
    public sealed class AdaptiveTrainingController : MonoBehaviour
    {
        public TrainingPolicyAsset policy;
        public string taskId = "assembly-demo";
        [Tooltip("只有原任务所有明确错误均已接入时才能勾选。未接入不会被当成零错误。")]
        public bool errorTrackingAvailable;
        public bool useDemoStorage;
        [Tooltip("连接原项目：重置任务完成后调用 BeginNextAttempt。不要在此事件里切换到下一关。")]
        public UnityEvent onRepeatRequested = new UnityEvent();
        [Tooltip("连接原项目继续流程。只有保存成功且必要步骤完成后才会触发。")]
        public UnityEvent onContinueRequested = new UnityEvent();
        public UnityEvent onTeacherReviewRequested = new UnityEvent();
        public UnityEvent onAttemptStarted = new UnityEvent();
        public UnityEvent onResultReady = new UnityEvent();

        private TrainingSession session;
        private LocalTrainingStore store;
        private readonly Stopwatch timer = new Stopwatch();
        private AttemptRecord pendingRecord;
        private bool dispatched;
        private bool repeatAuthorized;
        public string LastError { get; private set; }
        public AttemptRecord LastRecord { get; private set; }
        public bool HasActiveAttempt { get { return session != null && session.ActiveAttempt != null; } }
        public bool IsClosed { get { return session != null && session.IsClosed; } }
        public bool HasUnsavedRecord { get { return pendingRecord != null; } }
        public int CurrentAttempt { get { return session == null ? 0 : session.CompletedAttempts + (HasActiveAttempt ? 1 : 0); } }
        public int PlannedTotal { get { return session == null ? 0 : session.PlannedTotal; } }
        public string RecordsDirectory { get { EnsureStore(); return store.DirectoryPath; } }
        public int InvalidRecordCount { get { return store == null ? 0 : store.InvalidFileCount; } }

        public bool BeginTraining(string studentId)
        {
            return Guard(() =>
            {
                if (HasActiveAttempt || (session != null && !session.IsClosed) || pendingRecord != null)
                    throw new InvalidOperationException("当前训练未结束或记录未保存，不能覆盖。请完成训练或显式中断。");
                if (policy == null) throw new InvalidOperationException("请在 Inspector 连接本任务的 TrainingPolicyAsset。");
                session = new TrainingSession(studentId, taskId, policy.rules, useDemoStorage);
                LastRecord = null;
                repeatAuthorized = true;
                BeginAttemptInternal();
            });
        }

        public void BeginNextAttempt() { Guard(BeginAttemptInternal); }

        private void BeginAttemptInternal()
        {
            if (session == null || !repeatAuthorized || pendingRecord != null)
                throw new InvalidOperationException("尚未获得重练许可，或记录仍未保存。");
            session.BeginAttempt(errorTrackingAvailable, DateTime.UtcNow);
            repeatAuthorized = false;
            dispatched = false;
            timer.Restart();
            onAttemptStarted.Invoke();
        }

        // Call ONCE for a logical error, not every frame in OnTriggerStay/Update.
        public void ReportError(string code) { Guard(() => { RequireSession(); session.ReportError(code); }); }
        public void ReportInteractionIssue(string code) { Guard(() => { RequireSession(); session.ReportInteractionIssue(code); }); }

        public void CompleteAttempt(bool requiredStepsCompleted)
        {
            Guard(() =>
            {
                RequireSession();
                timer.Stop();
                LastRecord = session.CompleteAttempt(requiredStepsCompleted, timer.Elapsed.TotalSeconds, DateTime.UtcNow);
                pendingRecord = LastRecord;
                SavePendingInternal();
                onResultReady.Invoke();
            });
        }

        public void RetrySave() { Guard(SavePendingInternal); }

        public void ApplyDecision()
        {
            Guard(() =>
            {
                if (LastRecord == null || HasActiveAttempt || pendingRecord != null || dispatched)
                    throw new InvalidOperationException("没有可执行的新结果、记录未保存，或结果已执行。");
                dispatched = true;
                if (LastRecord.decision.action == TrainingAction.Repeat)
                {
                    repeatAuthorized = true;
                    onRepeatRequested.Invoke();
                }
                else if (LastRecord.decision.action == TrainingAction.Continue) onContinueRequested.Invoke();
                else onTeacherReviewRequested.Invoke();
            });
        }

        public void AbortTraining(string reason)
        {
            Guard(() =>
            {
                if (HasActiveAttempt)
                {
                    timer.Stop();
                    LastRecord = session.AbortAttempt(reason, timer.Elapsed.TotalSeconds, DateTime.UtcNow);
                    pendingRecord = LastRecord;
                    SavePendingInternal();
                }
                if (pendingRecord != null) throw new InvalidOperationException("记录尚未保存，不能结束。");
                session = null;
                repeatAuthorized = false;
                dispatched = true;
            });
        }

        public List<AttemptRecord> LoadRecords()
        {
            List<AttemptRecord> result = new List<AttemptRecord>();
            Guard(() => { EnsureStore(); result = store.LoadAll(); });
            return result;
        }

        public string ExportCsv()
        {
            string path = null;
            Guard(() => { EnsureStore(); path = store.ExportCsv(); });
            return path;
        }

        private void SavePendingInternal()
        {
            if (pendingRecord == null) return;
            EnsureStore();
            store.Save(pendingRecord);
            pendingRecord = null;
        }

        private void EnsureStore() { if (store == null) store = new LocalTrainingStore(useDemoStorage); }
        private void RequireSession() { if (session == null) throw new InvalidOperationException("尚未开始训练。"); }
        private bool Guard(Action action)
        {
            try { LastError = null; action(); return true; }
            catch (Exception exception)
            {
                LastError = exception.Message;
                UnityEngine.Debug.LogError("[MRAdaptiveTraining] " + LastError, this);
                return false;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && HasActiveAttempt) ReportInteractionIssue("application_paused");
        }

        private void OnDisable()
        {
            if (HasActiveAttempt) AbortTraining("任务组件停用、场景切换或应用退出");
            else if (pendingRecord != null) RetrySave();
        }
    }
}
