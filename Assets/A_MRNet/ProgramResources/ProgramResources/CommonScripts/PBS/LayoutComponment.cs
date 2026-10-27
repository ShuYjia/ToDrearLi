/****************************************************
	文件名：		LayoutComponment.cs

	功能：		刷新多级菜单显示
*****************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LayoutComponment : MonoBehaviour
{
	private RectTransform rectTransform;
	public GameObject secondMenu;
	//private Toggle firstToggle;

	private void Awake()
	{
		//firstToggle = GetComponent<Toggle>();
		rectTransform = GetComponent<RectTransform>();
        secondMenu = rectTransform.GetChild(1).gameObject;
    }
    private void Start()
    {
        
        //firstToggle.onValueChanged.AddListener((canOpen)=>OpenSecondMenu(canOpen));
    }

    public void OpenSecondMenu(bool canOpen)
	{
        secondMenu.gameObject.SetActive(canOpen);
		StartCoroutine(UpdateLayout(rectTransform));
    }

	IEnumerator UpdateLayout(RectTransform rect)
	{
        yield return 0;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        
    }
}
