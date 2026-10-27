using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetWork03_2.ExportRuntime
{
    public class ShowColider : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            if(StepTesk.tesks == StepTesk.Tesks.ACPowerSupply){
                GetComponent<BoxCollider>().enabled = true;
            }
        }
    }
}
