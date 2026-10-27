
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class TexCore_Diagram : MonoBehaviour
{
    public Transform itemParent;
    public GameObject pre_item;

    public float maxValue;

    private void Start()
    {
        List<float> floats = new List<float>();
        for (int i = 0; i < 50; i++)
        {
            floats.Add(Random.Range(0f, i));
        }
        Init(floats);
    }

    public void Init<T>(List<T> _vs){ }

    public void Init(List<float> _vs)
    {
        for (int i = 0; i < _vs.Count; i++)
        {
            GameObject newItem = Instantiate(pre_item, itemParent);
            newItem.SetActive(true);
            newItem.GetComponent<TexCore_BarDiagramItem>().Init((i + 1).ToString("D2"), _vs[i], _vs[i] / maxValue);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(itemParent.GetComponent<RectTransform>());
    }

}
