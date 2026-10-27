/****************************************************
	文件名：		TriggerNotifierBase.cs

	功能：		向父物体发送触碰消息
*****************************************************/
using System;
using UnityEngine;

public class TriggerNotifierBase : MonoBehaviour
{
    /// <summary>
    /// 父物体订阅的触发进入事件，用于进入触发区域的通知
    /// </summary>
    public Action<GameObject,GameObject> OnTriggerDetected;

    /// <summary>
    /// 父物体订阅的触发移除事件，用于离开触发区域时的通知
    /// </summary>
    public Action<GameObject,GameObject> OnTriggerRemoved;

    /// <summary>
    /// 父物体订阅的持续触发事件，用于在触发区域内时的持续通知
    /// </summary>
    public Action<GameObject, GameObject> OnTriggerStayDetected;

}



