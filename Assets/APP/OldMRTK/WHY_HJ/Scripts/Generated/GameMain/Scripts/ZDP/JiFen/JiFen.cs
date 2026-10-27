using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WHY_HJ.ExportRuntime
{
    public class JiFen : MonoBehaviour
    {
        public Text jifen;//// 用于显示积分的文本框
        //用来增加任务
        //ZDP
        public static string taskAGV = "AGV";
        public static string taskXiFu = "XiFu";
        public static string taskJianCha = "JianCha";
        public static string taskKaiJi = "KaiJi";
        public static string WanCheng = "WanCheng";
        public static string LingQu = "LingQu";
        //WHY
        public static string taskAtt_UPan = "Att";
        public static string taskAtt_Di = "AttDi";
        public static string taskXiTong = "XT";
        public static string taskJieMian = "JM";
        //JYJ
        public static string taskCDZ_anzhuang = "CDZ_az";
        public static string taskCDZ_peiwang = "CDZ_pw";
        public static string taskRCS = "RCS";
        //PQ
        public static string taskWangLuo = "WL";

        void Start()
        {
        }

        void Update()
        {
            var last_jifen = PlayerPrefs.GetFloat("jifen", 0);// 获取当前的积分值，如果没有则为0;  PlayerPrefs类是用来进行数据的存储与读取的
            jifen.text = last_jifen.ToString();// 将当前的积分值设置为积分文本框的文字
        }

        public static void FinishTask(string name)//标记一个任务完成，并给相应的积分
        {
            string state = PlayerPrefs.GetString(name, "");
            if (!LingQu.Equals(state))
            {
                PlayerPrefs.SetString(name, WanCheng);// 将对应任务的状态设置为完成,如果任务状态是已领取说明任务已经做过 ，再次重复操作领取按钮不跳出。
            }
            /* if (name == taskXiFu || name == taskJianCha)// // 任务XiFu、JianCha给1分
             {
                 GetJiFen(1);
             }
             else if (name == taskKaiJi || name == taskAGV)//任务KaiJi、AGV给2分
             {
                 GetJiFen(2);
             }*/

        }

        public static void GetJiFen(float score)// 增加积分
        {
            float last_jifen = PlayerPrefs.GetFloat("jifen", 0f);// 获取当前的积分值
            last_jifen = last_jifen + score;// 增加积分
            PlayerPrefs.SetFloat("jifen", last_jifen);// 新的积分值
        }
    }
}
