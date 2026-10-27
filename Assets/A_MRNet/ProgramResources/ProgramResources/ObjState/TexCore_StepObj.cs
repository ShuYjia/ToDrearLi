using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.VisualScripting;
using TMPro;
using Bpf.Diagnostics;

[Serializable]
public class NodeObjs
{
    public GameObject nodePrefab;
    public List<TopicObjs> nodeObjs;
}
[Serializable]
public class TopicObjs
{
    public GameObject topicPrefab;
    public List<StepObjs> topicObjs;
}
[Serializable]
public class StepObjs
{
    public int stepIndex;
    public List<GameObject> stepObjs;

    private List<TexCore_InteracterBase> stepScripts;

    public List<TexCore_InteracterBase> StepScripts 
    {
        get
        {
            if (stepScripts == null)
            {
                stepScripts = new List<TexCore_InteracterBase>();
                foreach (var item in stepObjs)
                {
                    TexCore_InteracterBase[] s = item.GetComponents<TexCore_InteracterBase>();
                    foreach (var items in s)
                    {
                        stepScripts.Add(items);
                    }
                }
                    
            }
            return stepScripts;
        }
        private set { }
    }

    /// <summary>
    /// Clears the reporter cache after a runtime level bootstrap replaces the
    /// serialized step object list.
    /// </summary>
    public void InvalidateStepScripts()
    {
        stepScripts = null;
    }
}

public class TexCore_StepObj : MonoBehaviour
{
    public TextMeshProUGUI log;
    public List<NodeObjs> interacterObjs;

    public TexCore_InteracterBase activeInteracter;

    public void OnEnable()
    {
        
        TexCore_EventCenter.AddListener<int, int, int>(TexCore_EventType.设置物体状态, MessageState);
        TexCore_EventCenter.AddListener<int, int, int>(TexCore_EventType.触发物体, Trigger);


    }
    public void OnDisabel()
    {
        
        TexCore_EventCenter.RemoveListener<int, int, int>(TexCore_EventType.设置物体状态, MessageState);
        TexCore_EventCenter.RemoveListener<int, int, int>(TexCore_EventType.触发物体, Trigger);
    }
    void OnDestroy()
    {
        TexCore_EventCenter.RemoveAllListener();
    }
    public void MessageState(int _node, int _topic, int _step)
    {
        for (int i = 0; i < interacterObjs.Count; i++)
        {
            for (int j = 0; j < interacterObjs[i].nodeObjs.Count; j++)
            {
                for (int a = 0; a < interacterObjs[i].nodeObjs[j].topicObjs.Count; a++)
                {

                    //print(string.Format( "node:{0}  topic:{1}  step:{2}",i,j,a));

                    foreach (var item in interacterObjs[i].nodeObjs[j].topicObjs[a].StepScripts)
                    {
                        int nodeState = _node.CompareTo(i);//确定是不是当前node
                        int topicState = _topic.CompareTo(j);
                        int stepState = _step.CompareTo(interacterObjs[i].nodeObjs[j].topicObjs[a].stepIndex);
                        if (nodeState == 0 && topicState == 0 && stepState == 0)//正在做当前题
                        {

                        }
                        else if (nodeState < 0 || (nodeState == 0 && topicState < 0) || (nodeState == 0 && topicState == 0 && stepState < 0))   //正在做之前的题
                        {

                        }
                        else if (nodeState > 0 || (nodeState == 0 && topicState > 0) || (nodeState == 0 && topicState == 0 && stepState > 0))//正在做之后的题
                        {
                            item.SetState(FinishState.已完成);
                            //print("执行" + item.transform.parent.name + "__" + item.state);
                        }

                    }
                }
            }
        }
        for (int i = interacterObjs.Count - 1; i >= 0; i--)
        {
            for (int j = interacterObjs[i].nodeObjs.Count - 1; j >= 0; j--)
            {
                for (int a = interacterObjs[i].nodeObjs[j].topicObjs.Count - 1; a >= 0; a--)
                {

                    //print(string.Format("node:{0}  topic:{1}  step:{2}", i, j, a));
                    foreach (var item in interacterObjs[i].nodeObjs[j].topicObjs[a].StepScripts)
                    {
                        int nodeState = _node.CompareTo(i);//确定是不是当前node
                        int topicState = _topic.CompareTo(j);
                        int stepState = _step.CompareTo(interacterObjs[i].nodeObjs[j].topicObjs[a].stepIndex);

                        if (nodeState == 0 && topicState == 0 && stepState == 0)//正在做当前题
                        {
                            item.SetState(FinishState.等待触发);
                            //print("执行" + item.transform.parent.name + "__" + item.state);
                        }
                        else if (nodeState < 0 || (nodeState == 0 && topicState < 0) || (nodeState == 0 && topicState == 0 && stepState < 0))   //正在做之前的题
                        {
                            item.SetState(FinishState.未完成);
                            //print("执行" + item.transform.parent.name + "__" + item.state);
                        }
                        else if (nodeState > 0 || (nodeState == 0 && topicState > 0) || (nodeState == 0 && topicState == 0 && stepState > 0))//正在做之后的题
                        {

                        }


                    }
                }
            }
        }
    }

    public void Trigger(int _node, int _topic, int _step)
    {
        if (interacterObjs[_node].nodeObjs[_topic].topicObjs.Count <= _step)
            return;
        foreach (var item in interacterObjs[_node].nodeObjs[_topic].topicObjs[_step].StepScripts)
        {
            item.SetState(FinishState.触发);
            BpfLog.Info(BpfLogCategory.Scene, "step.entered", "执行步骤", null,
                BpfLogField.Of("step", item.transform.parent.name),
                BpfLogField.Of("state", item.state));
        }

        //int nodeState = _node.CompareTo(enterTopic.nodeIndex);//确定是不是当前node
        //int topicState = _topic.CompareTo(enterTopic.topicIndex);
        //int stepState = _step.CompareTo(enterTopic.enterStep);

        //if (nodeState == 0 && topicState == 0 && stepState == 0)//正在做当前题
        //{
        //    SetState(FinishState.触发);
        //}

    }


}
