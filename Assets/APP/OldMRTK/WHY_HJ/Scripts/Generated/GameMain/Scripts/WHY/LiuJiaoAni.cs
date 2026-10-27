using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WHY_HJ.ExportRuntime
{
    public class LiuJiaoAni : MonoBehaviour
    {
        public Animator NeiLiuJiaoBanShou; // 引用Animator组件  

        void Start()
        {
            NeiLiuJiaoBanShou.speed = 0;
        }

        // Update is called once per frame
        void Update()
        {
            
        }
    }
}
