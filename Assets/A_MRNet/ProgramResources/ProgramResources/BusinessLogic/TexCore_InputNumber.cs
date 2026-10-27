using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TexCore_InputNumber : MonoBehaviour
{
    public TMP_InputField output;

    public TMP_InputField input;

    public static TexCore_InputNumber self;

    public GameObject btns;

    public void Awake()
    {
        if (self == null)
        {
            self = this;
        }
        else
        {
            this.enabled = false;
        }
        gameObject.SetActive(false);
    }
    public void InputActive(TMP_InputField tMP_InputField)
    {
        gameObject.SetActive(true);
        output = tMP_InputField;

    }

    public void OnClick(GameObject obj)
    {
        string num = obj.name.Split('_')[1];
        switch (num)
        {
            case "确认":
                output.text = input.text;
                input.text = string.Empty;
                gameObject.SetActive(false);
                break;
            case "删除":
                input.text = input.text.Substring(0, input.text.Length - 1);
                break;
            default:
                input.text += num;
                break;
        }
    }

}
