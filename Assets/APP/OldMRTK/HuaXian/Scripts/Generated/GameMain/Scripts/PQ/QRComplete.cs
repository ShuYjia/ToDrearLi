using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HuaXian.ExportRuntime
{
    public class QRComplete : MonoBehaviour
    {
        // Start is called before the first frame update
       // public Image progressBar; // 引用进度条的Image组件
        //public Text textProgress; // 引用显示进度的Text组件

        private bool isTriggered = false; // 是否正在触发
        private float timer = 0f; // 计时器
        private float totalTime = 10f; // 总时间
        //private float fillSpeed = 0.1f; // 进度条填充速度
        public GameObject QRPanel;//显示面板
        public Slider slider;//显示进度条
        public Text slidertext;//用来显示进度文本
        public Text TagText;//用来显示标题文本
        public Image CompleteImage;//用来显示粘贴成功的图片
        public GameObject GrabInteractable;//用来给摩擦的那个手势赋值，目前用的是一个小方块代替的


        void Start()
        {
            
        }


        private void OnTriggerEnter(Collider other)
        {
            
        }

        private void OnTriggerExit(Collider other)
        {
            isTriggered = false;
            QRPanel.gameObject.SetActive(false);
            if (other.gameObject.name != "MRTK RightHand Controller")
            {
                if (other.GetComponent<BoxCollider>()) 
                { 
                other.GetComponent<BoxCollider>().enabled = false;

                }
            }

        }

        private void OnTriggerStay(Collider other)
        {

            if (other.gameObject.name == "MRTK RightHand Controller") {
                QRPanel.gameObject.SetActive(true);
                isTriggered = true;
            }
           
        }
        // Update is called once per frame
        void Update()
        {
            if (isTriggered)
            {
                // 增加计时器
                timer += Time.deltaTime;

                // 计算进度百分比
                float progressPercentage = timer / totalTime;

                // 更新进度条和文本
                slider.value = progressPercentage;
                slidertext.text = $"{Mathf.RoundToInt(progressPercentage * 100)}%";

                // 如果达到100%，停止计时
                if (progressPercentage >= 1f)
                {
                    //StopAllCoroutines();
                    isTriggered = false;
                    CompleteImage.gameObject.SetActive(true);
                    //slider.value = 0f;
                    //slidertext.text = "0%";
                    slider.gameObject.SetActive(false);
                    TagText.gameObject.SetActive(false);
                    slidertext.gameObject.SetActive(false);
                    //打开右手上碰撞器
                    BoxCollider grabCollider = GrabInteractable != null ? GrabInteractable.GetComponent<BoxCollider>() : null;
                    if (grabCollider != null)
                    {
                        grabCollider.enabled = false;
                    }
                    //gameObject.GetComponent<BoxCollider>().enabled = false;
                    
                }
            }

        }

        //public void ShutOff() 
        //{
        //    CompleteImage.gameObject.SetActive(false);


        //}








        /*public void StartTrigger()
        {
            isTriggered = true;
            timer = 0f;
            StartCoroutine(WaitForTrigger());
        }

        // 等待触发结束
        IEnumerator WaitForTrigger()
        {
            yield return new WaitForSeconds(totalTime);
            isTriggered = false;
        }
        */





    }
}
