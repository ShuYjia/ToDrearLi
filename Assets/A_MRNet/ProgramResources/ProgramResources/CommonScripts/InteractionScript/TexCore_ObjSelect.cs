
//using HighlightingSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TexCore_ObjSelect : TexCore_SelectBase
{
    //public Highlighter highlighter;

    public Color hover = Color.yellow;
    public Color click = Color.green;
    public Color hit = Color.blue;

    public Transform parent;

    public bool isHint = false;

    public bool isTrigger = false;

    public bool isToggle = false;

    public bool isSelect = false;

    public bool isRotateSelf = false;
    public Vector3 rotate;

    public GameObject selfObj;

    public UnityEvent ClickEvent;

    public override void OnNomal()
    {
        base.OnNomal();
        isSelect = false;
        //highlighter.tween = false;
        //highlighter.ConstantOff();
        if (selfObj != null) selfObj.SetActive(false);
        if (isHint) OnHit();
        //->注释
        //if (isHint)
        //{
        //    SetState(SelectState.Hit);
        //}
        //
    }

    public override void OnHover()
    {
        base.OnHover();
        //if (isSelect == false)
        //    highlighter.ConstantOn(hover);
        //if (isHint)
        //{
        //    highlighter.TweenStop();
        //}
    }

    public override void OnClick()
    {
        base.OnClick();
        if(isHint)isHint = false;
       // if (highlighter.tween == true) highlighter.tween = false;
        //if (isToggle && isSelect)
        //    isSelect = !isSelect;
        //else
        //    isSelect = true;
        //if (isSelect)
        //{
        //    highlighter.ConstantOn(click);
        //    if (selfObj != null) selfObj.SetActive(true);
        //}
        //else
        //{
        //    highlighter.ConstantOff();
        //    if (selfObj != null) selfObj.SetActive(false);
        //}
        if (isToggle && isSelect)
        {
            isSelect = false;
            SetState(SelectState.Nomal);
        }
        else
        {
            if (itemParent != null)
            {
                ResetBrother();
            }
            isSelect = true;
            state = SelectState.Click;
           // highlighter.ConstantOn(click);
            ClickEvent?.Invoke();
        }


        if (isTrigger)
        {
            SetState(SelectState.Nomal);
        }
    }

    protected override void OnHit()
    {
        base.OnHit();
      //  highlighter.TweenStart();
        isHint = true;
    }

    public void ResetBrother()
    {
        for (int i = 0; i < itemParent.childCount; i++)
        {
            //if (i == transform.GetSiblingIndex()) continue;
            TexCore_ObjSelect obj = itemParent.GetChild(i).GetComponentInChildren<TexCore_ObjSelect>();
            if (obj != null)
            {
                //print("还原" + obj.name);
                obj.SetState(SelectState.Nomal);
                //obj.SetSprite();
            }

        }
    }

    public override void OnEnable()
    {

        //if (GetComponent<Highlighter>() == null)
        //{
        //    highlighter = gameObject.AddComponent<Highlighter>();
        //}
        //else
        //{
        //    highlighter = gameObject.GetComponent<Highlighter>();
        //}
        base.OnEnable();
        //->注释
        if (isHint)
        {
            SetState(SelectState.Hit);
        }

    }
    public void Update()
    {
        //if (isRotateSelf)
        //{
        //    if (State == State.悬停)
        //    {
        //        transform.Rotate(rotate, Space.World);
        //    }
        //}
    }

   
}


