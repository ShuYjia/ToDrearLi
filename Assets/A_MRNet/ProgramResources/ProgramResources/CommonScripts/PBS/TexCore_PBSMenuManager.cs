using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TexCore_PBSMenuManager : MonoBehaviour
{
    public static TexCore_PBSMenuManager self;

    public Transform btnParent;

    public Transform modelParent;

    public TexCore_DataClass PBSitem;

    public List<GameObject> btnPrefabs;

    Dictionary<string, TexCore_PBSMenuItem> allItem;

    private TexCore_PBSMenuItem lastItem;
    private TexCore_PBSMenuItem showItem;


    private void Awake()
    {
        self = this;
    }

    private void Start()
    {
        allItem = new Dictionary<string, TexCore_PBSMenuItem>();
        InitBPS(TexCore_ConfigManager.self.datas.datas.childData[0]);
    }
    public void InitBPS(TexCore_DataClass itemParent)
    {
        if (btnParent != null) btnParent = transform;
        PBSitem = itemParent;
        CreateBPSBtn(PBSitem,btnParent);
        //PBSAddModel(btnParent, modelParent);
    }
    /// <summary>
    /// 递归创建PBS按钮
    /// </summary>
    /// <param name="item">PBS节点数据类</param>
    /// <param name="parent">该节点的父节点</param>
    /// <param name="lever">该节点属于第几层级</param>
    public void CreateBPSBtn(TexCore_DataClass item,Transform parent, int lever = 0)
    {
        if (item!=null&&string.IsNullOrEmpty(item.name)==false)
        {
            GameObject newItem = Instantiate(btnPrefabs[lever < btnPrefabs.Count - 1 ? lever : btnPrefabs.Count-1], parent);
            newItem.GetComponent<TexCore_PBSMenuItem>().BtnInit(item, lever, parent);
            allItem.Add(item.name,newItem.GetComponent<TexCore_PBSMenuItem>());
            if (item.childData != null && item.childData.Count > 0) 
            {
                foreach (TexCore_DataClass childItem in item.childData)
                {
                    CreateBPSBtn(childItem, newItem.transform, lever + 1);
                }
            }
        }
    }


    public void OnClick(GameObject obj)
    {
        TexCore_PBSMenuItem item;
        if (obj.name.Equals("开关"))
        {
            item= obj.transform.parent.parent.GetComponent<TexCore_PBSMenuItem>();
        }
        else
        {
            allItem.TryGetValue(obj.name,out item);
        }
        item.OnClick(obj);
    }
    
    public void ChangeShowItem(TexCore_PBSMenuItem newItem)
    {
        if (showItem!=null)
        {
            showItem.SetShwoModel(false);
        }
        lastItem = showItem;
        showItem = newItem;
        showItem.SetShwoModel(true);
    }
}
