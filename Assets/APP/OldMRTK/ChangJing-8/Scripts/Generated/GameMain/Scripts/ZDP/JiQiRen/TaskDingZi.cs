using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace ChangJing_8.ExportRuntime
{
    public class TaskDingZi : MonoBehaviour
    {
        public GameObject ui1;
        public GameObject ui2;
        public GameObject ui3;
        public GameObject ui4;//UI指引-拿掉箱盖和推出机器人
        public GameObject ui5;// UI指引-按下机器人开关键
        public GameObject ZiGongSi;
        public GameObject ZiGongSi3;
        public GameObject ZiGongSi5;
        public GameObject ZiGongSi7;
        public GameObject ZiGongSi9;
        public GameObject ZiGongSi11;
        public GameObject ZiGongSi13;
        public GameObject ZiGongSi15;
        //public AudioSource audioSource;
        private bool isUIVisible = false; // 标志位，记录UI的显示状态
        private bool isPlayed = false;
        private bool isUI1Visible = false;
        private bool isUI2Visible = false;
        private bool isUI5Visible = true;


        // Start is called before the first frame update
        void Start()
        {
            ui1.SetActive(false);
            ui2.SetActive(false);
            ui3.SetActive(false);
            ui4.SetActive(false);
            ui5.SetActive(false);
            //audioSource = this.GetComponent<AudioSource>();
        }

        // Update is called once per frame
        void Update()
        {
            if (ui1.activeSelf) {
                isUI1Visible = true;
            }
            if (ui2.activeSelf)
            {
                isUI2Visible = true;
            }
            if (isUI5Visible && isUI1Visible && isUI2Visible && !ui1.activeSelf && !ui1.activeSelf) {
                isUI5Visible = false;
                ui5.SetActive(true);
            }
            bool allDisabled = (!ZiGongSi.GetComponent<MeshRenderer>().enabled || !ZiGongSi.activeSelf) &&
                               (!ZiGongSi3.GetComponent<MeshRenderer>().enabled || !ZiGongSi3.activeSelf) &&
                               (!ZiGongSi5.GetComponent<MeshRenderer>().enabled || !ZiGongSi5.activeSelf) &&
                               (!ZiGongSi7.GetComponent<MeshRenderer>().enabled || !ZiGongSi7.activeSelf) &&
                               (!ZiGongSi9.GetComponent<MeshRenderer>().enabled || !ZiGongSi9.activeSelf) &&
                               (!ZiGongSi11.GetComponent<MeshRenderer>().enabled || !ZiGongSi11.activeSelf) &&
                               (!ZiGongSi13.GetComponent<MeshRenderer>().enabled || !ZiGongSi13.activeSelf) &&
                               (!ZiGongSi15.GetComponent<MeshRenderer>().enabled || !ZiGongSi15.activeSelf);

            if (allDisabled)
            {
                if (!isUIVisible)
                {
                    //ui1.SetActive(true);
                    //ui2.SetActive(true);
                    //ui3.SetActive(true);
                    ui4.SetActive(true);
                    JiFen.FinishTask(JiFen.taskXiFu); // 调用JiFen脚本中的FinishTask方法，表示吸附任务完成
                    LoadSceneObj.task = LoadSceneObj.Tesks.PushoutGQR;
                    if (!isPlayed)
                    {
                        //audioSource.Play();
                        isPlayed = true;
                    }
                    isUIVisible = true;
                    GameObject MultCon = GameObject.Find("MultController");
                    if (LoadSceneObj.isMult)
                    {
                        MultCon.GetComponent<MultCheckInfo>().CheckMultTask(0f);
                    }
                    else
                    {
                        MultCon.GetComponent<ShiXun>().SendUserInfo();
                    }
                    GetComponent<TaskDingZi>().enabled = false;
                }
            }
            else
            {
                if (isUIVisible)
                {
                    ui1.SetActive(false);
                    ui2.SetActive(false);
                    ui3.SetActive(false);
                    ui4.SetActive(false);
                }
            }
        }
    }
}
