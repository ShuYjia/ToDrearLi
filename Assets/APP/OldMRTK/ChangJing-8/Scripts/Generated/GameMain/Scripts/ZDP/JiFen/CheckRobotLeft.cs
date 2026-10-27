using UnityEngine;
using MixedReality.Toolkit.UX;

namespace ChangJing_8.ExportRuntime
{
    public class CheckRobotLeft : MonoBehaviour
    {
        public GameObject text;
        public float showTime = 2000;
        private float LastTime = 0f;
        // Start is called before the first frame update
        void Start()
        {
            //PlayerPrefs.DeleteKey("checkLeft");
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
            PlayerPrefs.SetInt("checkLeft", 1);
            LastTime = Time.time * 1000f;
            text.SetActive(true);
        }

    }
}
