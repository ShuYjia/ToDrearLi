using UnityEngine;
using Bpf.Diagnostics;

/// <summary>
/// 挂载在摄像机上的物体拖拽脚本
/// 支持Layer为Tool的物体实时跟随鼠标移动，左键按住即可，滚轮控制前后
/// 新增：检测BoxCollider状态，失效后停止拖拽
/// </summary>
[RequireComponent(typeof(Camera))]
public class ToolObjectDragger : MonoBehaviour
{

    public static ToolObjectDragger self;

    [Header("拖拽设置")]
    [Tooltip("要拖拽的物体所在Layer")]
    public LayerMask toolLayer;
    [Tooltip("鼠标跟随灵敏度（值越大越灵敏，建议0.8-1.0）")]
    [Range(0.1f, 1.0f)] public float followSensitivity = 1.0f;
    [Tooltip("滚轮前后移动灵敏度")]
    public float scrollSensitivity = 0.2f;

    // 当前选中的物体
    public GameObject selectedObject;
    // 摄像机组件引用
    private Camera mainCamera;
    // 记录物体与摄像机的初始深度（z轴距离）
    private float objectDepth;
    // 缓存选中物体的BoxCollider组件
    private BoxCollider targetBoxCollider;

    void Awake()
    {
        self = this;
        mainCamera = GetComponent<Camera>();
        // 自动获取Tool层的LayerMask（也可手动在Inspector设置）
        toolLayer = LayerMask.GetMask("Tool");
    }

    void Update()
    {
        // 检测选中/取消选中物体
        CheckObjectSelection();

        // 如果有选中的物体，先检查Collider状态，再执行跟随逻辑
        if (selectedObject != null)
        {
            // 核心新增：检查Collider是否有效，无效则取消选中
            if (!IsColliderValid())
            {
                selectedObject = null;
                targetBoxCollider = null;
                return;
            }

            FollowMouse();
            HandleScrollMovement();
        }
    }

    /// <summary>
    /// 检测鼠标选中/取消选中Tool层物体
    /// 新增：缓存目标物体的BoxCollider
    /// </summary>
    void CheckObjectSelection()
    {
        // 鼠标左键按下时，检测并选中鼠标指向的Tool物体
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, toolLayer))
            {
                selectedObject = hit.collider.gameObject;
                // 缓存BoxCollider组件（优先取命中的collider，兼容子物体Collider）
                targetBoxCollider = hit.collider as BoxCollider ?? selectedObject.GetComponent<BoxCollider>();
                // 记录物体与摄像机的初始深度（保持z轴距离不变）
                objectDepth = mainCamera.WorldToScreenPoint(selectedObject.transform.position).z;
                ToolsManager.self.t = selectedObject;
            }
        }

        // 鼠标左键释放时，取消选中
        if (Input.GetMouseButtonUp(0))
        {
            selectedObject = null;
            targetBoxCollider = null;
            ToolsManager.self.t = null;
        }
    }

    /// <summary>
    /// 新增：检查BoxCollider是否有效（存在且启用）
    /// </summary>
    /// <returns>Collider有效返回true，无效返回false</returns>
    private bool IsColliderValid()
    {
        // 情况1：Collider组件不存在 → 无效
        if (targetBoxCollider == null)
        {
            BpfLog.Warning(BpfLogCategory.Input, "tool_object_dragger.is_collider_valid", "选中的物体 " + selectedObject.name + " 没有挂载BoxCollider组件！");
            return false;
        }

        // 情况2：Collider组件存在但已禁用 → 无效
        if (!targetBoxCollider.enabled)
        {
            BpfLog.Info(BpfLogCategory.Input, "tool_object_dragger.is_collider_valid", "物体 " + selectedObject.name + " 的BoxCollider已禁用，停止拖拽");
            return false;
        }

        // 情况3：物体被销毁 → 无效
        if (selectedObject == null)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 让物体实时跟随鼠标移动
    /// </summary>
    void FollowMouse()
    {
        // 将鼠标屏幕坐标转换为世界坐标（保持物体的初始深度不变）
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, objectDepth)
        );

        // 平滑跟随鼠标（灵敏度控制跟随的即时性，1.0为完全实时）
        selectedObject.transform.position = Vector3.Lerp(
            selectedObject.transform.position,
            new Vector3(mouseWorldPos.x, mouseWorldPos.y, selectedObject.transform.position.z),
            followSensitivity
        );
    }

    /// <summary>
    /// 处理鼠标滚轮的前后移动
    /// </summary>
    void HandleScrollMovement()
    {
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        if (scrollDelta != 0 && selectedObject != null)
        {
            // 前后移动（基于世界空间的z轴，也可改用摄像机前方向）
            Vector3 forwardMove = new Vector3(0, 0, scrollDelta * scrollSensitivity * 100);
            selectedObject.transform.Translate(forwardMove, Space.World);

            // 更新深度值（保证后续鼠标跟随的z轴正确）
            objectDepth = mainCamera.WorldToScreenPoint(selectedObject.transform.position).z;
        }
    }


}