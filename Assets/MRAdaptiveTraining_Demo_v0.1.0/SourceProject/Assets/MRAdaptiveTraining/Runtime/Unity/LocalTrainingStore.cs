using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace MRAdaptiveTraining
{
    // One file per attempt. A failed write never replaces an existing record.
    public sealed class LocalTrainingStore
    {
        public string DirectoryPath { get; private set; }
        public int InvalidFileCount { get; private set; }

        public LocalTrainingStore(bool demo)
        {
            DirectoryPath = Path.Combine(Application.persistentDataPath, "MRAdaptiveTraining", demo ? "DemoRecords" : "Records");
        }

        // Explicit directory for isolated verification or a host-controlled storage location.
        public LocalTrainingStore(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath)) throw new ArgumentException("记录目录不能为空。");
            DirectoryPath = Path.GetFullPath(directoryPath);
        }

        public void Save(AttemptRecord record)
        {
            if (!IsValid(record)) throw new InvalidDataException("训练记录结构无效，未写入。");
            Directory.CreateDirectory(DirectoryPath);
            string path = Path.Combine(DirectoryPath, record.attemptId + ".json");
            string json = JsonUtility.ToJson(record, true);
            if (File.Exists(path))
            {
                if (File.ReadAllText(path, Encoding.UTF8) == json) return;
                throw new IOException("相同记录标识已有不同内容，拒绝覆盖。");
            }
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, json, new UTF8Encoding(false));
                File.Move(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }

        public List<AttemptRecord> LoadAll()
        {
            InvalidFileCount = 0;
            List<AttemptRecord> result = new List<AttemptRecord>();
            if (!Directory.Exists(DirectoryPath)) return result;
            foreach (string path in Directory.GetFiles(DirectoryPath, "*.json"))
            {
                try
                {
                    AttemptRecord record = JsonUtility.FromJson<AttemptRecord>(File.ReadAllText(path, Encoding.UTF8));
                    if (!IsValid(record)) { ++InvalidFileCount; continue; }
                    result.Add(record);
                }
                catch (Exception exception)
                {
                    if (!(exception is IOException) && !(exception is ArgumentException)) throw;
                    ++InvalidFileCount;
                }
            }
            result.Sort((a, b) => string.CompareOrdinal(b.endedUtc, a.endedUtc));
            return result;
        }

        public string ExportCsv()
        {
            List<AttemptRecord> records = LoadAll();
            Directory.CreateDirectory(DirectoryPath);
            StringBuilder csv = new StringBuilder("学生,任务,训练标识,尝试标识,尝试序号,开始UTC,结束UTC,时长秒,必要步骤完成,错误采集完整,数据可靠,中断,中断原因,明确错误次数,错误代码,交互问题,学习状态,训练处理,判断依据,规则版本,演示数据\r\n");
            foreach (AttemptRecord r in records)
            {
                string[] values = { r.studentId, r.taskId, r.sessionId, r.attemptId,
                    r.attemptNumber.ToString(CultureInfo.InvariantCulture), r.startedUtc, r.endedUtc,
                    r.durationSeconds.ToString("F3", CultureInfo.InvariantCulture), r.requiredStepsCompleted.ToString(),
                    r.errorTrackingAvailable.ToString(), r.telemetryReliable.ToString(), r.interrupted.ToString(), r.interruptionReason,
                    r.errorCodes.Count.ToString(CultureInfo.InvariantCulture), string.Join("|", r.errorCodes.ToArray()),
                    string.Join("|", r.interactionIssues.ToArray()), TrainingLabels.State(r.decision.state),
                    TrainingLabels.Action(r.decision.action), r.decision.reason, r.policyVersion, r.isDemo.ToString() };
                for (int i = 0; i < values.Length; ++i) { if (i > 0) csv.Append(','); csv.Append(Cell(values[i])); }
                csv.Append("\r\n");
            }
            string output = Path.Combine(DirectoryPath, "TrainingReport_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N").Substring(0, 6) + ".csv");
            File.WriteAllText(output, csv.ToString(), new UTF8Encoding(true));
            return output;
        }

        private static string Cell(string value)
        {
            string text = value ?? "";
            string trimmed = text.TrimStart();
            if (trimmed.Length > 0 && "=+-@".IndexOf(trimmed[0]) >= 0) text = "'" + text;
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        private static bool IsValid(AttemptRecord r)
        {
            Guid id;
            DateTime timestamp;
            return r != null && r.schemaVersion == 1 && Guid.TryParseExact(r.attemptId, "N", out id)
                && !string.IsNullOrWhiteSpace(r.studentId) && !string.IsNullOrWhiteSpace(r.taskId)
                && Guid.TryParseExact(r.sessionId, "N", out id) && r.attemptNumber > 0
                && DateTime.TryParse(r.startedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out timestamp)
                && DateTime.TryParse(r.endedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out timestamp)
                && !double.IsNaN(r.durationSeconds) && !double.IsInfinity(r.durationSeconds) && r.durationSeconds >= 0
                && r.errorCodes != null && r.interactionIssues != null && r.decision != null
                && Enum.IsDefined(typeof(LearningState), r.decision.state) && Enum.IsDefined(typeof(TrainingAction), r.decision.action)
                && r.policySnapshot != null && r.policySnapshot.Validate() == null && r.policyVersion == r.policySnapshot.policyVersion;
        }
    }
}
