using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WHY_BZQ.ExportRuntime
{
    public class BZQ_LuoSi : MonoBehaviour
    {
        public GameObject targetReference; // 用于吸附的目标物体的Transform引用  
        public GameObject targetReference1;
        public GameObject targetReference2;
        public GameObject targetReference4;
        public GameObject targetReference5;
        public GameObject targetReference6;
        public GameObject targetReference7;
        public GameObject targetReference8;
        public GameObject targetReference9;
        public GameObject targetReference10;
        public GameObject targetReference11;
        public GameObject targetReference12;
        public GameObject targetReference13;
        public GameObject targetReference14;
        public GameObject targetReference15;
        public GameObject targetReference16;
        public GameObject targetReference17;
        public GameObject targetReference18;
        public GameObject targetReference19;
        public GameObject targetReference20;
        public GameObject targetReference21;
        public GameObject targetReference22;
        public GameObject targetReference23;
        public GameObject targetReference24;
        public GameObject targetReference25;
        public GameObject targetReference26;
        public GameObject targetReference27;
        public GameObject targetReference28;
        public GameObject targetReference29;
        public GameObject targetReference30;
        public GameObject targetReference31;
        public GameObject targetReference32;
        //public GameObject Ani;



        // 存储所有可能的目标物体的列表（目前只添加了targetReference）  
        List<GameObject> list = new List<GameObject>();
        // 用于存储吸附到的目标的位置和旋转  
        Vector3 pos;
        Quaternion qut;
        // 用于在PlayerPrefs中存储和检索数据的名称  
        string name = "";

        // Start is called before the first frame update
        void Start()
        {
            // 在开始时，由于name为空，所以此操作实际上不会存储任何值  
            PlayerPrefs.SetInt(name, 0);
            // 将targetReference添加到列表中  
            list.Add(targetReference);
            list.Add(targetReference1);
            list.Add(targetReference2);
            list.Add(targetReference4);
            list.Add(targetReference5);
            list.Add(targetReference6);
            list.Add(targetReference7);
            list.Add(targetReference8);
            list.Add(targetReference9);
            list.Add(targetReference10);
            list.Add(targetReference11);
            list.Add(targetReference12);
            list.Add(targetReference13);
            list.Add(targetReference14);
            list.Add(targetReference15);
            list.Add(targetReference16);
            list.Add(targetReference17);
            list.Add(targetReference18);
            list.Add(targetReference19);
            list.Add(targetReference20);
            list.Add(targetReference21);
            list.Add(targetReference22);
            list.Add(targetReference23);
            list.Add(targetReference24);
            list.Add(targetReference25);
            list.Add(targetReference26);
            list.Add(targetReference27);
            list.Add(targetReference28);
            list.Add(targetReference29);
            list.Add(targetReference30);
            list.Add(targetReference31);
            list.Add(targetReference32);
        }
        // Update is called once per frame
        void Update()
        {
            // 初始化最小距离和可能的目标物体  
            double min = double.MaxValue;
            GameObject trans = null;

            // 标记物体是否已完成吸附（0: 未完成, 1: 已完成, 2: 需要重置） 
            int finish = 0;

            // 如果name为空，则寻找最近的目标物体
            if ("".Equals(name))
            {
                foreach (GameObject item in list)
                {
                    if(item.activeSelf)
                    {
                        // 计算当前物体与目标物体之间的距离  
                        double sq = Math.Sqrt(Math.Pow(this.transform.position.x - item.transform.position.x, 2) +
                        Math.Pow(this.transform.position.y - item.transform.position.y, 2) +
                        Math.Pow(this.transform.position.z - item.transform.position.z, 2));

                        // 如果找到更近的物体，并且距离小于设定的阈值  
                        if (sq < min && sq < 0.1)//调整吸附距离
                        {
                            min = sq;
                            trans = item;
                        }
                    }
                }
            }
            // 如果name不为空，则从PlayerPrefs中获取finish的值  
            else
            {
                finish = PlayerPrefs.GetInt(name, 0);
            }
            // 如果找到了一个目标物体并且还没有完成吸附  
            if (trans != null && finish == 0)
            {
                // 更新name为吸附到的目标物体的名称 
                name = trans.gameObject.name;

                // 在PlayerPrefs中标记为已完成吸附  
                PlayerPrefs.SetInt(name, 1);

                // 保存目标物体的位置和旋转 
                pos = trans.transform.position;
                qut = trans.transform.rotation;

                // 从列表中移除已吸附的目标物体
                list.Remove(trans);

                // 将当前物体的位置和旋转设置为目标物体的位置和旋转  
                this.transform.position = pos;
                this.transform.rotation = qut;
            }

            // 如果已完成吸附，保持当前位置和旋转不变  
            if (finish == 1)
            {
                this.transform.position = pos;
                this.transform.rotation = qut;
                Debug.Log("1");
            }

            // 如果需要重置（例如，完成某个任务后）  
            if (finish == 2)
            {
                // 调用某个外部函数（可能在其他类中）来重置某个物体的状态  
                BZQ_LiuJiao.ResetZuanTou(name);
                // 清空name以便下次使用  
                name = "";
            }
        }
    }
}
