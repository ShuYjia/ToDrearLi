using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WHY_BZQ.ExportRuntime
{
    public class BZQ_LiuJiaoAni : MonoBehaviour
    {
        public Animator NeiLiuJiaoBanShou; // 引用Animator组件  
        public Animator SDZ_ZT_ShiZi;

        void Start()
        {
            NeiLiuJiaoBanShou.speed = 0;
            SDZ_ZT_ShiZi.speed = 0;
        }

        // Update is called once per frame
        void Update()
        {
            
        }
    }
}
