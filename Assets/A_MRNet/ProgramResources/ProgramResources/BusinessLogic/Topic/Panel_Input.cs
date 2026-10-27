using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;
using System.Net.NetworkInformation;
using Bpf.Diagnostics;

/// <summary>
/// 输入题类型
/// </summary>
public enum InputEnem
{ 
    纯输入题,
    输入题夹其他
}

[Serializable]
public class DataClass_Input : SelfDataBase
{
    public List<string> texts;
}
public class Panel_Input : PanelTopicBase, IGetData
{
    public InputEnem inputEnem;

    public GameObject btn_Confirm;

    public Transform itemParent;

    public DataClass_Input selfData;

    public List<TextMeshProUGUI> texts;

    public List<GameObject> uiElements = new List<GameObject>();

    public override void Start()
    {
        topicData = (DataClass_Topic)datas.selfDatas[0];
        selfData = (DataClass_Input)datas.selfDatas[1];
        base.Start();
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

        if (selfData != null && topicData.tasks.Count > 0)
        {

            switch (base.topicData.topicType)
            {
                case TopicType.填空:
                    //Todo 展示工具
                    if (selfData.texts!=null&&selfData.texts.Count>0)
                    {
                        for (int i = 0; i < selfData.texts.Count; i++)
                        {
                            texts[i].text = selfData.texts[i];
                        }
                    }
                    break;
                default:
                    break;
            }
        }
        TexCore_EventCenter.Broadcast<string>(TexCore_EventType.显示标题, topicData.hintTexts[stepIndex]);

        //foreach (var item in uiElements)
        //{
        //    if (item.gameObject.GetComponent<TMP_InputField>())
        //    {
        //        item.gameObject.AddComponent<InputField_TMP_WebGL>();
        //    }
        //}
    }

    public override void OnClick(GameObject obj)
    {
        base.OnClick(obj);
        BpfLog.Info(BpfLogCategory.UI, "panel_input.on_click", "点击：" + datas.name + obj.name);
        switch (obj.name)
        {
            case "确定":
                CalculateScore();
                TexCore_EventCenter.Broadcast(TexCore_EventType.触发物体, nodeIndex, topicIndex, 0);
                break;
            //case "下一步":
            //    base.Next();
            //    break;
            default:
               
                break;
                
        }
    }

    public override void CalculateScore()
    {
        base.CalculateScore();
        steps = new List<string>();
        int a = 0;

        foreach (GameObject item in uiElements)
        {
            if (item.GetComponent<TMP_InputField>())
            {
                TMP_InputField tMP_InputField = item.GetComponent<TMP_InputField>();
                if (!string.IsNullOrEmpty(tMP_InputField.text))
                {
                    a += 1;
                }
                steps.Add(tMP_InputField.text);
            }
            else if (item.GetComponent<TMP_Dropdown>())
            {
                TMP_Dropdown tMP_dropdown = item.GetComponent<TMP_Dropdown>();
                steps.Add(tMP_dropdown.captionText.text);
            }
            else 
            {
                int index = -1;
                foreach (TexCore_Interaction_UGUI itemChild in item.GetComponentsInChildren<TexCore_Interaction_UGUI>())
                {
                    if(itemChild.isSelect)
                    {
                        index = itemChild.transform.GetSiblingIndex();
                    }
                }
                steps.Add(index.ToString());
            }
        }

        List<string> right = topicData.tasks;
        int rightCout = 0;
        if (right.Count==steps.Count)//操作的步骤和需要操作的步骤数量相等
        {
            for (int i = 0; i < steps.Count; i++)
            {
                if (right[i].Contains("~"))
                {
                    float min = float.Parse(right[i].Split('~')[0]);
                    float max = float.Parse(right[i].Split('~')[1]);
                    if (min <= float.Parse(steps[i]) && float.Parse(steps[i]) <= max) 
                    {
                        rightCout++;
                    }
                }
                else
                {
                    if (right[i].Equals(steps[i]))
                    {
                        rightCout++;
                        if (uiElements[i].transform.Find("错误标识") != null)
                            uiElements[i].transform.Find("错误标识").gameObject.SetActive(false);
                    }
                    else
                    {
                        switch (TexCore_Const.mode)
                        {
                            case Mode.练习:
                                if (uiElements[i].transform.Find("错误标识") != null)
                                    uiElements[i].transform.Find("错误标识").gameObject.SetActive(true);
                                break;
                            case Mode.考核:
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        switch (TexCore_Const.mode)
        {
            case Mode.练习://需要提示
                if (right.Count == rightCout)
                {
                    TexCore_EventCenter.Broadcast(TexCore_EventType.显示提示, "<color=green>恭喜，全部输入正确</color>", 1.5f);
                    haveScore = topicData.score;
                }
                else
                {
                    if (errorNum < 2)
                    {
                        errorNum++;
                        TexCore_EventCenter.Broadcast(TexCore_EventType.显示提示, $"<color=red>输入错误</color>\n错误的输入框已经变为红色\n错误<color=red>{errorNum}</color>次", 1.5f);
                        return;
                    }
                    if (errorNum == 2)
                    {
                        errorNum++;
                        TexCore_EventCenter.Broadcast(TexCore_EventType.显示提示, $"已经输入错误<color=red>3</color>次\n正确答案已经全部显示", 1.5f);
                        for (int i = 0; i < uiElements.Count; i++)
                        {
                            if (uiElements[i].GetComponent<TMP_InputField>())
                            {
                                TMP_InputField tMP_InputField = uiElements[i].GetComponent<TMP_InputField>();
                                tMP_InputField.text = right[i];
                            }
                            else if (uiElements[i].GetComponent<TMP_Dropdown>())
                            {
                                TMP_Dropdown tMP_dropdown = uiElements[i].GetComponent<TMP_Dropdown>();
                                for (int j = 0; j < tMP_dropdown.options.Count; j++)
                                {
                                    if (tMP_dropdown.options[j].text.Equals(right[i]))
                                    {
                                        tMP_dropdown.value = j;
                                    }
                                }
                            }
                            else
                            {

                                foreach (TexCore_Interaction_UGUI item in uiElements[i].GetComponentsInChildren<TexCore_Interaction_UGUI>()) 
                                {
                                    if (item.transform.GetSiblingIndex().ToString() == right[i]) 
                                    {
                                        item.SetState(SelectState.Click);
                                    }
                                }
                            }
                        }
                        haveScore = 0;
                    }
                }
                break;
            case Mode.考核://不需要提示
                if (right.Count == rightCout)
                {
                    haveScore = topicData.score;
                }
                else
                {
                    haveScore = 0;
                }
                break;
        }
        Submit();
    }

    public override void Submit()
    {
        base.Submit();
        btn_Next.SetActive(true);
        btn_Confirm.SetActive(false);
    }

    public TexCore_DataClass GetData()
    {
        datas.selfDatas = new List<SelfDataBase>();
        datas.selfDatas.Add(topicData);
        datas.selfDatas.Add(selfData);
        return datas;
    }
}
