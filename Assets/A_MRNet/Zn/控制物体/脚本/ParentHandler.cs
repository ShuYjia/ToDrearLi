using Bpf.Diagnostics;
/****************************************************
    文件名：        ParentHandler.cs
    编写者：        Zn
    功能：          管理子物体触发事件及物体放置
*****************************************************/

using System;
using UnityEngine;

public class ParentHandler : MonoBehaviour
{
    /// <summary>
    /// 所有渲染器，用于控制物体颜色
    /// </summary>
    public MeshRenderer[] meshRenderers;

    /// <summary>
    /// 可放置位置时的有效颜色
    /// </summary>
    public Color validPlacementColor;

    /// <summary>
    /// 不可放置位置时的无效颜色
    /// </summary>
    public Color invalidPlacementColor;

    /// <summary>
    /// 物体的放置点，物体最终的放置位置
    /// </summary>
    public Transform placementPoint;

    /// <summary>
    /// 空的物体，作为放置时的参考位置
    /// </summary>
    public GameObject empty;

    /// <summary>
    /// 实际可视化的物体
    /// </summary>
    public GameObject real;

    /// <summary>
    /// 初次接触的碰撞体
    /// </summary>
    public GameObject _initialCollider;

    /// <summary>
    /// 子物体节点
    /// </summary>
    public Transform child;

    /// <summary>
    /// 传送位置
    /// </summary>
    public Transform pos;

    /// <summary>
    /// 是否跟随射线位置（如果为true，物体会跟随射线的方向移动）
    /// </summary>
    public bool isFollowingRay;

    /// <summary>
    /// 是否继续监测
    /// </summary>
    public bool isMonitor = true;

    public bool isPickup = true;

    public MeshRenderer minMapShow;

    /// <summary>
    /// 子物体的触发事件通知器数组
    /// </summary>
    private TriggerTriggerPoint[] _notifiers;

    /// <summary>
    /// 接触点
    /// </summary>
    private GameObject _contactPoint;

    private void Start()
    {
        InitializeTriggerNotifiers(); // 初始化子物体的触发事件通知器
        SetRendererColor(invalidPlacementColor); // 设置初始颜色为不可放置状态
    }

    /// <summary>
    /// 初始化子物体触发事件监听器，并订阅相关事件
    /// </summary>
    private void InitializeTriggerNotifiers()
    {
        _notifiers = GetComponentsInChildren<TriggerTriggerPoint>(); // 获取所有子物体的触发事件通知器
        foreach (var notifier in _notifiers)
        {
            notifier.OnTriggerDetected += OnChildTriggerEnter;  // 订阅触发进入事件
            notifier.OnTriggerRemoved += OnChildTriggerExit;    // 订阅触发离开事件
            notifier.OnTriggerStayDetected += OnChildTriggerStay; // 订阅持续触发事件
        }
    }

    /// <summary>
    /// 移动物体到指定的位置
    /// </summary>
    /// <param name="position">目标位置</param>
    public void MoveTo(Vector3 position)
    {
        // 根据是否跟随射线判断物体移动方式
        if (isFollowingRay) 
        {
            MoveToRay(position); // 跟随射线移动
        }
        else
        {
            MoveToPlacement(position); // 使用放置点位置移动
        }
    }

    /// <summary>
    /// 跟随射线移动物体
    /// </summary>
    /// <param name="position">目标位置</param>
    private void MoveToRay(Vector3 position)
    {
        empty.transform.position = position; // 直接移动空物体到目标位置
    }

    /// <summary>
    /// 根据初始碰撞体或放置点位置移动物体
    /// </summary>
    /// <param name="position">目标位置</param>
    private void MoveToPlacement(Vector3 position)
    {
        if (!_initialCollider.CompareTag("可放置")) // 如果初始碰撞体标签不是“可放置”
        {
            //Debug.Log(ObjectPlacer.Instance.IsHit);
            // 如果未命中其他物体，使用放置点位置
            if (!ObjectPlacer.Instance.IsHit)
            {
                placementPoint.position = position;
            }
            else if(empty.transform.eulerAngles!= _initialCollider.transform.eulerAngles)
            {
                empty.transform.eulerAngles = _initialCollider.transform.eulerAngles;
            }
        }
        else
        {
            placementPoint.position = position;
            //empty.transform.localPosition = Vector3.zero;
        }
    }

    /// <summary>
    /// 处理子物体进入触发区域时的事件
    /// </summary>
    /// <param name="child">触发进入的子物体</param>
    private void OnChildTriggerEnter(GameObject child,GameObject obj)
    {
        // 可以在此添加一些具体操作，比如调试输出
        BpfLog.Info(BpfLogCategory.XR, "parent_handler.on_child_trigger_enter", child.name + " 进入触发区域");
    }

    /// <summary>
    /// 处理子物体离开触发区域时的事件
    /// </summary>
    /// <param name="child">触发离开的子物体</param>
    private void OnChildTriggerExit(GameObject child, GameObject obj)
    {
        if (!isMonitor) return;

        // 调试输出
        BpfLog.Info(BpfLogCategory.XR, "parent_handler.on_child_trigger_exit", child.name + " 离开触发区域");

        // 重置相关属性和状态
        ResetPlacementState();
    }

    /// <summary>
    /// 重置放置状态和相关属性
    /// </summary>
    private void ResetPlacementState()
    {
        isFollowingRay = true;  // 重置为跟随射线状态
        SetRendererColor(invalidPlacementColor); // 设置为无效放置颜色
        empty.transform.SetParent(transform); // 设置空物体的父物体为当前物体
        placementPoint.SetParent(empty.transform); // 设置放置点的父物体为空物体
        placementPoint.localPosition = Vector3.zero; // 重置放置点位置

        _contactPoint = null; // 清空接触点
        _initialCollider = null; // 清空初始碰撞体
    }

    /// <summary>
    /// 处理子物体持续触发事件（即子物体一直在触发区域内）
    /// </summary>
    /// <param name="child">触发的子物体</param>
    /// <param name="other">另一个接触的物体</param>
    private void OnChildTriggerStay(GameObject child, GameObject other)
    {
        if (!isMonitor) return;

        // 调试输出
        //Debug.Log(child.name + " 持续触发 " + other.name + "\t" + transform.name);

        // 记录接触点和初始碰撞体
        RecordContactDetails(child, other);

        // 如果物体被放置位置命中，则更新状态
        if (ObjectPlacer.Instance.IsHit)
        {
            HandleValidPlacement(other); // 处理有效放置
        }
        else
        {
            ResetPlacementState();
        }
    }

    /// <summary>
    /// 记录接触点和初始碰撞体
    /// </summary>
    /// <param name="child">触发的子物体</param>
    /// <param name="other">接触的其他物体</param>
    private void RecordContactDetails(GameObject child, GameObject other)
    {
        _contactPoint = child; // 如果接触点为空，记录当前子物体
        _initialCollider = other; // 如果初始碰撞体为空，记录接触的物体
    }

    /// <summary>
    /// 处理有效放置状态
    /// </summary>
    /// <param name="other">接触的物体</param>
    private void HandleValidPlacement(GameObject other)
    {
        SetRendererColor(validPlacementColor); // 设置为有效放置颜色
        placementPoint.position = _contactPoint.transform.position; // 设置放置点位置
        placementPoint.SetParent(transform); // 设置放置点的父物体
        empty.transform.SetParent(placementPoint); // 设置空物体的父物体
        empty.transform.localPosition = new Vector3(0, empty.transform.localPosition.y, 0);
        //Debug.Log(_initialCollider);
        if (!_initialCollider.CompareTag("可放置")) // 如果初始碰撞体的标签不是“可放置”
        {
            
            placementPoint.position = other.transform.position; // 使用其他物体的位置
        }

        isFollowingRay = false; // 取消跟随射线
    }

    /// <summary>
    /// 生成新的对象，并更新物体状态
    /// </summary>
    public void CreateObject()
    {
        // 如果不再跟随射线，执行物体创建操作
        if (isFollowingRay) return;

        // 将当前物体添加到管理器中
        ParentHandler _initialParentHandler = _initialCollider.GetComponentInParent<ParentHandler>();
        if (_initialParentHandler)
        {
            CreateObjectInParent(_initialParentHandler); // 创建对象并设置父物体
        }
        else
        {
            CreateObjectInCurrent(); // 在当前物体上创建对象
        }

        // 停止监测
        isMonitor = false;

        ObjectUIManager.Instance.AddObject(transform.gameObject); // 将当前物体添加到管理器中
        RouteByName(transform.name);
        // 禁用初始碰撞体
        DisableInitialCollider();
    }

    public void RouteByName(string name)
    {
        //生成东西进入业务逻辑
        switch (name)
        {
            case "固定支架":
                break;
            case "全站仪":
            case "全站仪整体":
                if (_initialCollider.transform.parent.parent == null) return;
                TexCore_BusinessManager.OnClick(new GameObject($"{this._initialCollider.transform.parent.parent.name}_全站仪"));
                break;
            default:
                if (_initialCollider.transform.parent.parent == null) return; 
                TexCore_BusinessManager.OnClick(new GameObject($"{this._initialCollider.transform.parent.parent.name}_{this.name}"));
                break;
        }
        
    }

    /// <summary>
    /// 在父物体中创建对象
    /// </summary>
    /// <param name="parentHandler">初始父物体的ParentHandler</param>
    private void CreateObjectInParent(ParentHandler parentHandler)
    {
        transform.SetParent(parentHandler.child); // 设置当前物体为父物体的子物体
        real.transform.position = empty.transform.position; // 设置实际物体位置
        real.SetActive(true); // 激活实际物体
        empty.SetActive(false); // 隐藏空物体
        child.SetParent(transform); // 设置子物体为当前物体的子物体
        ChildToggleState(); // 切换子物体的显示状态
    }

    /// <summary>
    /// 在当前物体上创建对象
    /// </summary>
    private void CreateObjectInCurrent()
    {
        real.SetActive(true); // 激活实际物体
        real.transform.position = empty.transform.position; // 设置实际物体位置为空物体位置
        real.transform.eulerAngles = empty.transform.eulerAngles; // 设置实际物体位置为空物体位置
        empty.SetActive(false); // 隐藏空物体
        child.SetParent(transform); // 设置子物体为当前物体的子物体
        ChildToggleState(); // 切换子物体的显示状态
    }

    /// <summary>
    /// 禁用初始碰撞体
    /// </summary>
    private void DisableInitialCollider()
    {
        if (_initialCollider != null && !_initialCollider.CompareTag("可放置"))
        {
            _initialCollider.SetActive(false); // 禁用初始碰撞体
        }
    }

    /// <summary>
    /// 设置物体渲染器的颜色
    /// </summary>
    /// <param name="color">需要设置的颜色</param>
    public void SetRendererColor(Color color)
    {
        foreach (MeshRenderer renderer in meshRenderers)
        {
            // 遍历所有材质，修改它们的颜色
            foreach (var material in renderer.materials)
            {
                material.color = color; // 设置材质颜色
            }
        }
    }

    /// <summary>
    /// 切换物体的显示状态
    /// </summary>
    public void ToggleState()
    {
        transform.SetParent(ObjectUIManager.Instance.objectRoot);
        // 切换实际物体和空物体的显示状态
        real.SetActive(!real.activeInHierarchy); // 切换实际物体的显示状态
        empty.SetActive(!empty.activeInHierarchy); // 切换空物体的显示状态

        isMonitor = true; // 恢复监测状态

        // 重新激活初始碰撞体
        if (_initialCollider != null)
        {
            _initialCollider.SetActive(true); // 激活初始碰撞体
        }

        if (child != null)
        {
            child.SetParent(empty.transform); // 设置子物体的父物体为空物体
        }

        ChildToggleState(); // 切换子物体的显示状态
    }

    /// <summary>
    /// 切换子物体的显示状态
    /// </summary>
    public void ChildToggleState()
    {
        foreach (ParentHandler item in child.GetComponentsInChildren<ParentHandler>())
        {
            // 切换每个子物体的显示状态
            item.real.SetActive(!item.real.activeInHierarchy);
            item.empty.SetActive(!item.empty.activeInHierarchy);
        }
    }
}
