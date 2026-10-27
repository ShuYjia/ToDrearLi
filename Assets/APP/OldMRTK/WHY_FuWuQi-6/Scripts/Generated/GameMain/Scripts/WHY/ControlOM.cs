using MixedReality.Toolkit.SpatialManipulation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WHY_FuWuQi_6.ExportRuntime
{
    public class ControlOM : MonoBehaviour
    {
        public GameObject ObjectA;
        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            if (ObjectA != null)
            {
                // 检查物体A是否激活  
                bool isActive = ObjectA.activeInHierarchy;

                // 根据激活状态打印消息到控制台  
                if (isActive)
                {
                    gameObject.GetComponent<ObjectManipulator>().enabled = false;
                }
                else
                {
                    gameObject.GetComponent<ObjectManipulator>().enabled = true;
                }

                // 如果你想基于物体A的激活状态来禁用或启用物体B上的某个组件  
                // 你可以在这里添加相应的代码  
            }
        }
    }
}
