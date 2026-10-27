using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System;
using Bpf.Diagnostics;

[Serializable]
public class Option 
{
    public string headline;
    public string name;
    public string title;
    public List<string> texts;
    public List<string> options;
}


public class TexCore_HeadMenu1 : MonoBehaviour
{
    public static TexCore_HeadMenu1 self;
    public string node;
    public string moduleName;
    public TextMeshProUGUI headText;
    public Transform fullPlane;
    public TexCore_Interaction_UGUI opBtn;
    public GameObject pre_topBtn;
    public RectTransform topBtnParent;

    public Transform textParent;
    public GameObject textTemplate;

    public int activeOption=0;

    public List<Option> options;


    private void Awake()
    {
        self = this;
    }

    public void OnEnable()
    {
        //GameObject userName = GameObject.Find("用户名");
        //if(!string.IsNullOrEmpty(TexCore_Const.user.StuName))
        //{
        //    userName.GetComponent<TextMeshProUGUI>().text = TexCore_Const.user.StuName;
        //}
    }

    public void InitBtns()
    {
        for (int i = 0; i < options.Count; i++)
        {
            GameObject newBtn = Instantiate(pre_topBtn, topBtnParent);
            newBtn.SetActive(true);
            newBtn.name = "Op_" + options[i].name + "_" + i;
            newBtn.transform.GetChild(0).name = "Op_" + options[i].name + "_" + i;
            newBtn.GetComponentInChildren<TextMeshProUGUI>().text = options[i].name;
            newBtn.GetComponentInChildren<TexCore_Interaction_UGUI>().itemParent = topBtnParent;
            LayoutRebuilder.ForceRebuildLayoutImmediate(newBtn.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(topBtnParent);
        }
        //topBtnParent.GetChild(0).GetComponentInChildren<TexCore_Interaction_UGUI>().OnClick();
        //SetData();
        string text = TexCore_ConfigManager.self.datas.datas.FindRoot(node)
                   .Find(options[0].options[0])
                   .GetUrl();

        text = moduleName + "/" + text.Split("/")[2].Substring(2);

        headText.text = text;
        TexCore_PlaneManager.self.ShowPlane(options[0].options[0],
            TexCore_ConfigManager.self.datas.datas.FindRoot(node).Find(options[0].options[0]).GetUrl(), true, null);
    }
    public void OnClickMenu(string _name)
    {
        opBtn.isSelect = false;
        opBtn.SetState(SelectState.Nomal);

        string[] strs = _name.Split("_");
        activeOption = int.Parse(strs[2]);
        //SetData();

        Option nextOption = options[activeOption];
        string nextNode = nextOption.options[0];
        BpfLog.Info(BpfLogCategory.XR, "tex_core_head_menu1.on_click_menu", TexCore_ConfigManager.self.datas.datas.FindRoot(node).Find(nextNode).GetUrl());

        string text = TexCore_ConfigManager.self.datas.datas.FindRoot(node).Find(nextNode).GetUrl();

        text = moduleName + "/" + text.Split("/")[2].Substring(2);

        headText.text = text;

        //TexCore_PlaneManager.self.ShowPlane(nextNode, TexCore_ConfigManager.self.datas.datas.Find(nextNode).GetUrl(),true,null);
        TexCore_PlaneManager.self.ShowPlane(nextNode, TexCore_ConfigManager.self.datas.datas.FindRoot(node).Find(nextNode).GetUrl(),true,null);
        
    }

    public void SetData()
    {
        fullPlane.gameObject.SetActive(true);
        fullPlane.transform.GetChild(0).Find("GameObject/任务序号/模块名称").GetComponent<TextMeshProUGUI>().text = options[activeOption].headline;
        int index = options[activeOption].name.IndexOf('：');
        if (index != -1)
        {
            fullPlane.transform.GetChild(0).Find("GameObject/name").GetComponent<TextMeshProUGUI>().text = options[activeOption].name.Substring(index + 1);
        }
        fullPlane.transform.GetChild(0).Find("GameObject/步骤名称/title").GetComponent<TextMeshProUGUI>().text = options[activeOption].title;

        for (int i = 0; i < textParent.childCount; i++)
        {
            Destroy(textParent.GetChild(i).gameObject);
        }

        for (int i = 0; i < options[activeOption].texts.Count; i++)
        {
            GameObject _text = Instantiate(textTemplate, textParent);
            _text.GetComponent<TextMeshProUGUI>().text = options[activeOption].texts[i].Replace("\\n", "\n");
            _text.SetActive(true);
        }
        TexCore_Interaction_UGUI btn = topBtnParent.GetChild(activeOption).GetComponentInChildren<TexCore_Interaction_UGUI>();
        btn.ResetBrother();
        btn.isSelect = true;
        btn.state = SelectState.Click;
        btn.SetSprite();
        //fullPlane.transform.GetChild(0).Find("text").GetComponent<TextMeshProUGUI>().text = options[activeOption].text.Replace("\\n", "\n");

    }

    public void OnClick(GameObject obj)
    {
        switch (obj.name)
        {
            case "s_退出":TexCore_LoadScene.instance.OnlyLoad("00Navigation"); break;
           
            default:
                break;
        }
    }

    public void SkipModuleRefresh(string name)
    {
        opBtn.isSelect=false;
        opBtn.SetState(SelectState.Nomal);
        for (int i = 0; i < options.Count; i++)
        {
           for(int j = 0; j<options[i].options.Count; j++)
            {
                if (name.Equals(options[i].options[j]))
                {
                    if (activeOption == i)
                    {
                       

                    }
                    else
                    {
                        activeOption = i;
                        //SetData();
                    }
                    string text = TexCore_ConfigManager.self.datas.datas.FindRoot(node).Find(name).GetUrl();

                    text = moduleName + "/" + text.Split("/")[2].Substring(2);

                    headText.text = text;
                    BpfLog.Info(BpfLogCategory.XR, "tex_core_head_menu1.skip_module_refresh", TexCore_ConfigManager.self.datas.datas.FindRoot(node).Find(name).GetUrl());
                    TexCore_PlaneManager.self.ShowPlane(name, TexCore_ConfigManager.self.datas.datas.FindRoot(node).Find(name).GetUrl(), true, null);
                    return;
                }
            }
        }

        switch (TexCore_Const.mode)
        {
            case Mode.练习:
                TexCore_LoadScene.instance.OnlyLoad("00Navigation");
                break;
            case Mode.考核:
                TexCore_ExamData.self?.Show(true);
                break;
            default:
                break;
        }
        
        
       
    }

   

   
}
