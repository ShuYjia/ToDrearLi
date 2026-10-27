using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changjing_2.ExportRuntime
{
    public class banshouAnimation : MonoBehaviour
    {
        public Animator animator; // 引用Animator组件  

        // Start is called before the first frame update
        void Start()
        {

        }
        void OnTriggerEnter(Collider other)
        {
            // 检查是否与目标碰撞  
            if (other.CompareTag("chuizi"))
            {
                animator.enabled = true;  // 启用Animator组件 
            }
        }
        void OnTriggerExit(Collider other)
        {
            // 检查是否与目标碰撞  
            if (other.CompareTag("chuizi"))
            {
                animator.enabled = false;  // 启用Animator组件 
            }
        }
    }
}
