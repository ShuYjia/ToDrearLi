using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WHY_HJ.ExportRuntime
{
    public class GuidingArrowControl : MonoBehaviour
    {
        public GameObject GuidingArrowing;

        // Start is called before the first frame update
        void Start()
        {
            if (GuidingArrowing != null)
            {
                GuidingArrowing.SetActive(false);
            }
        }

        // Update is called once per frame
        void Update()
        {
            
        }
    }
}
