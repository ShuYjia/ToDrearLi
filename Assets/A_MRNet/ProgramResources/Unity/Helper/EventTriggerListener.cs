using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;


using System;
namespace Unity.Helper{
	/// <summary>
	/// Event trigger listener.
	/// 事件监听代理。
	/// </summary>
	public class EventTriggerListener : EventTrigger{
		public Action<GameObject, BaseEventData> onClick;
		public Action<GameObject, BaseEventData> onDown;
		public Action<GameObject, BaseEventData> onEnter;
		public Action<GameObject, BaseEventData> onExit;
		public Action<GameObject, BaseEventData> onUp;
		public Action<GameObject, BaseEventData> onSelect;
		public Action<GameObject, BaseEventData> onUpdateSelect;
		public Action<GameObject, BaseEventData> onBeginDrag;
		public Action<GameObject, BaseEventData> onScroll;
		public Action<GameObject, BaseEventData> onCancel;
		public Action<GameObject, BaseEventData> onDeselect;
		public Action<GameObject, BaseEventData> onDrag;
		public Action<GameObject, BaseEventData> onDrop;
		public Action<GameObject, BaseEventData> onEndDrag;

		static public EventTriggerListener Get (GameObject go){
			EventTriggerListener listener = go.GetComponent<EventTriggerListener>();
			if (listener == null) listener = go.AddComponent<EventTriggerListener>();
			return listener;
		}
		public override void OnPointerClick(PointerEventData eventData)
		{
            if (onClick != null) {
                //SoundManager.Instance.PlayEffect("Sounds/UI/button_2");
                onClick(gameObject, eventData);
            }
		}
		public override void OnPointerDown (PointerEventData eventData){
			if(onDown != null) onDown(gameObject, eventData);
		}
		public override void OnPointerEnter (PointerEventData eventData){
			if(onEnter != null) onEnter(gameObject, eventData);
		}
		public override void OnPointerExit (PointerEventData eventData){
			if(onExit != null) onExit(gameObject, eventData);
		}
		public override void OnPointerUp (PointerEventData eventData){
			if(onUp != null) onUp(gameObject, eventData);
		}
		public override void OnSelect (BaseEventData eventData){
			if(onSelect != null) onSelect(gameObject, eventData);
		}
		public override void OnUpdateSelected (BaseEventData eventData){
			if(onUpdateSelect != null) onUpdateSelect(gameObject, eventData);
		}
		public override void OnBeginDrag (PointerEventData eventData){
			if(onBeginDrag != null) onBeginDrag(gameObject, eventData);
		}
		public override void OnScroll (PointerEventData eventData){
			if(onScroll != null) onScroll(gameObject, eventData);
		}

		public override void OnCancel (BaseEventData eventData){
			if(onCancel != null) onCancel(gameObject, eventData);
		}

		public override void OnDeselect (BaseEventData eventData){
			if(onDeselect != null) onDeselect(gameObject, eventData);
		}

		public override void OnDrag (PointerEventData eventData){
			if(onDrag != null) onDrag(gameObject, eventData);
		}

		public override void OnDrop (PointerEventData eventData){
			if(onDrop != null) onDrop(gameObject, eventData);
		}

		public override void OnEndDrag (PointerEventData eventData){
			if(onEndDrag != null) onEndDrag(gameObject, eventData);
		}
	}

}