/*
* @Description: 
* @Author: Li Yongqi
* @Date: 2024-06-14
* @LastEditTime: 2024-06-14
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WHY_BZQ.ExportRuntime
{
    public class OnTaskComplate : MonoBehaviour
    {
        public GameObject pretask = null;
        public GameObject tasknode = null;
        public GameObject nextTasknode;

        public LoadSceneObj.Tesks tesks;
        
        // Start is called before the first frame update
        void Start()
        {
             if(pretask != null)
            {
                pretask.SetActive(false);
            }
        }

        // Update is called once per frame
        void Update()
        {
            if(LoadSceneObj.task == tesks)
            {
                if(tasknode != null)
                {
                    tasknode.SetActive(false);
                }
                if (nextTasknode != null)
                {
                    nextTasknode.SetActive(true);
                }
                GetComponent<OnTaskComplate>().enabled = false;
            }
            Debug.Log("当前任务："+LoadSceneObj.task);
            
        }
    }
}
