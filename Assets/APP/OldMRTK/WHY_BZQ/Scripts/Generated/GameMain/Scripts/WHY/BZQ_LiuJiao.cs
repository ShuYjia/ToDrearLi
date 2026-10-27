using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WHY_BZQ.ExportRuntime
{
    public class BZQ_LiuJiao : MonoBehaviour
    {
        public Animator animator;
        public Animator NeiLiuJiaoBanShou;
        public Animator SDZ_ZT_ShiZi;
        public GameObject DingZi;

        void Start()
        {
            string name = gameObject.name;
            PlayerPrefs.SetInt(name, 0);
            if (animator != null)
            {
                animator.speed = 0;
            }
            if (NeiLiuJiaoBanShou != null)
            {
                NeiLiuJiaoBanShou.speed = 0;
            }
            if (SDZ_ZT_ShiZi != null)
            {
                SDZ_ZT_ShiZi.speed = 0;
            }
        }

        void Update()
        {
            string name = gameObject.name;
            MeshRenderer objRenderer = DingZi != null ? DingZi.GetComponent<MeshRenderer>() : null;
            if (objRenderer == null || objRenderer.enabled == false)
            {
                PlayerPrefs.SetInt(name, 2);
            }

            if (PlayerPrefs.GetInt(name + "reset", 0) == 1)
            {
                PlayerPrefs.SetInt(name + "reset", 0);
                Reset();
            }
        }

        public void Grab()
        {
            string name = gameObject.name;
            int finish = PlayerPrefs.GetInt(name, 0);
            if (finish == 1)
            {
                if (animator != null)
                {
                    animator.speed = 1;
                }
                if (NeiLiuJiaoBanShou != null)
                {
                    NeiLiuJiaoBanShou.speed = 1;
                }
                if (SDZ_ZT_ShiZi != null)
                {
                    SDZ_ZT_ShiZi.speed = 1;
                }
            }
        }

        public static void ResetZuanTou(string name)
        {
            PlayerPrefs.SetInt(name + "reset", 1);
        }

        public void GrabExit()
        {
            if (animator != null)
            {
                animator.speed = 0;
            }
            if (NeiLiuJiaoBanShou != null)
            {
                NeiLiuJiaoBanShou.speed = 0;
            }
            if (SDZ_ZT_ShiZi != null)
            {
                SDZ_ZT_ShiZi.speed = 0;
            }
        }

        public void Reset()
        {
            if (NeiLiuJiaoBanShou != null)
            {
                NeiLiuJiaoBanShou.Play("NeiLiuJiaoBanShou", 0, 0f);
                NeiLiuJiaoBanShou.speed = 0;
            }
            if (SDZ_ZT_ShiZi != null)
            {
                SDZ_ZT_ShiZi.Play("NeiLiuJiaoBanShou", 0, 0f);
                SDZ_ZT_ShiZi.speed = 0;
            }

            if (animator != null)
            {
                animator.speed = 0;
            }
        }
    }
}
