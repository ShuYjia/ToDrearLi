using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TexCore_BarDiagramItem : MonoBehaviour
{
    public float value;

    public float startValue;

    public float scale;

    public Scrollbar scrollbar;
    public TextMeshProUGUI valueText;
    public TextMeshProUGUI nameText;

    public void Init(string name,float _value, float _scale)
    {
        nameText.text = name;
        InitValue(_value,_scale);
    }

    public void InitValue(float _value,float _scale)
    {
        this.value = _value;
        this.scale = _scale;
        valueText.text = value.ToString();
        scrollbar.size =  scale;
    }


}
