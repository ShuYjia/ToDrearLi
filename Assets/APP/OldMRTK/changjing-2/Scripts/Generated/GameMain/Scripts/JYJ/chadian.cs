using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Changjing_2.ExportRuntime
{
    public class chadian : MonoBehaviour
    {

        public Transform targetReference; // 用于吸附的目标物体的Transform引用  
        private Rigidbody rb; // 用于获取Rigidbody组件的引用  
        public GameObject AGV;
        public GameObject RCS;
        public GameObject ui1;
        public AudioSource audioSource;
        bool isPlayed = false;
        public GameObject MultCon;

        void Start()
        {
            MultCon = GameObject.Find("MultController");
            rb = GetComponent<Rigidbody>(); // 获取Rigidbody组件的引用
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Target")) // 检查是否与标记为"Target"的物体碰撞  
            {
                // 将物体A的位置设置为物体B的位置x轴
                transform.position = new Vector3(targetReference.position.x, targetReference.position.y, targetReference.position.z);
                transform.rotation = targetReference.rotation;
                JiFen.FinishTask(JiFen.taskCDZ_anzhuang);//调用JiFen脚本中的FinishTask方法，表示吸附任务完成
                
                // 锁定Rigidbody的Constraints值，包括rotation和position  
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ |
                                 RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ;
                AGV.SetActive (true);
                RCS.SetActive (true);
                ui1.SetActive(true);
                if (!isPlayed)
                {
                    audioSource.Play();
                    isPlayed = true;
                }
                LoadSceneObj.task = LoadSceneObj.Tesks.Pluginpower;
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
}
