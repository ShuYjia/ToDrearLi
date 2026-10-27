/****************************************************
	文件名：		TriggerNotifier.cs

	功能：		向父物体发送触碰消息
*****************************************************/
using System;
using UnityEngine;

public class TriggerTriggerPoint : TriggerNotifierBase
{
    /// <summary>
    /// 可触发的标签数组，按优先级排列
    /// </summary>
    public string[] validNames;

    public GameObject startObj;

    private void OnTriggerEnter(Collider other)
    {
        // 仅当物体标签有效时，触发进入事件
        if (IsValidName(other.gameObject))
        {
            UpdateStartObj(other.gameObject);
            OnTriggerDetected?.Invoke(this.gameObject, this.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (startObj != null && startObj.name == other.gameObject.name)
        {
            startObj = null;
        }
        OnTriggerRemoved?.Invoke(this.gameObject, this.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        // 仅当物体标签有效时，触发持续事件
        if (IsValidName(other.gameObject))
        {
            UpdateStartObj(other.gameObject);
            if (startObj.name == other.gameObject.name)
            {
                OnTriggerStayDetected?.Invoke(this.gameObject, other.gameObject);
            }
        }
    }

    /// <summary>
    /// 更新 startObj 为当前优先级最高的物体
    /// </summary>
    /// <param name="obj">当前检测的物体</param>
    private void UpdateStartObj(GameObject obj)
    {
        if (startObj == null || GetPriority(obj.name) < GetPriority(startObj.name))
        {
            //Debug.Log(obj.name);
            startObj = obj; // 优先级更高的物体赋值给 startObj
        }
    }

    /// <summary>
    /// 判断物体是否在 validNames 中
    /// </summary>
    /// <param name="obj">需要检测的物体</param>
    /// <returns>是否有效</returns>
    private bool IsValidName(GameObject obj)
    {
        return GetPriority(obj.name) != -1;
    }

    /// <summary>
    /// 获取物体在 validNames 中的优先级
    /// </summary>
    /// <param name="name">物体名称</param>
    /// <returns>优先级（索引值），不存在则返回 -1</returns>
    private int GetPriority(string name)
    {
        for (int i = 0; i < validNames.Length; i++)
        {
            if (validNames[i].Equals(name))
            {
                return i; // 数组索引越小优先级越高
            }
        }
        return -1;
    }

}



