using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WHY_FuWuQi_6.ExportRuntime
{
    public class ObjofBagDefine : MonoBehaviour
    {
        //实例化Obj类（调用）
        public Obj objects;
        public GameObject plyer;

        //Type of objects in the bag
        //物品类别，枚举
        public enum ObjType
        {
            tool,
            word,
            cad
        }

        // The system to which the item belongs
        //物品所属系统
        public enum ObjSystem
        {
              智慧物流系统

        }

        //The scene to which the item belongs
        //物品所属场景
        public enum ObgSence
        {
            华睿AGV场景
        }

        //Item attributes
        //物品属性，将类序列化才能在视图中显示
        [System.Serializable]
        public class Obj
        {
            public ObjType type;
            public ObjSystem system;
            public ObgSence sence;
            public int pageofobj;

        }

        //Define the item prefab
        //定义物品预制体
        public GameObject prefabOfObject;
        

        //The click event of the item's related button
        //物品相关按钮的点击事件
        
        public virtual void OnButtonClicked()
        {
            if (prefabOfObject == null)
            {
                Debug.LogWarning("Export module: ObjofBagDefine prefabOfObject is not assigned.", this);
                return;
            }

            Instantiate(prefabOfObject, transform.position, transform.rotation);
        }




         public void OnButtonClicked01()
        {
            if (prefabOfObject == null)
            {
                Debug.LogWarning("Export module: ObjofBagDefine prefabOfObject is not assigned.", this);
                return;
            }

            prefabOfObject.SetActive(!prefabOfObject.activeSelf);
        }


        void Start()
        {
            plyer = null;
        }


    }
}
