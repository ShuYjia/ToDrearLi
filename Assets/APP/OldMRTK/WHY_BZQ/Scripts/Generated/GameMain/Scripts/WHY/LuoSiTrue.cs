using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WHY_BZQ.ExportRuntime
{
    public class LuoSiTrue : MonoBehaviour
    {
        public GameObject LuoSi;
        public GameObject LuoSiOn;
        public GameObject XiFu;
        // Start is called before the first frame update
        void Update()
        {
            if (LuoSi != null)
            {
                XiFu.SetActive(true);
            }

            // 当物体a被销毁时
            if (LuoSi != null)
            {
                if (!LuoSi.GetComponent <MeshRenderer>().enabled)
                {
                    LuoSiOn.SetActive(true); // 激活物体b  
                }
            }
        }
    }
}
