/*
* @Description: 
* @Author: Li Yongqi
* @Date: 2024-06-14
* @LastEditTime: 2024-06-14
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WHY_FuWuQi_6.ExportRuntime
{
    public class StepTeskController : MonoBehaviour
    {
        //阶段开始任务
        public LoadSceneObj.Tesks starttask;
        //阶段结束任务
        public LoadSceneObj.Tesks endtask;
        //阶段任务简介
        // public GameObject  TeskIntro;
        // //阶段任务问题
        // public GameObject  TeskQuestion;
        //指引箭头
        public GameObject GuidingArrow;
        //指引位置数组
        public GameObject[] GuidingLocation;
        //指引ui提示数组
        public GameObject[] GuidanceTip;
        //指引语音数组
        // public GameObject[]  GuidanceVoice;
        public int taskposition;
        void Start()
        {
            taskposition = 0;

        }

        void Update()
        {
            if (GuidanceTip == null || GuidingLocation == null || GuidanceTip.Length == 0 || GuidingLocation.Length == 0)
            {
                return;
            }

            if ((LoadSceneObj.task - starttask) >= 0 && (LoadSceneObj.task - endtask) <= 0)
            {
                int nextTaskPosition = Mathf.Clamp(LoadSceneObj.task - starttask, 0, Mathf.Min(GuidanceTip.Length, GuidingLocation.Length) - 1);
                if (taskposition != nextTaskPosition)
                {
                    taskposition = nextTaskPosition;
                    foreach (GameObject i in GuidanceTip)
                    {
                        if (i == null)
                        {
                            continue;
                        }

                        i.SetActive(i == GuidanceTip[taskposition]);
                    }
                }

                if (taskposition >= 0 && taskposition < GuidingLocation.Length && GuidingLocation[taskposition] != null)
                {
                    UpdateArrow(GuidingLocation[taskposition]);
                }

                if (GuidingArrow != null && taskposition >= 0 && taskposition < GuidingLocation.Length && GuidingLocation[taskposition] != null)
                {
                    float dis = Vector3.Distance(GuidingArrow.transform.position, GuidingLocation[taskposition].transform.position);
                    if (dis < 1)
                    {
                        GuidingArrow.SetActive(false);
                    }
                    else if (GuidingArrow.activeSelf == false)
                    {
                        GuidingArrow.SetActive(true);
                    }
                }
            }
            else
            {
                gameObject.GetComponent<StepTeskController>().enabled = false;
                if (GuidingArrow != null)
                {
                    GuidingArrow.SetActive(false);
                }
                return;
            }

        }


        // 更新箭头指向
        public void UpdateArrow(GameObject obj)
        {
            if (GuidingArrow == null || obj == null)
            {
                return;
            }

            GuidingArrow.SetActive(true);
            GuidingArrow.transform.LookAt(obj.transform);
        }

    }
}
