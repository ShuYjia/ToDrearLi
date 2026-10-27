using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WHY_FuWuQi_6.ExportRuntime
{
    public class TextContent : MonoBehaviour
    {
        public GameObject objectA; // 物体A的引用    
        public GameObject objectB; // 物体B的引用    
        public Text text1; // 第一个文本组件的引用    
        public Text text2; // 第二个文本组件的引用    

        void Update()
        {
            // 确保text1和text2不为空，并且它们都有文本内容  
            if (text1 != null && text2 != null && !string.IsNullOrEmpty(text1.text) && !string.IsNullOrEmpty(text2.text))
            {
                // 判断text1和text2的文本内容是否相等  
                if (text1.text == text2.text)
                {
                    // 如果相等，则激活objectA，并可能地禁用objectB  
                    objectA.SetActive(true);
                    objectB.SetActive(false);
                }
                else
                {
                    // 如果不相等，则激活objectB，并可能地禁用objectA  
                    objectA.SetActive(false);
                    objectB.SetActive(true);
                }
            }
        }
    }
}
