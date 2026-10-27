using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WHY_FuWuQi_6.ExportRuntime
{
    public class TaskXiTongXS : MonoBehaviour
    {
        public GameObject ChongQi;
        private bool lastState;

        void Start()
        {
            // 确保使用正确的属性来检查激活状态。  
            lastState = ChongQi.activeSelf;
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
            PlayerPrefs.SetString(JiFen.taskXiTong, JiFen.WanCheng);
            // LoadSceneObj.task = LoadSceneObj.Tesks.Restartsystem2;
        }
    }
}
