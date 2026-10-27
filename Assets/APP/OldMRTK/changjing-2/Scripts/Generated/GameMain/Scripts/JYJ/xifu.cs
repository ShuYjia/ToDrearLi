using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changjing_2.ExportRuntime
{
    public class xifu : MonoBehaviour
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
               
                // 将物体A的位置设置为物体B的位置x轴
                transform.position = new Vector3(targetReference.position.x , targetReference.position.y, targetReference.position.z);
                transform.rotation = targetReference.rotation;
                // 锁定Rigidbody的Constraints值，包括rotation和position  
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ |
                                 RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ;
            }
        }
    }
}
