using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WHY_HJ.ExportRuntime
{
    public class HJ_MoveY : MonoBehaviour
    {
        private int collisionCount = 0; // 追踪碰撞次数   
        private Rigidbody SD; // 用于获取Rigidbody组件的引用  
        public GameObject gameObjectsA;//被敲击的物体
        public GameObject gameObjectsB;//确定最终位置的物体


        void OnCollisionEnter(Collision collision)
        {
            // 检查碰撞的物体是否是标签为ChuiZi  
            if (collision.collider.CompareTag("ChuiZi")) // 使用标签来识别锤子 
            {
                collisionCount++; // 增加碰撞次数  
                Debug.Log("+1");

                if (collisionCount < 3)
                {
                    // 向下移动0.01单位（注意这里使用了0.01而不是0.1，基于你的要求）  
                    transform.position = new Vector3(transform.position.x, transform.position.y - 0.01f, transform.position.z);
                    Debug.Log("xiayi");
                }
                if (collisionCount == 2)
                {
                    gameObjectsA.SetActive(false);
                    gameObjectsB.transform.position = gameObjectsA.transform.position;
                    gameObjectsB.SetActive(true);
                }
            }
        }
    }
}
