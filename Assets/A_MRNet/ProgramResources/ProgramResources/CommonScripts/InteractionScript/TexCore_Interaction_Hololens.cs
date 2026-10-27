using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using MixedReality.Toolkit.UX;

public class TexCore_Interaction_Hololens : TexCore_SelectBase
{
    public GameObject clickChild;

    public bool isSelect = false;

    public bool isBusinessManager = true;

    public PressableButton pressableButton;

    private void Start()
    {
        
        pressableButton.OnClicked.AddListener(OnClickEvent);
        SetSprite();
    }

    public void OnClickEvent() {


        if (isSelect)
        {
            isSelect = false;
            state = SelectState.Hover;
            SetSprite();
     
        }
        else
        {
            if (itemParent != null)
                ResetBrother();

            if (isSelect == false)
            {
                isSelect = true;
                state = SelectState.Click;
                SetSprite();
            }

            if (isBusinessManager)
            {
                TexCore_BusinessManager.OnClick(gameObject, SceneManager.GetActiveScene().name);
            }
        }

    }
    public override void OnNomal()
    {
        base.OnNomal();
        if (isSelect == false)
        {
            state = SelectState.Nomal;
            SetSprite();
        }
    }

    public override void OnHover()
    {
        base.OnHover();
        if (isSelect == false)
        {
            state = SelectState.Hover;
            SetSprite();
        }
    }
    public void SetSprite()//更新按钮显示状态
    {
        //print(state);
        if (clickChild != null)
        {
            //print(gameObject.name + isSelect);
            if (isSelect)
                clickChild.SetActive(true);
            else
                clickChild.SetActive(false);
        }
        switch (state)
        {
            case SelectState.Nomal:
                break;
            case SelectState.Hover:
                break;
            case SelectState.Click:
                break;
            case SelectState.Hit:
                break;
            default:
                break;
        }
    }

    public void ResetClick()
    {
        isSelect = false;
        SetState(SelectState.Nomal);
        SetSprite();

    }



    public void ResetBrother()
    {
        for (int i = 0; i < itemParent.childCount; i++)
        {
            TexCore_Interaction_Hololens obj = itemParent.GetChild(i).GetComponentInChildren<TexCore_Interaction_Hololens>();
            if (obj != null && obj.Equals(this) == false)
            {
                obj.isSelect = false;
                obj.SetState(SelectState.Nomal);
            }
        }
    }

}

   
