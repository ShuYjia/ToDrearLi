using UnityEngine;
using MixedReality.Toolkit.UX;

namespace ChangJing_8.ExportRuntime
{
    public class CheckRobotRight : MonoBehaviour
    {
        public GameObject text;
        private float LastTime = 0f;
        public float showTime = 2000;

        // Start is called before the first frame update
        void Start()
        {
           // PlayerPrefs.DeleteKey("checkRight");
        }
       
        // Update is called once per frame
        void Update()
        {
            if(Time.time*1000f - LastTime > showTime && LastTime>0)
            {
                text.SetActive(false);

            }
        }
        public void ShowCheck()
        {
            PlayerPrefs.SetInt("checkRight", 1);
            LastTime = Time.time * 1000f;
            text.SetActive(true);
        }

    }
}
