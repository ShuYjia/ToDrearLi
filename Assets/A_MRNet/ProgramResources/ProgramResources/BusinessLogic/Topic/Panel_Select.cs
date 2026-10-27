using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using Unity.VisualScripting;


public enum SelecionCount//选择题题型
{
    单选,
    多选,
    不定项选择
}
[Serializable]
public class DataClass_Option : SelfDataBase
{
    public SelecionCount selecionCount;
}
public class Panel_Select : PanelTopicBase,IGetData
{
    [Header("选择题数据")]
    public DataClass_Option selfData;//选择题类型
    public GameObject btn_Confirm;//题目内确定
    public Transform itemParent;//选择题_选项模板父物体

    /// <summary>
    /// 缓存题目内“确定”按钮，并在第一帧就把它压成隐藏。
    /// </summary>
    /// <remarks>
    /// 选择题.prefab 里嵌套的“确定”默认是激活的，实例化的同一帧就会渲染出
    /// 白底绿字按钮。预制体保留了 m_IsActive: 0 覆盖，Awake 再兜底隐藏一次，
    /// 保证第一帧不闪现；提交改由“选完即自动提交”完成，按钮不再作为交互入口。
    /// </remarks>
    private void Awake()
    {
        if (btn_Confirm == null)
        {
            Transform confirm = transform.Find("确定");
            if (confirm != null)
            {
                btn_Confirm = confirm.gameObject;
            }
        }

        HideConfirmButton();
    }

    /// <summary>
    /// 进入题目时保持题目内“确定”隐藏；提交不再经过该按钮。
    /// </summary>
    public override void OnEnable()
    {
        base.OnEnable();
        HideConfirmButton();
    }

    /// <summary>
    /// 隐藏题目内“确定”按钮。只改激活状态，不改动父物体与业务物体。
    /// </summary>
    private void HideConfirmButton()
    {
        if (btn_Confirm != null && btn_Confirm.activeSelf)
        {
            btn_Confirm.SetActive(false);
        }
    }

    public override void Start()
    {
        topicData = (DataClass_Topic)datas.selfDatas[0];
        selfData = (DataClass_Option)datas.selfDatas[1];
        base.Start();
        if (selfData != null && topicData.tasks.Count > 0) 
        {
            switch (base.topicData.topicType)
            {
                case TopicType.文本选择:
                    for (int i = 0; i < base.topicData.tasks.Count; i++)
                    {
                        string option = base.topicData.tasks[i];
                        GameObject newItem;
                        if (i == 0)
                             newItem =  itemParent.GetChild(0).gameObject;
                        else
                             newItem = Instantiate(itemParent.GetChild(0).gameObject, itemParent);

                        newItem.name = option;
                        if (selfData.selecionCount == SelecionCount.多选 || selfData.selecionCount == SelecionCount.不定项选择)
                        {
                            newItem.GetComponent<TexCore_SelectBase>().itemParent = null;
                        }
                        else
                        {
                            newItem.GetComponent<TexCore_SelectBase>().itemParent = itemParent;
                        }
                        newItem.GetComponentInChildren<TextMeshProUGUI>().text = option.Replace("\\n", "\n");
                    }
                    
                    LayoutRebuilder.ForceRebuildLayoutImmediate(itemParent.GetComponent<RectTransform>());
                    break;
                case TopicType.图片选择:
                    break;
                case TopicType.模型简单选择:
                    break;
                default:
                    break;
            }
        }
        switch (selfData.selecionCount)
        {
            case SelecionCount.单选:
                if (itemParent != null && itemParent.childCount > 0)
                {
                    for (int i = 0; i < itemParent.childCount; i++)
                    {
                        //TexCore_SelectBase selectBase = itemParent.GetChild(i).GetComponent<TexCore_SelectBase>();
                        //selectBase.itemParent = itemParent;
                    }
                }
                break;
            case SelecionCount.多选:
            case SelecionCount.不定项选择:
                if (itemParent != null && itemParent.childCount > 0)
                {
                    for (int i = 0; i < itemParent.childCount; i++)
                    {
                        //TexCore_SelectBase selectBase = itemParent.GetChild(i).GetComponent<TexCore_SelectBase>();
                        //selectBase.itemParent = null;
                    }
                }
                break;
            default:
                break;
        }

        //TexCore_EventCenter.Broadcast<string>(TexCore_EventType.播放音频, datas.GetUrl() + topicData.hintAudios[stepIndex]);
    }



    public override void OnClick(GameObject obj)
    {
        base.OnClick(obj);

        //if (TexCore_Const.ConfirmCharacter(character) == false) return;

        Print.Edit("点击："+datas.name + obj.name);

        if (isSubmit || topicData == null)
        {
            // 已提交的题目不再重复评分，避免选项点击把同一题再次入队推进。
            // 题目数据尚未解析时（Start 之前）也不评分，防止空引用。
            return;
        }

        if (obj.name.Equals("下一步"))
        {
            //if (controlPlane == null)
            //{
            //    TexCore_PlaneManager.self.LoadPlane(datas.NextData().name, datas.parent.name);
            //}
        }
        switch (obj.name)
        {
            case "确定":
                CalculateScore();
                break;
            //case "下一步":
            //    base.Next();
            //    break;
            default:
                

                if (steps.Contains(obj.name))
                {
                    //if (obj.GetComponent<TexCore_Interaction_UGUI>())
                    //{
                    //    if (obj.GetComponent<TexCore_Interaction_UGUI>().isUnSelect)
                    //        TexCore_EventCenter.Broadcast(TexCore_EventType.显示提示, "取消选择" + obj.name, 1.5f);
                    //}
                    
                    steps.Remove(obj.name);
                    return;
                }
                //if (obj.GetComponent<TexCore_Interaction_UGUI>())
                //{
                //    if (obj.GetComponent<TexCore_Interaction_UGUI>().isUnSelect)
                //        TexCore_EventCenter.Broadcast(TexCore_EventType.显示提示, "选择" + obj.name, 1.5f);
                //}

                switch (GetSelectionCount())
                {
                    case SelecionCount.单选:
                        steps = new List<string>();
                        break;
                    case SelecionCount.多选:
                    case SelecionCount.不定项选择:
                        break;
                    default:
                        break;
                }
                steps.Add(obj.name);

                // “确定”按钮不再显示，选择完成即自动提交。
                TryAutoSubmit();

                break;
        }
    }

    /// <summary>
    /// 选完即提交，替代原来的题目内“确定”按钮。
    /// </summary>
    /// <remarks>
    /// 单选：选中即评测；多选/不定项：选满配置的答案数量才评测。
    /// 练习模式答错且还没到错误上限时，<see cref="CalculateScore"/> 会直接
    /// 返回，因此这里不会提交、不会广播“完成题目”，题目保持可重答；
    /// 答对或达到错误上限才真正提交并触发场景自动跳题。
    /// 考核模式与原来的评分方式一致，直接提交。
    /// </remarks>
    private void TryAutoSubmit()
    {
        if (isSubmit || topicData == null || !HasCompletedSelection())
        {
            return;
        }

        CalculateScore();
    }

    /// <summary>
    /// 判断选择是否已经可以评测。单选选中任意一项即可；多选/不定项需要
    /// 选满 <see cref="DataClass_Topic.rightTasks"/> 配置的答案数量。
    /// </summary>
    private bool HasCompletedSelection()
    {
        if (steps == null || steps.Count == 0)
        {
            return false;
        }

        if (GetSelectionCount() == SelecionCount.单选)
        {
            return true;
        }

        int requiredCount = GetRightAnswerCount();
        return requiredCount > 0 && steps.Count >= requiredCount;
    }

    private int GetRightAnswerCount()
    {
        if (topicData == null || string.IsNullOrEmpty(topicData.rightTasks))
        {
            return topicData != null && topicData.tasks != null ? topicData.tasks.Count : 0;
        }

        return topicData.rightTasks.Split('_').Length;
    }

    /// <summary>
    /// 读取题型。<see cref="selfData"/> 在 <see cref="Start"/> 里才从配置数据解析，
    /// 早于 Start 的点击（或缺失配置）按单选处理，避免空引用中断答题流程。
    /// </summary>
    private SelecionCount GetSelectionCount()
    {
        return selfData != null ? selfData.selecionCount : SelecionCount.单选;
    }

    public bool isTask(string objName)
    {
        foreach (var step in topicData.tasks)
        {
            if(step.Equals(objName))
            {
                return true; 
            }
        }
        return false;
    }

    public override void CalculateScore()
    {
        base.CalculateScore();

        string[] rights = topicData.rightTasks.Split('_');//多选正确答案
        List<string> right = new List<string>(rights);

        int rightCout = 0;//正确数量
        for (int i = 0; i < steps.Count; i++)
        {
            if (right.Contains(steps[i]))//判断步骤里面有多少个正确答案
            {
                rightCout++;
            }
        }
        string t = "";//存储正确答案
        foreach (string item in right)
        {
            t += item + "  ";
        }

        bool isRight = right.Count == rightCout && steps.Count == right.Count;
        switch (TexCore_Const.mode)
        {
            case Mode.练习://需要提示
                switch (selfData.selecionCount)
                {
                    case SelecionCount.单选:
                    case SelecionCount.多选:
                    case SelecionCount.不定项选择:
                        if (isRight)
                        {
                            TexCore_EventCenter.Broadcast(TexCore_EventType.显示提示, "<color=green>恭喜，选择正确</color>", 1.5f);
                            haveScore = topicData.score;
                        }
                        else
                        {
                            if (errorNum < 2)
                            {
                                errorNum++;
                                //TexCore_EventCenter.Broadcast(TexCore_EventType.显示提示, $"<color=red>选择错误</color>\n<color=green>正确提示：</color>{mistakeText}\n错误<color=red>{errorNum}</color>次", 1.5f);
                                TexCore_EventCenter.Broadcast(TexCore_EventType.显示提示, $"<color=red>选择错误</color>\n错误<color=red>{errorNum}</color>次", 1.5f);
                                // 练习模式保留原有重答次数：本次错误不提交题目，
                                // 也不广播“完成题目”，否则场景流程会立刻跳到下一题。
                                HideConfirmButton();
                                return;
                            }
                            if (errorNum == 2)
                            {
                                errorNum++;
                                TexCore_EventCenter.Broadcast(TexCore_EventType.显示提示, $"已经选择错误<color=red>3</color>次\n正确答案是：\n{t}", 1.5f);
                                haveScore = 0;
                            }
                        }
                        break;
                    default:
                        break;
                }
                break;
            case Mode.考核://不需要提示
                switch (selfData.selecionCount)
                {
                    case SelecionCount.单选:
                    case SelecionCount.多选:
                    case SelecionCount.不定项选择:
                        haveScore = isRight ? topicData.score : 0;

                        break;
                    default:
                        break;
                }
                break;
        }
        Submit();
    }

    public override void Submit()
    {
        if (isSubmit)
        {
            return;
        }

        base.Submit();
        if (itemParent!=null&&itemParent.childCount>0)
        {
            for (int i = 0; i < itemParent.childCount; i++)
            {
                Collider[] colliders = itemParent.GetChild(i).GetComponentsInChildren<Collider>();
                foreach (Collider item in colliders)
                {
                    item.enabled = false;
                }
            }
        }
        TexCore_EventCenter.Broadcast(TexCore_EventType.触发物体, nodeIndex, topicIndex, 0);
        HideConfirmButton();
        if (btn_Next != null)
        {
            btn_Next.SetActive(true);
        }
    }

    public TexCore_DataClass GetData()
    {
        datas.selfDatas = new List<SelfDataBase>();
        datas.selfDatas.Add(topicData);
        datas.selfDatas.Add( selfData);
        return datas;
    }
}
