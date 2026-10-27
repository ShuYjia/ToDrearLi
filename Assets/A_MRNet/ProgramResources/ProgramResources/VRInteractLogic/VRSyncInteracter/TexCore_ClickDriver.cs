//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Drives a linear mapping based on position between 2 positions
//
//=============================================================================

using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

    public class TexCore_ClickDriver : MonoBehaviour,IHandHoverUpdate
    {
        public string startName;

        public bool isNeedTool = false;
        public UnityEvent OnClick;

        public bool isClickDisable = false;
    TexCore_LinearDrive linear;

        protected virtual void Awake()
        {
            startName = gameObject.name;
           
        }
        private void Start()
        {
           
            if (isClickDisable)
            {
                OnClick.AddListener(() =>
                {
                    gameObject.SetActive(false);
                });
            }

            linear = gameObject.GetComponent<TexCore_LinearDrive>();
            if (linear != null && linear.needTool == true)
            {
                OnClick.AddListener(() => { linear.needTool = false; });
            }
        }

        public void ResetName()
        {
            
            gameObject.name = startName;
            if (linear != null && linear.needTool == false)
            {
                GetComponent<Collider>().enabled = true;
                Destroy(this);
            }
        }

        GrabTypes grab;
       
    public bool CantianToolOpen(string tool)
    {
        MutilNodePanelBase node = TexCore_PlaneManager.self.activePlane as MutilNodePanelBase;
        if (node != null)
        {
            foreach (var item in node.activeChild)
            {
                if (item.isEnter) continue;

                Panel_Operate operate = item.active as Panel_Operate;
                if (operate == null) continue;
                    
                if (operate.topicData.tasks[operate.stepIndex].Split('/')[0].Equals(tool))
                {
                    OnClick?.Invoke();
                    return true;
                }
            }
        }
        else
        {
            NodePanelBase snode = TexCore_PlaneManager.self.activePlane as NodePanelBase;
            if (snode != null)
            {
                Panel_Operate operate = snode.activeChild as Panel_Operate;
                if (operate == null) return false;

                if (operate.topicData.tasks[operate.stepIndex].Split('/')[0].Equals(tool))
                {
                    OnClick?.Invoke();
                    return true;
                }
            }
        }
            return false;
        }
        public void HandHoverUpdate(SingleTransform hand)
        {
            //if (TexCore_Const.partfrom == Partfrom.PC) return;
            if (hand.startingGrabType!= GrabTypes.None&& grab!= hand.startingGrabType)
            {
                grab = hand.startingGrabType;

                if (isNeedTool==false)
                    OnClick?.Invoke();
                else
                {
                    TexCore_RPCHandTool handtool = hand.virtualTransform.GetComponent<TexCore_RPCHandTool>();
                    if (handtool != null)
                    {
                        if (handtool.tool != null)
                        {
                            
                            gameObject.name = handtool.tool.name + "_" + startName;
                            CantianToolOpen(handtool.tool.name);
                        }
                    }
                }

                TexCore_BusinessManager.OnClick(gameObject, SceneManager.GetActiveScene().name,hand.virtualTransform.root.name);
                ResetName();
            }
        }
       
        private void OnHandHoverEnd(SingleTransform hand)
        {
            grab = GrabTypes.None;

        }

    }

