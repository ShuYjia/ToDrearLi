using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Page03Data : TexCore_PlaneBase
{
    public static Page03Data instance;

    public Transform btnParent;
    public GameObject prebtn_Module;
    public GameObject prebtn_Files;
    public List<RectTransform> allBtn;

    public Dictionary<string, Panel_ShowModel> childModel;

    public TexCore_PlaneBase activeChild;

    public GameObject textData;

    private void Awake()
    {
        instance = this;
        
    }
    public void OnEnable()
    {
        //TexCore_EventCenter.AddListener(TexCore_EventType.播放音频);
    }
    public void StartPanel()
    {
        datas = TexCore_ConfigManager.self.datas.datas.Find(datas.name);//读取配置文件
        InitFilePBS(datas.childData, btnParent, 1);//生成按钮

        btnParent.GetChild(0).GetComponentInChildren<TexCore_Interaction_UGUI>().SetState(SelectState.Click);//默认打开第一个按钮
        GetModel(datas.childData[0].name);//默认显示第一个按钮的模型
    }

    public void InitFilePBS(List<TexCore_DataClass> _tree, Transform parent, int lever)
    {
        for (int i = 0; i < _tree.Count; i++)
        {
            GameObject newBtn;
            if (lever==1)//创建文件夹
            {
                newBtn = Instantiate(prebtn_Module, parent);
                newBtn.SetActive(true);
                newBtn.name =  lever + "_" + _tree[i].name;
                TextMeshProUGUI text = newBtn.GetComponentInChildren<TextMeshProUGUI>();
                text.text = _tree[i].name.Substring(2);
                //text.margin = new Vector4(10f + lever * 20f, 0, 0, 0);

            }
            else//不是根节点也不是文件夹，创建文件
            {
                newBtn = Instantiate(prebtn_Files, parent);
                newBtn.SetActive(true);
                newBtn.name =   lever + "_" + _tree[i].name;
                TextMeshProUGUI text = newBtn.GetComponentInChildren<TextMeshProUGUI>();
                text.text = _tree[i].name;
                //text.margin = new Vector4(10f + lever * 20f, 0, 0, 0);
            }
            newBtn.transform.GetChild(0).name = newBtn.name;
            
            newBtn.transform.GetChild(0).GetComponent<TexCore_Interaction_UGUI>().itemParent = parent;
            allBtn.Add(newBtn.GetComponent<RectTransform>());

            if (_tree[i].childData != null && _tree[i].childData.Count > 0)
            {
                InitFilePBS(_tree[i].childData, newBtn.transform.GetChild(1), lever + 1);
                newBtn.GetComponentInChildren<TexCore_Interaction_UGUI>().SetState(SelectState.Click);
            }
        }
        //LayoutRebuilder.ForceRebuildLayoutImmediate(parent.GetComponent<RectTransform>());
    }
    public void GetModel(string name)
    {
        if (childModel == null) childModel = new Dictionary<string, Panel_ShowModel>();
        foreach (var item in childModel.Values)
        {
            item.gameObject.SetActive(false);
        }
        if (childModel.ContainsKey(name))
        {
            Panel_ShowModel m = childModel[name];
            m.ResetModel();
            m.gameObject.SetActive(true);
            activeChild = m;
        }
        else
        {
            TexCore_PlaneBase b = TexCore_PlaneManager.self.LoadPlane(name, datas.GetUrl(), transform);
            Panel_ShowModel m = b .GetComponent< Panel_ShowModel>();
            m.gameObject.SetActive(true);
            m.textData = textData;
            childModel.Add(name,m);
            activeChild = m;
        }
        
    }
    public override void OnClick(GameObject obj)
    {
        base.OnClick(obj);
        switch (obj.name)
        {
            case "退出":
                Application.Quit();
                break;
            default:
                string[] ns = obj.name.Split("_");
                switch (ns[0])
                {
                    case "1":
                        //开关子节点
                        //刷新排列
                        GameObject child = obj.transform.parent.GetChild(1).gameObject;
                        child.SetActive(!child.activeSelf);
                        //if (child.activeSelf)
                        //    child.GetComponent<LayoutUpdate>().WaiteUpdateLayoutSingle();
                        //else
                        //    obj.transform.parent.GetComponent<LayoutUpdate>().WaiteUpdateLayoutSingle();
                        if (child.activeSelf)
                        {
                            GetModel(ns[1]);
                            //还原其它按钮
                            
                            //关闭其它模型
                        }
                        

                        break;
                    case "2":
                        //获取信息
                        activeChild.OnClick(obj);

                        break;
                }
                break;
        }
    }
}
