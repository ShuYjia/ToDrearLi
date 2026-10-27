using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TexCore_InputModule : BaseInputModule
{
	private GameObject submitObject;

	//-------------------------------------------------
	private static TexCore_InputModule _instance;
	public static TexCore_InputModule instance
	{
		get
		{
			if (_instance == null)
				_instance = GameObject.FindObjectOfType<TexCore_InputModule>();

			return _instance;
		}
	}


	//-------------------------------------------------
	public override bool ShouldActivateModule()
	{
		if (!base.ShouldActivateModule())
			return false;

		return submitObject != null;
	}


	//-------------------------------------------------
	public void HoverBegin(GameObject gameObject)
	{
		PointerEventData pointerEventData = new PointerEventData(eventSystem);
		ExecuteEvents.Execute(gameObject, pointerEventData, ExecuteEvents.pointerEnterHandler);
	}


	//-------------------------------------------------
	public void HoverEnd(GameObject gameObject)
	{
		PointerEventData pointerEventData = new PointerEventData(eventSystem);
		pointerEventData.selectedObject = null;
		ExecuteEvents.Execute(gameObject, pointerEventData, ExecuteEvents.pointerExitHandler);
	}
	public void Click(GameObject gameObject)
	{
		PointerEventData pointerEventData = new PointerEventData(eventSystem);
		pointerEventData.selectedObject = null;
		ExecuteEvents.Execute(gameObject, pointerEventData, ExecuteEvents.pointerClickHandler);
	}


	//-------------------------------------------------
	public void Submit(GameObject gameObject)
	{
		submitObject = gameObject;
	}


	//-------------------------------------------------
	public override void Process()
	{
		if (submitObject)
		{
			BaseEventData data = GetBaseEventData();
			data.selectedObject = submitObject;
			ExecuteEvents.Execute(submitObject, data, ExecuteEvents.submitHandler);

			submitObject = null;
		}
	}
}

