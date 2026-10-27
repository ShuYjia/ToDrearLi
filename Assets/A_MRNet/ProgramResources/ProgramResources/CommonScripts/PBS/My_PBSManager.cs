using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Bpf.Diagnostics;

[Serializable]
public class PBSMenu
{
    public string name;
    public List<PBSMenu> childs;
}

public class My_PBSManager : MonoBehaviour
{
   
    public static My_PBSManager self;
    public Transform btnParent;
    //public TexCore_DataClass PBSitem;
    public List<GameObject> btnPrefabs;


    private My_PBSItem lastItem;
    public My_PBSItem showItem;

    public UnityEvent BuildOnver;

    [SerializeField]
    public List<List<My_PBSItem>> allItem;
    void Awake()
    {
        self = this;
        if (btnParent == null)
        {
            btnParent = transform;
        }
        
        //InitPBS();
    }
    public void OnEnable()
    {
        TexCore_EventCenter.AddListener<List<PBSMenu>>(TexCore_EventType.更新目录, InitPBS);
        TexCore_EventCenter.AddListener<string>(TexCore_EventType.更新题目, OnlySetActive);
        TexCore_EventCenter.AddListener<string>(TexCore_EventType.完成题目, SetState);
        TexCore_EventCenter.AddListener<string>(TexCore_EventType.点击目录, OnClick);
    }

    public void InitPBS(List<PBSMenu> menu)
    {
        for (int i = 0; i < btnParent.childCount; i++)
        {
            Destroy(btnParent.GetChild(i).gameObject);
        }
        allItem = new List<List<My_PBSItem>>();
        CreateBPSBtns(menu, btnParent);
    }
    /// <summary>
    /// 递归创建PBS按钮
    /// </summary>
    /// <param name="item">PBS节点数据类</param>
    /// <param name="parent">该节点的父节点</param>
    /// <param name="lever">该节点属于第几层级</param>
    public void CreateBPSBtns(List<PBSMenu> items, Transform parent, int lever = 0)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && string.IsNullOrEmpty(items[i].name) == false)
            {
                GameObject newItem = Instantiate(btnPrefabs[lever < btnPrefabs.Count - 1 ? lever : btnPrefabs.Count - 1], parent);
                newItem.SetActive(true);
                My_PBSItem pbsitem = newItem.GetComponent<My_PBSItem>();
                pbsitem.BtnInit(items[i].name, lever, parent);
                pbsitem.GetComponentInChildren<TexCore_UISelect>().itemParent = parent;
                //allItem.Add(items[i].name, newItem.GetComponent<My_PBSItem>());
                if (allItem.Count<=lever)
                {
                    allItem.Add(new List<My_PBSItem>());
                    //allItem[lever] = new List<My_PBSItem>();
                }
                allItem[lever].Add(pbsitem);
                if (items[i].childs != null && items[i].childs.Count > 0)
                {
                    CreateBPSBtns(items[i].childs, newItem.transform.Find("root"), lever + 1);
                }
                newItem.transform.Find("root").gameObject.SetActive(false);
                StartCoroutine(pbsitem.UpdateLayout());
            }
        }
        BuildOnver?.Invoke();
    }
    public void ChangeShowItem(My_PBSItem newItem)
    {
        BpfLog.Info(BpfLogCategory.TexCore, "my_pbs_manager.change_show_item", null, null, BpfLogField.Of("name", gameObject.name));
        if (showItem!=null&& showItem.name.Equals(newItem.name)) return;
        if (showItem != null)
        {
            showItem.SetActive(false);
        }
        lastItem = showItem;
        showItem = newItem;
        showItem.SetActive(true);
    }
    public void OnlyNodeSetActive(string _name)
    {
        My_PBSItem item = null;
        foreach (var items in allItem[0])
        {
            if (items.name.Equals(_name))
            {
                item = items;
            }
        }
        if (item != null)
        {
            ChangeShowItem(item);
        }
    }
    public void SetNodeActive(My_PBSItem items)//将一个一级目录按钮设置为活动状态
    {
        ChangeShowItem(items);
        TexCore_UISelect uiParent = items.GetComponent<TexCore_UISelect>();
        uiParent.ResetBrother();
    }
    public void SetState(string name)//将一个二级目录按钮的状态设置成已完成
    {
        //My_PBSItem item = null;
        if (allItem == null) return;
        foreach (var items in allItem[1])
        {
            if (items.name.Equals("p_" + name))
            {
                //item = items;
                items.state.SetState(SelectState.Click);
                
            }
        }
        UpdateAllState();
    }

    public void UpdateAllState()//刷新所有一级目录按钮的完成状态
    {
        foreach (var item in allItem[0])
        {
            int count = 0;
            foreach (var item1 in item.childItem)
            {
                if (item1.state.state == SelectState.Click) count++;
            }
            if (count >= item.childItem.Count) item.state.SetState(SelectState.Click);
        }
    }
    public void OnlySetActive(string name)//做到某题的时候调用
    {
        if (allItem == null) return;
        My_PBSItem item = null;
        foreach (var items in allItem[1])//在二级目录中查找这个题目对应的按钮
        {
            if (items.name.Equals("p_"+name))
            {
                item = items;
            }
        }

        if (item != null)
        {
            //将这个按钮设置成激活状态，并将兄弟节点关闭
            TexCore_UISelect texCore_UISelect = item.GetComponent<TexCore_UISelect>();
            texCore_UISelect.ResetBrother();
            texCore_UISelect.state = SelectState.Click;
            texCore_UISelect.SetSprite();

            //将这个按钮对应的一级目录按钮设置成激活状态
            foreach (var items in allItem[0])
            {
                foreach (var itemes in items.childItem)
                {
                    if (itemes.name.Equals("p_" + name))
                    {
                        ChangeShowItem(items); 
                        TexCore_UISelect uiParent = items.GetComponent<TexCore_UISelect>();
                        uiParent.ResetBrother();
                        //uiParent.state = SelectState.Click;
                        //uiParent.SetSprite();
                        return;
                    }
                }
            }
        }
    }
    public void OnClick(string name)
    {
        if (allItem == null) return;
        My_PBSItem item = null;
        foreach (var items in allItem[0])
        {
            if (items.name.Equals(name))
            {
                item = items;
                item.OnClick(name);
                return;
            }
        }
        if (item==null)
        {
            foreach (var items in allItem[1])
            {
                if (items.name.Equals(name))
                {
                    item = items;
                    item.OnClick(name);
                    return;
                }
            }
        }
    }
}
