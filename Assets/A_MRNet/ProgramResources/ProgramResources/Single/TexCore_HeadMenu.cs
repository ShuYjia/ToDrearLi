using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System;
using App.Runtime.RoomCalibration;
using Bpf.Diagnostics;

public class TexCore_HeadMenu : MonoBehaviour
{
    public static TexCore_HeadMenu self;

    public string node;
    public Text title;
    public GameObject pre_lever1Btn;
    public GameObject pre_lever2Btn;
    public RectTransform topBtnParent;

    public RectTransform SecondBtnParent;

    public ScrollRect scrollRect;

    public Transform activeL1;

    public int startIndex = 0;
    public int endIndex = 2;
    [SerializeField] private List<string> allowedModuleNames = new List<string>();
    private void Awake()
    {
        self = this;
    }
    private void OnEnable()
    {
        //title.text = node.Substring(2);
        TexCore_EventCenter.AddListener<string>(TexCore_EventType.更新节点, SkipModuleRefresh);
        TexCore_EventCenter.AddListener<string>(TexCore_EventType.更新题目, SkipL2);
    }
    private void OnDisable()
    {
        TexCore_EventCenter.RemoveListener<string>(TexCore_EventType.更新节点, SkipModuleRefresh);
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
                    if (!IsModuleAllowed(dataClass.childData[i].name))
                    {
                        continue;
                    }

                    GameObject newBtn = Instantiate(pre_lever1Btn,topBtnParent);
                    newBtn.SetActive(true);
                    newBtn.name = "L1_" + dataClass.childData[i].name;
                    newBtn.transform.GetChild(0).name = "L1_" + dataClass.childData[i].name;
                    newBtn.GetComponentInChildren<TextMeshProUGUI>().text = dataClass.childData[i].name;
                    newBtn.GetComponentInChildren<TexCore_SelectBase>().itemParent = topBtnParent;
                    foreach (var item in dataClass.childData[i].childData)
                    {
                        GameObject newBtn2 = Instantiate(pre_lever2Btn, newBtn.transform.GetChild(1));
                        newBtn2.SetActive(true);
                        newBtn2.name = "L2_" + item.name;
                        newBtn2.transform.GetChild(0).name = "L2_" + item.name;
                        newBtn2.GetComponent<TexCore_SelectBase>().itemParent = newBtn.transform.GetChild(1);
                        newBtn2.GetComponentInChildren<TextMeshProUGUI>().text = item.name;
                    }
                }
                if (topBtnParent.childCount == 0)
                {
                    return;
                }

                MenuRefresh();
                LayoutRebuilder.ForceRebuildLayoutImmediate(topBtnParent);
                OnClickMenu(topBtnParent.GetChild(0).GetComponentInChildren<TexCore_Interaction_Hololens>().name);
                LayoutRebuilder.ForceRebuildLayoutImmediate(topBtnParent);
            }
        }

        if (TexCore_LoadScene.IsBusinessScene(SceneManager.GetActiveScene().name))
        {
            RoomOriginCalibrationManager.EnsureRuntimeForActiveScene();
        }
    }
    public void OnClickMenu(string name)
    {
        int separatorIndex = name.IndexOf('_');
        if (separatorIndex <= 0 || separatorIndex >= name.Length - 1)
        {
            BpfLog.Error(BpfLogCategory.XR, "tex_core_head_menu.on_click_menu", "Invalid menu name: " + name);
            return;
        }

        string menuLevel = name.Substring(0, separatorIndex);
        string targetName = name.Substring(separatorIndex + 1);
        BpfLog.Error(BpfLogCategory.XR, "tex_core_head_menu.on_click_menu", null, null, BpfLogField.Of("menu_level", menuLevel));
        BpfLog.Error(BpfLogCategory.XR, "tex_core_head_menu.on_click_menu", null, null, BpfLogField.Of("target_name", targetName));
        switch (menuLevel)
        {
            case "L1":
                if (!IsModuleAllowed(targetName))
                {
                    return;
                }

                BpfLog.Info(BpfLogCategory.XR, "tex_core_head_menu.on_click_menu", TexCore_ConfigManager.self.datas.datas.FindRoot(node).Find(targetName).GetUrl());
                //TexCore_PlaneManager.self.ShowPlane(name, TexCore_ConfigManager.self.datas.datas.Find(name).GetUrl(),true,null);
                TexCore_PlaneManager.self.ShowPlane(targetName, TexCore_ConfigManager.self.datas.datas.FindRoot(node).Find(targetName).GetUrl(), true, null);
                //SkipModuleRefresh(name);
                break;
            case "L2":
                BpfLog.Error(BpfLogCategory.XR, "tex_core_head_menu.on_click_menu", null, null, BpfLogField.Of("target_name", targetName));
                TexCore_PlaneManager.self.activePlane.GetComponent<NodePanelBase>().LoadChild(targetName);
                break;
            default:
                break;
        }
       
    }

    public void OnClick(GameObject obj)
    {
        switch (obj.name)
        {
            case "s_退出": ReturnHome(); break;
            case "s_左切换":
                if (startIndex > 0) 
                {
                    startIndex--;
                    endIndex--;
                    MenuRefresh();
                }
                break;
            case "s_右切换":
                if (endIndex < topBtnParent.childCount-1)
                {
                    startIndex++;
                    endIndex++;
                    MenuRefresh();
                }
                break;
            default:
                break;
        }
    }

    public void ReturnHome()
    {
        if (TexCore_LoadScene.instance != null)
        {
            TexCore_LoadScene.instance.OnlyLoad("00Navigation");
        }
        else
        {
            SceneManager.LoadScene(TexCore_LoadScene.StartupSceneName);
        }
    }

    public void SkipModuleRefresh(string name)
    {
        if (!IsModuleAllowed(name))
        {
            return;
        }

        Transform btn = topBtnParent.Find("L1_" + name);
        if (btn == null) 
        {
            TexCore_LoadScene.instance.OnlyLoad("00Navigation");
        }
        else
        {
            TexCore_Interaction_Hololens sele = btn.GetComponentInChildren<TexCore_Interaction_Hololens>();
            sele.ResetBrother();
            sele.state = SelectState.Click;
            sele.isSelect = true;
            sele.SetSprite();
           
            activeL1 = btn;
        }
        MenuRefresh();
    }

    public bool IsModuleAllowed(string moduleName)
    {
        return allowedModuleNames == null || allowedModuleNames.Count == 0 ||
               allowedModuleNames.Contains(moduleName);
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
            TexCore_Interaction_Hololens sele = btn.GetComponentInChildren<TexCore_Interaction_Hololens>();
            sele.ResetBrother();
            sele.state = SelectState.Click;
            sele.isSelect = true;
            sele.SetSprite();
        }
        MenuRefresh();
    }

    private void UpdateMenuRange(int btnIndex)
    {
        if (btnIndex == topBtnParent.childCount - 2)
        {
            startIndex = btnIndex - 1;
            endIndex = btnIndex + 1;
        }

        startIndex = btnIndex < topBtnParent.childCount - 2 ? btnIndex : startIndex;

        endIndex = btnIndex + 2 < topBtnParent.childCount ? btnIndex + 2 : endIndex;
            //Mathf.Min(btnIndex + 2, topBtnParent.childCount - 1);
    }

    /// <summary>
    /// 菜单刷新
    /// </summary>
    private void MenuRefresh()
    {
       //for (int i = 0; i < topBtnParent.childCount; i++)
       //{
       //    var child = topBtnParent.GetChild(i).gameObject;
       //    bool shouldBeActive = (i >= startIndex && i <= endIndex);

       //    // 仅在需要改变状态时调用 SetActive
       //    if (child.activeSelf != shouldBeActive)
       //    {
       //        child.SetActive(shouldBeActive);
       //    }
       //}
       //LayoutRebuilder.ForceRebuildLayoutImmediate(topBtnParent.GetComponent<RectTransform>());
       //LayoutRebuilder.ForceRebuildLayoutImmediate(transform.GetComponent<RectTransform>());
    }
}
