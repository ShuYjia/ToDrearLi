/**
 * 脚本用途：发射一条射线，连接物体与业务逻辑
 * 挂载    ：挂载在能看到物体的相机上
 * **/
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class TexCore_MouseCameraTarget : MonoBehaviour
{
    public static TexCore_MouseCameraTarget self;
    private Camera cam;
    public TexCore_SelectBase lastObj;

    public Transform targetTransform;

    public string scene;

    public Transform clickTemp;
    public Vector2 mousePos;
    public Transform XRrig;
    public bool isSetCam;
    void Awake()
    {
        self = this;
        cam = GetComponent<Camera>();
        scene = SceneManager.GetActiveScene().name;
    }

    private void OnEnable()
    {
        if (isSetCam)
        {
            ObjectPlacer.Instance.mainCamera = cam;
        }
    }

    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            if (lastObj != null)
            {
                if (lastObj.state != SelectState.Click)
                {
                    lastObj.SetState(SelectState.Nomal);
                }
                lastObj = null;
            }
            return;
        }
    }

    public void TargetingRaycast()
    {
 
        if (targetTransform != null)
        {
            TexCore_SelectBase obj = targetTransform.GetComponentInParent<TexCore_SelectBase>();
            if (obj != null)
            {
                if (lastObj != null && obj != lastObj)
                {
                    if (lastObj.state != SelectState.Click)
                        lastObj.SetState(SelectState.Nomal);
                    lastObj = null;
                }

                lastObj = obj;
                if (obj.state == SelectState.Nomal || obj.state == SelectState.Hover)
                {
                    lastObj.SetState(SelectState.Hover);
                   
                }


                clickTemp = obj.transform;

                if (clickTemp != null && clickTemp.name.Equals(obj.name))
                {
                    clickTemp = null;
                    mousePos = Vector2.zero;
                    obj.SetState(SelectState.Click);
                    if (scene != "03OperatePage")
                    {
                        TexCore_BusinessManager.OnClick(targetTransform.gameObject, scene);

                    }
                    else if (scene == "03OperatePage" && (obj.name[0] == 'n' || obj.name[0].Equals('w')))
                    {
                        TexCore_BusinessManager.OnClick(targetTransform.gameObject, scene);
                        TexCore_InputModule.instance.Click(targetTransform.gameObject);
                    }
                }

                targetTransform = null;
            }
            else
            {
                if (lastObj != null)
                {
                    if (lastObj.state != SelectState.Click)
                    {
                        lastObj.SetState(SelectState.Nomal);
                    }
                    lastObj = null;
                }
            }
        }
        else
        {
            if (lastObj != null)
            {
                if (lastObj.state != SelectState.Click)
                {
                    lastObj.SetState(SelectState.Nomal);
                }
                lastObj = null;
            }
            //if (TexCore_TipsManager.Self != null) TexCore_TipsManager.Self.HideTips();
        }
    }

    public void CameraMove(Transform tr)
    {
        DOTween.KillAll();
        XRrig.transform.DOMove(tr.position, 1);
        XRrig.transform.DORotate(tr.eulerAngles, 1);
    }
}


