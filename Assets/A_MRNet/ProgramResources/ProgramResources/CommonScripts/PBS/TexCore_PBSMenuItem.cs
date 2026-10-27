using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TexCore_PBSMenuItem : MonoBehaviour
{
    public TexCore_DataClass data;
    public Transform parent;
    public List<TexCore_PBSMenuItem> childItem;
    public int lever;

    public TextMeshProUGUI title;


    public void BtnInit(TexCore_DataClass _data,int _lever,Transform _perent)
    {
        this.data = _data;
        this.lever = _lever;
        this.parent = _perent;
        if (parent.GetComponent<TexCore_PBSMenuItem>()!=null)
        {
            parent.GetComponent<TexCore_PBSMenuItem>().AddChild(this);
        }
        gameObject.name = data.name;
        string[] reName = data.name.Split('_');
        title.text = reName[reName.Length-1];
        for (int i = 0; i < transform.GetChild(0).childCount; i++)
        {
            transform.GetChild(0).GetChild(i).localPosition = Vector3.left * (lever+1 )* -15;
        }
        
    }
    public void AddChild(TexCore_PBSMenuItem _child)
    {
        if (childItem == null) childItem = new List<TexCore_PBSMenuItem>();
        childItem.Add(_child);
    }

    public void OnClick(GameObject obj)
    {
       
        if (obj.name.Equals("开关"))
        {
            ShowChild(!obj.GetComponent<TexCore_UISelect>().isSelect);
            StartCoroutine(UpdateLayout());
        }
        else if (obj.name.Equals(gameObject.name))
        {
            TexCore_PBSMenuManager.self.ChangeShowItem(this);
        }
    }

    public void ShowChild(bool isShow)
    {
        foreach (TexCore_PBSMenuItem item in childItem)
        {
            item.gameObject.SetActive(isShow);
            //TexCore_MainCamera.Self.centerObj=item.gameObject;
        }
    }

    public IEnumerator UpdateLayout()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(TexCore_PBSManager.self.btnParent.GetComponent<RectTransform>());
        TexCore_PBSMenuItem parentItem = parent.GetComponent<TexCore_PBSMenuItem>();
        if (parentItem!=null)
        {
            StartCoroutine(parentItem.UpdateLayout());
        }
        yield return 0;
    }

    public void SetShwoModel(bool isShow)
    {
        GetComponentInChildren<TexCore_SelectBase>().SetState(isShow ? SelectState.Click : SelectState.Nomal);
       //TODO 切换题目
    }
}
