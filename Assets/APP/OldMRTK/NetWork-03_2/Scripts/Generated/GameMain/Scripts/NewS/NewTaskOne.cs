using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NetWork03_2.ExportRuntime
{
    public class NewTaskOne : MonoBehaviour
    {
        public StepTesk.Tesks tesk;
        public Transform targetReference; // 用于吸附的目标物体的Transform引用  
        private Rigidbody rb; // 用于获取Rigidbody组件的引用  
        public Image taskImage;//用于显示任务页面
        public bool isTaskFinished = false;
        public GameObject lastStep;

        void Start()
        {
            rb = GetComponent<Rigidbody>(); // 获取Rigidbody组件的引用
        }

        void OnTriggerEnter(Collider other)
        {
            if (lastStep == null || taskImage == null || targetReference == null || rb == null)
            {
                return;
            }

            if (lastStep.tag == "yes")
            {
                if (other.CompareTag("Target")) // 检查是否与标记为"Target"的物体碰撞  
                {
                    // 将物体A的位置设置为物体B的位置x轴
                    transform.position = new Vector3(targetReference.position.x, targetReference.position.y, targetReference.position.z);
                    transform.rotation = targetReference.rotation;
                    //WHY
                    JiFen.FinishTask(JiFen.taskWangLuo);//调用JiFen脚本中的FinishTask方法，表示吸附任务完成
                    // 锁定Rigidbody的Constraints值，包括rotation和position  
                    rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ |
                                 RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ;
                    taskImage.gameObject.SetActive(true);
                    taskImage.gameObject.tag = "yes";
                    StepTesk.tesks = tesk;
                    isTaskFinished = true;
                    if (taskImage.gameObject.tag == "yes")
                    {
                        taskImage.gameObject.SetActive(false);
                    }
                }
            }

        }
    }
}
