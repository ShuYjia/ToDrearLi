using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WHY_FuWuQi_6.ExportRuntime
{
    public class UCheckTask : MonoBehaviour
    {
            public GameObject MultCon;
        public GameObject targetObject; // 指定碰撞的目标物体  

        private void OnTriggerEnter(Collider other)
        {
                  MultCon = GameObject.Find("MultController");
            // 判断碰撞到的物体是否为目标物体B  
            if (other.gameObject == targetObject)
            {
                LoadSceneObj.task = LoadSceneObj.Tesks.TakeU;
                if (LoadSceneObj.isMult)
                    {
                        MultCon.GetComponent<MultCheckInfo>().CheckMultTask(0f);
                    }
                    else
                    {
                        MultCon.GetComponent<ShiXun>().SendUserInfo();
                    }
                LoadSceneObj.score += 1;
                Debug.Log("+1");
            }
        }
        // Start is called before the first frame update
        /*void Start()
        {
            LoadSceneObj.task = LoadSceneObj.Tesks.TakeU;
            LoadSceneObj.score += 1;
        }*/
    }
}
