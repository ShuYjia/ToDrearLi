using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changjing_2.ExportRuntime
{
    public class chongdianzhuang : MonoBehaviour
    {
        public GameObject game1;
        public GameObject ui1;
        public GameObject MultCon;
        public AudioSource audioSource;
        bool isPlayed = false;
        // Start is called before the first frame update
        void Start()
        {

        MultCon = GameObject.Find("MultController");
        }

        private void OnTriggerEnter(Collider other)
        {

            if (other.gameObject.CompareTag("dingwei")) // 当B物体与A物体发生碰撞时  
            {

                
                ui1.SetActive(true);
                if (!isPlayed)
                {
                    audioSource.Play();
                    isPlayed = true;
                }
                LoadSceneObj.task = LoadSceneObj.Tesks.Installchargingpile;
                 if (LoadSceneObj.isMult)
                    {
                        MultCon.GetComponent<MultCheckInfo>().CheckMultTask(1f);
                    }
                    else
                    {
                        MultCon.GetComponent<ShiXun>().SendUserInfo();
                    }
                if (LoadSceneObj.isMult)
                {
                    game1.GetComponent<NetworkedObject>().CmdSetVisibilityTrue();
                    GetComponent<NetworkedObject>().CmdSetVisibilityFalse();

                }
                else
                {
                    // 销毁B物体  
                    Destroy(this.gameObject);
                    // 显示A物体  
                    game1.SetActive(true);

                }
                
            }
        }
    }
}
