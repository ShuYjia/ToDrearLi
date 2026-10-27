using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changjing_2.ExportRuntime
{
    public class dinggaitishi : MonoBehaviour
    {
        public GameObject UI1;

        public GameObject MultCon;
        public Vector3 lastPosition;
        private bool ui1HasBeenShown = false; // 新增的布尔值，用于追踪UI1是否已显示
        public AudioSource audioSource;
        private bool isPlayed = false;

        // Start is called before the first frame update
        void Start()
        {
            // 初始化位置为当前位置  
            lastPosition = transform.position;
            audioSource = GetComponent<AudioSource>();
             MultCon = GameObject.Find("MultController");

        }
        private void LateUpdate()
        {
            // 检查位置是否发生变化（这里只检查位置，你也可以检查旋转或缩放）  
            if (transform.position != lastPosition)
            {
                
                // 只有当UI1尚未显示过时，才设置其为激活状态  
                if (!ui1HasBeenShown && UI1 != null)
                {
                    UI1.SetActive(true);
                    ui1HasBeenShown = true; // 标记UI1为已显示
                                            // 
                }
                if (!isPlayed)
                {
                    audioSource.Play();
                    isPlayed = true;
                }
                LoadSceneObj.task = LoadSceneObj.Tesks.Removelid;
                if (LoadSceneObj.isMult)
                    {
                        MultCon.GetComponent<MultCheckInfo>().CheckMultTask(0f);
                    }
                    else
                    {
                        MultCon.GetComponent<ShiXun>().SendUserInfo();
                    }
                    GetComponent<dinggaitishi>().enabled = false;
            }
        }
        // 假设这是UI1上按钮点击事件的处理方法  

    }
}
