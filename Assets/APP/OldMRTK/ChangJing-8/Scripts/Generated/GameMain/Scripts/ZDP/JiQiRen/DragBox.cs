using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace ChangJing_8.ExportRuntime
{
    public class DragBox : MonoBehaviour
    {
        public GameObject ZiGongSi;
        public GameObject ZiGongSi3;
        public GameObject ZiGongSi5;
        public GameObject ZiGongSi7;
        public GameObject ZiGongSi9;
        public GameObject ZiGongSi11;
        public GameObject ZiGongSi13;
        public GameObject ZiGongSi15;
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
            if ((!ZiGongSi.activeSelf && !ZiGongSi3.activeSelf &&
                !ZiGongSi5.activeSelf && !ZiGongSi7.activeSelf &&
                !ZiGongSi9.activeSelf && !ZiGongSi11.activeSelf &&
                !ZiGongSi13.activeSelf && !ZiGongSi15.activeSelf))
            {
            }
            else if ((ZiGongSi.GetComponent<MeshRenderer>().enabled || ZiGongSi3.GetComponent<MeshRenderer>().enabled ||
                ZiGongSi5.GetComponent<MeshRenderer>().enabled || ZiGongSi7.GetComponent<MeshRenderer>().enabled ||
                ZiGongSi9.GetComponent<MeshRenderer>().enabled || ZiGongSi11.GetComponent<MeshRenderer>().enabled ||
                ZiGongSi13.GetComponent<MeshRenderer>().enabled || ZiGongSi15.GetComponent<MeshRenderer>().enabled))
            {
                transform.position = StartPosition;
                transform.rotation = StartRotation;
            }
        }
    }
}
