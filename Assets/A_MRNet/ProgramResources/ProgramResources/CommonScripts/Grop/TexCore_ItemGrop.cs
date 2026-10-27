using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class TexCore_ItemGrop : MonoBehaviour
{
    public GameObject pre_Item;

    public List<GameObject> items;

    public void CreateMultiItem(TexCore_DataClass parent,Action<GameObject> action=null)
    {
        if (parent.childData == null && parent.childData.Count < 1) return;
        //ClearChild();
        for (int i = 0; i < parent.childData.Count; i++)
        {
            GameObject newItem = Instantiate(pre_Item, transform);
            newItem.SetActive(true);
            newItem.gameObject.name = parent.childData[i].name;
            newItem.GetComponentInChildren<TextMeshProUGUI>().text = parent.childData[i].name;
            items.Add(newItem);
            if (action!=null)
            {
                action(newItem);
            }
        }
        
    }

    public void CreateMultiItem(Array names)
    {
        foreach (var item in names)
        {
            CreatSingleItem(item.ToString());
        }
    }

    public GameObject CreatSingleItem(string name,string image="")
    {
        //ClearChild();
        GameObject newItem = Instantiate(pre_Item, transform);
        newItem.SetActive(true);
        newItem.gameObject.name = name;
        newItem.GetComponentInChildren<TextMeshProUGUI>().text = name ;
        if (string.IsNullOrEmpty(image))
        {

        }
        items.Add(newItem);
        return newItem;
    }
    public List<string> GetActiveToggles()
    {
        List<string> a = new List<string>();
        for (int i = 0; i < items.Count; i++)
        {
            TexCore_UISelect _UISelect = items[i].GetComponent<TexCore_UISelect>();
            if (_UISelect!=null&& _UISelect.isSelect)
            {
                a.Add(items[i].name);
            }
        }
        return a;
    }
    public void SetToggleNomal(bool nomalState = false)
    {
        for (int i = 0; i < items.Count; i++)
        {
            TexCore_UISelect _UISelect = items[i].GetComponent<TexCore_UISelect>();
            if (_UISelect != null )
            {
                if (nomalState)
                {
                    _UISelect.SetState(SelectState.Click);
                }
                else
                {
                    _UISelect.SetState(SelectState.Nomal);
                }
            }
        }
    }


    public void ClearChild()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
        items.Clear();
    }
}
