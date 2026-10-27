using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changjing_2.ExportRuntime
{
    public class PersonMove : MonoBehaviour
    {

        public GameObject personposition;
        public Vector3 position;
        // Start is called before the first frame update
        void Start()
        {
            if (this.gameObject.activeInHierarchy && personposition != null)
            {
                personposition.transform.position = position;
            }
        }

        // Update is called once per frame
        void Update()
        {
            
        }
    }
}
