using DG.Tweening;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TerrainTools;


public class ToolCircularClim : MonoBehaviour
{
    public Dir turnAxis;

    public float maxAngle;
    public float minAngle;
    public float enterAngle;

    public string toolName;
    public TexCore_VRTool toolObj;

    public Transform toolEnd;

    public TexCore_VirtualCircular virtualCircular;

    public GrabTypes startGrabType;

    public bool isOver = false;

    private VRHandTurnEvent toolTurn;

    private TexCore_CircularDrive circularDrive;

    private Vector3 ToolNomalScale;

    private void Start()
    {
        circularDrive = GetComponent<TexCore_CircularDrive>();
    }
    public void HandHoverUpdate(SingleTransform st)
    {
        if (st.startingGrabType != GrabTypes.None&&isOver==false  ) //手柄按下了按键
        {
            if (toolObj==null&& st.startingGrabType != GrabTypes.None)//手柄为第一次按下
            {
                TexCore_RPCHandTool ht = st.virtualTransform.GetComponent<TexCore_RPCHandTool>();
                if (ht != null && ht.tool != null)
                {
                    if (ht.tool.name.Equals(toolName))
                    {
                        toolEnd.SetParent(transform.parent);

                        gameObject.GetComponent<Collider>().enabled = false;
                        startGrabType = st.startingGrabType;
                        toolObj = ht.tool.GetComponent<TexCore_VRTool>();
                        ToolNomalScale = ht.tool.transform.localScale;

                        toolObj.transform.DOScale(toolEnd.localScale, 1f);
                        toolObj.transform.SetParent(transform);
                        toolObj.transform.DOMove(toolEnd.position, 0.6f);
                        toolObj.transform.DORotate(toolEnd.eulerAngles, 0.6f);

                        toolObj.enableCatch = false;
                        toolTurn = toolObj.gameObject.AddComponent<VRHandTurnEvent>();
                        toolTurn.targets = new List<GameObject>();
                        toolTurn.targets.Add(gameObject);
                        toolObj.GetComponent<Collider>().enabled = true;
                        toolObj.gameObject.layer = 0;
                        ht.UserToolExit();
                    }
                }
            }
            if(toolObj!=null) //手柄按住拖动
            {
                if (circularDrive .outAngle>= maxAngle)
                {
                    maxAngle += maxAngle;
                    toolObj.transform.DOMove(toolEnd.position,0.6f);
                    toolObj.transform.DORotate(toolEnd.eulerAngles, 0.6f);
                }
                else if (circularDrive.outAngle <= minAngle)
                {
                    minAngle += minAngle;
                    toolObj.transform.DOMove(toolEnd.position, 0.6f);
                    toolObj.transform.DORotate(toolEnd.eulerAngles, 0.6f);
                }
                if (Mathf.Abs(circularDrive.outAngle - enterAngle)<6f)
                {
                    Destroy(toolTurn.GetComponent<VRHandTurnEvent>());
                    virtualCircular.gameObject.SetActive(true);
                    virtualCircular.Open();
                    toolObj.transform.DOScale(ToolNomalScale, 1f);
                    toolObj.ReStartTool();
                    //GetComponent<Collider>();
                    if (TexCore_Const.partfrom==Partfrom.PC)
                    {
                        Destroy(toolObj.gameObject);
                    }
                    toolObj = null;
                    isOver = true;
                }
            }
           
        }
    }
}
