using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SelectState
{
    Nomal,
    Hover,
    Click,
    Hit
}
public enum InteractionType
{
    Nomal,
    PBS
}

public class TexCore_SelectBase : MonoBehaviour
{
    /// <summary>
    /// 选择状态
    /// </summary>
    public  SelectState state;

    public InteractionType InteractionType;

    private TexCore_SelectBase select;

    public Transform itemParent;


    public virtual void OnEnable()
    {
       

        //SetState(state);
    }

    public void SetState(SelectState newState)
    {
        state = newState;

        switch (state)
        {
            case SelectState.Nomal:
                OnNomal();
                break;
            case SelectState.Hover:
                OnHover();
                break;
            case SelectState.Click:
                OnClick();
                break;
            case SelectState.Hit:
               OnHit();
                break;
            default:
                break;
        }
    }
    /// <summary>
    /// 默认状态
    /// </summary>
    public virtual void OnNomal()
    {
        //UI和3d对象都会调用给的一个方法
    }
    /// <summary>
    /// 悬停状态
    /// </summary>
    public virtual void OnHover()
    {
        TexCore_EventCenter.Broadcast<string>(TexCore_EventType.交互音频, "Hover");
    }
    /// <summary>
    /// 点击状态
    /// </summary>
    public virtual void OnClick()
    {
        TexCore_EventCenter.Broadcast<string>(TexCore_EventType.交互音频, "Click");

    }
    /// <summary>
    /// 提示状态
    /// </summary>
    protected virtual void OnHit()
    {

    }
}
