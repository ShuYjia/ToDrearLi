using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using Bpf.Diagnostics;

[Serializable]
public class DataPanelNode : SelfDataBase
{
    public List<DataNode> dataNodes;
}

[Serializable]
public class DataNode
{
    public string title;
    public List<string> text;
    public List<DataNode> childs;
}


public class DataPanel : TexCore_PlaneBase, IGetData, IPanelInit
{
    public DataPanelNode selfDatas;

    public Transform btn_parent;
    public GameObject btn_left;

    public Transform dataParent;
    public GameObject showDatas;

    private void Start()
    {

        selfDatas = (DataPanelNode)datas.selfDatas[0];//读取配置文件中的数据

        SetMenu(selfDatas.dataNodes,0);

        SetData(selfDatas.dataNodes[0].title, selfDatas.dataNodes);
    }

    public void SetMenu(List<DataNode> Nodes,int emptyCoutn=0)
    {
        for (int i = 0; i < Nodes.Count; i++)
        {
            GameObject newBtn = Instantiate(btn_left, btn_parent);
            newBtn.SetActive(true);
            string title = "";
            for (int j = 0; j < emptyCoutn; j++)
            {
                title += "       ";
            }  
            newBtn.name = Nodes[i].title;
            newBtn.GetComponentInChildren<TextMeshProUGUI>().text =title+ Nodes[i].title;
            if (Nodes[i].childs!=null)
            {
                SetMenu(Nodes[i].childs, emptyCoutn + 1);
            }
        }
        StartCoroutine(SetSizeFromChild(btn_parent.GetComponent<RectTransform>()));
    }

    public bool SetData(string name,List<DataNode> nodes)
    {
        foreach (DataNode item in nodes)
        {
            if (item.title.Equals(name))
            {
                if (item.text!=null&&item.text.Count>0)
                {
                    for (int i = 0; i < item.text.Count; i++)
                    {
                        GameObject newData = Instantiate(showDatas,dataParent);
                        newData.SetActive(true);
                        string text = "";
                        text += item.text[i]+"\n";
                        newData.GetComponentInChildren<TextMeshProUGUI>().text = text.Replace("\\n", "\n");
                    }
                    StartCoroutine(SetSizeFromChild(dataParent.GetComponent<RectTransform>()));
                    return true;
                }
                else
                {
                    if (item.childs!=null&& item.childs.Count>0)
                    {
                        return SetData(item.childs[0].title, item.childs);
                    }
                }
            }
            else
            {
                if (item.childs != null && item.childs.Count > 0)
                {
                    if (SetData(name, item.childs))
                    {
                        return true;
                    }
                    
                }
            }
        }
        return false;

    }
    public IEnumerator SetSizeFromChild(RectTransform rectTransform)
    {
        Vector2 size = rectTransform.sizeDelta;
        size.y = 0;
        for (int i = 0; i < rectTransform.childCount; i++)
        {
            size.y += (int) rectTransform.GetChild(i).GetComponent<RectTransform>().sizeDelta.y;
        }
        rectTransform.sizeDelta = size ;
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }


    public override void OnClick(GameObject obj)
    {
        base.OnClick(obj);

        switch (obj.name)
        {
            case "返回":
                TexCore_LoadScene.instance.OnlyLoad("02StartPage");
                break;
            default:
                for (int i = 0; i < dataParent.childCount; i++)
                {
                    Destroy(dataParent.GetChild(i).gameObject);
                }
                if (SetData(obj.name, selfDatas.dataNodes))
                {
                    BpfLog.Info(BpfLogCategory.UI, "data_panel.on_click", "找到数据");
                }
                else
                {
                    BpfLog.Info(BpfLogCategory.UI, "data_panel.on_click", "未找到数据");
                }
                break;
        }

    }

    public TexCore_DataClass GetData()
    {
        datas.selfDatas = new List<SelfDataBase>();
        datas.selfDatas.Add(selfDatas);
        return datas;
    }

    public IEnumerator PanelInit()
    {
        yield return null;
    }
}
