using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LayoutUpdate : MonoBehaviour
{
    RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        WaiteUpdateLayoutSingle();
    }

    public void WaiteUpdateLayoutSingle()
    {
        //StartCoroutine("U");
        if (rectTransform == null) return;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        LayoutUpdate p = transform.parent.GetComponent<LayoutUpdate>();
        if (p != null)
        {
            p.WaiteUpdateLayoutSingle();
        }
    }

    IEnumerator U()
    {
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        yield return 0;
        LayoutUpdate p = transform.parent.GetComponent<LayoutUpdate>();
        if (p!=null)
        {
            p.WaiteUpdateLayoutSingle();
        }
    }

    private void OnDisable()
    {
        WaiteUpdateLayoutSingle();
    }
}
