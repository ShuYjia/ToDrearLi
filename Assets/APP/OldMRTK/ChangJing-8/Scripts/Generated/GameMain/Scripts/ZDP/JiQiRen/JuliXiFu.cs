using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChangJing_8.ExportRuntime
{
    public class JuliXiFu:MonoBehaviour
    {
        public GameObject targetReference; // 用于吸附的目标物体的Transform引用  
        public GameObject targetReference3;
        public GameObject targetReference5;
        public GameObject targetReference7;
        public GameObject targetReference9;
        public GameObject targetReference11;
        public GameObject targetReference13;
        public GameObject targetReference15;
        List<GameObject> list = new List<GameObject>();
        Vector3 pos;
        Quaternion qut;
        string name = "";

        // Start is called before the first frame update
        void Start()
        {
            PlayerPrefs.SetInt(name, 0);
            list.Add(targetReference);
            list.Add(targetReference3);
            list.Add(targetReference5);
            list.Add(targetReference7);
            list.Add(targetReference9);
            list.Add(targetReference11);
            list.Add(targetReference13);
            list.Add(targetReference15);
        }
        // Update is called once per frame
        void Update()
        {
            double min = double.MaxValue;
            GameObject trans = null;
            int finish = 0;
            if ("".Equals(name))
            {
                foreach (GameObject item in list)
                {
                    double sq = Math.Sqrt(Math.Pow(this.transform.position.x - item.transform.position.x, 2) +
                    Math.Pow(this.transform.position.y - item.transform.position.y, 2) +
                    Math.Pow(this.transform.position.z - item.transform.position.z, 2));
                    if (sq < min && sq < 0.2)//调整吸附距离
                    {
                        min = sq;
                        trans = item;
                    }
                }
            }
            else {
                finish = PlayerPrefs.GetInt(name, 0);
            }
            // 位置吸附
            if (trans != null&&finish==0) {
                name = trans.gameObject.name;

                PlayerPrefs.SetInt(name, 1);
                pos = trans.transform.position;
                qut = trans.transform.rotation;
                list.Remove(trans);
                this.transform.position = pos;
                this.transform.rotation = qut;
            }
            // 位置固定
            if (finish == 1) {
                this.transform.position = pos;
                this.transform.rotation = qut;
            }
            if (finish == 2) {
                DingZiDongHua.ResetZuanTou(name);
                name = "";
            }
        }
    }
}
