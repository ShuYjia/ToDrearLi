using Bpf.Diagnostics;
/****************************************************
	文件名：		TriggerLineDetection.cs

	功能：		向父物体发送触碰消息
*****************************************************/
using System;
using UnityEngine;

public class TriggerLineDetection : TriggerNotifierBase
{

    private void OnTriggerEnter(Collider other)
    {
        BpfLog.Info(BpfLogCategory.TexCore, "trigger_line_detection.on_trigger_enter", "11");
        OnTriggerDetected?.Invoke(this.gameObject,this.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        OnTriggerRemoved?.Invoke(this.gameObject,this.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        OnTriggerStayDetected?.Invoke(this.gameObject, other.gameObject);
    }
}



