using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System;
using Bpf.Diagnostics;

public class TexCore_HeadMenuL1 : MonoBehaviour
{
    public static TexCore_HeadMenuL1 self;

    public string node;
    public GameObject pre_lever1Btn;
    public RectTransform topBtnParent;
    public ScrollRect scrollRect;

    public Transform activeL1;


    private void Awake()
    {
        self = this;
    }
    private void OnEnable()
    {
        TexCore_EventCenter.AddListener<string>(TexCore_EventType.更新题目, SkipL2);
    }
    private void OnDisable()
    {
        TexCore_EventCenter.RemoveListener<string>(TexCore_EventType.更新题目, SkipL2);
    }
    public void InitBtns()
    {
        TexCore_DataClass dataClass = TexCore_ConfigManager.self.datas.datas.Find(node);
        if (dataClass!=null)
        {
            if (dataClass.childData != null && dataClass.childData.Count > 0) 
            {
                for (int i = 0; i < dataClass.childData.Count; i++)
                {
                    GameObject newBtn = Instantiate(pre_lever1Btn,topBtnParent);
                    newBtn.SetActive(true);
                    newBtn.name = "L1_" + dataClass.childData[i].name;
                    newBtn.transform.GetChild(0).name = "L1_" + dataClass.childData[i].name;
                    newBtn.GetComponentInChildren<TextMeshProUGUI>().text = dataClass.childData[i].name.Substring(2);
                    newBtn.GetComponentInChildren<TexCore_SelectBase>().itemParent = topBtnParent;
                    
                }
                LayoutRebuilder.ForceRebuildLayoutImmediate(topBtnParent);

                BpfLog.Info(BpfLogCategory.XR, "tex_core_head_menu_l1.init_btns", TexCore_ConfigManager.self.datas.datas.Find(node).GetUrl());
                TexCore_PlaneManager.self.ShowPlane(node, TexCore_ConfigManager.self.datas.datas.Find(node).GetUrl(), true, null);
            }
        }
    }
    public void OnClickMenu(string name)
    {
        string[] strs = name.Split("_");
        switch (strs[0])
        {
            case "L1":
                TexCore_PlaneManager.self.activePlane.GetComponent<NodePanelBase>().LoadChild(strs[1]);
                break;
            default:
                break;
        }
       
    }

   

    public void SkipL2(string name)
    {
        Transform btn = activeL1.GetChild(1).Find("L2_" + name);
        if (btn == null)
        {
            TexCore_LoadScene.instance.OnlyLoad("00Navigation");
        }
        else
        {
            TexCore_Interaction_UGUI sele = btn.GetComponentInChildren<TexCore_Interaction_UGUI>();
            sele.ResetBrother();
            sele.state = SelectState.Click;
            sele.isSelect = true;
            sele.SetSprite();
        }
    }

    
}
