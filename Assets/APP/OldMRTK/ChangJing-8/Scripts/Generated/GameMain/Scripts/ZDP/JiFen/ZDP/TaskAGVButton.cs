using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChangJing_8.ExportRuntime
{
    public class TaskAGVButton : MonoBehaviour
    {
        public GameObject AGVCanvas;
        private bool lastState;

        void Start()
        {
            lastState = AGVCanvas.activeSelf;
        }

        void Update()
        {
            if (AGVCanvas.activeSelf != lastState)
            {
                //lastState = AGVCanvas.activeSelf;
                //if (!AGVCanvas.activeSelf)
                //{
                    CompleteTask();
               // }
            }
        }

        private void CompleteTask()
        {
            PlayerPrefs.SetString(JiFen.taskAGV, JiFen.WanCheng);
            LoadSceneObj.task = LoadSceneObj.Tesks.AGVnetWork;
            GameObject MultCon = GameObject.Find("MultController");
                    if (LoadSceneObj.isMult)
                    {
                        MultCon.GetComponent<MultCheckInfo>().CheckMultTask(3f);
                    }
                    else
                    {
                        MultCon.GetComponent<ShiXun>().SendUserInfo();
                    }
                    GetComponent<TaskAGVButton>().enabled = false;
        }
    }
}
