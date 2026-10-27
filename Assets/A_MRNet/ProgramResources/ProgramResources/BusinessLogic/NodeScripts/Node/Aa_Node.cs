/****************************************************
	文件名：		Aa_Node.cs

	功能：		Nothing
*****************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class DataClass : SelfDataBase
{
    [XmlElement("场景数据")]
    public SceneData sceneData;
    public List<string> texts;
}

public class Aa_Node : NodePanelBase, IGetData, IPanelInit
{
    public DataClass dataClass;//A面板的私有数据类

    public static Aa_Node instance;

    public bool OnSelect = false;

    //public TextMeshProUGUI tip;//Hover提示

    internal override void Awake()
    {
        instance = this;
        base.Awake();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        StartCoroutine(Init());//等待自己的场景加载完成，
    }

    public override void Start()
    {
        dataClass = (DataClass)datas.selfDatas[0];//读取配置文件中的数据
        base.Start();
    }
    private void Update()
    {

    }

    public override void ResetSelfObjPos()//当该面板的场景跳转完成后，将私有物体中的三维对象都移动到场景中
    {
        //selfObjs[0].SetActive(true);
        //selfObjs[0].transform.SetParent(null);
        //if (selfObjs != null && selfObjs.Count > 0 && selfObjs[0] != null)
        //{
        //    TexCore_PlaneManager.self.ResetObjPos(selfObjs[0].transform, datas.name);
        //}

    }


    public override void OnChildOver()//当某一题做完时
    {
        int index = activeChild.datas.GetIndexSelf();
        switch (index)
        {
            case 1:
            case 2:
            case 3:
            case 4:
            case 5:
            case 6:
                
                break;
            default:
                break;
        }
    }

    public override void OnChildBuid()//当一个子节点被克隆出来后，做的第一件该题的初始化方法
    {
        base.OnChildBuid();

        int index = activeChild.datas.GetIndexSelf();

        switch (index)
        {
            case 0:
                break;
            case 1:
                break;
            case 2:
                break;
            case 3:
            case 4:
            case 5:
            case 6:
                break;
            default: break;
        }


    }

    public override void OnChildStep(GameObject obj)//当某一题的某个步骤达成时
    {
        base.OnChildStep(obj);
        switch (activeChild.datas.GetIndexSelf())//第一题
        {
            case 0:
            case 1:
            case 2:
               
                break;
        }
    }

    public IEnumerator Init()
    {
        if (string.IsNullOrEmpty(dataClass.sceneData.sceneName)==false)
        {
            while (SceneManager.GetActiveScene().name.Equals(dataClass.sceneData.sceneName) == false)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        //将该面板中，需要移动到场景内的三维物体都拖出去，封装代码块，为了方便复制该脚本后的更改

        ResetSelfObjPos();

        //if (dataClass.sceneData.userNewCamera)
        //{
        //    TexCore_PlaneManager.self.mainCamera.SetActive(false);
        //}

        //将相机移动至该面板的初始位置
        //MoveCamera(new Vector3(dataClass.sceneData.posX, dataClass.sceneData.posY, dataClass.sceneData.posZ),
        //    new Vector3(dataClass.sceneData.rotX, dataClass.sceneData.rotY, dataClass.sceneData.rotZ));
    }

    public TexCore_DataClass GetData()//接口，调用预制体上的序列化数据，用于生成配置文件
    {
        datas.selfDatas = new List<SelfDataBase>();
        datas.selfDatas.Add(dataClass);

        return datas;
    }

    public IEnumerator PanelInit()//面板预处理（在该面板的实例创建出来之前调用）
    {
        if (string.IsNullOrEmpty(dataClass.sceneData.sceneName) == false)//如果该面板需要更换场景
        {
            TexCore_LoadScene.instance.OnlyLoad(dataClass.sceneData.sceneName);//加载一个新场景
            while (SceneManager.GetActiveScene().name.Equals(dataClass.sceneData.sceneName) == false)//等待场景加载完成
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
        //camera = GameObject.Find("Camera_" + datas.name);
        //if (camera != null)
        //{
        //    camera.SetActive(true);
        //    print("相机找到：" + SceneManager.GetActiveScene().name + " Camera_" + datas.name);
        //}
        //else
        //{
        //    print("相机未找到：" + SceneManager.GetActiveScene().name + " Camera_" + datas.name);
        //}
    }

    public override void OnHover(GameObject obj)
    {
        base.OnHover(obj);
        //if (obj.GetComponent<TexCore_ObjSelect>() != null)
        //{
        //    tip.gameObject.SetActive(true);
        //    tip.text = obj.name;
        //    tip.rectTransform.anchoredPosition = Vector2 .Lerp(tip.rectTransform.anchoredPosition, Input.mousePosition,Time.deltaTime*10);
        //}
        //else
        //{
        //    tip.gameObject.SetActive(false);
        //}

    }

}
