using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TexCore_Interaction : MonoBehaviour
{
    public InteracionType interacionType;


    public UnityAction OnNomal;

    public UnityAction OnHover;

    public UnityAction OnHint;

    public UnityAction OnClick;

    public UnityAction OndisSelect;

    public void SetInteractionType(InteracionType _interacionType)
    {
        switch (_interacionType)
        {
            case InteracionType.selectable:
                if (OnNomal!=null)
                {
                    OnNomal();
                }
                break;
            case InteracionType.hint:
                if (OnHint != null)
                {
                    OnHint();
                }
                break;
            case InteracionType.hover:
                if (OnHover != null)
                {
                    OnHover();
                }
                break;
            case InteracionType.click:
                if (OnClick != null)
                {
                    OnClick();
                }
                break;
            case InteracionType.disSelect:
                if (OndisSelect != null)
                {
                    OndisSelect();
                }
                break;
            default:
                break;
        }
    }

    ///// <summary>
    ///// 默认状态
    ///// </summary>
    //protected virtual void OnNomal()
    //{
    //    //UI和3d对象都会调用给的一个方法
    //}
    ///// <summary>
    ///// 悬停状态
    ///// </summary>
    //protected virtual void OnHover()
    //{

    //}
    ///// <summary>
    ///// 点击状态
    ///// </summary>
    //protected virtual void OnClick()
    //{

    //}
    ///// <summary>
    ///// 提示状态
    ///// </summary>
    //protected virtual void OnHit()
    //{

    //}
}
