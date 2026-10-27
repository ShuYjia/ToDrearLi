using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MRAdaptiveTraining
{
    public sealed class TeacherDashboard : MonoBehaviour
    {
        public AdaptiveTrainingController controller;
        [Tooltip("演示场景由 AdaptiveTrainingDemo 绘制；独立使用时勾选。仅桌面 IMGUI，非 XR 面板。")]
        public bool standaloneWindow;
        private List<AttemptRecord> records = new List<AttemptRecord>();
        private string studentFilter = "";
        private string taskFilter = "";
        private string exportMessage = "";
        private Vector2 scroll;
        private Font font;

        private void OnEnable()
        {
            if (controller == null) return;
            controller.onResultReady.AddListener(Refresh);
            Refresh();
        }

        private void OnDisable()
        {
            if (controller != null) controller.onResultReady.RemoveListener(Refresh);
        }

        private void OnDestroy() { if (font != null) Destroy(font); }
        public void Refresh() { if (controller != null) records = controller.LoadRecords(); }

        private void OnGUI()
        {
            if (!standaloneWindow) return;
            if (font == null) font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "PingFang SC" }, 16);
            GUIStyle body = new GUIStyle(GUI.skin.label) { font = font, fontSize = 16, wordWrap = true };
            GUIStyle heading = new GUIStyle(body) { fontSize = 22, fontStyle = FontStyle.Bold };
            GUIStyle button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 16, fixedHeight = 36 };
            GUILayout.BeginArea(new Rect(16, 16, Screen.width - 32, Screen.height - 32));
            scroll = GUILayout.BeginScrollView(scroll);
            DrawPanel(body, heading, button);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        public void DrawPanel(GUIStyle body, GUIStyle heading, GUIStyle button)
        {
            if (controller == null) { GUILayout.Label("教师面板尚未连接训练控制器。", body); return; }
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("02  教师查看", heading);
            GUILayout.Label(controller.useDemoStorage ? "当前查看：演示数据（不代表真实学生）" : "当前查看：本机学生记录", body);
            GUILayout.BeginHorizontal();
            GUILayout.Label("学生筛选", body, GUILayout.Width(90));
            studentFilter = GUILayout.TextField(studentFilter, 64, GUILayout.Height(30));
            GUILayout.Label("任务筛选", body, GUILayout.Width(90));
            taskFilter = GUILayout.TextField(taskFilter, 64, GUILayout.Height(30));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新记录", button)) Refresh();
            if (GUILayout.Button("导出全部记录 CSV", button))
            {
                string path = controller.ExportCsv();
                exportMessage = path == null ? "导出失败：" + controller.LastError : "已导出：" + path;
            }
            GUILayout.EndHorizontal();
            if (controller.InvalidRecordCount > 0) GUILayout.Label("发现 " + controller.InvalidRecordCount + " 个无法读取的记录文件，未纳入统计，请检查原始文件。", body);
            if (!string.IsNullOrEmpty(exportMessage)) GUILayout.Label(exportMessage, body);

            List<AttemptRecord> filtered = records.Where(r =>
                r.studentId.IndexOf(studentFilter, StringComparison.OrdinalIgnoreCase) >= 0 &&
                r.taskId.IndexOf(taskFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            GUILayout.Label("筛选结果：" + filtered.Select(r => r.studentId).Distinct().Count() + " 名学生，" + filtered.Count + " 次尝试。", body);
            foreach (var task in filtered.GroupBy(r => r.taskId))
            {
                List<AttemptRecord> latest = task.GroupBy(r => r.studentId).Select(g => g.First()).ToList();
                int proficient = latest.Count(r => r.decision.state == LearningState.Proficient);
                int basic = latest.Count(r => r.decision.state == LearningState.Basic);
                int needs = latest.Count(r => r.decision.state == LearningState.NeedsPractice);
                int pending = latest.Count(r => r.decision.state == LearningState.PendingReview);
                GUILayout.Label("任务 " + task.Key + " ｜ 最新状态：掌握较好 " + proficient + "，基本掌握 " + basic + "，需要加强 " + needs + "，待核实 " + pending, body);
                List<AttemptRecord> reliable = task.Where(r => r.telemetryReliable && r.errorTrackingAvailable && !r.interrupted).ToList();
                var errors = reliable.SelectMany(r => r.errorCodes).GroupBy(code => code).OrderByDescending(g => g.Count()).Take(3);
                string common = string.Join("；", errors.Select(g => g.Key + " × " + g.Count()).ToArray());
                GUILayout.Label("可靠记录 " + reliable.Count + " / " + task.Count() + " ｜ 必要步骤完成 " + task.Count(r => r.requiredStepsCompleted) + " / " + task.Count() + " ｜ 常见错误：" + (common.Length == 0 ? "暂无可靠错误记录" : common), body);
            }
            GUILayout.Space(8);
            GUILayout.Label("最近 30 条尝试（完整记录可导出）", body);
            foreach (AttemptRecord r in filtered.Take(30))
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(r.studentId + " ｜ " + r.taskId + " ｜ 第 " + r.attemptNumber + " 次 ｜ " + TrainingLabels.State(r.decision.state) + " ｜ " + r.durationSeconds.ToString("F1") + " 秒", body);
                GUILayout.Label("明确错误 " + r.errorCodes.Count + "，交互问题 " + r.interactionIssues.Count + " ｜ " + TrainingLabels.Action(r.decision.action) + " ｜ UTC " + r.endedUtc, body);
                GUILayout.Label(r.decision.reason, body);
                GUILayout.EndVertical();
            }
            if (filtered.Count == 0) GUILayout.Label("尚无记录。完成上方训练后刷新；换一个学生标识可演示班级汇总。", body);
            GUILayout.Label("记录目录：" + controller.RecordsDirectory, body);
            GUILayout.EndVertical();
        }
    }
}
