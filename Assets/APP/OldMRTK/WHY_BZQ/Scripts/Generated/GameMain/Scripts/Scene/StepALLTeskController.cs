/*
* @Description: 
* @Author: Li Yongqi
* @Date: 2024-07-11
* @LastEditTime: 2024-07-11
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WHY_BZQ.ExportRuntime
{
    public class StepALLTeskController : MonoBehaviour
    {
        public GameObject MultCon;
        public GameObject[] gameObjects;
        private bool taskcomplete = false;
        public LoadSceneObj.Tesks tesks;

        void Update()
        {
            taskcomplete = true;
            foreach (var item in gameObjects)
            {
                if (!item.activeSelf)
                {
                    taskcomplete = false;
                    break;
                }
            }
            if (taskcomplete)
            {
                MultCon = GameObject.Find("MultController");
                if (taskcomplete)
                {
                    if (LoadSceneObj.task != tesks)
                    {
                        LoadSceneObj.task = tesks;
                        if (LoadSceneObj.isMult)
                        {
                            MultCon.GetComponent<MultCheckInfo>().CheckMultTask(1f);
                        }
                        else
                        {
                            MultCon.GetComponent<ShiXun>().SendUserInfo();
                        }

                    }
                }
                GetComponent<StepALLTeskController>().enabled = false;
            }


        }


    }
}
