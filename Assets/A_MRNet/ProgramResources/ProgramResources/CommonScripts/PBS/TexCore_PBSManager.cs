using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class TexCore_PBSManager : MonoBehaviour
{
    public static TexCore_PBSManager self;

    public Transform btnParent;

    public Transform modelParent;

    public TexCore_DataClass PBSitem;

    public List<GameObject> btnPrefabs;
    [SerializeField]
    public Dictionary<string, TexCore_PBSItem> allItem;

    private TexCore_PBSItem lastItem;
    public TexCore_PBSItem showItem;

    private void Awake()
    {
        self = this;
        if (btnParent==null)
        {
            btnParent = transform;
        }
        allItem = new Dictionary<string, TexCore_PBSItem>();
        //InitBPS(TexCore_ConfigManager.self.datas.datas.childData[0]);
        
        //ResetPos();
    }
    public void InitPBS()
    {
        
        CreateBPSBtns(TexCore_ConfigManager.self.datas.datas.childData[2].childData, btnParent);
    }

    private void Start()
    {
        
        //GetComponentInChildren<TexCore_PBSItem>().ShowChild(true);
    }
    public void InitBPS(TexCore_DataClass itemParent)
    {
        PBSitem = itemParent;
        CreateBPSBtn(PBSitem,btnParent,0);
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
            TexCore_PBSItem pbsitem= newItem.GetComponent<TexCore_PBSItem>();
            pbsitem.BtnInit(item.name, lever, parent);
            allItem.Add(item.name,newItem.GetComponent<TexCore_PBSItem>());
            if (item.childData != null && item.childData.Count > 0) 
            {
                foreach (TexCore_DataClass childItem in item.childData)
                {
                    CreateBPSBtn(childItem, newItem.transform.GetChild(1), lever + 1);
                }
                newItem.transform.GetChild(1).gameObject.SetActive(false);
                StartCoroutine(pbsitem.UpdateLayout());
            }
        }
    }
    /// <summary>
    /// 递归创建PBS按钮
    /// </summary>
    /// <param name="item">PBS节点数据类</param>
    /// <param name="parent">该节点的父节点</param>
    /// <param name="lever">该节点属于第几层级</param>
    public void CreateBPSBtns(List<TexCore_DataClass> items, Transform parent, int lever = 0)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && string.IsNullOrEmpty(items[i].name) == false)
            {
                GameObject newItem = Instantiate(btnPrefabs[lever < btnPrefabs.Count - 1 ? lever : btnPrefabs.Count - 1], parent);
                TexCore_PBSItem pbsitem = newItem.GetComponent<TexCore_PBSItem>();
                pbsitem.BtnInit(items[i].name, lever, parent);
                pbsitem.GetComponentInChildren<TexCore_UISelect>().itemParent = parent;
                allItem.Add(items[i].name, newItem.GetComponent<TexCore_PBSItem>());
                //newItem.GetComponent<PhotonView>().viewID = 900 + allItem.Count;

                if (items[i].childData != null && items[i].childData.Count > 0)
                {
                    CreateBPSBtns(items[i].childData, newItem.transform.Find("root"), lever + 1);
                    
                }
                newItem.transform.Find("root").gameObject.SetActive(false);
                StartCoroutine(pbsitem.UpdateLayout());
            }
        }
        
    }

    /// <summary>
    /// 递归根据Transform子集匹配按钮和模型
    /// </summary>
    /// <param name="_btnParent">遍历该按钮的所有子节点</param>
    /// <param name="_modelParent">遍历该模型的所有子节点</param>
    public void PBSAddModel(Transform _btnParent,Transform _modelParent)
    {
        for (int i = 0; i < _btnParent.childCount; i++)
        {
            TexCore_PBSItem item = _btnParent.GetChild(i).GetComponent<TexCore_PBSItem>();
            item.model = _modelParent.GetChild(i);
            item.model.name = item.name;
            if (item.childItem!=null&&item.childItem.Count>0)
            {
                foreach (TexCore_PBSItem childItem in item.childItem)
                {
                    PBSAddModel(item.transform,item.model);

                }
            }
        }
    }

    public void OnClick(GameObject obj)
    {
        TexCore_PBSItem item;
        //switch (obj.name.Split('_')[1])
        //{
        //    case "1":
        //        break;
        //    default:
        //        break;
        //}
        if (obj.name.Equals("开关"))
        {
            item= obj.transform.parent.parent.GetComponent<TexCore_PBSItem>();
        }
        else
        {
            //allItem.TryGetValue("p_"+obj.name,out item);
            item= obj.transform.GetComponent<TexCore_PBSItem>();
        }
        
        item.OnClick(obj);
    }
    
    public void ChangeShowItem(TexCore_PBSItem newItem)
    {
        if (showItem!=null)
        {
            showItem.SetActive(false);
        }
        lastItem = showItem;
        showItem = newItem;
        showItem.SetActive(true);
    }
    public void OnlyNodeSetActive(string _name)
    {
        TexCore_PBSItem item;
        allItem.TryGetValue(_name, out item);
        if (item != null)
        {
            ChangeShowItem(item);
        }
    }
    public void OnlySetActive(string name)
    {
        TexCore_PBSItem item;
        allItem.TryGetValue(name, out item);
        if (item != null)
        {
            TexCore_UISelect texCore_UISelect = item.GetComponent<TexCore_UISelect>();
            texCore_UISelect.ResetBrother();
            texCore_UISelect.state = SelectState.Click;
            texCore_UISelect.SetSprite();

        }
    }
}
