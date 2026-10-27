/****************************************************
	文件名：		FormPrompt.cs

	功能：		表格提示
*****************************************************/
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FormPrompt : MonoBehaviour
{
	public string showText;

    private void Awake()
    {
		switch (TexCore_Const.mode)
		{
			case Mode.练习:
				gameObject.SetActive(true);
				break;
			case Mode.考核:
                gameObject.SetActive(false);
                break;
			default:
				break;
		}
	}

    public void ShowText()
	{
		TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, showText, 1.5f);
    }
}
