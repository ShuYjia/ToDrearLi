using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetWork03_2.ExportRuntime
{
    public class Attach_Di : MonoBehaviour
    {

        public Transform targetReference; // 用于吸附的目标物体的Transform引用  
        private Rigidbody rb; // 用于获取Rigidbody组件的引用  

        void Start()
        {
            rb = GetComponent<Rigidbody>(); // 获取Rigidbody组件的引用  
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Target")) // 检查是否与标记为"Target"的物体碰撞  
            {
                // 将物体A的位置设置为物体B的位置x轴+0.1
                if (targetReference == null || rb == null)
                {
                    return;
                }

                transform.position = new Vector3(targetReference.position.x, targetReference.position.y + 0.0125f, targetReference.position.z - 0.01f);
                transform.rotation = targetReference.rotation;

                // 将物体A的旋转角度变为X+90度  
                transform.rotation = Quaternion.Euler(new Vector3(transform.rotation.eulerAngles.x + 90, 0, 0));
                JiFen.FinishTask(JiFen.taskAtt_Di);//调用JiFen脚本中的FinishTask方法，表示吸附任务完成
                // LoadSceneObj.task = LoadSceneObj.Tesks.Linksocket2;

                // 锁定Rigidbody的Constraints值，包括rotation和position  
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ |
                                 RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ;
            }
        }
    }
}
