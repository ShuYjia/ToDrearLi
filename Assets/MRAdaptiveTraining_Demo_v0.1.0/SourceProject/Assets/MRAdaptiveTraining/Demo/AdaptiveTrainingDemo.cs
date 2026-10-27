using System;
using UnityEngine;

namespace MRAdaptiveTraining
{
    // Desktop demonstration only. Bind the controller to existing XR UI for PICO.
    public sealed class AdaptiveTrainingDemo : MonoBehaviour
    {
        public AdaptiveTrainingController controller;
        public TeacherDashboard dashboard;
        private string student = "demo-student-001";
        private int step;
        private string message = "点击开始，然后按顺序完成三个操作。";
        private bool decisionApplied;
        private Vector2 scroll;
        private Font chineseFont;
        private GUIStyle heading;
        private GUIStyle body;
        private GUIStyle button;

        private void Awake()
        {
            if (controller == null) return;
            controller.onRepeatRequested.AddListener(Repeat);
            controller.onContinueRequested.AddListener(Continue);
            controller.onTeacherReviewRequested.AddListener(Review);
        }

        private void OnDestroy()
        {
            if (controller == null) return;
            controller.onRepeatRequested.RemoveListener(Repeat);
            controller.onContinueRequested.RemoveListener(Continue);
            controller.onTeacherReviewRequested.RemoveListener(Review);
            if (chineseFont != null) Destroy(chineseFont);
        }

        private void Repeat()
        {
            // This mock task owns only a step counter. Real projects must reset ALL task state here.
            step = 0;
            message = "已重置操作状态，开始下一次练习。";
            decisionApplied = false;
            controller.BeginNextAttempt();
        }

        private void Continue() { message = "本任务训练已结束，演示进入后续教学。原项目在这里连接下一任务。"; }
        private void Review() { message = "训练已暂停，等待教师处理。系统不会无限追加练习。"; }

        private void Operate(int expected)
        {
            if (step != expected)
            {
                controller.ReportError("wrong_step_order");
                message = "操作顺序不正确，记录一次明确错误。请继续完成必要步骤。";
                return;
            }
            ++step;
            message = step == 3 ? "必要步骤已完成，可以提交本次训练。" : "步骤完成，请继续下一步。";
        }

        private void InitializeStyles()
        {
            if (heading != null) return;
            chineseFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "PingFang SC", "Arial Unicode MS" }, 18);
            heading = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, wordWrap = true, font = chineseFont };
            body = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true, font = chineseFont };
            button = new GUIStyle(GUI.skin.button) { fontSize = 17, fixedHeight = 40, font = chineseFont };
        }

        private void OnGUI()
        {
            InitializeStyles();
            if (controller == null) { GUI.Label(new Rect(20, 20, 600, 60), "请连接 AdaptiveTrainingController。", body); return; }
            float width = Mathf.Min(1100, Screen.width - 32);
            GUILayout.BeginArea(new Rect((Screen.width - width) / 2, 16, width, Screen.height - 32));
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("MR 教学 · 自适应训练演示", heading);
            GUILayout.Label("单人本地演示 ｜ 规则评价 ｜ 数据与正式记录分开保存", body);
            GUILayout.Space(10);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("01  学生训练", heading);
            GUILayout.Label("模拟装配：领取工具 → 安装部件 → 确认结果。任务初始安排共 3 次；连续 2 次无错误可提前结束。", body);
            GUILayout.BeginHorizontal();
            GUILayout.Label("学生标识", body, GUILayout.Width(100));
            GUI.enabled = !controller.HasActiveAttempt && !controller.HasUnsavedRecord && (controller.CurrentAttempt == 0 || controller.IsClosed);
            student = GUILayout.TextField(student, 64, GUILayout.Height(32));
            if (GUILayout.Button("开始新训练", button, GUILayout.Width(160)))
            {
                if (controller.BeginTraining(student)) { step = 0; message = "训练已开始。"; decisionApplied = false; }
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Label("当前尝试：" + controller.CurrentAttempt + "    当前计划总次数：" + controller.PlannedTotal + "    必要步骤：" + step + " / 3", body);
            GUI.enabled = controller.HasActiveAttempt;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("1 领取工具", button)) Operate(0);
            if (GUILayout.Button("2 安装部件", button)) Operate(1);
            if (GUILayout.Button("3 确认结果", button)) Operate(2);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("报告交互异常", button)) { controller.ReportInteractionIssue("tracking_unstable"); message = "已记录交互异常，本次不直接用于能力判断。"; }
            if (GUILayout.Button("提交本次结果", button)) { controller.CompleteAttempt(step == 3); decisionApplied = false; }
            GUILayout.EndHorizontal();
            GUI.enabled = true;
            GUILayout.Label(message, body);
            if (controller.LastRecord != null && !controller.HasActiveAttempt)
            {
                TrainingDecision d = controller.LastRecord.decision;
                GUILayout.Space(6);
                GUILayout.Label("状态：" + TrainingLabels.State(d.state) + "  ｜  " + TrainingLabels.Action(d.action), heading);
                GUILayout.Label(d.reason, body);
                if (controller.HasUnsavedRecord)
                {
                    GUILayout.Label("记录保存失败，训练调整已阻止。请解决文件写入问题后重试。", body);
                    if (GUILayout.Button("重试保存", button)) controller.RetrySave();
                }
                GUI.enabled = !controller.HasUnsavedRecord && !decisionApplied;
                if (GUILayout.Button("执行安排：" + TrainingLabels.Action(d.action), button))
                {
                    decisionApplied = true;
                    controller.ApplyDecision();
                }
                GUI.enabled = true;
            }
            if (!string.IsNullOrEmpty(controller.LastError)) GUILayout.Label("提示：" + controller.LastError, body);
            GUILayout.EndVertical();
            GUILayout.Space(12);
            if (dashboard != null) dashboard.DrawPanel(body, heading, button);
            GUILayout.Space(10);
            GUILayout.Label("演示规则未经教学效度验证。速度不单独决定状态；必要步骤不能跳过。头显交互和多人联网需要在目标工程接入。", body);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.enabled = true;
        }
    }
}
