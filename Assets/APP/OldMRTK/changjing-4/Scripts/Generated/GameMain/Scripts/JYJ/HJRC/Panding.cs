using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

namespace Changjing_4.ExportRuntime
{
    public class Panding : MonoBehaviour
    {// 存储所有标记为"Huojia"的物体  
     

        private List<GameObject> huojias = new List<GameObject>();
        public GameObject MultCon;

        void Start()
        {
            MultCon = GameObject.Find("MultController");
            // 初始化时，查找所有标记为"Huojia"的物体  
            GameObject[] allHuojias = GameObject.FindGameObjectsWithTag("huojia");

            // 将找到的物体添加到列表中  
            foreach (GameObject huojia in allHuojias)
            {
                huojias.Add(huojia);
            }
        }

        void Update()
        {
            // 检查是否所有“火架”物体都消失了  
            if (AreAllHuojiasGone())
            {
                // 执行某个函数  
                ExecuteFunction();
            }
        }

        bool AreAllHuojiasGone()
        {
            // 遍历列表中的每个物体  
            foreach (GameObject huojia in huojias)
            {
                // 如果物体不为空且在层级中激活，则返回false  
                if (huojia != null && huojia.activeInHierarchy)
                {
                    return false;
                }
            }
            // 如果所有物体都消失了或禁用了，返回true  
            return true;
        }

        void ExecuteFunction()
        {
            // 这里写上当所有“火架”物体都消失时你想执行的代码  
            LoadSceneObj.task = LoadSceneObj.Tesks.Completeentry;
            if (LoadSceneObj.isMult)
                    {
                        MultCon.GetComponent<MultCheckInfo>().CheckMultTask(10f);
                    }
                    else
                    {
                        MultCon.GetComponent<ShiXun>().SendUserInfo();
                    }
            Debug.Log("所有货架物体都消失了！");
        }
    }
}
