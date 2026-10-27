using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
using UnityEngine.Events;
using Bpf.Diagnostics;

[Serializable]
public class InteracterFunction
{

    [Header("触发事件")]
    public UnityEvent OnTriggerEvent;

    [Header("是否控制模型显示")]
    public bool isShow;
    public bool showActive;

    [Header("是否高亮提示")]
    public bool isSetState;
    public SelectState state;

    [Header("是否控制碰撞体")]
    public bool isSetCollider;
    public bool colliderActive;

    [Header("是否回到初始位置")]
    public bool isResetPos;

    [Header("是否移动到指定位置")]
    public bool isTarget;
    public float tweenTime;
    public Transform transformTarget;




    //public bool isMove;
    //public Vector3 pos;

    //public bool isRotate;
    //public Vector3 rot;
    [Header("是否控播放指定动画")]
    public bool isPlayAnimation;
    public string clipName;
}

public class TexCore_InteracterFunction : TexCore_InteracterBase
{
    [Header("未完成状态设置方法")]
    public InteracterFunction OnUnFinishedFunc;
    [Header("进行中状态设置方法")]
    public InteracterFunction OnUnderWayFunc;
    [Header("触发状态设置方法")]
    public InteracterFunction OnTriggerFunc;
    [Header("已经完成状态设置方法")]
    public InteracterFunction OnFinishFunc;


    protected MeshRenderer[] meshRenderers;
    protected SkinnedMeshRenderer[] skinnedMeshRenderers;
    protected Collider[] colliders;
    protected Animator animator;
    protected TexCore_SelectBase selectBase;

    protected Vector3 startPos;
    protected Vector3 startRot;

    private void Start()
    {
        meshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();
        skinnedMeshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
        colliders = gameObject.GetComponentsInChildren<Collider>();
        animator = gameObject.GetComponent<Animator>();
        startPos = transform.position;
        startRot = transform.eulerAngles;
        selectBase = gameObject.GetComponent<TexCore_SelectBase>();
    }

    public void TriggerFunction(InteracterFunction functions)
    {
            InteracterFunction item = functions;
            item.OnTriggerEvent?.Invoke();
            if (item.isShow)
            {
                foreach (var meshRenderer in meshRenderers)
                    meshRenderer.enabled = item.showActive;

                foreach (var meshRenderer in skinnedMeshRenderers)
                    meshRenderer.enabled = item.showActive;
            }
            if (item.isSetState)
            {
                selectBase.SetState(item.state);
            }
             if (item.isSetCollider)
            {
                foreach (var collider in colliders)
                    collider.enabled = item.colliderActive;
            }
            if (item.isTarget)
            {
                ObjMove(item.transformTarget, item.tweenTime);
            }
            if (item.isResetPos)
            {
                transform.position = startPos;
                transform.eulerAngles = startRot;
            }
            if (item.isPlayAnimation)
            {
                animator.Play(item.clipName);
            }

    }

    public override void UnFinished()
    {
        base.UnFinished();
        TriggerFunction(OnUnFinishedFunc);
    }


    public override void UnderWay()
    {
        base.UnderWay();
        TriggerFunction(OnUnderWayFunc);
    }

    public override void OnTrigger()
    {
        base.OnTrigger();
        TriggerFunction(OnTriggerFunc);
    }

    public override void Finish()
    {
        base.Finish();
        TriggerFunction(OnFinishFunc);
    }


    public override void OnDisorderTrigger(GameObject obj) {
        base.OnDisorderTrigger(obj);
        BpfLog.Error(BpfLogCategory.Input, "tex_core_interacter_function.on_disorder_trigger", null, null, BpfLogField.Of("name", obj.name));
        if (obj ==this.gameObject) {
            SetState(FinishState.触发);
        }
    }



    public void ObjMove(Transform transformTarget)
    {
        transform.DOMove(transformTarget.position, 1);
        transform.DORotate(transformTarget.eulerAngles, 1);
    }
    private Tween currentMoveTween;
    private Tween currentRotateTween;

    public void ObjMove(Transform transformTarget, float time = 1)
    {
        // 如果当前有移动动画，先结束之前的动画
        if (currentMoveTween != null && currentMoveTween.IsActive())
        {
            currentMoveTween.Kill();
        }

        // 如果当前有旋转动画，先结束之前的动画
        if (currentRotateTween != null && currentRotateTween.IsActive())
        {
            currentRotateTween.Kill();
        }

        if (transformTarget != null) 
        {
            // 播放新的移动动画
            currentMoveTween = transform.DOMove(transformTarget.position, time);

            // 播放新的旋转动画
            currentRotateTween = transform.DORotate(transformTarget.eulerAngles, time);

        }

    }
}
