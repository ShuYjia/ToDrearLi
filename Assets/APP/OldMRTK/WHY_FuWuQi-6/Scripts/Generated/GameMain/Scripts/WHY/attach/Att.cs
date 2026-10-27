using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WHY_FuWuQi_6.ExportRuntime
{
    public class Att : MonoBehaviour
    {

        public Transform targetReference; // 用于吸附的目标物体的Transform引用  
        private Rigidbody rb; // 用于获取Rigidbody组件的引用  
        public GameObject ZhiYinUI;
        public GameObject ZhiYinUIBG;

        void Start()
        {
            rb = GetComponent<Rigidbody>(); // 获取Rigidbody组件的引用  
        }

        void OnTriggerEnter(Collider other)
        {


            if (other.CompareTag("Target")) // 检查是否与标记为"Target"的物体碰撞  
            {
                // 将物体A的位置设置为物体B的位置x轴
                transform.position = new Vector3(targetReference.position.x, targetReference.position.y + 0.0125f, targetReference.position.z);
                transform.rotation = targetReference.rotation;
                //ZDP
                JiFen.FinishTask(JiFen.taskAtt_UPan);//调用JiFen脚本中的FinishTask方法，表示吸附任务完成
                ZhiYinUIBG.SetActive(true);
                ZhiYinUI.SetActive(true);
                ZhiYinUI.GetComponent<AudioSource>().Play();
                LoadSceneObj.task = LoadSceneObj.Tesks.InsertUSB;

                // 锁定Rigidbody的Constraints值，包括rotation和position  
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ |
                                 RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ;
            }
        }
    }
}
