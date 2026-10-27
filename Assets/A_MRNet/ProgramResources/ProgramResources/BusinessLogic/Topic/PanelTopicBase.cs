using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using System.Xml.Serialization;
using Unity;
using DG.Tweening;
using Bpf.Diagnostics;

public enum TopicType//题目类型（模板预制体）
{
    文本选择,
    图片选择,
    模型简单选择,
    模型操作,
    无序模型操作,
    连线,
    填空,
    对话,
    画线,
    放置,
    无序,
    视频
}
[Serializable]
public class DataClass_Topic : SelfDataBase 
{
    public List<string> hintTexts;
    public List<string> hintAudios;

    [XmlElement("场景数据")]
    public SceneData sceneData;

    public TopicType topicType;
    public string rightTasks;
    public int score;
    public List<string> tasks;//选项，步骤

}
public class PanelTopicBase : TexCore_PlaneBase,IPanelInit
{
    [Header("题目面板基类数据")]
    public TexCore_PlaneBase controlPlane;//持有上一级面板
    public DataClass_Topic topicData;//题目数据
    public List<string> steps;//已选选项，已做步骤
    public TextMeshProUGUI topicTitle;//UI题目内的标题
    public GameObject btn_Next;//题目内下一步
    public IProperties selfObjs;//操作物体
    [Space()]
    public int stepIndex = 0;//执行到第几步
    public int haveScore = 0;//得分
    public int errorNum = 0;//错误次数
    [Space()]
    public string operater;//?
    public string correctText;//选择正确解答
    public string mistakeText;//选择错误解答
    [Space()]
    public bool trigger = false;///?
    public bool isSubmit = false;//是否已经完成该步骤
    public bool isHeadline = false;//true_使用题目内标题,false，使用公共标题
    public bool isShow;//是否显示知识库
    private void Awake()
    {
        if (btn_Next == null)
        {
            btn_Next = transform.Find("下一步").gameObject;
        }
    }
    public virtual void Start()
    {
        if (!isHeadline)
        {
            TexCore_EventCenter.Broadcast<bool>(TexCore_EventType.开关标题, true);
            TexCore_EventCenter.Broadcast<string>(TexCore_EventType.显示标题, topicData.hintTexts[stepIndex]);
        }
        else
        {
            TexCore_EventCenter.Broadcast<bool>(TexCore_EventType.开关标题, false);
            if (topicTitle!=null)
            {
                topicTitle.text = topicData.hintTexts[stepIndex];
            }
        }
        switch (TexCore_Const.mode)
        {
            case Mode.练习:
                //KnowledgeManager.self.SetNode(datas.name);
                break;
            case Mode.考核:
                //KnowledgeManager.self.gameObject.transform.parent.gameObject.SetActive(false);
                break;
            default:
                break;
        }

        //将相机移动到每道题需要在的位置上
        //topicData.sceneData.ReadTrans();
        //if (topicData.sceneData.pos != Vector3.zero)
        //{
        //    TexCore_PlaneManager.self.mainCamera.transform.DOMove(topicData.sceneData.pos, 1f);
        //    TexCore_PlaneManager.self.mainCamera.transform.DORotate(topicData.sceneData.rot, 1f);
        //}
        nodeIndex = (controlPlane.datas.GetIndexSelf());
        topicIndex = (datas.GetIndexSelf());
        topicIndexesResolved = true;
        TexCore_EventCenter.Broadcast<int, int, int>(TexCore_EventType.设置物体状态, nodeIndex, topicIndex, 0);
        NotifyStepVoiceChanged();
    }
    protected int nodeIndex;
    protected int topicIndex;

    /// <summary>
    /// False until <see cref="Start"/> has resolved the node and topic indexes.
    /// OnEnable runs before Start on first activation, and announcing a step
    /// before the indexes are known would build a wrong <see cref="StepVoiceKey"/>.
    /// </summary>
    private bool topicIndexesResolved;

    /// <summary>
    /// Reports the current step to the step-voice coordinator. Panels call this
    /// after every step change, and it doubles as the first-entry notification.
    /// </summary>
    public void NotifyStepVoiceChanged()
    {
        NotifyStepVoiceChanged(stepIndex);
    }

    /// <summary>
    /// Reports a specific step index. Use this when the caller has already
    /// advanced <see cref="stepIndex"/> and needs the new value announced.
    /// </summary>
    public void NotifyStepVoiceChanged(int changedStepIndex)
    {
        if (topicData == null || !topicIndexesResolved)
        {
            return;
        }

        // Self bootstraps the shared entry point at runtime; it is null outside
        // play mode, where there is nothing to announce.
        TexCore_StepVoiceCoordinator coordinator = TexCore_StepVoiceCoordinator.Self;
        if (coordinator == null)
        {
            return;
        }

        coordinator.NotifyStepChanged(this, nodeIndex, topicIndex, changedStepIndex);
    }

    public virtual void OnEnable()
    {
        if (isSubmit)
        {
            btn_Next.SetActive(true);
        }
        else
        {
            btn_Next.SetActive(false);
            steps = new List<string>();
        }
        TexCore_EventCenter.Broadcast<string>(TexCore_EventType.更新题目, datas.name);
        TexCore_EventCenter.Broadcast(TexCore_EventType.关闭提示);
        // Entering a topic starts a fresh narration context: the same step index
        // reached again after a rollback or a re-entry is allowed to replay.
        if (TexCore_StepVoiceCoordinator.Self != null)
        {
            TexCore_StepVoiceCoordinator.Self.ResetRecall();
            NotifyStepVoiceChanged();
        }
        //My_PBSManager.self.OnlySetActive(datas.name);
        //TexCore_PBSManager.self.OnlySetActive(datas.name);
        if (selfObjs != null && selfObjs._properties.Length > 0 && selfObjs._properties[0] != null)
        {
           // TexCore_PlaneManager.self.ResetObjPos(selfObjs._properties[0].value.transform, datas.name);
        }
        //TexCore_EventCenter.Broadcast<string>(TexCore_EventType.显示标题, topicData.hintTexts[stepIndex]);
        if (!isShow) return;
        KnowledgeManager.Self.SetNode(datas.name);
        KnowledgeManager.Self.transform.GetChild(0).gameObject.SetActive(true);
    }
    public virtual void OnDisable()
    {
        if (TexCore_StepVoiceCoordinator.self != null)
        {
            TexCore_StepVoiceCoordinator.self.Stop();
        }

        foreach (Property item in selfObjs._properties)
        {
            if (item != null&&item.value!=null)
            {
                item.value?.SetActive(false);
            }
        }
    }
    public virtual void CalculateScore()
    {
        
    }

    public PanelTopicBase GetTopicScript()
    {
        BpfLog.Error(BpfLogCategory.UI, "panel_topic_base.get_topic_script", null, null, BpfLogField.Of("topic_type", topicData.topicType));
        switch (topicData.topicType)
        {
            case TopicType.文本选择:
            case TopicType.图片选择:
            case TopicType.模型简单选择:
                return gameObject.GetComponent<Panel_Select>();
            case TopicType.模型操作:
                return gameObject.GetComponent<Panel_Operate>();
            case TopicType.无序模型操作:
                return gameObject.GetComponent<Panel_DistributeOperate>();
            case TopicType.连线:
                return null;
            case TopicType.填空:
                return gameObject.GetComponent<Panel_Input>();
            case TopicType.对话:
                return gameObject.GetComponent<Panel_Dialogue>();
            case TopicType.画线:
                return gameObject.GetComponent<Panel_Drawing>();
            case TopicType.放置:
                return gameObject.GetComponent<Panel_Place>();
            case TopicType.无序:
                return gameObject.GetComponent<Panel_OtherOperations>();
            case TopicType.视频:
                return gameObject.GetComponent<TexCore_VideoTopic>();
            default:
                break;
        }
        return null;
    }
    public virtual void Submit()
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
            exam.Question =datas. name;//题干
            exam.QuestionType = 3.ToString();

            string answer = "";
            for (int i = 0; i < steps.Count; i++)
            {
                if (i != steps.Count - 1)
                    answer += steps[i] + "_";
                else
                    answer += steps[i];
            }
            exam.Answers = answer;


            exam.IsCorrect = haveScore == topicData.score ? "1" : "0"; ;
            exam.Mark = haveScore;
            exam.Correct = topicData.rightTasks;

           
            TexCore_ExamData.self.AddAnswer( exam);
           
        }

    }
    public virtual void Next()
    {
        
    }

    public IEnumerator PanelInit()
    {
       
        
        yield return 0;
    }
}
