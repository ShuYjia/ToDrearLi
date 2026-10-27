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
public class Panel_DistributeOperate : PanelTopicBase, IGetData
{
    public GameObject btn_Confirm;

    /// <summary>
    /// 控制题目是否打开下一步按钮
    /// </summary>
    public bool isNextShow;
    /// <summary>
    /// 错误提示
    /// </summary>
    public bool isFalseHint;

    public float hodelNextStepTime = 0;


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
        topicData = (DataClass_Topic)datas.selfDatas[0];
        base.Start();
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

        BpfLog.Info(BpfLogCategory.UI, "panel_distribute_operate.on_click", "点击：" + datas.name + obj.name);

        trigger = false;
        if (stepIndex >= topicData.tasks.Count) return;

        string[] task= topicData.tasks[stepIndex].Split('/');

        if (task.Length>=2)
        {
            if (errorNum < 2) 
            {
                if(ToolsManager.self.t == null)
                {
                    errorNum++;
                    TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, $"<color=red>请先选择工具再操作</color>\n错误<color=red>{errorNum}</color>次", 1.5f);
                    return;
                }
                if (!ToolsManager.self.t.name.Substring(2).Equals(task[0]) && errorNum != 2)
                {
                    errorNum++;
                    TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, $"<color=red>工具选择</color>\n错误<color=red>{errorNum}</color>次", 1.5f);
                    return;
                }
                if(!obj.name.Equals(task[1]))
                {
                    errorNum++;
                    //TexCore_EventCenter.Broadcast(TexCore_EventType.显示提示, $"<color=red>选择错误</color>\n<color=green>正确提示：</color>{mistakeText}\n错误<color=red>{errorNum}</color>次", 1.5f);
                    TexCore_EventCenter.Broadcast(TexCore_EventType.显示提示, $"<color=red>选择错误</color>\n错误<color=red>{errorNum}</color>次", 1.5f);
                    return;
                }

            }
            if (errorNum == 2)
            {
                errorNum++;
                TexCore_EventCenter.Broadcast(TexCore_EventType.显示提示, $"已经选择错误<color=red>3</color>次\n正确答案是：\n工具：{task[0]}\t需要操作物体：{task[1]}", 1.5f);
                TexCore_EventCenter.Broadcast(TexCore_EventType.触发物体, nodeIndex, topicIndex, stepIndex);
                operater = obj.name;
                trigger = true;
                string toolName = "错误工具";
                if (ToolsManager.self.t != null)  
                {
                    toolName = ToolsManager.self.t.name;
                }
                steps.Add(toolName + "/" + obj.name);
                stepIndex += 1;
                if (stepIndex < topicData.hintTexts.Count) //更新下一步的提示
                {
                    TexCore_EventCenter.Broadcast<string>(TexCore_EventType.显示标题, topicData.hintTexts[stepIndex]);
                }
                Invoke("NextStepState", hodelNextStepTime);
                errorNum = 0;
                if (ToolsManager.self != null) ToolsManager.self.Close();


            }
            else if(obj.name.Equals(task[1]))//选对工具且操作时正在使用工具，进入下一步
            {
                operater = obj.name;
                trigger = true;

                TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, "操作正确", 1.5f);
                steps.Add(topicData.tasks[stepIndex]);
                stepIndex += 1;
                if (stepIndex < topicData.hintTexts.Count) //更新下一步的提示
                {
                    TexCore_EventCenter.Broadcast<string>(TexCore_EventType.显示标题, topicData.hintTexts[stepIndex]);
                }

                BpfLog.Error(BpfLogCategory.UI, "panel_distribute_operate.on_click", null, null, BpfLogField.Of("step_index", stepIndex));
                BpfLog.Error(BpfLogCategory.UI, "panel_distribute_operate.on_click", null, null, BpfLogField.Of("count", topicData.tasks.Count));

                //只有所有任务全完成了
                if (stepIndex == topicData.tasks.Count)
                {
                    TexCore_EventCenter.Broadcast(TexCore_EventType.触发物体, nodeIndex, topicIndex, stepIndex);
                    Invoke("NextStepState", hodelNextStepTime);
                    if (ToolsManager.self != null) ToolsManager.self.Close();
                }
                else { 
                    GameObject temp = ToolObjectDragger.self.selectedObject;
                    TexCore_EventCenter.Broadcast(TexCore_EventType.多物体无序操作, temp);
                }




                //operater = obj.name;
                //trigger = true;
                //TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, "操作正确", 1.5f);
                //steps.Add(topicData.tasks[stepIndex]);
                //stepIndex += 1;
                //if (stepIndex < topicData.hintTexts.Count) //更新下一步的提示
                //{
                //    TexCore_EventCenter.Broadcast<string>(TexCore_EventType.显示标题, topicData.hintTexts[stepIndex]);
                //}
                //if (stepIndex < topicData.hintAudios.Count) 
                //{ 
                //    TexCore_EventCenter.Broadcast<string>(TexCore_EventType.播放音频, datas.GetUrl() + topicData.hintAudios[stepIndex]);
                //}
                //Invoke("NextStepState", hodelNextStepTime);
                //if (ToolsManager.self != null) ToolsManager.self.Close();

            }
        }
        else
        {
            //不带工具步骤
            if (obj.name.Equals(topicData.tasks[stepIndex]))//如果点击的是当前步骤需要操作的物体
            {
                TexCore_EventCenter.Broadcast(TexCore_EventType.触发物体, nodeIndex, topicIndex, stepIndex);
                operater = obj.name;
                trigger = true;
                if(topicData.score!=0)
                {
                    TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, "操作正确", 1.5f);
                }
                steps.Add(topicData.tasks[stepIndex]);
                stepIndex += 1;
                if (stepIndex < topicData.hintTexts.Count) //更新下一步的提示
                {
                    TexCore_EventCenter.Broadcast<string>(TexCore_EventType.显示标题, topicData.hintTexts[stepIndex]);
                }
                Invoke("NextStepState", hodelNextStepTime);
            }
            else
            {
                if (isFalseHint)
                {
                    //btn_Next.SetActive(true);
                    TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, "操作错误，请重新操作", 1.5f);
                }
            }
        }
        if (stepIndex >= topicData.tasks.Count)//如果操作完成
        {
            CalculateScore();
            //btn_Next.SetActive(true);
        }
    }

    public void NextStepState()
    {
        if(gameObject.activeInHierarchy)
        {
            TexCore_EventCenter.Broadcast(TexCore_EventType.设置物体状态, nodeIndex, topicIndex, stepIndex);
        }
    }

    public override void CalculateScore()
    {
        if (steps == null || steps.Count <= 0)//如果没有操作完成
        {
            TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, "请操作后再继续", 1.5f);
            return;
        }
        base.CalculateScore();

        //TexCore_FuncLibrary.instance.CallFunc(operates[0].text, this);
        StartCoroutine(CaluclateScore());
    }
    IEnumerator CaluclateScore()
    {
        yield return null;
        string[] rights = topicData.tasks.ToArray();
        List<string> right = new List<string>(rights);

        if (right.Count == steps.Count)//操作的步骤和需要操作的步骤数量相等
        {
            int rightCout = 0;
            for (int i = 0; i < steps.Count; i++)
            {
                if (right.Contains(steps[i]))
                {
                    rightCout++;
                }
            }
            if (right.Count == rightCout)
            {
                TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, "恭喜，回答正确！", 1.5f);
                haveScore = topicData.score;
            }
            else
            {
                string t = "";
                foreach (string item in right)
                {
                    t += item + "  ";
                }
                TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, "操作错误，正确答案是：\n" + t, 1.5f);
                haveScore = 0;
            }
        }
        else//操作的步骤和需要操作的步骤数量不相等
        {
            string t = "";
            foreach (string item in right)
            {
                t += item + "  ";
            }
            TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, "操作错误，正确答案是：\n" + t, 1.5f);
            haveScore = 0;
        }

        Submit();
    }
    public override void Submit()
    {
        //base.Submit();
        SubmitConfig();
        if (btn_Confirm != null) btn_Confirm.SetActive(false);
        if (isNextShow)
        {
            StartCoroutine(ShowNext());
        }
        else
        {
            btn_Next.GetComponent<TexCore_Interaction_UGUI>().OnClick();
        }
    }

    public IEnumerator ShowNext()
    {
        yield return new WaitForSeconds(hodelNextStepTime);
        btn_Next.SetActive(true);
    }
    public  void SubmitConfig()
    {
        TexCore_EventCenter.Broadcast<string>(TexCore_EventType.完成题目, datas.name);
        if (isSubmit == false)
        {
            isSubmit = true;
        }
        //Invoke("SubMit", 0.5f);
        if (TexCore_Const.mode ==Mode.考核)
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
