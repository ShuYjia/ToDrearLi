using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HuaXian.ExportRuntime
{
    public class QRPaste : MonoBehaviour
    {

        //public Transform targetReference; // 用于吸附的目标物体的Transform引用  
        private Rigidbody rb; // 用于获取Rigidbody组件的引用
        public float offset = 5.0f; // 设置旋转角度容差
        public Image imageTip;//用于UI提醒
        public GameObject GestureRightHand;//用于右手控制器
        // Start is called before the first frame update
        void Start()
        {
            rb = GetComponent<Rigidbody>(); // 获取Rigidbody组件的引用 
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Target")) // 检查是否与标记为"Target"的物体碰撞  
            {
                // 将物体A的位置设置为物体B的位置
                transform.position = new Vector3(other.transform.position.x, 0.01f, other.transform.position.z);
                transform.rotation = new Quaternion(0, transform.rotation.y, 0, 1);
                // 锁定Rigidbody的Constraints值，只锁定位置不锁定旋转，以免用手摩擦二维码的时候会移位
                rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ;
                

                //打开右手上碰撞器
                BoxCollider handCollider = GestureRightHand != null ? GestureRightHand.GetComponent<BoxCollider>() : null;
                if (handCollider != null)
                {
                    handCollider.enabled = true;
                }

                // 计算A物体与B物体的旋转角度差值
                float angleDifference = Mathf.Abs(GetAngleDifference(transform, other.transform));
                Debug.Log("当前角度差值: " + angleDifference);

                // 检查角度差值是否在容差范围内
                if (angleDifference <= offset)
                {
                    // 将B物体的旋转角度赋给A物体
                    transform.rotation = other.transform.rotation;
                    Debug.Log("已在粘贴阈值之内给予旋转角度修正");
                   
                    //将二维码的旋转角度锁定，以免用手摩擦二维码的时候会移位
                    //rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
                }
                else 
                {
                    Debug.Log("粘贴角度超过阈值，不予修正");

                }
                imageTip.gameObject.SetActive(true);
                //transform.rotation = targetReference.rotation;
                //// 锁定Rigidbody的Constraints值，包括rotation和position  
                //rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ |
                //                 RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ;
            }
        }

        float GetAngleDifference(Transform a, Transform b)
        {
            Vector3 aForward = a.forward;
            Vector3 bForward = b.forward;
            // 计算两个向量之间的角度
            float angle = Vector3.Angle(aForward, bForward);
            // 计算两个向量叉乘的Z值，判断旋转方向
            float sign = Mathf.Sign(Vector3.Dot(Vector3.Cross(aForward, bForward), Vector3.up));
            return angle * sign;
        }


    }
}
