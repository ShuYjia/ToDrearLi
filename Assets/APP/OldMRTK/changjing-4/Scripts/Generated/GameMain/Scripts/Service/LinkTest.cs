using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System;
using MixedReality.Toolkit.UX;

// https://console-docs.apipost.cn/preview/1bb3ced5997330b9/d1475ba9800af58f?target_id=7f3c6410-432a-4da2-811c-1468ff0ebc17
namespace Changjing_4.ExportRuntime
{
    public class LinkTest : MonoBehaviour
    {

        //序列化接受到的json数据
        [Serializable]
        public class userdatainfo
        {
            public int code;
            public string message;
            public data data;

        }
        [Serializable]
        public class data
        {
            public user user;
            public string token;
        }
        [Serializable]
        public class user
        {
            public int id;
            public int user_type;
            public string name;
            public string mobile;
            public string avatar;
            public string email;
            public userData userData;
            public checkInfo checkInfo;
        }
        [Serializable]
        public class userData
        {
            public string name;
            public string student_id;
            public string[] category;
        }

        [Serializable]
        public class checkInfo
        {
            public string fk_training_check_id; //考核任务ID
            public string fk_training_group_id; //考核分组ID
            public int[] check_task; //考核详细步骤ID集
        }

        //定义接口URL和需要提交的数据
        private string postUrl = "http://mradmin.iot-vr.cn/login/app";
        private const string username = "username";
        private const string password = "password";

        //定义接收到的Json格式的字符串和序列化的类，以便于读取Json数据
        private string jsonData;
        public userdatainfo userinfo;
        public GameObject ULui;//登录界面
        public GameObject HtGui;//引导界面
        public GameObject puUI;
        

        public MRTKUGUIInputField userid;
        public MRTKUGUIInputField userpas;


        //用户登录协程
        IEnumerator UserLogin()
        {

            WWWForm form = new WWWForm();
            // 向表单中添加用户名字段，并设置默认??

            form.AddField(username, userid.text);
            form.AddField(password, userpas.text);
            UnityWebRequest request = UnityWebRequest.Post(postUrl, form);
            yield return request.SendWebRequest();//提交表单并收到返回??
            Debug.Log(request.result);
            //判断网络连接情况
            if (request.result == UnityWebRequest.Result.ProtocolError || request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError(request.error);
            }
            else
            {
                jsonData = request.downloadHandler.text;//将返回的值转为字符串类型
                Debug.Log(jsonData);
                userinfo = JsonUtility.FromJson<userdatainfo>(jsonData);//将返回值实例化方便读取返回??
                SaveUserLoginJson(UserLoginJsonPath(),jsonData);//调用保存方法
                request.Dispose();//当request完成请求后，使用Dispose()方法来释放资源，避免内存泄漏

            }
        }

        // 测试使用
        // void Start()
        // {
        //     //判断文件是否存在
        //     if (!File.Exists(UserLoginJsonPath()))
        //     {
        //         StartCoroutine(UserLogin());
        //     }
        //     else
        //     {
        //         // 读取文件数据
        //         string file = UserLoginJsonPath();
        //         // 使用using语句来管理资源。using语句会在离开作用域时自动调用Dispose()方法，从而确保资源被正确释放??
        //         using StreamReader streamReader = new StreamReader(file);
        //         // 将读取的数据转为相关类的实例，方便调用读??
        //         userinfo = JsonUtility.FromJson<userdatainfo>(streamReader.ReadToEnd());

        //     }

        // }
        void Start()
        {
            if (!File.Exists(UserLoginJsonPath()))
            {
                UserLoginUI();
            }
            else
            {
               
                // 读取文件数据
                string file = UserLoginJsonPath();
                // 使用using语句来管理资源。using语句会在离开作用域时自动调用Dispose()方法，从而确保资源被正确释放??
                using StreamReader streamReader = new StreamReader(file);
                // 将读取的数据转为相关类的实例，方便调用读??
                userinfo = JsonUtility.FromJson<userdatainfo>(streamReader.ReadToEnd());
                puUI.SetActive(true);
                HtGui.SetActive(true);
                ULui.SetActive(false);

            }
        }

        private void UserLoginUI()
        {
            // 打开登录界面
            ULui.SetActive(true);
            
        }

        // 登录
        public void UserLoginButton()
        {
            StartCoroutine(UserLogin());
            
            
        }

        // 设置保存位置
        public string UserLoginJsonPath()
        {
            return Application.streamingAssetsPath + "/UserLogin.json";
        }

        // 定义保存Json数据的方
        private StreamWriter writer;
        // 传入保存地址和字符串类型的json数据
        public void SaveUserLoginJson(string path, string jsondata)
        {
            // 如果相关文件不存在，则新建一个文
            if (!File.Exists(path))
            {
                writer = File.CreateText(path);
            }
            else
            {
                // 如果文件存在，先将原来的文件删除
                File.Delete(path);
                writer = File.CreateText(path);
            }
            // writer.Flush()：调用Flush方法将缓冲区的数据刷新到文件中
            // 在将数据写入文件时，通常会使用缓冲区以提高性能。调用Flush
            // 方法将缓冲区中的数据发送到文件，以确保数据被立即写入文件
            // writer.Dispose()：调用Dispose方法释放资源。在完成对文
            // 流的操作后，调用Dispose方法将释放与文件流相关的资源，如
            // 统资源或内存资源
            // writer.Close()：调用Close方法关闭文件流。在完成对文件流
            // 操作后，调用Close方法关闭文件流，使其无法进行任何 further 
            // operations。在调用Close方法之前，最好先调用Dispose方法
            // 释放资源
            writer.Flush();
            writer.Dispose();
            writer.Close();
            File.WriteAllText(path, jsondata);
            Debug.Log("保存成功");

            puUI.SetActive(true);
            HtGui.SetActive(true);
            ULui.SetActive(false);
            }

            
    }
}
