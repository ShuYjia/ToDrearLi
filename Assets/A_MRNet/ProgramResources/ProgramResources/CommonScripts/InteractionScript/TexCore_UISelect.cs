using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class TexCore_UISelect : TexCore_SelectBase
{
    public Color nomal = Color.white;
    public GameObject nomalImage;
    public Color hover = Color.yellow;
    public GameObject hoverImage;
    public Color click = Color.blue;
    public GameObject clickImage;
    //public Transform parent;

    public bool isHint = false;

    public bool isToggle = false;

    public bool isTrigger = false;

    public bool isSelect = false;

    public List<Image> bgs;

    public bool isNomalColiderSize = true;

    private RectTransform rect;

    private new BoxCollider collider;

    //public bool userUGUI=false;

    public UnityAction ClickEvent;

    public virtual void Awake()
    {
        rect = GetComponent<RectTransform>();
        collider = GetComponent<BoxCollider>();

    }
    private void Start()
    {

        SetSprite();
    }
    public override void OnNomal()
    {
        base.OnNomal();
        isSelect = false;
        foreach (Image item in bgs)
        {
            item.color = nomal;
        }
        SetSprite();
    }

    public override void OnHover()
    {
        base.OnHover();
        
        SetSprite();
    }

    public override void OnClick()
    {
        base.OnClick();
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
            SetSprite();
            if (ClickEvent!=null)
            {
                ClickEvent();
            }

            //IPointerClickHandler pointer = GetComponent<IPointerClickHandler>();
            //if (pointer != null&&gameObject.GetComponent<TexCore_RPCUISelect>()==null)
            //{
               // TexCore_InputModule.instance.Click(gameObject);
            //}
        }
        if (isTrigger)
        {
            SetState(SelectState.Nomal);
        }
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

    public void SetSprite()
    {
        if (nomalImage != null) nomalImage.SetActive(false);
        if (hoverImage != null) hoverImage.SetActive(false);
        if (clickImage != null) clickImage.SetActive(false);
        switch (state)
        {
            case SelectState.Nomal:
                if (nomalImage != null) nomalImage.SetActive(true);
                foreach (Image item in bgs)
                {
                    item.color = nomal;
                }
                break;
            case SelectState.Hover:
                if (hoverImage != null) hoverImage.SetActive(true);
                foreach (Image item in bgs)
                {
                    item.color = hover;
                }
                break;
            case SelectState.Click:
                if (clickImage != null) clickImage.SetActive(true);
                foreach (Image item in bgs)
                {
                    item.color = click;
                }
                break;
            case SelectState.Hit:
                break;
            default:
                break;
        }
    }

    public virtual void Update()
    {
        if (isNomalColiderSize)
        {
            if (rect.rect.width != 0)
            {
                isNomalColiderSize = false;
                if (collider == null)
                {
                    collider = gameObject.AddComponent<BoxCollider>();
                }
                Vector3 colliderSize = new Vector2(rect.rect.width, rect.rect.height);
                colliderSize.z = 1f;
                collider.size = colliderSize;
                isNomalColiderSize = false;
            }
        }
    }
}
