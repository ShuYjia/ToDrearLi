using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changjing_2.ExportRuntime
{
    public class chuizitishi : MonoBehaviour
    {
        public GameObject ui1;
        public GameObject ui2;
        public GameObject MultCon;
        public AudioSource audioSource1;
        public AudioSource audioSource2;
        bool isAudioSource1Played = false;
        bool isAudioSource2Played = false;
        // Start is called before the first frame update
        void Start()
        {
            MultCon = GameObject.Find("MultController");
           
            //audioSource1 = GetComponent<AudioSource>();
           // audioSource2 = GetComponent<AudioSource>();
        }

        void OnTriggerEnter(Collider other)
        {

            if (other.CompareTag("chui"))
            {
                ui1.SetActive(true);
                if (!isAudioSource1Played)
                {
                    audioSource1.Play();
                    isAudioSource1Played = true;
                    LoadSceneObj.task = LoadSceneObj.Tesks.Nailing;
                    if (LoadSceneObj.isMult)
                    {
                        MultCon.GetComponent<MultCheckInfo>().CheckMultTask(0f);
                    }
                    else
                    {
                        MultCon.GetComponent<ShiXun>().SendUserInfo();
                    }
                }
            }
            else if (other.CompareTag("banshou"))
            {
                ui2.SetActive(true);
                if (!isAudioSource2Played)
                {
                    audioSource2.Play();
                    isAudioSource2Played = true;
                    LoadSceneObj.task = LoadSceneObj.Tesks.Wrenchin;
                    if (LoadSceneObj.isMult)
                    {
                        MultCon.GetComponent<MultCheckInfo>().CheckMultTask(0f);
                    }
                    else
                    {
                        MultCon.GetComponent<ShiXun>().SendUserInfo();
                    }


                        
                }
            }


        }
    }
}
