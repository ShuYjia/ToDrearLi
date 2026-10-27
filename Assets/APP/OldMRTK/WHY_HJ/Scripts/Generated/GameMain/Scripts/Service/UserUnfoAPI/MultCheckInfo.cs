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
using System.Linq;

namespace WHY_HJ.ExportRuntime
{
    public class MultCheckInfo : MonoBehaviour
    {
        public GameObject shixun;
        public GameObject multiplayer;
        public LinkTest.userdatainfo userinfo;
        //判断是否进入多人模式
        public void CheckMultUserInfo()
        {
            if (shixun == null)
            {
                shixun = GameObject.Find("SHIXUN");
            }

            if (multiplayer == null)
            {
                multiplayer = GameObject.Find("DuoRenXieTong");
            }
            string file = Application.streamingAssetsPath + "/UserLogin.json";
            // 使用using语句来管理资源。using语句会在离开作用域时自动调用Dispose()方法，从而确保资源被正确释放。
            using StreamReader streamReader = new StreamReader(file);
            // 将读取的数据转为相关类的实例，方便调用读取
            userinfo = JsonUtility.FromJson<LinkTest.userdatainfo>(streamReader.ReadToEnd());

            if(userinfo.data.user.checkInfo != null && userinfo.data.user.checkInfo.fk_training_check_id != null
            && userinfo.data.user.checkInfo.fk_training_group_id != null && userinfo.data.user.checkInfo.check_task != null)
            {
                LoadSceneObj.isMult = true;
                if (shixun != null) shixun.SetActive(false);
                if (multiplayer != null) multiplayer.SetActive(true);
            }
            else
            {
                LoadSceneObj.isMult = false;
                if (shixun != null) shixun.SetActive(true);
                if (multiplayer != null) multiplayer.SetActive(false);
            }
        }

        //判断多人是否得分,任务完成后调用
        public void CheckMultTask(float score){
            if(LoadSceneObj.isMult){
                if(userinfo.data.user.checkInfo.check_task.Contains((int)LoadSceneObj.task)){
                    MultInformation info = GetComponent<MultInformation>();
                    if (info != null)
                    {
                        info.AddScore((int)LoadSceneObj.task, score);
                    }
                }
            }
        }
    }
}
