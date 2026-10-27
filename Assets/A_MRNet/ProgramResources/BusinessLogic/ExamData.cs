using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bpf.Diagnostics;

public class ExamData : MonoBehaviour
{
    public static ExamData instance;

    public Dictionary<string, ExamRecordBase> allOperate;

    private void Awake()
    {
        instance = this;
    }
    public void AddExamData()
    {

    }

    public void SendAnswer()
    {
        //将每个题目做完后保存的内容，转换成可发送的数据类型数组

        List<ExamRecordBase> sendAll = new List<ExamRecordBase>();

        foreach (ExamRecordBase item in allOperate.Values)
        {
           
            sendAll.Add(item);
        }
        BpfLog.Info(BpfLogCategory.Network, "exam_data.send_answer", "实训组合数据成功");
        //TexCore_Const.exams1 = sendAll;
        ////string postData = JsonConvert.SerializeObject(sendAll);
        ////byte[] bodyRaw = Encoding.UTF8.GetBytes(postData);
        ////Debug.Log("组合数据成功" + postData);

        //LXF_SendData.instance.SubmitRecord(sendAll);

    }

}
