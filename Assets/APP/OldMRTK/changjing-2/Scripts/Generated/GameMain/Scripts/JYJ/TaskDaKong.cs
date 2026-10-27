using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace Changjing_2.ExportRuntime
{
    public class TaskDaKong : MonoBehaviour
    {
        public GameObject ui1;
        public GameObject MultCon;
        public GameObject kong1;
        public GameObject kong2;
        public GameObject kong3;
        public GameObject kong4;
        public GameObject target1;
        public GameObject target2;
        public GameObject target3;
        public GameObject target4;
        public AudioSource audioSource;
        bool isPlayed = false;
        private bool isUIVisible;  // 标志位，记录UI的显示状态



        //public GameObject pengzhuang1;
        //public GameObject pengzhuang2;
        //public GameObject pengzhuang3;
        //public GameObject pengzhuang4;
        // Start is called before the first frame update
        void Start()
        {
            

           if(ui1 != null){
                ui1.SetActive(false);
           }
            
            isUIVisible = false;
             MultCon = GameObject.Find("MultController");
        }

        // Update is called once per frame
        void Update()
        {

            if ((!kong1.GetComponent<MeshRenderer>().enabled || !kong1.activeSelf) &&
               (!kong2.GetComponent<MeshRenderer>().enabled || !kong2.activeSelf) &&
               (!kong3.GetComponent<MeshRenderer>().enabled || !kong3.activeSelf) &&
               (!kong4.GetComponent<MeshRenderer>().enabled || !kong4.activeSelf) )
               
            {
               
                target1.SetActive(false);
                target2.SetActive(false);
                target3.SetActive(false);
                target4.SetActive(false);
                
                if (!isPlayed)
                {
                    audioSource.Play();
                    isPlayed = true;
                }
                if (!isUIVisible)
                {
                    if(ui1 != null)
                    {
                        ui1.SetActive(true);
                    }
                    
                    isUIVisible = true;
                }
                LoadSceneObj.task = LoadSceneObj.Tesks.Drillholes;
                if (LoadSceneObj.isMult)
                    {
                        MultCon.GetComponent<MultCheckInfo>().CheckMultTask(0f);
                    }
                    else
                    {
                        MultCon.GetComponent<ShiXun>().SendUserInfo();
                    }
                    GetComponent<TaskDaKong>().enabled = false;
                //pengzhuang1.SetActive(false);
                //pengzhuang2.SetActive(false);
                //pengzhuang3.SetActive(false);
                //pengzhuang4.SetActive(false);
                //JiFen.FinishTask(JiFen.taskXiFu);//调用JiFen脚本中的FinishTask方法，表示吸附任务完成
                //LoadSceneObj.task = LoadSceneObj.Tesks.Robotpositioning9;

            }
            else
            {
                if (isUIVisible)
                {
                    // 物体A在物体B的范围内或部分重叠，隐藏UI
                    if (ui1 != null)
                    {ui1.SetActive(false);
                    }
                    
                    isUIVisible = false;
                }
            }
            /*else if ((ZiGongSi.GetComponent<MeshRenderer>().enabled || ZiGongSi3.GetComponent<MeshRenderer>().enabled ||
                ZiGongSi5.GetComponent<MeshRenderer>().enabled || ZiGongSi7.GetComponent<MeshRenderer>().enabled ||
                ZiGongSi9.GetComponent<MeshRenderer>().enabled || ZiGongSi11.GetComponent<MeshRenderer>().enabled ||
                ZiGongSi13.GetComponent<MeshRenderer>().enabled || ZiGongSi15.GetComponent<MeshRenderer>().enabled))
            {
             if ((!kong1.GetComponent<MeshRenderer>().enabled || !kong1.activeSelf) &&
            (!kong2.GetComponent<MeshRenderer>().enabled || !kong2.activeSelf) &&
            (!kong3.GetComponent<MeshRenderer>().enabled || !kong3.activeSelf) &&
            (!kong4.GetComponent<MeshRenderer>().enabled || !kong4.activeSelf))

            {
                ui2.SetActive(true);
                pengzhuang1.SetActive(false);
                pengzhuang2.SetActive(false);
                pengzhuang3.SetActive(false);
                pengzhuang4.SetActive(false);

            }*/
        }
    }
}
