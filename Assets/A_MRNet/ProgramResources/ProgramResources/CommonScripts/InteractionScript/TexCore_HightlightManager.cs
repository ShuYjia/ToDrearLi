using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TexCore_HightlightManager : MonoBehaviour
{
    public static TexCore_HightlightManager self;

    //public List<TexCore_HighlighPlus> allHightlight;

    public Color nomal;
    public float nomalV;
    public Color hover;
    public float hoverV;
    public Color click;
    public float clickV;
    public Color hint;
    public float hintV;

    private void Awake()
    {
        self = this;
    }

    //public void ItemInit(TexCore_HighlighPlus item)
    //{
    //    if (allHightlight == null) allHightlight = new List<TexCore_HighlighPlus>();
    //    allHightlight.Add(item);
    //    if (item.useNomalColor)
    //    {
    //        item.nomal = nomal;
    //        item.hover = hover;
    //        item.click = click;
    //        item.hit = hint;
    //    }
    //    if (item.useNomalWidth)
    //    {
    //        item.nomalV = nomalV;
    //        item.hoverV = hoverV;
    //        item.clickV = clickV;
    //        item.hitV = hintV;
    //    }
    //}

    //public void RemoveItem(TexCore_HighlighPlus item)
    //{
    //    foreach (var _item in allHightlight)
    //    {
    //        if (item.Equals(_item))
    //        {
    //            allHightlight.Remove(item);
    //        }
    //    }
    //}

    //public void SetHighlightEnable(bool isEnable)
    //{
    //    foreach (var item in allHightlight)
    //    {
    //        if(item!=null)
    //            item.effect.SetHighlighted(isEnable);
    //    }
    //}
}
