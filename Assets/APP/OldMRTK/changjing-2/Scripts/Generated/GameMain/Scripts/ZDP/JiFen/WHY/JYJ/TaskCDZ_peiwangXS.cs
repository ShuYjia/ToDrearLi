using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changjing_2.ExportRuntime
{
    public class TaskCDZ_peiwangXS : MonoBehaviour
    {
        public GameObject ChongQi;
        private bool lastState;
        public GameObject MultCon;

        void Start()
        {
            // 确保使用正确的属性来检查激活状态。  
            lastState = ChongQi.activeSelf;
            MultCon = GameObject.Find("MultController");
        }

        void Update()
        {
            // 检查当前激活状态是否与上次状态不同  
            if (ChongQi.activeSelf != lastState)
            {
                // 更新lastState为当前状态  
                lastState = ChongQi.activeSelf;
                // 如果GameObject未激活，则完成任务  
                if (!ChongQi.activeSelf)
                {
                    CompleteTask();
                }
            }
        }

        private void CompleteTask()
        {
            // 假设在项目的其他位置已经定义了JiFen.taskXiTong和JiFen.WanCheng  
            //PlayerPrefs.SetString(JiFen.taskCDZ_peiwang, JiFen.WanCheng);
            LoadSceneObj.task = LoadSceneObj.Tesks.AGVnetwork;
             if (LoadSceneObj.isMult)
                    {
                        MultCon.GetComponent<MultCheckInfo>().CheckMultTask(1f);
                    }
                    else
                    {
                        MultCon.GetComponent<ShiXun>().SendUserInfo();
                    }
        }
    }
}
