/**
 * 脚本用途：发射一条射线，连接物体与业务逻辑
 * 挂载    ：挂载在能看到物体的相机上
 * **/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TexCore_Interaction_CameraTarget : MonoBehaviour
{
    public LayerMask targetingLayerMask = -1;
    private float targetingRayLength = 1000f;
    private Camera cam;

    public TexCore_Interaction lastObj;

    public Transform targetTransform;

    public string scene;

    public Transform clickTemp;

    void Awake()
    {
        cam = GetComponent<Camera>();
        scene = SceneManager.GetActiveScene().name;
    }
    void Update()
    {
        TargetingRaycast();
    }

    public void TargetingRaycast()
    {
        targetTransform = null;
        if (cam != null)
        {
            RaycastHit hitInfo;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hitInfo, targetingRayLength, targetingLayerMask.value))
            {
                targetTransform = hitInfo.collider.transform;
            }
        }
        if (targetTransform != null)
        {
            //鼠标悬浮三维物体：高亮显示
            TexCore_Interaction obj = targetTransform.GetComponentInParent<TexCore_Interaction>();
            if (obj != null)
            {
                if (lastObj != null && obj != lastObj)
                {
                    if (lastObj.interacionType != InteracionType.click)
                        lastObj.SetInteractionType(InteracionType.selectable);
                    lastObj = null;
                }

                lastObj = obj;

                if (obj.interacionType == InteracionType.selectable || obj.interacionType == InteracionType.hover)
                {
                    lastObj.SetInteractionType(InteracionType.hover);
                    //TexCore_BusinessManager.OnHover(targetTransform.gameObject);
                }

                if (Input.GetMouseButtonDown(0))
                {
                    clickTemp = obj.transform;

                }
                if (Input.GetMouseButtonUp(0))
                {
                    if (clickTemp != null && clickTemp.name.Equals(obj.name))
                    {
                        clickTemp = null;
                        obj.SetInteractionType(InteracionType.click);
                        TexCore_BusinessManager.OnClick(targetTransform.gameObject, scene);
                    }
                }
            }
            else
            {
                if (lastObj != null)
                {
                    if (lastObj.interacionType != InteracionType.click)
                    {
                        lastObj.SetInteractionType(InteracionType.selectable);
                    }
                    lastObj = null;
                }
            }
        }
        else
        {
            if (lastObj != null)
            {
                if (lastObj.interacionType != InteracionType.click)
                {
                    lastObj.SetInteractionType(InteracionType.selectable);
                }
                lastObj = null;
            }
            //if (TexCore_TipsManager.Self != null) TexCore_TipsManager.Self.HideTips();
        }
    }
}
