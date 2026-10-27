/*
* @Description: 
* @Author: Li Yongqi
* @Date: 2024-12-18
* @LastEditTime: 2024-12-18
*/

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

namespace WHY_BZQ.ExportRuntime
{
    [System.Serializable]
    public class ScoreItem
    {
        public int sort_step;
        public float score;

        public ScoreItem(int sortStep, float score)
        {
            this.sort_step = sortStep;
            this.score = score;
        }
    }

    //发送得分结果
    public class MultInformation : MonoBehaviour
    {
        public string postUrl = "http://mradmin.iot-vr.cn/api/training_examRecords";
        private LinkTest.userdatainfo userinfo;
        public int user_id;
        public string group_id;
        private string utoken = "";
        public List<ScoreItem> scoreList = new List<ScoreItem>();

        // 添加分数
        public void AddScore(int sortStep, float score)
        {
            scoreList.Add(new ScoreItem(sortStep, score));
        }

        public void GetUserInfo(){
            string file = Application.streamingAssetsPath + "/UserLogin.json";
            // 使用using语句来管理资源。using语句会在离开作用域时自动调用Dispose()方法，从而确保资源被正确释放。
            using StreamReader streamReader = new StreamReader(file);
            // 将读取的数据转为相关类的实例，方便调用读取
            userinfo = JsonUtility.FromJson<LinkTest.userdatainfo>(streamReader.ReadToEnd());

            // userinfo = GetComponent<MultCheckInfo>().userinfo;
            
            Debug.Log(userinfo);
            user_id = userinfo.data.user.id;
            group_id = userinfo.data.user.checkInfo.fk_training_group_id;
            utoken = userinfo.data.token;
            // TestSore();
            // Debug.Log("user_id:" + user_id);
        }



        public void SendMultUserInfo(){
            GetUserInfo();
            StartCoroutine(Send());
        }

        IEnumerator Send()
        {
            string scoreJson = SerializeScoreList(scoreList);
            WWWForm form = new WWWForm();
            // 向表单中添加用户名字段，并设置默认??
            form.AddField("user_id", user_id);
            form.AddField("group_id", group_id);
            form.AddField("score", scoreJson);
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

        public void TestSore(){
            AddScore(1, 0.5f);
            AddScore(2, 1);
        }

        private static string SerializeScoreList(List<ScoreItem> scores)
        {
            if (scores == null || scores.Count == 0)
            {
                return "[]";
            }

            StringBuilder builder = new StringBuilder();
            builder.Append('[');
            for (int i = 0; i < scores.Count; i++)
            {
                ScoreItem item = scores[i];
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append("{\"sort_step\":");
                builder.Append(item.sort_step);
                builder.Append(",\"score\":");
                builder.Append(item.score.ToString(System.Globalization.CultureInfo.InvariantCulture));
                builder.Append('}');
            }

            builder.Append(']');
            return builder.ToString();
        }

    }
}
