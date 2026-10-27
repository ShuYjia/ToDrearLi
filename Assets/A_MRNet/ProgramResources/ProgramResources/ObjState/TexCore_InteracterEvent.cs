using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Bpf.Diagnostics;

public class TexCore_InteracterEvent : TexCore_InteracterBase
{
    [Header("未完成事件")]
    public UnityEvent OnUnFinishedEvent;

    [Header("进行中事件")]
    public UnityEvent OnUnderWayEvent;

    [Header("触发事件")]
    public UnityEvent OnTriggerEvent;

    [Header("已完成事件")]
    public UnityEvent OnFinishEvent;

    public override void Finish()
    {
        base.Finish();
        OnFinishEvent?.Invoke();
        BpfLog.Info(BpfLogCategory.Input, "tex_core_interacter_event.finish", "完成");
    }

    public override void UnderWay()
    {
        base.UnderWay();
        OnUnderWayEvent?.Invoke();
    }

    public override void OnTrigger()
    {
        base.OnTrigger();
        OnTriggerEvent?.Invoke();
        BpfLog.Info(BpfLogCategory.Input, "tex_core_interacter_event.on_trigger", "触发");
    }

    public override void UnFinished()
    {
        base.UnFinished();
        OnUnFinishedEvent?.Invoke();
        BpfLog.Info(BpfLogCategory.Input, "tex_core_interacter_event.un_finished", "未完成");
    }
    
}
