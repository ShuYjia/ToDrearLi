using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WHY_HJ.ExportRuntime
{
    public class HJ_JiaoShui : MonoBehaviour
    {
        public GameObject Ani;
        private void Start()
        {
            Ani.SetActive(false);
        }
        private void OnTriggerEnter(Collider other)
        {
            // 如果进入的对象标签与触发器对象标签相同，动画播放
            if (other.gameObject.tag == gameObject.tag)
            {
                Ani.SetActive(true);
            }
        }
            private void OnTriggerExit(Collider other)
        {
            // 如果离开的对象标签与触发器对象标签相同，动画停止
            if (other.gameObject.tag == gameObject.tag)
            {
                Ani.SetActive(false);
            }
        }
    }
}
