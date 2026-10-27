using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChangJing_8.ExportRuntime
{
    public class PengZhuangWanCheng : MonoBehaviour
    {
        // Start is called before the first frame update
      
        bool HaveSetUp = false;
        public static string checkLeft = "checkLeft";
        public static string checkRight = "checkRight";
        int left = 0;
        int right = 0;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (left == 0)
            {
                left = PlayerPrefs.GetInt(checkLeft, 0);
            }
            if (right == 0)
            {
                right = PlayerPrefs.GetInt(checkRight, 0);
            }
            if (left == 1 && right == 1 && !HaveSetUp)
            {
                JiFen.FinishTask(JiFen.taskJianCha);
                LoadSceneObj.task = LoadSceneObj.Tesks.Checkfunctionality;
                GameObject MultCon = GameObject.Find("MultController");
                    if (LoadSceneObj.isMult)
                    {
                        MultCon.GetComponent<MultCheckInfo>().CheckMultTask(1f);
                    }
                    else
                    {
                        MultCon.GetComponent<ShiXun>().SendUserInfo();
                    }
                    GetComponent<PengZhuangWanCheng>().enabled = false;

            }
            string finish = PlayerPrefs.GetString(JiFen.taskJianCha, "");
         
         
        }
        
    }
}
