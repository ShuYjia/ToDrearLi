using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Text.RegularExpressions;
using Bpf.Diagnostics;



//[Serializable]
//public class DataClass_Operate : SelfDataBase
//{
//    public List<string> tools;
//    //public string rightTool;
//}
public class Panel_Place : PanelTopicBase, IGetData
{
    public GameObject btn_Confirm;

    /// <summary>
    /// 控制题目是否打开下一步按钮
    /// </summary>
    public bool isNextShow;
    /// <summary>
    /// 错误提示以及下一步按钮是否打开
    /// </summary>
    public bool isFalseHint;


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
        base.OnClick(obj);
        //if (TexCore_Const.ConfirmCharacter(character) == false) return;

        BpfLog.Info(BpfLogCategory.UI, "panel_place.on_click", "点击：" + datas.name +"\t"+ obj.name);

        if (stepIndex >= topicData.tasks.Count) return;

        string[] task= topicData.tasks[stepIndex].Split('/');//底座/物体
        string[] operate = obj.name.Split("_");

        switch (TexCore_Const.mode)
        {
            case Mode.练习:
                if (task.Length >= 2 && operate.Length > 1) //如果这一步的正确答案包含底座/物体且传递过来的物体大于1
                {
                    if (operate[0].Equals(task[0]) && operate[1].Equals(task[1]))//物体是正确的，且摆放在正确的物体上面
                    {
                        operater = operate[operate.Length - 1];
                        TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, $"<color=green>{task[1]}</color>架设成功", 1.5f);
                        steps.Add(topicData.tasks[stepIndex]);
                        stepIndex += 1;
                        haveScore = topicData.score;
                        if (stepIndex < topicData.hintTexts.Count) //更新下一步的提示
                        {
                            TexCore_EventCenter.Broadcast<string>(TexCore_EventType.显示标题, topicData.hintTexts[stepIndex]);
                        }
                        Submit();
                    }
                    else
                    {
                        if (errorNum < 2)
                        {
                            if (!operate[1].Equals(task[1]))
                            {
                                errorNum++;
                                TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, $"架设物体不对，请架设<color=green>{task[1]}</color>\n架设错误<color=red>{errorNum}</color>次", 1.5f);
                                return;
                            }
                            if (!operate[0].Equals(task[0]) && operate[1].Equals(task[1]))
                            {
                                errorNum++;
                                TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, $"架设位置不对，请重新架设到<color=green>{task[0]}</color>\n架设错误<color=red>{errorNum}</color>次", 1.5f);
                                return;
                            }
                        }
                        if (errorNum == 2)
                        {
                            errorNum++;
                            haveScore = 0;
                            TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, $"架设位置不对，下一步已打开\n架设错误<color=red>{errorNum}</color>次", 1.5f);
                            Submit();
                        }
                    }
                }
                else
                {
                    //TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, $"????????????????????????????", 1.5f);
                }
                break;
            case Mode.考核:
                if (task.Length >= 2 && operate.Length > 1) //如果这一步的正确答案包含底座/物体且传递过来的物体大于1
                {
                    if (operate[0].Equals(task[0]) && operate[1].Equals(task[1]))//物体是正确的，且摆放在正确的物体上面
                    {
                        operater = operate[operate.Length - 1];
                        steps.Add(topicData.tasks[stepIndex]);
                        stepIndex += 1;
                        haveScore = topicData.score;
                        if (stepIndex < topicData.hintTexts.Count) //更新下一步的提示
                        {
                            TexCore_EventCenter.Broadcast<string>(TexCore_EventType.显示标题, topicData.hintTexts[stepIndex]);
                        }
                        Submit();
                    }
                    else
                    {
                        haveScore = 0;
                        Submit();
                    }
                }
                else
                {
                    //TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, $"????????????????????????????", 1.5f);
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
