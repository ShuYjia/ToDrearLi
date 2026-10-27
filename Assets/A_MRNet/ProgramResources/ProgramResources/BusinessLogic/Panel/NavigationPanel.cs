using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Bpf.Diagnostics;

public class NavigationPanel : TexCore_PlaneBase
{
    public static NavigationPanel instance;

    public string back = "01LoginPage";
    public string jumpLink;
    public string startPanel;
    public string endPanel;

    public GameObject showPanel;
    public GameObject childPanel;

    public GameObject examListPanel;

    public GameObject returnBtn;
    public GameObject quitBtn;

    private void Start()
    {
        instance = this;

        if(string.IsNullOrEmpty(TexCore_Const.startPanel))
        {
            Init(startPanel);
        }
        else
        {
            Init(TexCore_Const.startPanel);
            TexCore_Const.startPanel = null;
        }
    }

    public void OnEnable()
    {
        GameObject userName = GameObject.Find("用户名");
        //userName.GetComponent<TextMeshProUGUI>().text = TexCore_Const.user.StuName;
    }

    public override void OnClick(GameObject obj)
    {
        base.OnClick(obj);
        HandleNavigation(obj);
    }

    private void HandleNavigation(GameObject obj)
    {
        switch (obj.name)
        {
            case "退出":
                ReturnCondition();
                break;
            case "返回":
                ReturnCondition();
                break;
            case "实训原理":
                Init("06实训原理");
                break;
            case "实训目的":
                Init("07实训目的");
                break;
            case "开始实训":
                Init("03调查选择");
                break;
            case "练习模式":
            case "考核模式":
                HandleMode(obj.name);
                break;
            default:
                showPanel.GetComponent<ProcessDrag>()?.OnButtonClick(obj);
                if(examListPanel.gameObject.activeSelf) examListPanel.GetComponent<ExamListPanel>().OnClick(obj);
                break;
        }
    }
    static bool chooseFirstScene = false;

    private void HandleMode(string mode)
    {
        Mode selectedMode = mode == "练习模式" ? Mode.练习 : Mode.考核;
        TexCore_Const.mode = selectedMode;
        //bool chooseFirstScene = Random.Range(0f, 1f) < 0.5f;
        string sceneName = GetSceneName(endPanel, selectedMode, chooseFirstScene);
        switch (TexCore_Const.mode)
        {
            case Mode.练习:
                if (!string.IsNullOrEmpty(sceneName))
                {
                    TexCore_LoadScene.instance.OnlyLoad(sceneName);
                }
                break;
            case Mode.考核:
                // todo 获取考试列表，选择一个考试进入
                TexCore_Const.examScene = sceneName;
                examListPanel.SetActive(true);
                break;
            default:
                break;
        }
       
    }
    public void  SelectExam(ExamListResult result)
    {

    }

    private static bool bl1 = false;
    private static bool bl2 = false;
    private static bool bl3 = false;
    private static bool bl4 = false;

    private string GetSceneName(string panel, Mode mode, bool chooseFirstScene)
    {
        switch (panel)
        {
            case "MR多人协同系统":
                return mode == Mode.练习 ? "练习" : "考核";
            default:
                return string.Empty;
        }
    }

    public void GetExamReturn(ExamListResult result)
    {
        BpfLog.Info(BpfLogCategory.Network, "navigation_panel.get_exam_return", null, null, BpfLogField.Of("msg", result.Msg));
        if (result.Data.Length > 0)
        {
            int examid = result.Data[0].FlowID;
            TexCore_Background.instance.BeginExam(examid, BeginExamId);
        }
    }

    public void BeginExamId(BeginExamResult result)
    {
        BpfLog.Info(BpfLogCategory.Network, "navigation_panel.begin_exam_id", null, null, BpfLogField.Of("msg", result.Msg));
        if (result.Data.ExamID != 0)
        {
            TexCore_Const.topicDic = result;
            TexCore_LoadScene.instance.OnlyLoad("");
        }
    }

    public void Init(string name)
    {
        foreach (Transform t in childPanel.transform)
        {
            t.gameObject.SetActive(t.name == name);
            if (t.gameObject.activeSelf)
            {
                showPanel = t.gameObject;
            }
        }
        if (showPanel.name.Equals("01导航"))
        {
            quitBtn.SetActive(true);
            returnBtn.SetActive(false);
        }
        else
        {
            returnBtn.SetActive(true);
            quitBtn.SetActive(false);
        }
    }

    private void ReturnCondition()
    {
        string panelName = showPanel.name;
        if (panelName.Equals("01导航"))
        {
            Application.Quit();
            return;
            //TexCore_LoadScene.instance.OnlyLoad(back);
        }

        string targetPanel = panelName switch
        {
            "XXXXX" => "XXXXX",
            _ => null
        };
        if (targetPanel != null)
        {
            Init(targetPanel);
        }
    }
}
