using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml.Serialization;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;
using Unity;
using System.Linq;
using Bpf.Diagnostics;

[Serializable]
public class MutilLineIndex 
{
    public int startIndex;
    public int endIndex;
    public List<int> indexs;
}
[Serializable]
public class ActiveOperate
{
    public MutilLineIndex line;
    public int activeIndex = 0;
    public Panel_Operate active;
    public bool isEnter = false;

    public ActiveOperate(PanelTopicBase topicBase)
    {
        active = (Panel_Operate)topicBase.GetTopicScript();
    }
    public ActiveOperate(MutilLineIndex line)
    {
        this.line = line;
    }

    public bool Contain(int index)
    {
        for (int i = 0; i < line.indexs.Count; i++)
        {
            if (line.indexs[i] == index) return true;
        }
        return false;
    }

    public int NextIndex()
    {
        
        if (line.indexs[ activeIndex] >= line.endIndex)
        {
            isEnter = true;
            return -1;
        }
        activeIndex++;
        return line.indexs[activeIndex];
    }
    public void SetActive(PanelTopicBase topicBase)
    {
        active = (Panel_Operate)topicBase.GetTopicScript();
    }
}


    public class MutilNodePanelBase : TexCore_PlaneBase
{
    public Transform childNodeParent;//将所有子节点生成到这个Transform下面

    public List<PanelTopicBase> childTopics;//加载过的子节点都会存到这个列表中，缓存池
    //public int childIndex = 0;//当前正在操作的子节点索引

    public List<ActiveOperate> activeChild;//当前正在操作的子节点

    public GameObject btn_Next;//下一步按钮，点击跳转到下一个Node面板

    //public List<GameObject> selfObjs;//这个面板所有需要操作的物体

    public IProperties selfObjs;//这个面板所有需要操作的物体
   
    public List<MutilLineIndex> mutilLine;

    public List<PBSMenu> PBSMenu;

    internal virtual void Awake()
    {

    }

    public virtual void OnEnable()
    {
        //My_PBSManager.self.OnlyNodeSetActive(datas.name);
        TexCore_EventCenter.Broadcast<List<PBSMenu>>(TexCore_EventType.更新目录, PBSMenu);
        TexCore_EventCenter.Broadcast<string>(TexCore_EventType.更新节点, datas.name);
        //TexCore_PBSManager.self.OnlyNodeSetActive(datas.name);
    }

    public virtual void Start()
    {

        btn_Next.SetActive(false);//关闭下一步按钮


        if (datas.childData.Count > 0)//如果有子节点
        {
            LoadChild(datas.childData[0].name);//加载第一个子节点
        }
    }
    public virtual void ResetSelfObjPos()//当该面板的场景跳转完成后，将私有物体中的三维对象都移动到场景中
    {

    }
    public virtual void OnChildOver()//当某一题做完时
    {

    }
    public virtual void OnChildBuid(PanelTopicBase topic)//当一个子节点被克隆出来后，做的第一件该题的初始化方法
    {

    }

    public virtual void OnChildStep(PanelTopicBase activeTopic,GameObject obj)//当某一题的某个步骤达成时
    {
        BpfLog.Info(BpfLogCategory.UI, "mutil_node_panel_base.on_child_step", "调用父类OnChildBuid1");
    }

    public virtual void NextTopic()//跳转到下一个子节点
    {
        OnChildOver();
        int nextOperate = -1;
        if (activeChild.Count == 1)
            nextOperate = activeChild[0].active.datas.GetIndexSelf() + 1;
        else
        {
            foreach (var item in activeChild)
            {
                if (item.line.endIndex+1 > nextOperate) nextOperate = item.line.endIndex+1;
            }
        }

        if (nextOperate < datas.childData.Count) //如果当前不是最后一个子节点
        {
            foreach (var item in activeChild)
            {
                item.active.gameObject.SetActive(false);
            }
            int nextIndex = -1;
            if (activeChild.Count == 1)
                nextIndex = activeChild[0].active.datas.GetIndexSelf() + 1;
            else
                nextIndex= activeChild[activeChild.Count - 1].line.endIndex + 1;

            activeChild = new List<ActiveOperate>();

            bool isSingle = true;
            for (int i = 0; i < mutilLine.Count; i++)
            {
                if (mutilLine[i].startIndex == nextIndex)
                {
                    isSingle = false;
                    //加载多个子节点
                    LoadMutilChild(mutilLine[i]);
                   
                }
            }
            if (isSingle)
                LoadChild(datas.childData[nextIndex].name);//加载下一个子节点GameObject
        }
        else
        {
            //该面板功能已完成

            TexCore_EventCenter.Broadcast(TexCore_EventType.显示提示, datas.name + "的所有操作已完成，点击进入下一个场景", 1.5f);
            btn_Next.SetActive(true);//将该面板的下一步按钮打开
            //TexCore_ExamData.self.SubmitAll();
        }
    }

    public void MutilNext(int topicIndex)
    {
        for (int i = 0; i < activeChild.Count; i++)
        {
            if (activeChild[i].isEnter==false && activeChild[i].Contain(topicIndex))
            {
                activeChild[i].active.gameObject.SetActive(false);
                int nextIndex = -1;
                nextIndex = activeChild[i].NextIndex();
                if (nextIndex!=-1)
                {
                    LoadChild(datas.childData[nextIndex].name, activeChild[i]);
                }
            }
        }
    }

    public void LoadMutilChild(MutilLineIndex line)
    {
        ActiveOperate activeOperate = new ActiveOperate(line);
        activeChild.Add(activeOperate);

        LoadChild(datas.childData[activeOperate.line.indexs[0]].name,activeOperate);
    }
    public void MenuLoadChild(string childName)
    {
        LoadChild(childName);
    }
    public virtual void LoadChild(string childData,ActiveOperate activeOperate=null)//加载下一个子节点GameObject
    {
        GameObject newPlane = null;
        BpfLog.Info(BpfLogCategory.UI, "mutil_node_panel_base.load_child", "MutilNodePanelBase");
        //从资源加载工具中首次加载这个GameObject
        newPlane = TexCore_PlaneManager.self.LoadPlane(childData, datas.GetUrl(), childNodeParent).gameObject;

        //获取子节点的题目基类，才能判断该题应该获取什么类型的脚本
        PanelTopicBase topic = newPlane.GetComponent<PanelTopicBase>();

        topic.controlPlane = this;//为子节点设置它的父节点

        //childIndex = childData.GetIndexSelf();//更新活动子节点的索引
        if (activeOperate==null)
        {
            activeChild.Add(new ActiveOperate( topic));//更新活动子节点
        }
        else
        {
            activeOperate.SetActive(topic);//更新活动子节点
        }
        //根据题目类型，决定从新实例化出来的游戏对象上，获取什么类型的题目脚本，并存到子节点的列表中

        childTopics.Add(topic.GetTopicScript());

        //每道题的初始效果,将代码块封装，方便复制该脚本后的更改
        OnChildBuid(topic.GetTopicScript());

        //TopicPanel.instance.UpdateLeftBnts(datas);
        //TopicPanel.instance.UpdateLeftActiveBtn(activeChild.datas);
        //将相机移动到每道题需要在的位置上
        //SceneData sceneData = newPlane.GetComponentInChildren<PanelTopicBase>().topicData.sceneData;
        //MoveCamera(new Vector3(sceneData.posX, sceneData.posY, sceneData.posZ),
        //    new Vector3(sceneData.rotX, sceneData.rotY, sceneData.rotZ));
    }
    public override void OnClick(GameObject obj)
    {
        //base.OnClick(obj);
        if (obj.tag.Equals("MenuBtn"))//如果点击的是菜单层（用于题目之间的自由跳转）
        {
            for (int i = 0; i < datas.parent.childData.Count; i++)
            {
                if (obj.name.Equals(datas.parent.childData[i].name))//如果点击的是顶部按钮
                {
                    TexCore_PlaneManager.self.ShowPlane(obj.name, datas.parent.childData[i].name, true);//加载一个兄弟节点
                    
                    return;
                }
            }
            for (int i = 0; i < datas.childData.Count; i++)
            {
                if (obj.name.Equals(datas.childData[i].name))//如果点击的是左部按钮
                {
                    btn_Next.SetActive(false);
                    LoadChild(datas.childData[i].name);//加载一个子节点
                    return;
                }
            }
            return;
        }
        string[] ns = obj.name.Split('_');
        switch (ns[0])
        {
            case "下一步":
                if (ns.Length > 2)
                {
                    if (activeChild.Count == 1)
                    { NextTopic(); return; }

                    int i;
                    int.TryParse(ns[1], out i);
                    MutilNext(i);

                    int enterCount = 0;
                    foreach (var item in activeChild)//遍历所有操作，累计完成的操作数量
                    {
                        if (item.isEnter) enterCount++;
                    }
                    if (enterCount >= activeChild.Count)//如果所有操作都完成了
                        NextTopic();
                }
                else
                {
                    TexCore_ExamData.self.SubmitAll();
                    TexCore_DataClass nextPlane = datas.NextData();
                    if (nextPlane != null)
                        TexCore_PlaneManager.self.ShowPlane(nextPlane.name, nextPlane.GetUrl(), true);//加载下一个兄弟节点

                    else
                    {
                        //TopicPanel.instance.enter.SetActive(true);
                    }
                }
                return;
            default:
                for (int i = 0; i < activeChild.Count; i++)
                {
                    if (activeChild[i].isEnter == false) activeChild[i].active.OnClick(obj);//贯穿调用子节点的OnClick

                    //每道题的每一步,将代码块封装，方便复制该脚本后的更改
                    OnChildStep(activeChild[i].active, obj);
                }
                break;
        }
        
    }

    public virtual void OnDisable()
    {
        foreach (Property item in selfObjs._properties)//自身节点关闭时，将所有私有对象都关闭（有部分私有节点被移动至场景内）
        {
            if (item != null)
            {
                item.value?.SetActive(false);
            }
        }
    }

    public virtual void OnDestroy()
    {
        foreach (Property item in selfObjs._properties)//自身节点关闭时，将所有私有对象都关闭（有部分私有节点被移动至场景内）
        {
            if (item != null)
            {
                if (item.value != null)
                {
                    Destroy(item.value);
                }
            }
        }
    }

    public void MoveCamera(Vector3 pos, Vector3 rot)//将相机移动至某个位置和旋转的代码块，使用doTween插件，在一秒内移动
    {
        if (pos != Vector3.zero)
        {
            TexCore_PlaneManager.self.mainCamera.transform.DOMove(pos, 1f);
        }

        if (rot != Vector3.zero)
        {
            //TexCore_PlaneManager.self.mainCamera.transform.DORotate(rot, 1f);
            //TexCore_MainCamera _MainCamera = TexCore_PlaneManager.self.mainCamera.GetComponent<LXF_MainCamera>();
            //if (_MainCamera != null)
            //{
            //    _MainCamera.rotationX = -rot.x;
            //    _MainCamera.RotationY = rot.y;
            //    _MainCamera.rotationZ = rot.z;

            //    _MainCamera.topicX = -rot.x;
            //    _MainCamera.topicY = rot.y;
            //}
        }
    }

    #region 
    /*
    public IEnumerator IEColliderIsHide(string name, bool isOn = true, float time = 1)
    {
        yield return new WaitForSeconds(time);

        ColliderIsHide(name, isOn);
    }

    //--------------------------------------------------------

    /// <summary>
    /// 将SelfObjs里面的物体碰撞体打开或者关闭
    /// </summary>
    /// <param name="name"></param>
    /// <param name="isOn"></param>
    public void ColliderIsHide(string name, bool isOn = true)
    {
        GetObj(name).GetComponent<Collider>().enabled = isOn;
        try
        {
            GetObj(name).GetComponent<TexCore_HighlighPlus>().SetState(SelectState.Hit);
        }
        catch (Exception)
        {
            Debug.LogError("高亮提示出错");
        }
        //GetObj(name).SendMessage("RPCSetCollision", isOn);
    }

    /// <summary>
    /// 制退机动画控制器
    /// </summary>
    public Animator animator;

    public virtual void PartReturn(string objname, float time = 2)
    {
        StartCoroutine(IEPartReturn(objname));
    }

    IEnumerator IEPartReturn(string objname, float time = 2)
    {
        yield return new WaitForSeconds(time);

        PartsPutAway partsPutAway = GetObj(objname).GetComponent<PartsPutAway>();

        partsPutAway.PartMove();

        //GetObj(objname).transform.DOScale(Vector3.zero, 1);
    }
    public virtual GameObject GetObj(string name)
    {
        return selfObjs.GetProperty(name);
    }
    */
    #endregion
}
