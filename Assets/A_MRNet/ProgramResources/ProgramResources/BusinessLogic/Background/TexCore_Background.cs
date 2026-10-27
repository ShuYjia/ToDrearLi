using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Bpf.Diagnostics;

public class TexCore_Background : MonoBehaviour
{
    public static TexCore_Background instance;

    public void Awake()
    {
        if (TexCore_Background.instance != null)
        {
            Destroy(TexCore_Background.instance.gameObject);
        }
        instance = this;
    }

    /// <summary>
    /// 登录方法
    /// </summary>
    /// <param name="id">账号</param>
    /// <param name="pass">密码</param>
    /// <param name="loginReturn">登录成功回调函数</param>
    public void StudentLogin(string id, string pass, Action<LoginResult> loginReturn)
    {
        string ip = BackInterface.Self.IP;
        string inter = BackInterface.Self.DoStuLogin;
        string token = "";
        User user = new User(id, pass);
        StartCoroutine(WebLoding<LoginResult>(ip, inter, token, loginReturn, user));
    }

    public void TeacherLogin(string id, string pass, Action<MgrLoginResult> loginReturn)
    {
        string ip = BackInterface.Self.IP;
        string inter = BackInterface.Self.DoMgrLogin;
        string token = "";
        User user = new User(id, pass);
        StartCoroutine(WebLoding<MgrLoginResult>(ip, inter, token, loginReturn, user));
    }
    /// <summary>
    /// 登出方法
    /// </summary>
    public void Logout(Action<WebResult> action)
    {
        string ip = BackInterface.Self.IP;
        string inter = BackInterface.Self.Logout;
        string token = TexCore_Const.token;
        StartCoroutine(WebLoding(ip, inter, token, action));
    }

    public void GetExamNow(Action<ExamListResult> action)
    {
        string ip = BackInterface.Self.IP;
        string inter = BackInterface.Self.GetExamNow;
        string token = TexCore_Const.token;
        StartCoroutine(WebLoding<ExamListResult>(ip, inter, token, action));
    }

    /// <summary>
    /// 开始考试方法
    /// </summary>
    /// <param name="beginReturn">开始考试回调方法</param>
    public void BeginExam(int examid, Action<BeginExamResult> beginReturn)
    {
        string ip = BackInterface.Self.IP;
        string inter = BackInterface.Self.BeginExam+"?examid="+examid;
        string token = ""/*TexCore_Const.token*/;
        //beginReturn +=TexCore_TipsManager.Self.PrintResule;
        StartCoroutine(WebLoding<BeginExamResult>(ip, inter, token, beginReturn));
    }
    //public void UpLoadPicture(SendTexture sendTexture,Action<WebResult> action)
    //{
    //    string ip = BackInterface.Self.IP;
    //    string inter = BackInterface.Self.UploadExamPicBase64;
    //    string token = TexCore_Const.token;
    //    StartCoroutine(WebLoding<WebResult>(ip, inter, token, action,sendTexture));
    //}
    /// <summary>
    /// 提交答卷方法
    /// </summary>
    /// <param name="bases">答卷数据</param>
    /// <param name="subMitReturn">提交成功回调</param>
    public void SubmitRecord(List<ExamRecordBase> bases, Action<WebResult> subMitReturn)
    {
        string ip = BackInterface.Self.IP;
        string inter = BackInterface.Self.SubmitRecord;
        string token = TexCore_Const.token;
        StartCoroutine(WebLoding<WebResult>(ip,inter,token, subMitReturn, bases));
    }

    public void GetFileTree(Action<GetFileTreeResult> FileReturn)
    {
        string ip = BackInterface.Self.IP;
        string inter = BackInterface.Self.QueryPointTreeList;
        string token = TexCore_Const.token;
        StartCoroutine(WebLoding<GetFileTreeResult>(ip, inter, token, FileReturn));
    }

    public void GetOnlineFile(string code, Action<GetFileResult> FileReturn)
    {
        string ip = BackInterface.Self.IP;
        string inter = BackInterface.Self.QueryPointFiles+"?point=" + code; ;
        string token ="" /*TexCore_Const.token*/;
        StartCoroutine(WebLoding<GetFileResult>(ip, inter, token, FileReturn));
    }

    /// <summary>
    /// 获取练习题方法
    /// </summary>
    /// <param name="GetExercisesReturn">获取成功回调</param>
    public void GetExercises(Action<GetExercisesReturn> GetExercisesReturn)
    {

        StartCoroutine(WebLoding(BackInterface.Self.IP, BackInterface.Self.GetExercises + "single=" + BackInterface.Self.单选题数量 + "&multi="+ BackInterface.Self.单选题数量 + "&judge=" + BackInterface.Self.判断题数量, "", GetExercisesReturn));
    }



    /// <summary>
    /// 网络请求
    /// </summary>
    /// <param name="ip">服务器ip地址</param>
    /// <param name="inter">请求接口</param>
    /// <param name="Token">接口加密码</param>
    /// <param name="action">回调函数</param>
    /// <param name="data">发送数据</param>
    /// <returns></returns>
    public IEnumerator WebLoding<T>(string ip, string inter, string Token = "", Action<T> action = null, object data = null)
    {
        string url = "http://" + ip + inter.Split('_')[1] + (Token == "" ? "" : "?ju=" + Token);//请求地址组合
        BpfLog.Info(BpfLogCategory.Asset, "tex_core_background.info", "组合地址成功：" + url);
        using (var request = new UnityWebRequest(url, inter.Split('_')[0])) // 开启web请求,参数2为请求为POST还是GET
        {
            if (data != null)//如果需要发送数据
            {
                string postData = JsonConvert.SerializeObject(data);//将数据转换成json ，待会发给后台。
                byte[] bodyRaw = Encoding.UTF8.GetBytes(postData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                BpfLog.Info(BpfLogCategory.Asset, "tex_core_background.info", "组合数据成功" + postData);
            }
            //Debug.Log("设置数据头文件");
            request.SetRequestHeader("Content-Type", "application/json");//octet-stream
            DownloadHandler downloadHandler = new DownloadHandlerBuffer();
            //Debug.Log("创建下载器");
            request.downloadHandler = downloadHandler;
            BpfLog.Info(BpfLogCategory.Asset, "tex_core_background.info", "等待下载");
            yield return request.SendWebRequest(); //等待执行完毕 ---上面的都是固定写法，你可以不用理解
            BpfLog.Info(BpfLogCategory.Asset, "tex_core_background.info", "下载完成");
            if (request.downloadHandler.text != null && request.downloadHandler.text != "") //拿到返回的数据
            {
                BpfLog.Info(BpfLogCategory.Asset, "tex_core_background.info", "下载数据不为空"+request.downloadHandler.text);

                T result = JsonUtility.FromJson<T>(request.downloadHandler.text);
                BpfLog.Info(BpfLogCategory.Asset, "tex_core_background.info", "成功组合数据");
                if (action != null)
                {
                    action(result);
                }
            }
            else
            {
                BpfLog.Info(BpfLogCategory.Asset, "tex_core_background.info", "发起网络请求失败： 确认过闸接口 {}" + request.error);
            }
        }
    }
}
