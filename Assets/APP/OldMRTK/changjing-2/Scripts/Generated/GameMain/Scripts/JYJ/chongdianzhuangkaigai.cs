using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changjing_2.ExportRuntime
{
    public class chongdianzhuangkaigai : MonoBehaviour
    {
       
        public GameObject liujiaoluosi1;
        public GameObject liujiaoluosi2;
        Vector3 StartPosition;
        Quaternion StartRotation;
        // Start is called before the first frame update
        void Start()
        {
            StartPosition = transform.position;
            StartRotation = transform.rotation;
           
        }

        // Update is called once per frame
        void Update()
        {
            if (liujiaoluosi1.GetComponent<MeshRenderer>().enabled || liujiaoluosi2.GetComponent<MeshRenderer>().enabled)


            {
                transform.position = StartPosition;
                transform.rotation = StartRotation;
                
            }
        }
    }
}
