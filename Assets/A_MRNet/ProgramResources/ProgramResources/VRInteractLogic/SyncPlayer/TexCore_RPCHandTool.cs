using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Events;
using Bpf.Diagnostics;


public class TexCore_RPCHandTool : MonoBehaviour
{
    public List<Texture2D> tools;
    
    public TexCore_RPCHand hand;

    public GameObject tool;

    public int exitCount=12;

    public Transform hoverPoint;
    public Transform attachPoint;
    public Transform endPoint;
    public int catchLayer = 7;

    public Vector3 hoverStartPoint;
    public Vector3 attachStartPoint;

    public UnityEvent<bool> OnHandChang;


    public virtual void Start()
    {
        hand = transform.GetComponent<TexCore_RPCHand>();
        hand.onGrabType.AddListener(OnExit);
        //endPoint = transform.Find("EndPoint");
        //hoverPoint = transform.Find("HoverPoint");
        //attachPoint = transform.Find("ObjectAttachmentPoint");
        hoverStartPoint = hoverPoint.localPosition;
        attachStartPoint = attachPoint.localPosition;

    }

    public void OnClick(GameObject obj)
    {
        tool = obj;
        string objName = obj.name.Split('_')[1];
        for (int i = 0; i < tools.Count; i++)
        {
            if (objName.Equals(tools[i].name))
            {
                SetMouseCursor(tools[i].name);
            }
            BpfLog.Info(BpfLogCategory.Network, "tex_core_rpc_hand_tool.on_click", tools[i].name);
        }
    }

    public void SetMouseCursor(string name = null)
    {
        if (string.IsNullOrEmpty(name))
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            return;
        }

        BpfLog.Info(BpfLogCategory.Network, "tex_core_rpc_hand_tool.set_mouse_cursor", "设置鼠标光标");
        foreach (Texture2D item in tools)
        {
            if (item.name == name)
            {
                Cursor.SetCursor(item, Vector2.zero, CursorMode.ForceSoftware);
            }
        }
    }

    public virtual void SetTool(GameObject t)
    {
        //设置持有工具
        if (tool != null )
        {
            ExitHandModelTool();
        }
        tool = t;

        //设置鼠标图标
        if (TexCore_Const.partfrom==Partfrom.PC)
        {
            string objName = t.name;
            for (int i = 0; i < tools.Count; i++)
            {
                if (objName.Equals(tools[i].name))
                {
                    SetMouseCursor(tools[i].name);
                }
                //Debug.Log(tools[i].name);
            }
        }
        else
        {
            //设置手部模型持有工具
            SetHandModelTool(t);
        }
    }

    public void SetHandModelTool(GameObject t)
    {
        //设置手模型持有工具
        if (tool == null) return;
        
        t.GetComponent<Collider>().enabled = false;
        t.layer = catchLayer;

        //设置工具为手的子物体
        t.transform.SetParent(endPoint);
        t.transform.position = endPoint.position;
        t.transform.rotation = endPoint.rotation;
        //将手的碰撞体设置成工具的
        TexCore_VRTool _VRTool = t.GetComponent<TexCore_VRTool>();

        //hoverPoint.SetParent(_VRTool.toolSphereTransform);
        //hoverPoint.localPosition = Vector3.zero;
        //attachPoint.SetParent(_VRTool.toolSphereTransform);
        //attachPoint.localPosition = Vector3.zero;
        hoverPoint.gameObject.SetActive(false);
        attachPoint.gameObject.SetActive(false);

        Tween tween = t.transform.DOScale(Vector3.one, 0.3f);
        tween.onComplete += (() => {
            hoverPoint.position = _VRTool.toolSphereTransform.position;
            attachPoint.position = _VRTool.toolSphereTransform.position;

            hoverPoint.gameObject.SetActive(true);
            attachPoint.gameObject.SetActive(true);
        });
        

        OnHandChang?.Invoke(true);
    }
    public virtual void OnExit(GrabTypes grabTypes,int clickTime)//手松开模型
    {
        
        if (grabTypes == GrabTypes.Grip)
        {
            if (clickTime >= exitCount)
            {
                //让持有的工具离开手，回到原位
                if (tool!=null)
                {
                    BpfLog.Info(BpfLogCategory.Network, "tex_core_rpc_hand_tool.on_exit", "将工具拿开手");
                    ExitHandModelTool();
                }
            }
        }
    }
    public virtual void ExitHandModelTool()
    {
        ExitHand();
        tool.SendMessage("OnExit");
        tool = null;
    }

    public void ExitHand()
    {
        if (TexCore_Const.partfrom == Partfrom.VR)
        {
            hoverPoint.localPosition = hoverStartPoint;
            attachPoint.localPosition = attachStartPoint;
        }
        
    }

    public virtual void UserToolExit()
    {
        if (TexCore_Const.partfrom == Partfrom.VR)
        {
            hoverPoint.localPosition = hoverStartPoint;
            attachPoint.localPosition = attachStartPoint;
        }
        tool = null;
    }

    public virtual void UserToolExit(Transform toolEnd)
    {
        if (TexCore_Const.partfrom == Partfrom.VR)
        {
            hoverPoint.localPosition = hoverStartPoint;
            attachPoint.localPosition = attachStartPoint;
        }
        tool.SendMessage("OnExitMoveTo", toolEnd);
        tool = null;
    }
}
