/*
* @Description: 
* @Author: Li Yongqi
* @Date: 2024-12-18
* @LastEditTime: 2024-12-18
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using System;
namespace WHY_BZQ.ExportRuntime
{
    public class ShiXun : MonoBehaviour
    {
        private LinkTest.userdatainfo userinfo;
        public string postUrl = "http://mradmin.iot-vr.cn/api/training_studyRecords";

        public int user_id;
        public int sort_step;

        private string utoken = "";

        public void GetUserInfo(){
            string file = Application.streamingAssetsPath + "/UserLogin.json";
            // 使用using语句来管理资源。using语句会在离开作用域时自动调用Dispose()方法，从而确保资源被正确释放。
            using StreamReader streamReader = new StreamReader(file);
            // 将读取的数据转为相关类的实例，方便调用读取
            userinfo = JsonUtility.FromJson<LinkTest.userdatainfo>(streamReader.ReadToEnd());
            user_id = userinfo.data.user.id;
            utoken = userinfo.data.token;
            sort_step = (int)LoadSceneObj.task;
            //sort_step = 25;
        }

        public void SendUserInfo(){
            if(LoadSceneObj.isMult){
                return;
            }
            GetUserInfo();
            StartCoroutine(Send());
        }

        IEnumerator Send()
        {

            WWWForm form = new WWWForm();
            // 向表单中添加用户名字段，并设置默认??

            form.AddField("user_id", user_id);
            form.AddField("sort_step", sort_step);
            Debug.Log(form.ToString());
            UnityWebRequest request = UnityWebRequest.Post(postUrl, form);
            
            request.SetRequestHeader("x-token", utoken);
            yield return request.SendWebRequest();//提交表单并收到返回??
            //判断网络连接情况
            if (request.result == UnityWebRequest.Result.ProtocolError || request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError(request.error);
                
            }
            else
            {
                string jsonData = request.downloadHandler.text;//将返回的值转为字符串类型
                Debug.Log(jsonData);
                request.Dispose();//当request完成请求后，使用Dispose()方法来释放资源，避免内存泄漏

            }
        }

    }
}
