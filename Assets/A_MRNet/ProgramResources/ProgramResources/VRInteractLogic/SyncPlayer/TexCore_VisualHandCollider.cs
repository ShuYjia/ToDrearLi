
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static UnityEngine.GraphicsBuffer;
using Bpf.Diagnostics;

public class TexCore_VisualHandCollider : MonoBehaviour
{

    private void Start()
    {
        
    }
    private void FixedUpdate()
    {
        if (target != null)
        {
            BpfLog.Info(BpfLogCategory.Network, "tex_core_visual_hand_collider.fixed_update", null, null, BpfLogField.Of("name", target.name));
        }
        //if (hand!=null)
        //{
        //    if (hand.hoveringInteractable == null)
        //    {
        //        target = null;print("target=null");
        //    }
        //    else
        //    {
        //        target = hand.hoveringInteractable.gameObject;print("target:"+target.name);
        //    }
        //    //target = hand.hoveringInteractable == null ? null : hand.hoveringInteractable.gameObject;
        //   // Debug.Log("hand:" + hand.hoveringInteractable + "__/target+" + target);
        //    //target = target.gameObject.activeSelf ? target : null;
        //}
        //if (target!=null)
        //{
        //    if (target.GetComponent<Collider>().enabled==false)
        //    {
        //        target = null;
        //        Debug.Log("target.collider==null");
        //    }
        //}
    }
    public GameObject target;

    private void OnTriggerEnter(Collider other)
    {
        target = other.gameObject;
    }
    private void OnTriggerStay(Collider other)
    {
        //Debug.Log(visualHand.gameObject.name + ":触发到" + other.gameObject);
        target = other.gameObject;
    }
    private void OnTriggerExit(Collider other)
    {
        target = null;
    }

    private void OnCollisionEnter(Collision collision)
    {
        target = collision.gameObject;
    }
    private void OnCollisionStay(Collision collision)
    {
        target = collision.gameObject;
    }
    private void OnCollisionExit(Collision collision)
    {
        target = null;
    }
}
