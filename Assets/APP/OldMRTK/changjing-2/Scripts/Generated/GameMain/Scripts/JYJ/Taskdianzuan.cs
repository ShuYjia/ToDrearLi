using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changjing_2.ExportRuntime
{
    public class Taskdianzuan : MonoBehaviour
    {
         public GameObject MultCon;
         public GameObject ZiGongSi;
        public GameObject ZiGongSi3;
        public GameObject ZiGongSi5;
        public GameObject ZiGongSi7;
        public GameObject ZiGongSi9;
        public GameObject ZiGongSi11;
        public GameObject ZiGongSi13;
        
        public GameObject ZiGongSi15;
        // Start is called before the first frame update
        void Start()
        {
           MultCon = GameObject.Find("MultController"); 
        }

        // Update is called once per frame
        void Update()
        {
            if ((!ZiGongSi.GetComponent<MeshRenderer>().enabled || !ZiGongSi.activeSelf) &&
               (!ZiGongSi3.GetComponent<MeshRenderer>().enabled || !ZiGongSi3.activeSelf) &&
               (!ZiGongSi5.GetComponent<MeshRenderer>().enabled || !ZiGongSi5.activeSelf) &&
               (!ZiGongSi7.GetComponent<MeshRenderer>().enabled || !ZiGongSi7.activeSelf) &&
               (!ZiGongSi9.GetComponent<MeshRenderer>().enabled || !ZiGongSi9.activeSelf) &&
               (!ZiGongSi11.GetComponent<MeshRenderer>().enabled || !ZiGongSi11.activeSelf) &&
               (!ZiGongSi13.GetComponent<MeshRenderer>().enabled || !ZiGongSi13.activeSelf) &&
               (!ZiGongSi15.GetComponent<MeshRenderer>().enabled || !ZiGongSi15.activeSelf) )
               
            {
                 LoadSceneObj.task = LoadSceneObj.Tesks.Removetacks;
                 if (LoadSceneObj.isMult)
                    {
                        MultCon.GetComponent<MultCheckInfo>().CheckMultTask(0f);
                    }
                    else
                    {
                        MultCon.GetComponent<ShiXun>().SendUserInfo();
                    }
                    GetComponent<Taskdianzuan>().enabled = false;
            }
        }
    }
}
