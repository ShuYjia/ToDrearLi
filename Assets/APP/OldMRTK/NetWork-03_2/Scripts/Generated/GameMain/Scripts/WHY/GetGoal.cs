using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetWork03_2.ExportRuntime
{
    public class GetGoal : MonoBehaviour
    {
        public LoadSceneObj.Tesks tesks;
        public GameObject MultCon;

        public GameObject targetObject; // 指定碰撞的目标物体  

        private void OnTriggerEnter(Collider other)
        {

            // 判断碰撞到的物体是否为目标物体B  
            if (other.gameObject == targetObject)
            {
                LoadSceneObj.score += 0.5f;
                Debug.Log("+1");
                LoadSceneObj.task = tesks;
                MultCon = GameObject.Find("MultController");
                if (tesks != LoadSceneObj.Tesks.None)
                {
                    LoadSceneObj.task = tesks;
                    if (MultCon == null)
                    {
                        Debug.LogWarning("Export module: MultController was not found in the active scene.");
                        return;
                    }

                    if (LoadSceneObj.isMult)
                    {
                        MultCheckInfo multCheck = MultCon.GetComponent<MultCheckInfo>();
                        if (multCheck != null)
                        {
                            multCheck.CheckMultTask(1f);
                        }
                    }
                    else
                    {
                        ShiXun shiXun = MultCon.GetComponent<ShiXun>();
                        if (shiXun != null)
                        {
                            shiXun.SendUserInfo();
                        }
                    }
                }
            }
        }
    }
}
