using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HuaXian.ExportRuntime
{
    public class CheckCollider : MonoBehaviour
    {
        // Start is called before the first frame update
        public GameObject HX;
        public GameObject HJ;
        public GameObject[] PastingCheck; // 创建一个GameObject类型的数组来存放需要检查的对象
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            bool allCollidersOff = true; // 初始化一个布尔值表示所有碰撞器是否都关闭
            foreach (var obj in PastingCheck)
            {
                if (obj.GetComponent<Collider>() != null && obj.GetComponent<Collider>().enabled)
                {
                    allCollidersOff = false; // 如果找到一个开启的碰撞器，将allCollidersOff设置为false
                    break; // 并跳出循环
                }
            }

            if (allCollidersOff)
            {
                Debug.Log("All colliders are off."); // 如果所有碰撞器都关闭，输出提示信息

                LoadSceneObj.task = LoadSceneObj.Tesks.QRCodePasting;
                GameObject multCon = GameObject.Find("MultController");
                if (multCon == null)
                {
                    Debug.LogWarning("Export module: MultController was not found in the active scene.");
                }
                else if (LoadSceneObj.isMult)
                {
                    MultCheckInfo multCheck = multCon.GetComponent<MultCheckInfo>();
                    if (multCheck != null)
                    {
                        multCheck.CheckMultTask(7f);
                    }
                }
                else
                {
                    ShiXun shiXun = multCon.GetComponent<ShiXun>();
                    if (shiXun != null)
                    {
                        shiXun.SendUserInfo();
                    }
                }

                if (HJ != null)
                {
                    HJ.SetActive(true);
                }
                if (HX != null)
                {
                    HX.SetActive(false);
                }

            }
        }
    }
}
