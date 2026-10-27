using MixedReality.Toolkit.UX;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJing_8.ExportRuntime
{
    public class ShowBuyPage : MonoBehaviour
    {
        public GameObject BuyNextStepTip;
        public GameObject yuebuzu;
        static bool activeTip = false;
        static bool activeYebuzu = false;
        static float LastTimeMillis =  0f;


        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            float CurTimeMillis = Time.time*1000f;

            if (activeYebuzu&&CurTimeMillis - LastTimeMillis < 500)
            {
                activeYebuzu = true;
            }
            else {
                activeYebuzu = false;

            }

            yuebuzu.SetActive(activeYebuzu);
            BuyNextStepTip.SetActive(activeTip);

        }

        public static void ShowPage() {
            Debug.Log("ShowPage");
            activeTip = true;
        }
        public void  Yes() {
            var gold = PlayerPrefs.GetInt("jifen", 0);
            gold = gold - 10;
            if (gold >= 0)
            {
                // 保存金币
                PlayerPrefs.SetInt("jifen", gold);
                activeTip = false;
                BuyNextStep.ShowAllAnimation();
            }
            else {
                activeTip = false;

                activeYebuzu = true;
                LastTimeMillis  = Time.time * 1000f;
            }
        }

        public void No()
        {
            activeTip = false;
        }
    }
}
