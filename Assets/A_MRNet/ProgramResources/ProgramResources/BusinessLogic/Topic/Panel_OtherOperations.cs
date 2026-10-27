using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Text.RegularExpressions;
using Bpf.Diagnostics;

public enum TopicTypeEnum
{ 
    有序,
    无序
}

//[Serializable]
//public class DataClass_Operate : SelfDataBase
//{
//    public List<string> tools;
//    //public string rightTool;
//}

/// <summary>
/// 大步骤
/// </summary>
[Serializable]
public class MajorStep
{
    public string majorStepName;//大步骤名称
    public List<BabyStep> stepDetails;

    public MajorStep(string majorStepName)
    {
        this.majorStepName = majorStepName;
        stepDetails = new List<BabyStep>();
    }
}

/// <summary>
/// 步骤细节
/// </summary>
[Serializable]
public class BabyStep
{
    public string babyStepName;//步骤名称
    public bool isBabyStep;//是否完成

    public BabyStep(string babyStepName, bool isBabyStep)
    {
        this.babyStepName = babyStepName;
        this.isBabyStep = isBabyStep;
    }
}

public class Panel_OtherOperations : PanelTopicBase, IGetData
{
    public List<MajorStep> majorSteps;
    
    public GameObject btn_Confirm;

    /// <summary>
    /// 控制题目是否打开下一步按钮
    /// </summary>
    public bool isNextShow;
    /// <summary>
    /// 错误提示以及下一步按钮是否打开
    /// </summary>
    public bool isFalseHint;

    public TopicTypeEnum topicType;
    //public DataClass_Operate selfData;

    public override void OnEnable()
    {
        base.OnEnable();
        if (btn_Confirm==null)
        {
            Transform b = transform.Find("确定");
            if (b != null)
                btn_Confirm = b.gameObject;
        }
        if (isSubmit)
        {
            if (btn_Confirm != null) btn_Confirm.SetActive(false);
        }
        else
        {
            //if (btn_Confirm != null) btn_Confirm.SetActive(true);
        }
    }
    public override void Start()
    {
        base.Start();
        topicData = (DataClass_Topic)datas.selfDatas[0];
        //selfData = (DataClass_Operate)datas.selfDatas[1];
        if (!isHeadline)
        {
            TexCore_EventCenter.Broadcast<bool>(TexCore_EventType.开关标题, true);
            TexCore_EventCenter.Broadcast<string>(TexCore_EventType.显示标题, topicData.hintTexts[stepIndex]);
        }
        else
        {
            TexCore_EventCenter.Broadcast<bool>(TexCore_EventType.开关标题, false);
            if (topicTitle != null)
            {
                topicTitle.text = topicData.hintTexts[stepIndex];
            }
        }

        TexCore_EventCenter.Broadcast<string>(TexCore_EventType.显示标题, topicData.hintTexts[stepIndex]);
        //TexCore_EventCenter.Broadcast<string>(TexCore_EventType.播放音频, datas.GetUrl() + topicData.hintAudios[stepIndex]);

    }
    public override void OnClick(GameObject obj)
    {
        BpfLog.Error(BpfLogCategory.UI, "panel_other_operations.on_click", "走没走到这里啊");
        BpfLog.Error(BpfLogCategory.UI, "panel_other_operations.on_click", null, null, BpfLogField.Of("name", obj.name));

        base.OnClick(obj);
        switch (obj.name)
        {
            case "确定":
                CalculateScore();
                break;
            default:
                BpfLog.Error(BpfLogCategory.UI, "panel_other_operations.on_click", null, null, BpfLogField.Of("topic_type", topicType));
                switch (topicType)
                {
                    case TopicTypeEnum.有序:
                        if (stepIndex >= topicData.tasks.Count) return;
                        break;
                    case TopicTypeEnum.无序:
                        break;
                    default:
                        break;
                }

                BpfLog.Error(BpfLogCategory.UI, "panel_other_operations.on_click", "走到这里了吗");
                //步骤重复则返回
                for (int i = 0; i < steps.Count; i++)
                {
                    if (obj.name.Equals(steps[i]))
                    {
                        return;
                    }
                }

                steps.Add(obj.name);
                stepIndex += 1;
                NotifyStepVoiceChanged();

                if (stepIndex < topicData.hintTexts.Count)  //更新下一步的提示
                {
                    TexCore_EventCenter.Broadcast<string>(TexCore_EventType.显示标题, topicData.hintTexts[stepIndex]);
                }

                if (stepIndex >= topicData.tasks.Count)//如果操作完成
                {
                    CalculateScore();
                }
                break;
        }
    }
    public override void CalculateScore()
    {
        base.CalculateScore();

        string[] rights = topicData.tasks.ToArray();
        List<string> right = new List<string>(rights);
        int rightCout = 0;

        string t = "";
        for (int i = 0; i < right.Count; i++)
        {
            if (i == right.Count - 1)
            {
                t += right[i];
            }
            else
            {
                t += right[i] + ">>";
            }
        }

        switch (TexCore_Const.mode)
        {
            case Mode.练习:
                switch (topicType)
                {
                    case TopicTypeEnum.有序:
                        if (right.Count == steps.Count)//操作的步骤和需要操作的步骤数量相等
                        {

                            for (int i = 0; i < steps.Count; i++)
                            {
                                if (right[i] == steps[i])
                                {
                                    rightCout++;
                                }
                            }
                            if (right.Count == rightCout)
                            {
                                TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, "<color=green>恭喜，设备操作正确</color>", 1.5f);
                                haveScore = topicData.score;
                                Submit();
                            }
                            else
                            {
                                if (errorNum < 2)
                                {
                                    errorNum++;
                                    TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, $"<color=red>设备操作顺序有误</color>\n错误<color=red>{errorNum}</color>次", 1.5f);
                                    steps.Clear();
                                    stepIndex = 0;
                                    NotifyStepVoiceChanged();
                                    if (stepIndex < topicData.hintTexts.Count)  //更新下一步的提示
                                    {
                                        TexCore_EventCenter.Broadcast<string>(TexCore_EventType.显示标题, topicData.hintTexts[stepIndex]);
                                    }
                                    return;
                                }
                                if (errorNum == 2)
                                {
                                    errorNum++;

                                    TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, $"<color=red>设备操作顺序有误</color>\n错误<color=red>{errorNum}</color>次设备操作有误正确步骤是：\n" + t, 1.5f);
                                    haveScore = 0;
                                    Submit();
                                }
                            }
                        }
                        else//操作的步骤和需要操作的步骤数量不相等
                        {
                            if (errorNum < 2)
                            {
                                errorNum++;
                                TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, $"<color=red>设备操作步骤缺少</color>\n错误<color=red>{errorNum}</color>次", 1.5f);
                                steps.Clear();
                                stepIndex = 0;
                                NotifyStepVoiceChanged();
                                if (stepIndex < topicData.hintTexts.Count)  //更新下一步的提示
                                {
                                    TexCore_EventCenter.Broadcast<string>(TexCore_EventType.显示标题, topicData.hintTexts[stepIndex]);
                                }
                                return;
                            }
                            if (errorNum == 2)
                            {
                                errorNum++;
                                TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, $"<color=red>设备操作步骤缺少</color>\n错误<color=red>{errorNum}</color>次设备操作有误正确步骤是：\n" + t, 1.5f);
                                haveScore = 0;
                                Submit();
                            }
                        }
                        break;
                    case TopicTypeEnum.无序:
                        for (int i = 0; i < steps.Count; i++)
                        {
                            if (right.Contains(steps[i]))
                            {
                                rightCout++;
                            }
                        }
                        if (right.Count == rightCout)
                        {
                            TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, "<color=green>恭喜，设备操作正确</color>", 1.5f);
                            haveScore = topicData.score;
                            Submit();
                        }
                        else
                        {
                            if (errorNum < 2)
                            {
                                errorNum++;
                                TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, $"<color=red>设备操作步骤缺少</color>\n错误<color=red>{errorNum}</color>次", 1.5f);
                                return;
                            }
                            if (errorNum == 2)
                            {
                                errorNum++;
                                TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, $"<color=red>设备操作步骤缺少</color>\n错误<color=red>{errorNum}</color>次设备操作有误正确步骤是：\n" + t, 1.5f);
                                haveScore = 0;
                                Submit();
                            }
                        }
                        break;
                    default:
                        break;
                }
                break;
            case Mode.考核:
                switch (topicType)
                {
                    case TopicTypeEnum.有序:
                        if (right.Count == steps.Count)//操作的步骤和需要操作的步骤数量相等
                        {
                            for (int i = 0; i < steps.Count; i++)
                            {
                                if (right[i] == steps[i])
                                {
                                    rightCout++;
                                }
                            }
                            if (right.Count == rightCout)
                            {
                                haveScore = topicData.score;
                                Submit();
                            }
                            else
                            {
                                haveScore = 0;
                                Submit();
                            }
                        }
                        else//操作的步骤和需要操作的步骤数量不相等
                        {
                            haveScore = 0;
                            Submit();
                        }
                        break;
                    case TopicTypeEnum.无序:
                        for (int i = 0; i < steps.Count; i++)
                        {
                            if (right.Contains(steps[i]))
                            {
                                rightCout++;
                            }
                        }
                        if (right.Count == rightCout)
                        {
                            haveScore = topicData.score;
                            Submit();
                        }
                        else
                        {
                            haveScore = 0;
                            Submit();
                        }
                        break;
                    default:
                        break;
                }
                break;
            default:
                break;
        }
    }

    public override void Submit()
    {
        //base.Submit();
        SubmitConfig();
        if (btn_Confirm != null) btn_Confirm.SetActive(false);
        if (isNextShow)
        {
            btn_Next.SetActive(true);
        }
        else
        {
            btn_Next.GetComponent<TexCore_Interaction_UGUI>().OnClick();
        }
    }
    public  void SubmitConfig()
    {
        TexCore_EventCenter.Broadcast<string>(TexCore_EventType.完成题目, datas.name);
        if (isSubmit == false)
        {
            isSubmit = true;
        }
        //Invoke("SubMit", 0.5f);
        if (TexCore_Const.mode == Mode.考核)
        {
            ExamRecordBase exam = new ExamRecordBase();
            exam.ExamID = TexCore_Const.ExamID;
            exam.StudentNo = TexCore_Const.user.StuNo;
            //exam.StudentNo = (TexCore_ExamData.self.answer.Count + 1).ToString("D3");

            exam.OperNo = TexCore_Const.user.StuNo;//操作人学号
            exam.QuestionRel = TexCore_Const.QuestionRel.ToString("D3"); /*allOperate[i].index*/;//题目编号，001-999
            TexCore_Const.QuestionRel++;
            exam.PointCode = TexCore_PlaneManager.self.activePlane.datas.parent.name;//一级目录序号
            exam.SubMenu = TexCore_PlaneManager.self.activePlane.datas.name;//二级目录名称
            exam.Question = datas.name;//题干
            exam.QuestionType = 3.ToString();
            exam.Answers = addText(steps);
            exam.IsCorrect = haveScore == topicData.score ? "1" : "0"; ;
            exam.Mark = haveScore;
            exam.Correct = addText(topicData.tasks);

            TexCore_ExamData.self.AddAnswer(exam);
        }

    }
    public string addText(List<string> _str)
    {
        string str = "";
        for (int i = 0; i < _str.Count; i++)
        {
            if (i != _str.Count - 1)
                str += steps[i] + "_";
            else
                str += steps[i];
        }
        str = Regex.Replace(str, "[a-z]", "", RegexOptions.IgnoreCase);
        str = Regex.Replace(str, "[0-9]", "", RegexOptions.IgnoreCase);
        return str;
    }

    public TexCore_DataClass GetData()
    {
        datas.selfDatas = new List<SelfDataBase>();
        datas.selfDatas.Add(topicData);
        //datas.selfDatas.Add(selfData);
        return datas;
    }
}
