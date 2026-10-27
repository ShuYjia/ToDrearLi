using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class RulerAnim : MonoBehaviour
{
    public RulerBody body;

    public Transform bodyStart;
    public Vector3 bodyEnd;
    public float pre_distane;

    public bool startEnable=true;
    public float animTime = 1.5f;
    NodePanelBase panelBase;
    public void OnEnable()
    {
        bodyEnd=body.transform.position;
        body.transform.position = bodyStart.position;
        if (startEnable) BodyAnim();
    }

    public void BodyAnim()
    {
        if(body.realDis==false)
        {
            body.text.gameObject.SetActive(false);
        }

        body.transform.DOMove(bodyEnd, 1.5f).onComplete= ShowTest;
    }
    public void ShowTest()
    {
        panelBase = transform.GetComponentInParent<NodePanelBase>();
        panelBase.activeChild.selfObjs.GetProperty("UI").SetActive(true);
        ObjectToggle objectToggle = transform.GetComponentInParent<ObjectToggle>();
        panelBase.activeChild.selfObjs.GetProperty("ÌîÐ´Êý¾Ý×é").transform.Find(objectToggle.name).gameObject.SetActive(true);
    }

}
