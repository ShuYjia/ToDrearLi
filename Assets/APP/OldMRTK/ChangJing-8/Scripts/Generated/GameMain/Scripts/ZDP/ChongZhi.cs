using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChangJing_8.ExportRuntime
{
    public class ChongZhi : MonoBehaviour
    {
        // Start is called before the first frame update
        public void Reset()
        {
            //Debug.Log("reset");
            PlayerPrefs.SetInt("jifen", 0);
            PlayerPrefs.SetInt("checkRight", 0);
            PlayerPrefs.SetInt("checkLeft", 0);
            PlayerPrefs.SetString(JiFen.taskJianCha, "");
            PlayerPrefs.SetString(JiFen.taskKaiJi, "");
            PlayerPrefs.SetString(JiFen.taskXiFu, "");       
            PlayerPrefs.SetString(JiFen.taskAGV, "");
            PlayerPrefs.SetString(JiFen.taskAtt_UPan, "");
            PlayerPrefs.SetString(JiFen.taskJieMian, "");
            PlayerPrefs.SetString(JiFen.taskAtt_Di, "");
            PlayerPrefs.SetString(JiFen.taskXiTong, "");
            PlayerPrefs.SetString(JiFen.taskCDZ_anzhuang, "");
            PlayerPrefs.SetString(JiFen.taskCDZ_peiwang, "");
            PlayerPrefs.SetString(JiFen.taskRCS, "");
            PlayerPrefs.SetString(JiFen.taskWangLuo, "");
        }
    }
}
