using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class TexCore_Interaction_UGUI : TexCore_SelectBase, IPointerClickHandler,IPointerEnterHandler,IPointerExitHandler
{
    public GameObject clickChild;

    public Color nomal = Color.white;
    public GameObject nomalImage;
    public Color hover = Color.yellow;
    public GameObject hoverImage;
    public Color click = Color.blue;
    public GameObject clickImage;


    public bool isToggle = false;

    public bool isUnSelect = false;

    public bool isTrigger = false;

    public bool isSelect = false;

    public bool isSole = false;

    public bool isBusinessManager = true;

    public List<Image> bgs;

    public UnityEvent ClickEvent;

    
    private void Start()
    {
        SetSprite();
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
                if (nomalImage != null) nomalImage.SetActive(true);
                if (hoverImage != null) hoverImage.SetActive(false);
                if (clickImage != null) clickImage.SetActive(false);
                foreach (Image item in bgs)
                    item.color = nomal;
                break;
            case SelectState.Hover:
                if (nomalImage != null) nomalImage.SetActive(false);
                if (hoverImage != null) hoverImage.SetActive(true);
                if (clickImage != null) clickImage.SetActive(false);
                foreach (Image item in bgs)
                    item.color = hover;
                break;
            case SelectState.Click:
                if (nomalImage != null) nomalImage.SetActive(false);
                if (hoverImage != null) hoverImage.SetActive(false);
                if (clickImage != null) clickImage.SetActive(true);
                foreach (Image item in bgs)
                    item.color = click;
                break;
            case SelectState.Hit:
                break;
            default:
                break;
        }
    }

    public float doubleClickHodel;
    public int clickCount = 0;

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

    public override void OnClick()
    {
        base.OnClick();
        if (isToggle && isSelect)
        {
            if (isSole) return;
            isSelect = false;
            state = SelectState.Hover;
            SetSprite();
            if (isUnSelect)
            {
                ClickEvent?.Invoke();
                TexCore_BusinessManager.OnClick(gameObject, SceneManager.GetActiveScene().name);
            }
        }
        else
        {
            if (itemParent != null)
                ResetBrother();

            if (isSelect==false)
            {
                isSelect = true;
                state = SelectState.Click;
                SetSprite();
            }
            

            ClickEvent?.Invoke();
            if(isBusinessManager)
            {
                TexCore_BusinessManager.OnClick(gameObject, SceneManager.GetActiveScene().name);
            }

            if (isTrigger)
                Invoke("ResetClick", 0.2f);
        }
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
       OnNomal();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
       OnClick();
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
            TexCore_Interaction_UGUI obj = itemParent.GetChild(i).GetComponentInChildren<TexCore_Interaction_UGUI>();
            if (obj != null && obj.Equals(this)==false) 
            {
                obj.isSelect = false;
                obj.SetState(SelectState.Nomal);
            }
        }
    }
}
