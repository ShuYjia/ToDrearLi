using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using Unity;
using Bpf.Diagnostics;


public class NodePanelBase : TexCore_PlaneBase
{
    public Transform childNodeParent;//将所有子节点生成到这个Transform下面

    public List<PanelTopicBase> childTopics;//加载过的子节点都会存到这个列表中，缓存池
    //public int childIndex = 0;//当前正在操作的子节点索引

    public PanelTopicBase activeChild;//当前正在操作的子节点

    public GameObject btn_Next;//下一步按钮，点击跳转到下一个Node面板

    //public List<GameObject> selfObjs;//这个面板所有需要操作的物体

    //public IProperties selfObjs;//这个面板所有需要操作的物体

    public GameObject bag;

    public Node node;//每个Node下的知识

    internal virtual void Awake()
    {

    }

    public virtual void OnEnable()
    {
        BpfLog.Error(BpfLogCategory.UI, "node_panel_base.on_enable", null, null, BpfLogField.Of("name", datas.name)) ;
        SetSceneNodeActive(datas.parent?.name, datas.name);
        //My_PBSManager.self.OnlyNodeSetActive(datas.name);
        TexCore_EventCenter.Broadcast<string>(TexCore_EventType.更新节点, datas.name);
        //switch (TexCore_Const.mode)
        //{
        //    case Mode.练习:
        //        KnowledgeManager.self.SetNode(datas.name);
        //        break;
        //    case Mode.考核:
        //        KnowledgeManager.self.gameObject.transform.parent.gameObject.SetActive(false);
        //        break;
        //    default:
        //        break;
        //}

        //TexCore_PBSManager.self.OnlyNodeSetActive(datas.name);
        //bag = BagManager.Instance.bagUI.gameObject;
        KnowledgeManager.Self.SetNode(datas.name);

        KnowledgeManager.Self.transform.GetChild(0).gameObject.SetActive(true);
    }

    private static void SetSceneNodeActive(string courseName, string nodeName)
    {
        if (string.IsNullOrEmpty(nodeName)) return;

        GameObject[] sceneRoots = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject sceneRoot in sceneRoots)
        {
            if (sceneRoot.name != "有交互物体" && sceneRoot.name != "无交互物体") continue;

            foreach (Transform courseRoot in sceneRoot.transform)
            {
                if (!string.IsNullOrEmpty(courseName))
                {
                    if (courseRoot.name != courseName)
                    {
                        continue;
                    }
                }
                else
                {
                    bool containsNode = false;
                    foreach (Transform nodeRoot in courseRoot)
                    {
                        if (nodeRoot.name == nodeName)
                        {
                            containsNode = true;
                            break;
                        }
                    }

                    if (!containsNode) continue;
                }

                foreach (Transform nodeRoot in courseRoot)
                {
                    nodeRoot.gameObject.SetActive(nodeRoot.name == nodeName);
                }
            }
        }
    }

    public virtual void Start()
    {
        
        btn_Next.SetActive(false);//关闭下一步按钮

       
        if (datas.childData.Count > 0)//如果有子节点
        {
            //Debug.Log(datas.childData[0].name);
            LoadChild(datas.childData[0].name);//加载第一个子节点
        }
    }
    public virtual void ResetSelfObjPos()//当该面板的场景跳转完成后，将私有物体中的三维对象都移动到场景中
    {

    }
    public virtual void OnChildOver()//当某一题做完时
    {
      
    }
    public virtual void OnChildBuid()//当一个子节点被克隆出来后，做的第一件该题的初始化方法
    {
        
    }

    public virtual void OnChildStep(GameObject obj)//当某一题的某个步骤达成时
    {
        //print("调用父类OnChildBuid1");
    }

    public virtual void NextTopic()//跳转到下一个子节点
    {
        OnChildOver();

        if (activeChild.datas.GetIndexSelf()+1 < datas.childData.Count) //如果当前不是最后一个子节点
        {
            LoadChild(datas.childData[activeChild.datas.GetIndexSelf()+1].name);//加载下一个子节点GameObject
        }
        else
        {
            //该面板功能已完成

             TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示,
                 datas.name + "的所有操作已完成，点击进入下一个场景", 1.5f);
            btn_Next.SetActive(true);//将该面板的下一步按钮打开
        }
    }

    public virtual  void LoadChild(string childData)//加载下一个子节点GameObject
    {
        GameObject newPlane = null;
        for (int i = childTopics.Count - 1; i >= 0; i--) //便利所有加载过的子节点，查找下一个要加载的节点
        {
            if (childTopics[i].name.Equals(childData))
            {
                //如果该题加载过
                Destroy(childTopics[i].gameObject);
                childTopics.RemoveAt(i);
                //newPlane = childTopics[i].gameObject;
                //newPlane.gameObject.SetActive(true);//则将该题显示出来
                //activeChild = newPlane.GetComponent<PanelTopicBase>().GetTopicScript();//将活动索引改成当前子节点的索引

            }
            else
            {
                //如果这个节点不是要加载的节点，则关闭
                childTopics[i].gameObject.SetActive(false);
            }
        }
        if (newPlane == null)//如果该题没加载过
        {
            //Debug.LogWarning("11111111111111111111"+TexCore_ConfigManager.self.datas.datas.FindRoot(TexCore_HeadMenu.self.node).Find(name).GetUrl());
            //从资源加载工具中首次加载这个GameObject


            BpfLog.Error(BpfLogCategory.UI, "node_panel_base.load_child", null, null, BpfLogField.Of("name", name));
            BpfLog.Error(BpfLogCategory.UI, "node_panel_base.load_child", null, null, BpfLogField.Of("child_data", childData));
            // 题目数据必须取自本节点自己的 childData：课程里多个模块存在同名题目
            // （05/08/09/11 都有“06完成整个模块”），课程级 Find 会命中别的模块，
            // 让题目读到别模块的 tasks/hintTexts，GetIndexSelf() 也会返回错误的题目序号。
            newPlane = TexCore_PlaneManager.self.LoadPlane(childData,
                TexCore_ConfigManager.self.datas.datas.FindRoot(TexCore_HeadMenu.self.node).Find(name).GetUrl(),
                childNodeParent,
                FindChildData(childData)).gameObject;

            //获取子节点的题目基类，才能判断该题应该获取什么类型的脚本
            PanelTopicBase topic = newPlane.GetComponent<PanelTopicBase>();

            topic.controlPlane = this;//为子节点设置它的父节点

            //childIndex = childData.GetIndexSelf();//更新活动子节点的索引
            activeChild = topic.GetTopicScript();//更新活动子节点

            //根据题目类型，决定从新实例化出来的游戏对象上，获取什么类型的题目脚本，并存到子节点的列表中
            childTopics.Add(activeChild);
        }

        //每道题的初始效果,将代码块封装，方便复制该脚本后的更改
        OnChildBuid();

    }

    /// <summary>
    /// 在本节点自己的 childData 里查找题目数据。课程里多个模块存在同名题目
    /// （05/08/09/11 都叫“06完成整个模块”），不能用课程级 Find 定位，
    /// 否则会拿到别的模块的数据与序号。
    /// </summary>
    private TexCore_DataClass FindChildData(string childData)
    {
        if (datas == null || datas.childData == null)
        {
            return null;
        }

        for (int i = 0; i < datas.childData.Count; i++)
        {
            TexCore_DataClass item = datas.childData[i];
            if (item != null && item.name == childData)
            {
                return item;
            }
        }

        return null;
    }

    public  override void OnClick(GameObject obj)
    {
        //Debug.Log("NodePanelBase:" + obj.name);
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
        switch (obj.name)
        {
            case "下一步":
                ClickNext();
                break;
            default:
                activeChild.OnClick(obj);//贯穿调用子节点的OnClick

                //每道题的每一步,将代码块封装，方便复制该脚本后的更改
                OnChildStep(obj);

                break;
        }
    }

    public  void ClickNext()
    {
        if (activeChild.datas.GetIndexSelf() + 1 < datas.childData.Count)//监听子面板的下一步
        {
            TexCore_EventCenter.Broadcast<string>(TexCore_EventType.完成题目, activeChild.datas.name);
            NextTopic();//加载下一个子节点
        }
        else//监听自己的下一步
        {
            TexCore_DataClass nextPlane = datas.NextData();
            if (nextPlane != null&&nextPlane.parent.name.Equals(datas.parent.name))
            {
               
                //TexCore_HeadMenu.self?.SkipModuleRefresh(nextPlane.name);
                //TexCore_HeadMenu.self?.SkipModuleRefresh(nextPlane.name);

                TexCore_PlaneManager.self.ShowPlane(nextPlane.name, nextPlane.GetUrl(), true);//加载下一个兄弟节点

            }
            else
            {
                switch (TexCore_Const.mode)
                {
                    case Mode.练习:
                        TexCore_Const.startPanel = "04模式选择";
                        if (TexCore_LoadScene.instance != null)
                        {
                            TexCore_LoadScene.instance.OnlyLoad("00Navigation");
                        }
                        else
                        {
                            SceneManager.LoadScene(TexCore_LoadScene.StartupSceneName);
                        }
                        break;
                    case Mode.考核:

                        TexCore_ExamData.self?.Show(true);
                        break;
                    default:
                        break;
                }
                
                //TopicPanel.instance.enter.SetActive(true);
            }
        }
    }

    //public virtual  void OnDisable()
    //{
    //    foreach (Property item in selfObjs._properties)//自身节点关闭时，将所有私有对象都关闭（有部分私有节点被移动至场景内）
    //    {
    //        if (item!=null)
    //        {
    //            item.value?.SetActive(false);
    //        }
    //    }
    //}

    //public virtual void OnDestroy()
    //{
    //    foreach (Property item in selfObjs._properties)//自身节点关闭时，将所有私有对象都关闭（有部分私有节点被移动至场景内）
    //    {
    //        if (item!=null)
    //        {
    //            if (item.value!=null)
    //            {
    //                Destroy(item.value);
    //            }
    //        }
    //    }
    //}

    public  void MoveCamera(Vector3 pos, Vector3 rot)//将相机移动至某个位置和旋转的代码块，使用doTween插件，在一秒内移动
    {
        if (pos != Vector3.zero)
        {
            TexCore_PlaneManager.self.mainCamera.transform.DOMove(pos, 1f);
            TexCore_PlaneManager.self.mainCamera.transform.DORotate(rot, 1f);
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


}
