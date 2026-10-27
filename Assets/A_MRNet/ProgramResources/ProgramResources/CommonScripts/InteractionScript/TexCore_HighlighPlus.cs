/*
using HighlightPlus;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TexCore_HighlighPlus : TexCore_SelectBase
{
    public  HighlightEffect effect;

    public bool isRotateSelf = false;
    public Vector3 rotate;

    public GameObject selfObj;

    public bool useNomalColor=true;
    public bool useNomalWidth = true;
    public Color nomal = Color.white;
    public float nomalV = 0.25f;
    public Color hover = Color.yellow;
    public float hoverV = 0.25f;
    public Color click = Color.blue;
    public float clickV = 0.25f;
    public Color hit = Color.green;
    public float hitV = 0.25f;

    //public Transform parent;

    public bool isHint = false;

    public bool isToggle = false;

    public bool isTrigger = false;

    public bool isSelect = false;

    public UnityAction ClickEvent;

   
    public override void OnEnable()
    {
        if (TexCore_HightlightManager.self != null)
            TexCore_HightlightManager.self.ItemInit(this);
    }
    private void Start()
    {
        effect = GetComponent<HighlightEffect>();
        SetEffect();
    }
    public void OnDisable()
    {
        if (TexCore_HightlightManager.self != null)
            TexCore_HightlightManager.self.RemoveItem(this);
    }
    public override void OnNomal()
    {
        base.OnNomal();
        isSelect = false;
        if (isHint)
        {
            SetState(SelectState.Hit);
            return;
        }
        SetEffect();
    }

    public override void OnHover()
    {
        base.OnHover();

        SetEffect();
    }

    public override void OnClick()
    {
        base.OnClick();
        isHint = false;
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
            SetEffect();
            if (ClickEvent != null)
            {
                ClickEvent();
            }
        }
        if (isTrigger)
        {
            SetState(SelectState.Nomal);
        }
    }

    protected override void OnHit()
    {
        base.OnHit();
        isHint = true;
        SetEffect();
    }
    public void ResetBrother()
    {
        for (int i = 0; i < itemParent.childCount; i++)
        {
            //if (i == transform.GetSiblingIndex()) continue;
            TexCore_UISelect obj = itemParent.GetChild(i).GetComponentInChildren<TexCore_UISelect>();
            if (obj != null)
            {
                //print("还原" + obj.name);
                obj.SetState(SelectState.Nomal);
                obj.SetSprite();
            }

        }
    }

    public void SetEffect()
    {
        switch (state)
        {
            case SelectState.Nomal:
                effect.SetHighlighted(false);
                break;
            case SelectState.Hover:
                effect.SetHighlighted(true);
                effect.outlineColor = hover;
                effect.outline = hoverV;
                break;
            case SelectState.Click:
                effect.SetHighlighted(true);
                effect.outlineColor = click;
                effect.outline = clickV;
                break;
            case SelectState.Hit:
                effect.SetHighlighted(true);
                effect.outlineColor = hit;
                effect.outline = hitV;
                //effect.glowHQColor = hit;
                //effect.glow = hitV;
                break;
            default:
                break;
        }
    }

    
}
*/