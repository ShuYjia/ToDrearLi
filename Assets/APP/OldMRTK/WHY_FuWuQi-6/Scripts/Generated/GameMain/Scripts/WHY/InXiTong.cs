using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WHY_FuWuQi_6.ExportRuntime
{
    public class InXiTong : MonoBehaviour
    {
        public GameObject myPanel; // 指向要控制的UI元素的引用  
        public GameObject targetObject; // 指定碰撞的目标物体  
        public bool isVisible = false; // 控制UI元素是否可见的标志位  

        private void OnTriggerEnter(Collider other)
        {
            // 判断碰撞到的物体是否为目标物体B  
            if (other.gameObject == targetObject)
            {
                // 发生碰撞时显示或隐藏UI元素  
                myPanel.SetActive(isVisible);
                isVisible = !isVisible; // 切换UI元素的可见状态  
            }
        }
    }
}
