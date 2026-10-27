using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BackInterface
{
    private static BackInterface self;
    public static BackInterface Self
    {
        get
        {
            if (self == null)
            {
                self = new BackInterface();
            }
            return self;
        }
        set
        {
            self = value;
        }
    }


    public string IP = "162.14.111.55/bdc";

    public string GetConfig = "GET_/api/Config/GetConfig";

    public string SetConfig = "POST_/api/Config/SetConfig";

    // public string Start = "GET_/api/Exam/Start";//老师开启允许考试开关

    // public string Finish = "GET_/api/Exam/Finish";//老师结束考试（关闭允许开始考试开关）
#region 考试
    public string GetExamNow = "GET_/api/Exam/GetExamNow";// 获取当前学生现在可以参加的考试

    public string BeginExam = "GET_/api/Exam/BeginExam";//学生开始答题

    public string GetExercises = "GET_/api/Exam/GetExercises?";//获取练习题

    public string SubmitRecord = "POST_/api/Exam/SubmitRecord";//提交指定考题的答题，并结束考试

    public string UploadExamPicBase64= "POST_/api/Exam/UploadExamPicBase64";//提交图片在实训报告中
#endregion

#region FireMgr
    public string GetFiles = "GET_/api/FileMgr/GetFiles";//获取文件目录信息

    public string UpdateFileCache = "GET_/api/FileMgr/UpdateFileCache";//更新文件缓存

#endregion

    //public string GetReport = "GET_/api/Report/GetReport";

    public string DoStuLogin = "POST_/api/UserLogin/DoStuLogin";//学生登录接口, 登录接口调用成功后，才能调用其他接口 浏览器调用会自动建立1小时的有效Token 如果是客户端或unity3d调用则需要在后续的请求中使用 结果回传的Code作为用户Token, 并在请求的url上使用Url参数ju传入该Token 例如： http://xxxx.com/api/Exam/BeginExam?ju=XXXXXXXXXXX

    public string DoMgrLogin = "POST_/api/UserLogin/DoMgrLogin";//教师登录接口

    public string GetCurrentUser = "GET_/api/UserLogin/GetCurrentUser";//获取当前用户信息

    public string Logout = "GET_/api/UserLogin/Logout";//登出系统

#region OnlineFire
    public string QueryPointTreeList = "POST_/api/OnlineFile/QueryPointTreeList";// 获取所有的科目-模块-知识点 的结构树
    public string QueryPointFiles = "POST_/api/OnlineFile/QueryPointFiles";//  获取指定知识点下的课件列表


    #endregion

    public int 单选题数量 = 6;
    public int 多选题数量 = 0;
    public int 判断题数量 = 0;

}