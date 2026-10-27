using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changjing_2.ExportRuntime
{
    public class tiema : MonoBehaviour
    {
        public GameObject ui1;

        public GameObject MultCon;
        public Transform targetReference; // 用于吸附的目标物体的Transform引用
        public Transform parentOfTargetReference; // 用于B物体的父物体C的Transform引用
        private Rigidbody rb; // 用于获取Rigidbody组件的引用  
        public AudioSource audioSource;
        bool isPlayed = false;
        private bool ui1HasBeenShown = false; // 新增的布尔值，用于追踪UI1是否已显示
        void Start()
        {
            MultCon = GameObject.Find("MultController");
            rb = GetComponent<Rigidbody>(); // 获取Rigidbody组件的引用
            
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Erweima")) // 检查是否与标记为"Erweima"的物体碰撞  
            {
                 
                    if (!ui1HasBeenShown && ui1 != null)
                    {
                       ui1.SetActive(true);
                        ui1HasBeenShown = true; // 标记UI1为已显示
                                                // 
                    }
                    if (!isPlayed)
                    {
                        audioSource.Play();
                        isPlayed = true;
                    }
                
                
                // 将物体A的位置设置为物体B的位置x轴
                transform.position = new Vector3(targetReference.position.x, targetReference.position.y, targetReference.position.z);
                transform.rotation = targetReference.rotation;
                // 锁定Rigidbody的Constraints值，包括rotation和position  
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ |
                                 RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ;
                
                if (!isPlayed)
                {
                    audioSource.Play();
                    isPlayed = true;
                }
                LoadSceneObj.task = LoadSceneObj.Tesks.PasteQRcode;
                if (LoadSceneObj.isMult)
                    {
                        MultCon.GetComponent<MultCheckInfo>().CheckMultTask(1f);
                    }
                    else
                    {
                        MultCon.GetComponent<ShiXun>().SendUserInfo();
                    }
            }
            // 设置A物体为C物体的子物体  
            if (parentOfTargetReference != null) // 确保parentOfTargetReference不是null  
            {
                transform.SetParent(parentOfTargetReference);
            }
            else
            {
                Debug.LogError("parentOfTargetReference is not set! Please set it in the inspector or script.");
            }
            
        }
    }
}
