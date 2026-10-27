using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WHY_FuWuQi_6.ExportRuntime
{
    public class TaskChange : MonoBehaviour
    {
        public GameObject MultCon;
        public LoadSceneObj.Tesks tesks;

        public void ChangeTask()
        {
            MultCon = GameObject.Find("MultController");
            LoadSceneObj.task = tesks;
            if (MultCon == null)
            {
                Debug.LogWarning("Export module: MultController was not found in the active scene.");
                return;
            }

            if (LoadSceneObj.isMult)
            {
                MultCheckInfo multCheck = MultCon.GetComponent<MultCheckInfo>();
                if (multCheck != null)
                {
                    multCheck.CheckMultTask(1f);
                }
            }
            else
            {
                ShiXun shiXun = MultCon.GetComponent<ShiXun>();
                if (shiXun != null)
                {
                    shiXun.SendUserInfo();
                }
            }
        }

        public void Add1()
        {
            LoadSceneObj.score += 1;
            Debug.Log("+1");
        }
        public void Add3()
        {
            LoadSceneObj.score += 3;
            Debug.Log("+3");
        }
    }
}
