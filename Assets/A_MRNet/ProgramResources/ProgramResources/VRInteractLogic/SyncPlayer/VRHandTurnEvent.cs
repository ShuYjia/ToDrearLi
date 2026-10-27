using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static UnityEngine.GraphicsBuffer;

//[RequireComponent(typeof(Interactable))]
public class VRHandTurnEvent : MonoBehaviour
{
    public List<GameObject> targets;

    public void OnHandHoverBegin(SingleTransform st)
    {
        if (targets == null || targets.Count <= 0) return;
        foreach (var item in targets)
        {
            item.SendMessage("OnHandHoverBegin", st, SendMessageOptions.DontRequireReceiver);
        }
    }
    public void HandHoverUpdate(SingleTransform st)
    {
        if (targets == null || targets.Count <= 0) return;
        foreach (var item in targets)
        {
            item.SendMessage("HandHoverUpdate", st, SendMessageOptions.DontRequireReceiver);
        }
    }
    public void OnHandHoverEnd(SingleTransform st)
    {
        if (targets == null || targets.Count <= 0) return;
        foreach (var item in targets)
        {
            item.SendMessage("OnHandHoverEnd", st, SendMessageOptions.DontRequireReceiver);
        }
    }
    public void OnAttachedToHand(SingleTransform st)
    {
        if (targets == null || targets.Count <= 0) return;
        foreach (var item in targets)
        {
            item.SendMessage("OnAttachedToHand", st, SendMessageOptions.DontRequireReceiver);
        }
    }
    public void HandAttachedUpdate(SingleTransform st)
    {
        if (targets == null || targets.Count <= 0) return;
        foreach (var item in targets)
        {
            item.SendMessage("HandAttachedUpdate", st, SendMessageOptions.DontRequireReceiver);
        }
    }
    public void OnDetachedFromHand(SingleTransform st)
    {
        if (targets == null || targets.Count <= 0) return;
        foreach (var item in targets)
        {
            item.SendMessage("OnDetachedFromHand", st, SendMessageOptions.DontRequireReceiver);
        }
    }
}
