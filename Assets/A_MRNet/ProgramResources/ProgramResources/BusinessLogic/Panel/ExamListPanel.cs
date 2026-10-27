
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExamListPanel : TexCore_PlaneBase
{
    public GameObject pre_examBtn;
    public Transform examParent;

    public int selectExam = -1;

    public GameObject listPanel;
    public GameObject textPanel;
    public TextMeshProUGUI result;
    private void Start()
    {
        TexCore_Background.instance.GetExamNow(BuildExamBtns);
    }
    public void ReStart()
    {
        selectExam = -1;
        for (int i = 0; i < examParent.childCount; i++)
        {
            Destroy(examParent.GetChild(i).gameObject);
        }
        TexCore_Background.instance.GetExamNow(BuildExamBtns);
    }

    public void BuildExamBtns(ExamListResult result)
    {
        if (result.State == false) return;
        for (int i = 0; i < result.Data.Length; i++)
        {
            GameObject examBtn = Instantiate(pre_examBtn, examParent);
            examBtn.SetActive(true);
            examBtn.name ="e_"+ result.Data[i].FlowID;
            examBtn.GetComponentInChildren<TexCore_SelectBase>().itemParent = examParent;
            examBtn.GetComponentInChildren<TextMeshProUGUI>().text = result.Data[i].ModuleCode;
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(examParent.GetComponent<RectTransform>());
    }

    public override void OnClick(GameObject obj)
    {
        base.OnClick(obj);
        switch (obj.name)
        {
            case "Return":
                if (listPanel.activeSelf==true)
                    TexCore_PlaneManager.self.ReturnPlane();
                else
                {
                    listPanel.SetActive(true);
                    textPanel.SetActive(false);
                }

                break;
            case "Quit":
                TexCore_LoadScene.instance.OnlyLoad("01LoginPage");
                break;
            case "关闭":
                gameObject.SetActive(false);

                break;
            case "刷新考试":
                ReStart();
                break;
            case "确定":
                
                if (selectExam!=-1)
                {
                    TexCore_Background.instance.BeginExam(selectExam, StartExam);
                    result.text = "正在进入考试";
                }
                else
                {
                    result.text = "未选择考试";
                }
                break;
            case "进入考试":
                
                TexCore_PlaneManager.self.ShowPlane(datas.childData[0].name, datas.childData[0].GetUrl(), true);
                break;
            default:
                string[] names = obj.name.Split('_');
                if (names[0].Equals("e"))
                {
                    //选择某个考试
                     int.TryParse( names[1],out selectExam);
                    TexCore_Const.ExamID = selectExam;
                }
                break;
        }
    }
    public void StartExam(BeginExamResult result)
    {
        this.result.text = result.Msg;
        if (result.State || Input.GetKey(KeyCode.T)) 
        {
            //listPanel.SetActive(false);
            //textPanel.SetActive(true);
            TexCore_Const.examData = result.Data;
            TexCore_LoadScene.instance.OnlyLoad(TexCore_Const.examScene);
        }
        //if (Input.GetKey(KeyCode.T))
        //{
        //    TexCore_Const.examData = result.Data;
        //    TexCore_PlaneManager.self.ShowPlane(datas.childData[0].name, datas.childData[0].GetUrl(), true);
        //}
    }
}
