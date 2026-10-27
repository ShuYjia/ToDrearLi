using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;
using Bpf.Diagnostics;

/// <summary>
/// 完成状态
/// </summary>
public enum FinishState 
{ 
    未完成,
    等待触发,
    触发,
    已完成
}



/// <summary>
/// 该物体所属的题目和步骤
/// </summary>
[Serializable]
public class EnterTopic 
{
    public GameObject enterNode;
    public GameObject enterTopic;
    public int enterStep;

    internal int nodeIndex;
    internal int topicIndex;
    public EnterTopic(GameObject _enterNode,GameObject _enterTopic,int _enterIndex)
    {
        enterNode = _enterNode;
        enterTopic = _enterTopic;
        enterStep = _enterIndex;
    }
    
    //[Header("未完成事件")]
    //public UnityEvent OnUnFinished;

    //[Header("进行中事件")]
    //public UnityEvent OnUnderWay;

    //[Header("已完成事件")]
    //public UnityEvent OnFinish;
}


/// <summary>
/// 场景中交互物体基类
/// </summary>
public class TexCore_InteracterBase : MonoBehaviour
{
    public bool startIsActive = true;

    [Header("所属题目")]
    public EnterTopic enterTopic;

    [Header("物体状态")]
    public FinishState state;


    public void OnEnable()
    {
        if (startIsActive == false) return;
        TexCore_EventCenter.AddListener<GameObject>(TexCore_EventType.多物体无序操作, OnDisorderTrigger);

        //enterTopic.nodeIndex = Convert.ToInt32(enterTopic.enterNode.name.Substring(0, 2));
        //enterTopic.topicIndex = Convert.ToInt32(enterTopic.enterTopic.name.Substring(0, 2));
        //TexCore_EventCenter.AddListener<int, int, int>(TexCore_EventType.设置物体状态, MessageState);
        //TexCore_EventCenter.AddListener<int, int, int>(TexCore_EventType.触发物体, Trigger);
    }
    public void OnDisabel()
    {
        if (startIsActive == false) return;
        TexCore_EventCenter.RemoveListener<GameObject>(TexCore_EventType.多物体无序操作, OnDisorderTrigger);

        //TexCore_EventCenter.RemoveListener<int, int, int>(TexCore_EventType.设置物体状态, MessageState);
        //TexCore_EventCenter.RemoveListener<int, int, int>(TexCore_EventType.触发物体, Trigger);
    }


    public void MessageState(int _node, int _topic,int _step)
    {
        
        int nodeState = _node.CompareTo(enterTopic.nodeIndex);//确定是不是当前node
        int topicState = _topic.CompareTo(enterTopic.topicIndex);
        int stepState = _step.CompareTo(enterTopic.enterStep);

        if (nodeState == 0 && topicState == 0 && stepState == 0)//正在做当前题
        {
            SetState(FinishState.等待触发);
        }
        else if (nodeState < 0 || (nodeState == 0 && topicState < 0) || (nodeState == 0 && topicState == 0 && stepState < 0))   //正在做之前的题
        {
            SetState(FinishState.未完成);
        }
        else if (nodeState > 0 || (nodeState == 0 && topicState > 0) || (nodeState == 0 && topicState == 0 && stepState > 0))//正在做之后的题
        {
            SetState(FinishState.已完成);
        }
        BpfLog.Info(BpfLogCategory.Input, "tex_core_interacter_base.message_state", gameObject.name + state);
    }

    public void Trigger(int _node, int _topic, int _step)
    {
        int nodeState = _node.CompareTo(enterTopic.nodeIndex);//确定是不是当前node
        int topicState = _topic.CompareTo(enterTopic.topicIndex);
        int stepState = _step.CompareTo(enterTopic.enterStep);

        if (nodeState == 0 && topicState == 0 && stepState == 0)//正在做当前题
        {
            SetState(FinishState.触发);
        }
        
    }

    public void SetState(FinishState _state)
    {
        state = _state;
        switch (state)
        {
            case FinishState.未完成:
                UnFinished();
                break;
            case FinishState.等待触发:
                UnderWay();
                break;
            case FinishState.触发:
                OnTrigger();
                break;
            case FinishState.已完成:
                Finish();
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 未完成状态设置方法
    /// </summary>
    public virtual void UnFinished()
    {

    }
    /// <summary>
    /// 进行中状态设置方法
    /// </summary>
    public virtual void UnderWay()
    {

    }
    /// <summary>
    /// 触发方法
    /// </summary>
    public virtual void OnTrigger()
    {

    }
    /// <summary>
    /// 已完成状态设置方法
    /// </summary>
    public virtual void Finish()
    {

    }


    public virtual void OnDisorderTrigger(GameObject temp)
    {

    }
}
