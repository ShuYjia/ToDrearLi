using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChangJing_8.ExportRuntime
{
    public class AttachToParent : MonoBehaviour
    {
        // 用于记录当前物体A是否已经是某个物体的子物体  
        private bool isAttachedToParent = false;
        // 记录物体A原本附着的父物体，以便后续可以解除父子关系  
        private Transform originalParent;
        public GameObject MultCon;
        void Start()
        {
            MultCon = GameObject.Find("MultController");
            
        }

        // 当物体A的触发器与另一个物体（如物体B或物体C）的触发器重叠时调用  
        private void OnTriggerEnter(Collider other)
        {
            if (other == null)
            {
                return;
            }

            if (other.CompareTag("jiqiren"))
            {
                if (!isAttachedToParent)
                {
                    originalParent = transform.parent;
                    Transform parentTransform = other.transform;
                    transform.SetParent(parentTransform);
                    isAttachedToParent = true;
                    Debug.Log("已经吸附");
                    LoadSceneObj.task = LoadSceneObj.Tesks.manipulationrobot;
                    ReportTaskProgress(0f);
                }
            }
            else if (other.CompareTag("huojia"))
            {
                if (isAttachedToParent && transform.parent != null && transform.parent.CompareTag("jiqiren"))
                {
                    transform.SetParent(null);
                    isAttachedToParent = false;
                    transform.position = other.transform.position;
                    transform.rotation = other.transform.rotation;
                    LoadSceneObj.task = LoadSceneObj.Tesks.Handlingrack;
                    ReportTaskProgress(0f);
                    Collider selfCollider = GetComponent<Collider>();
                    if (selfCollider != null)
                    {
                        selfCollider.enabled = false;
                    }

                    Destroy(other.gameObject);
                }
            }
        }


        private void ReportTaskProgress(float score)
        {
            if (MultCon == null)
            {
                MultCon = GameObject.Find("MultController");
            }

            if (MultCon == null)
            {
                Debug.LogWarning("Export module: MultController is missing from the active scene. AttachToParent progress callback was skipped.", this);
                return;
            }

            if (LoadSceneObj.isMult)
            {
                MultCheckInfo multCheck = MultCon.GetComponent<MultCheckInfo>();
                if (multCheck != null)
                {
                    multCheck.CheckMultTask(score);
                }
            }
            else
            {
                ShiXun shiXun = MultCon.GetComponent<ShiXun>();
                if (shiXun != null)
                {
                    shiXun.SendUserInfo();
                }
            }
        }
    }
}
