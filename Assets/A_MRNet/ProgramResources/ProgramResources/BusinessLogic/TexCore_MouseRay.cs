using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TexCore_MouseRay : MonoBehaviour
{
    public LayerMask layerMask;
    public int maxDis;

    public TexCore_OverlapCollision virtualHand;
    public List<GameObject> hoverObjs = new List<GameObject>();

    public Camera m_camera;

    public GameObject lastObj;
    public GameObject clickTemp;
    private void Awake()
    {
    }

    private void OnEnable()
    {
        if (m_camera==null)
        {
            m_camera = GetComponent<Camera>();
        }
    }

    public void FixedUpdate()
    {
        Ray ray = m_camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit raycastHit;
        if (Physics.Raycast(ray,out raycastHit,maxDis,layerMask))
        {
            virtualHand.transform.position = raycastHit.point;

            if (lastObj != null && lastObj.Equals(raycastHit.collider.gameObject) == false) 
            {
                lastObj.GetComponent<TexCore_ObjSelect>().SetState(SelectState.Nomal);
                lastObj = null;
            }
            TexCore_ObjSelect obs = raycastHit.collider.gameObject.GetComponent<TexCore_ObjSelect>();
            if (obs!=null)
            {
                lastObj = obs.gameObject;
                if (Input.GetMouseButtonDown(0))
                    clickTemp = obs.gameObject;
                if (Input.GetMouseButtonUp(1) && clickTemp.Equals(clickTemp)) 
                {
                    clickTemp = null;
                    obs.SetState(SelectState.Click);
                    TexCore_BusinessManager.OnClick(clickTemp, SceneManager.GetActiveScene().name);
                }
            }
        }
        else if(lastObj!=null)
        {
            lastObj.GetComponent<TexCore_ObjSelect>().SetState(SelectState.Nomal);
            lastObj = null;
        }
        if (virtualHand.colliders != null && virtualHand.colliders.Length > 0)
        {
            GrabTypes grabTypes;
            if (Input.GetMouseButton(0)) grabTypes = GrabTypes.Pinch;
            else if (Input.GetMouseButton(0)) grabTypes = GrabTypes.Pinch;
            else if (Input.GetMouseButton(1)) grabTypes = GrabTypes.Grip;
            else grabTypes = GrabTypes.None;

            for (int i = 0; i < virtualHand.colliders.Length; i++)
            {
                virtualHand.colliders[i].gameObject.SendMessage("HandHoverUpdate", new SingleTransform(virtualHand.transform, grabTypes));

                //TexCore_ObjSelect OS = virtualHand.colliders[i].gameObject.GetComponent<TexCore_ObjSelect>();
                //if (OS!=null)
                //{
                //    if (grabTypes != GrabTypes.None)
                //    {
                //        if ( OS.state != SelectState.Click)
                //        {
                //            OS.SetState(SelectState.Click);
                //        }
                //    }
                //}
            }
        }

    }

   
    
}
