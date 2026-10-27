using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace WHY_FuWuQi_6.ExportRuntime
{
    public class Video : MonoBehaviour
    {
        public GameObject button;
        // Start is called before the first frame update
        void Start()
        {
            button.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {
            if (!GetComponent<VideoPlayer>().isPlaying)
            {
                button.SetActive(true);
                // 当视频停止播放时，按钮出现
            }
            else
            {
                button.SetActive(false);
                // 当视频播放时，按钮消失
            }
        }
    }
}
