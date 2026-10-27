using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WHY_FuWuQi_6.ExportRuntime
{
    public class TextDontNull : MonoBehaviour
    {
        public GameObject objectA; // 物体B的引用    
        public Text text1; // 第一个文本组件的引用    
        public Text text2; // 第二个文本组件的引用  
        public Text text3; // 第三个文本组件的引用  
        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            // 确保text1不为空，并且它们都有文本内容  
            if (text1 != null && !string.IsNullOrEmpty(text1.text)&& text2 != null && !string.IsNullOrEmpty(text2.text)&& text3 != null && !string.IsNullOrEmpty(text3.text))
            {
                objectA.SetActive(true);
            }
            else
            {
                objectA.SetActive(false);
            }
        }
    }
}
