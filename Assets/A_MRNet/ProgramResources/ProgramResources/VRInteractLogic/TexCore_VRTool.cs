using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using DG.Tweening;
using UnityEngine.Events;

public class TexCore_VRTool : MonoBehaviour
{
    public bool enableCatch = true;
    public Transform startTransform;

    public Transform catchHand;

    public Transform toolSphereTransform;

    public Vector3 startPos;
    public Vector3 startRot;
    public Vector3 startScale;

    public UnityEvent<bool> OnHandChang;

    public bool isInHand = false;

    protected virtual void Awake()
    {
        toolSphereTransform = transform.Find("SphereTransform");
        startTransform = transform.parent;
        startScale = Vector3.one;
    }

    protected virtual void Start()
    {
        //startPos = transform.position;
        //startRot = transform.eulerAngles;

        startPos = transform.localPosition;
        startRot = transform.localEulerAngles;

        //OnHandChang.AddListener((b) => {
        //    gameObject.SendMessage("ChangeIsSyncTrans", b);
        //});
    }
    public void ReStartTool()
    {
        enableCatch = true;
        GetComponent<Collider>().enabled = true;
        gameObject.layer = 0;
    }
    public void OnExit()
    {
        transform.SetParent(startTransform);
        transform.DOLocalMove(startPos, 2f);
        transform.DOScale(startScale, 2f);
        Tween tween= transform.DOLocalRotate(startRot, 2f);
        startingGrabType = GrabTypes.None;
        tween.onComplete+=(() => {
            ReStartTool();
            OnHandChang?.Invoke(false);
        });
        //gameObject.SendMessage("ResetName");
    }
    public void OnExitPoint()
    {
        GetComponent<Collider>().enabled = true;
        startingGrabType = GrabTypes.None;
        gameObject.layer = 0;
    }
    public void OnExitMoveTo(Transform endPoint)
    {
        transform.SetParent(endPoint);
        transform.DOMove(endPoint.position, 1f);
        startingGrabType = GrabTypes.None;
        Tween tween = transform.DORotate(endPoint.eulerAngles, 1f);
        tween.onComplete += (() => {
            GetComponent<Collider>().enabled = true;
            gameObject.layer = 0;
        });
    }
  
    public GrabTypes startingGrabType;
    protected virtual void HandHoverUpdate(SingleTransform hand)
    {
        if (!enableCatch) return;


        if (/*TexCore_Const.partfrom == Partfrom.VR  &&*/ hand.startingGrabType == GrabTypes.Pinch && startingGrabType != hand.startingGrabType) 
        {
            startingGrabType = hand.startingGrabType;
            TexCore_RPCHandTool t = hand.virtualTransform.GetComponent<TexCore_RPCHandTool>();
            if (t != null)
            {
                t.SetTool(gameObject);

                catchHand = hand.virtualTransform;

                OnHandChang?.Invoke(true);
                if (t.tool == null)
                {
                    //interactable.enabled = false;
                    //GetComponent<Collider>().enabled = false;

                    
                }

            }

        }
    }

    
    public void OnHandHoverEnd(SingleTransform hand)
    {
        if (TexCore_Const.partfrom == Partfrom.PC) 
        {
            startingGrabType = GrabTypes.None;
        }

    }

}
