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



namespace ChangJing_8.ExportRuntime
{
    public class StepTesk : MonoBehaviour
    {
        public enum Tesks
        {
            None,
            CabinetPowerSupply,
            APPowerSupply,
            MonitorAGV,
            ServerConnectionAC,
            JHJConnectionAC,
            APConnectionJHJ,
            ServerPowerSupply,
            JHJPowerSupply,
            ACPowerSupply,
            ServerStartup,
        }
        public static Tesks tesks;
        //指引箭头
        public GameObject GuidingArrow;
        //指引位置数组
        public GameObject[] GuidingLocation;
        //指引ui提示数组
        public GameObject[] GuidanceTip;
        //当前任务序号
        public int taskposition;

        void Start()
        {
            tesks = Tesks.None;
            taskposition = 0;
            //bg.SetActive(false);
        }

        void Update()
        {
            if (GuidanceTip == null || GuidingLocation == null || GuidanceTip.Length == 0 || GuidingLocation.Length == 0)
            {
                return;
            }

            int nextTaskPosition = Mathf.Clamp((int)tesks, 0, Mathf.Min(GuidanceTip.Length, GuidingLocation.Length) - 1);
            if (taskposition != nextTaskPosition)
            {
                taskposition = nextTaskPosition;

                if (taskposition >= 0 && taskposition < GuidanceTip.Length && GuidanceTip[taskposition])
                {
                    UpdateTip(taskposition);
                }
            }

            if (taskposition >= 0 && taskposition < GuidingLocation.Length && GuidingLocation[taskposition] != null)
            {
                UpdateArrow(GuidingLocation[taskposition]);
            }
        }


        // 更新箭头指向
        public void UpdateArrow(GameObject obj)
        {
            if (GuidingArrow == null || obj == null)
            {
                return;
            }

            if (GuidingArrow.activeInHierarchy)
            {
                GuidingArrow.SetActive(true);
                float dis = Vector3.Distance(GuidingArrow.transform.position, obj.transform.position);
                if (dis < 1)
                {
                    GuidingArrow.SetActive(false);
                }
                else if (GuidingArrow.activeSelf == false)
                {
                    GuidingArrow.SetActive(true);
                }

                GuidingArrow.transform.LookAt(obj.transform);
            }
            //GuidingArrow.SetActive(true);
            //float dis = Vector3.Distance(GuidingArrow.transform.position, GuidingLocation[taskposition].transform.position);
            //    if (dis < 1)
            //    {
            //        GuidingArrow.SetActive(false);
            //    }
            //    else{
            //        if (GuidingArrow.activeSelf == false)
            //        {
            //            GuidingArrow.SetActive(true);
            //        }
                    
            //    }
            //GuidingArrow.transform.LookAt(obj.transform);
        }
        // 更新任务提示
        public void UpdateTip(int taskposition)
        {
            // 遍历所有提示对象
            foreach (GameObject obj in GuidanceTip)
            {
                // 如果提示对象存在
                if (obj)
                {
                    // 将提示对象设置为不活跃
                    obj.SetActive(false);
                }
            }
           //bg.SetActive(true);
            // 将当前任务位置的提示对象设置为活跃
            if (taskposition >= 0 && taskposition < GuidanceTip.Length && GuidanceTip[taskposition] != null)
            {
                GuidanceTip[taskposition].SetActive(true);
            }
        }


    }
}
