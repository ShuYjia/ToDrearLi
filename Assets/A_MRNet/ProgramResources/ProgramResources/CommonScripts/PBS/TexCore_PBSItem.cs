using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

[Serializable]
public class TexCore_PBSItem : MonoBehaviour
{
    //public TexCore_DataClass data;
    public string data;
    public Transform parent;
    public List<TexCore_PBSItem> childItem;
    public int lever;

    public TextMeshProUGUI title;

    public Transform model;

    public void BtnInit(string _data,int _lever,Transform _perent)
    {
        this.data = _data;
        this.lever = _lever;
        this.parent = _perent;
        if (parent != null && parent.GetComponent<TexCore_PBSItem>() != null) 
        {
            parent.GetComponent<TexCore_PBSItem>().AddChild(this);
        }
        gameObject.name = "p_" + _data;
        string[] reName = _data.Split('_');
        title.text = reName[reName.Length-1];
        for (int i = 0; i < transform.GetChild(0).childCount; i++)
        {
            transform.GetChild(0).GetChild(i).localPosition = Vector3.left * (lever+1 )* -15;
        }
        
    }
    public void AddChild(TexCore_PBSItem _child)
    {
        if (childItem == null) childItem = new List<TexCore_PBSItem>();
        childItem.Add(_child);
    }

    public void OnClick(GameObject obj)
    {

        //if (obj.name.Equals("开关"))
        //{
        //    ShowChild(!obj.GetComponent<TexCore_UISelect>().isSelect);
        //    StartCoroutine(UpdateLayout());
        //}
        //else if (obj.name.Equals(gameObject.name))
        //{
        //    TexCore_PBSManager.self.ChangeShowItem(this);
        //}

        //if (model!=null)
        //{
        //    if (model.GetComponent<NodePanelBase>()!=null)
        //    {
        //        //切换一级目录
        //        TexCore_PBSManager.self.ChangeShowItem(this);

        //    }
        //    else
        //    {
        //        //切换二级目录
        //        model.GetComponent<NodePanelBase>().LoadChild(data);

        //    }
        //}
        //else
        //{
        //    TexCore_PlaneManager.self.ShowPlane(data, "02操作/" + data,true);
        //}

        if (lever==0)
        {
            TexCore_PlaneManager.self.ShowPlane(data, "02操作/" + data, true);
        }
        else
        {
            TexCore_PlaneManager.self.activePlane.SendMessage("LoadChild",data);
        }

    }

    public void ShowChild(bool isShow)
    {
        //transform.Find("root").gameObject.SetActive(isShow);
        if (isShow==true)
        {
            //显示题目：确认是否是活动的一级目录
            //TexCore_PlaneManager.self.ShowPlane(data.name, "02操作/" + data.name);
        }
        //foreach (TexCore_PBSItem item in childItem)
        //{
        //    item.gameObject.SetActive(isShow);
        //    //TexCore_MainCamera.Self.centerObj=item.gameObject;
        //}
    }

    public IEnumerator UpdateLayout()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform.Find("root").GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        if (parent!=null)
        {
            TexCore_PBSItem parentItem = parent.GetComponent<TexCore_PBSItem>();
            if (parentItem != null)
            {
                StartCoroutine(parentItem.UpdateLayout());
            }
            else
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(TexCore_PBSManager.self.btnParent.GetComponent<RectTransform>());
            }
        }
        yield return 0;
    }

    
    public void SetShwoModel(bool isShow)
    {
        GetComponent<TexCore_SelectBase>().SetState(isShow ? SelectState.Click : SelectState.Nomal);
        if (model!=null)
        {
            model.gameObject.SetActive(isShow);
        }
        transform.Find("root").gameObject.SetActive(isShow);
        StartCoroutine(UpdateLayout());
        if (isShow)
        {
            Transform firstChild = transform.Find("root").GetChild(0);
            if (firstChild != null)
            {
                firstChild.GetComponent<TexCore_SelectBase>().SetState(SelectState.Click);
                firstChild.GetComponent<TexCore_PBSItem>().OnClick(firstChild.gameObject);
            }
        }
        else
        {
            Transform root = transform.Find("root");
            if (root != null)
            {
                root.gameObject.SetActive(false);
            }
        }
    }
    public void SetActive(bool isShow)
    {
        GetComponent<TexCore_SelectBase>().state=(isShow ? SelectState.Click : SelectState.Nomal);
        GetComponent<TexCore_UISelect>().SetSprite();
        if (model != null)
        {
            model.gameObject.SetActive(isShow);
        }
        transform.Find("root").gameObject.SetActive(isShow);
        StartCoroutine(UpdateLayout());
    }
}
