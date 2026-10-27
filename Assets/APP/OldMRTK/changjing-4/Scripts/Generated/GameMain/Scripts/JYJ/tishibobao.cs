using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changjing_4.ExportRuntime
{
    public class tishibobao : MonoBehaviour
    {
        public AudioSource audioSource;
        private bool isPlayed = false;

        // Start is called before the first frame update  
        void Start()
        {
            // 确保在Start中获取了AudioSource组件  
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        // 这个方法可以从其他脚本或UI按钮中调用  
        public void PlayAudio()
        {
            if (!isPlayed)
            {
                audioSource.Play();
                isPlayed = true;
            }
        }
    }
}
