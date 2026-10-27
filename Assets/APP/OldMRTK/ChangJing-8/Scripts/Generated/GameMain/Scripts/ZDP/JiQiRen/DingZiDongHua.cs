using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.InputSystem;
using System.Threading;

namespace ChangJing_8.ExportRuntime
{
    public class DingZiDongHua : MonoBehaviour
    {
        public Animator animator; // 引用Animator组件  
        public Animator SDZ_ZT_ShiZi; // 引用Animator组件  
        public Animator SDZ_ZT_LiuJiao;
        public Animator SDZ_BanJi;
        public Animator polySurface7;

        public GameObject DingZi;

        bool firstShow = true;

        // Start is called before the first frame update
        void Start()
        {
            string name = this.gameObject.name;
            PlayerPrefs.SetInt(name, 0);
            animator.speed = 0;
            SDZ_ZT_ShiZi.speed = 0;
            SDZ_ZT_LiuJiao.speed = 0;
            SDZ_BanJi.speed = 0;
            polySurface7.speed = 0;
        }

        // Update is called once per frame
        void Update()
        {
            string name = this.gameObject.name;
            MeshRenderer objRenderer = DingZi.GetComponent<MeshRenderer>();
            if (objRenderer == null|| objRenderer.enabled ==false) {
                if (firstShow) {
                    firstShow = false;
                    TryShowBuyPage();
                    PlayerPrefs.SetInt(name, 2);
                 }
            }
            if (PlayerPrefs.GetInt(name + "reset", 0) == 1) {
                Debug.Log(name + "reset");
                PlayerPrefs.SetInt(name + "reset", 0);
                Reset();  
            }
        }

        public void Grab() {
            string name = this.gameObject.name;
            int finish = PlayerPrefs.GetInt(name, 0);
            if (finish == 1) {
                animator.speed = 1;
                SDZ_ZT_ShiZi.speed = 1;
                SDZ_ZT_LiuJiao.speed = 1;
                polySurface7.speed = 1;
                SDZ_BanJi.speed = 1;
            }

        }

        static public void ResetZuanTou(string name) {
            PlayerPrefs.SetInt(name + "reset", 1);
        }
        public void GrabExit()
        {
            SDZ_BanJi.Play("SDZ_BanJi", 0, 0f);
            //SDZ_ZT_ShiZi.Play("SDZ_ZT_ShiZi", 0,0f);
            //SDZ_ZT_LiuJiao.Play("SDZ_ZT_LiuJiao", 0,0f);
            //polySurface7.Play("polySurface7", 0, 0f);
            animator.speed = 0;
            SDZ_ZT_ShiZi.speed = 0;
            SDZ_ZT_LiuJiao.speed = 0;
            polySurface7.speed = 0;
            SDZ_BanJi.speed = 0;

        }

        public void Reset()
        {
            SDZ_BanJi.Play("SDZ_BanJi", 0, 0f);
            SDZ_ZT_ShiZi.Play("SDZ_ZT_ShiZi", 0, 0f);
            SDZ_ZT_LiuJiao.Play("SDZ_ZT_LiuJiao", 0, 0f);
            polySurface7.Play("polySurface7", 0, 0f);
            animator.speed = 0;
            SDZ_ZT_ShiZi.speed = 0;
            SDZ_ZT_LiuJiao.speed = 0;
            polySurface7.speed = 0;
            SDZ_BanJi.speed = 0;

        }


        private static void TryShowBuyPage()
        {
            System.Type showBuyPageType = null;
            foreach (System.Reflection.Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                showBuyPageType = assembly.GetType("ShowBuyPage");
                if (showBuyPageType != null)
                {
                    break;
                }
            }

            if (showBuyPageType == null)
            {
                Debug.LogWarning("Export module: ShowBuyPage type was not found in the active project.");
                return;
            }

            System.Reflection.MethodInfo showPageMethod = showBuyPageType.GetMethod("ShowPage", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (showPageMethod == null)
            {
                Debug.LogWarning("Export module: ShowBuyPage.ShowPage() was not found in the active project.");
                return;
            }

            try
            {
                showPageMethod.Invoke(null, null);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Export module: ShowBuyPage.ShowPage() invocation failed: {ex.Message}");
            }
        }
    }
}
