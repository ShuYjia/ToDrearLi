using UnityEngine;
using Bpf.Diagnostics;


public class TexCore_RPCPCHandTool : TexCore_RPCHandTool
{
    public override void Start()
    {
        hand = transform.GetComponent<TexCore_RPCHand>();
        hand.onGrabType.AddListener(OnExit);
    }

    public override void SetTool(GameObject t)
    {
        //设置持有工具
        if (tool != null)
        {
            ExitHandModelTool();
        }

        GameObject toolObj = Instantiate(t);
        toolObj.name = t.name;
        toolObj.transform.position = new Vector3(0, -10, 0);
        tool = toolObj;

        //设置手部模型持有工具
        //SetHandModelTool(t);

        //设置鼠标图标
        BpfLog.Info(BpfLogCategory.Network, "tex_core_rpcpc_hand_tool.set_tool", null, null, BpfLogField.Of("t", t));
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

    public override void OnExit(GrabTypes grabTypes, int clickTime)//手松开模型
    {
        if (grabTypes == GrabTypes.Grip)
        {
            if (clickTime >= exitCount)
            {
                //让持有的工具离开手，回到原位
                if (tool != null)
                {
                    ExitHandModelTool();
                    
                }
            }
        }
    }
    public override void ExitHandModelTool()
    {
        BpfLog.Info(BpfLogCategory.Network, "tex_core_rpcpc_hand_tool.exit_hand_model_tool", "移除工具");
        tool.SendMessage("OnExit");
        SetMouseCursor();
        //tool = null;
        Destroy(tool);
    }

    public override void UserToolExit(Transform toolEnd)
    {
        tool.SendMessage("OnExitMoveTo", toolEnd);
        tool = null;
    }
}
