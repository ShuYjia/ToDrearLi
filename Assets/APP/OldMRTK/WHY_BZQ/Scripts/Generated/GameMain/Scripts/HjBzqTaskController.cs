using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WHY_BZQ.ExportRuntime
{
    public class HjBzqTaskController : MonoBehaviour
    {
        public GameObject gameObjectFinish;
        public GameObject[] shi;
        public LoadSceneObj.Tesks taskfinish;
        public bool isFinish;
        public GameObject UI;
        // Start is called before the first frame update
        void Start()
        {
            FindChild(gameObjectFinish);//找到节点下的所有子物体

        }
        void FindChild(GameObject child)
        {
            //利用for循环 获取物体下的全部子物体
            for (int c = 0; c < child.transform.childCount; c++)
            {
                //如果子物体下还有子物体 就将子物体传入进行回调查找 直到物体没有子物体为止
                
                shi[c] = child.transform.GetChild(c).gameObject;


            }
        }


        // Update is called once per frame
        void Update()
        {
            foreach (GameObject item in shi)
            {
                if (!item.activeSelf)
                {
                    isFinish = false;
                    return;
                }
                if(shi[shi.Length-1] == item){
                    isFinish = true;
                }
            }

            if (isFinish)
            {
                if(LoadSceneObj.task != taskfinish){
                    LoadSceneObj.task = taskfinish;
                }
                else
                {
                    GetComponent<HjBzqTaskController>().enabled = false;
                }
                UI.SetActive(true);//why0729加
            }
        }
    }
}
