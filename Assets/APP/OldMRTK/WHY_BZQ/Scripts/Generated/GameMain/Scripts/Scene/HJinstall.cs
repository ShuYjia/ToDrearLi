using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WHY_BZQ.ExportRuntime
{
    public class HJinstall : MonoBehaviour
    {
        public GameObject GuidingArrow;
        // Start is called before the first frame update
        public GameObject[] installscenes;
        public bool isguide = false;

        public int index = 10;

        // Update is called once per frame
        void Update()
        {
            if (installscenes == null || installscenes.Length == 0 || GuidingArrow == null)
            {
                return;
            }

            isguide = true;
            foreach (GameObject item in installscenes)
            {
                if (item == null || !item.activeSelf)
                {
                    isguide = false;
                    break;
                }
            }

            if (isguide)
            {
                GuidingArrow.SetActive(true);
                GuidingArrow.transform.LookAt(this.transform);
            }

            float dis = Vector3.Distance(GuidingArrow.transform.position, this.transform.position);
            if (dis < index)
            {
                GuidingArrow.SetActive(false);
            }
            else if (GuidingArrow.activeSelf == false)
            {
                GuidingArrow.SetActive(true);
            }
        }
    }
}
