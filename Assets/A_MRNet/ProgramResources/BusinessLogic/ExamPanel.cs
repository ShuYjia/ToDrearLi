using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Bpf.Diagnostics;

public class ExamPanel : TexCore_PlaneBase
{
    public TextMeshProUGUI data;
    public TextMeshProUGUI userName;

    public TextMeshProUGUI result;

    public static ExamPanel instance;

    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        //data.text = datas.texts[0];
        if (TexCore_Const.user!=null)
        {
            userName.text = TexCore_Const.user.StuName;
        }
        
    }
    public override void OnClick(GameObject obj)
    {
        base.OnClick(obj);
        if (obj.name== "实训考核")
        {
            //TexCore_LoadScene.instance.OnlyLoad("03TopicPage");
        }
        else if (obj.name == "理论考核")
        {
            //TexCore_LoadScene.instance.OnlyLoad("06TopicPage");
        }
        else if (obj.name == "return")
        {
            if (Input.GetKey(KeyCode.T) == false)//测试用,按住T键则不执行退出登录
            {
                //print("退出登录");
                //TexCore_SendData.instance.Logout();
            }
            TexCore_LoadScene.instance.OnlyLoad("02StartPage");

        }
        else if (obj.name.Equals("提交全部答案"))
        {
            BpfLog.Info(BpfLogCategory.Network, "exam_panel.on_click", "调用提交方法");
            if (SendAllExams())
            {
                //List<ExamRecordBase> sendAll = new List<ExamRecordBase>(TexCore_Const.exams1);
                //foreach (ExamRecordBase item in TexCore_Const.exams2)
                //{
                //    sendAll.Add(item);
                //}
                //LXF_SendData.instance.SubmitRecord(sendAll,Submit);
            }
        }
       
    }
   
    internal void Submit(WebResult obj)
    {
        result.text = obj.Msg;
        if (obj.State)
        {
            TexCore_LoadScene.instance.OnlyLoad("02StartPage");
        }
    }

    public bool SendAllExams()
    {
        int a = 0;
        string text = "";
        //if (TexCore_Const.exams1 != null && TexCore_Const.exams1.Count > 0)
        //{
        //    text += "实训考核模块已完成\n";
        //    a++;
        //}
        //else
        //{
        //    text += "实训考核模块未完成\n";
        //}
        //if (TexCore_Const.exams2 != null && TexCore_Const.exams2.Count > 0)
        //{
        //    text += "理论考核模块已完成\n";
        //    a++;
        //}
        //else
        //{
        //    text += "理论考核模块未完成\n";
        //}
        text += "理论考核模块已完成\n";
        result.text += text.Replace("\\n", "\n");
        hodel = 3;
        isHodel = true;
        return a == 2;
    }
    public bool isHodel = false;
    public float hodel = 0;
    private void Update()
    {
        if (isHodel)
        {
            if (hodel > 0f)
            {
                hodel -= Time.deltaTime;


            }
            else
            {
                result.text = "";
                isHodel = false;
            }
        }
    }
}

