

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;


public class TexCore_RPCHand : MonoBehaviour
{
    public bool isSyncTrans = true;
    public GrabTypes grabType;
    public TexCore_VisualHandCollider visualCollider;
    public GameObject target;
    public GameObject lastTarget;

    public UnityEvent<GrabTypes,int> onGrabType;
    public GrabTypes lastGrabType;
    public int clickTime = 0;

    public UnityEvent<SingleTransform> onHandHoverBegin;
    public UnityEvent<SingleTransform> onHandHoverUpdate;
    public UnityEvent<SingleTransform> onHandHoverEnd;

    public UnityEvent<SingleTransform> onAttachedToHand;
    public UnityEvent<SingleTransform> onHandAttachedUpdate;
    public UnityEvent<SingleTransform> onDetachedFromHand;

    protected void Awake()
    {
       
        visualCollider = transform.GetComponentInChildren<TexCore_VisualHandCollider>();
    }

    private void FixedUpdate()
    {

        if (TexCore_Const.isSinglePerson)
        {
            //grabType = hand.GetGrab();
            if (grabType != GrabTypes.None && grabType == lastGrabType)
                clickTime += 1;
            else
                clickTime = 0;
            lastGrabType = grabType;

            onGrabType?.Invoke(grabType, clickTime);
            target = visualCollider.target;
            //Debug.Log(transform.name + "__/" + target + "/");
            OnButtonEvent();
            return;
        }
    }
   
    public void OnSyncHandPos(float[] trans)
    {
        transform.position = new Vector3(trans[0], trans[1], trans[2]);
        transform.eulerAngles = new Vector3(trans[3], trans[4], trans[5]);
    }

    
    public void OnButtonEvent()
    {
        
        if (lastTarget != null&&((target==null||target!=lastTarget)/*||isHoverObj==false*/))
        {
            SingleTransform st = (new SingleTransform(transform, grabType));
            onHandHoverEnd?.Invoke(st);
            try
            {
                lastTarget.SendMessage("OnHandHoverEnd", st, SendMessageOptions.DontRequireReceiver);
                //TexCore_HighlighPlus hi = lastTarget.GetComponent<TexCore_HighlighPlus>();
                //if(hi!=null)
                  //  hi.SetState(SelectState.Nomal);
            }
            catch (Exception){}
            lastTarget = null;
        }
        if (lastTarget==null&&target!=null )
        {
            SingleTransform st = (new SingleTransform(transform, grabType));
            onHandHoverBegin?.Invoke(st);
            try
            {
                target.SendMessage("OnHandHoverBegin", st, SendMessageOptions.DontRequireReceiver);
                //TexCore_HighlighPlus hi = target.GetComponent<TexCore_HighlighPlus>();
                //if (hi != null)
                //{
                //    if(grabType==GrabTypes.None)
                //        hi.SetState(SelectState.Hover);
                //    else
                //        hi.SetState(SelectState.Click);
                //}
                    
            }
            catch (Exception){ }
        }
        if (target!=null)
        {
            lastTarget = target;
            SingleTransform st = (new SingleTransform(transform, grabType));
            onHandHoverUpdate?.Invoke(st);
            try
            {
                target.SendMessage("HandHoverUpdate", st, SendMessageOptions.DontRequireReceiver);
            }
            catch (Exception) { }
        }

    }

}

public enum GrabTypes {
    None,
    Pinch,
    Grip
}

[Serializable]
public class SingleTransform
{
    public Transform virtualTransform;
    public GrabTypes startingGrabType;

    public SingleTransform(Transform _base, GrabTypes grab)
    {
        this.virtualTransform = _base;
        this.startingGrabType = grab;
    }
}