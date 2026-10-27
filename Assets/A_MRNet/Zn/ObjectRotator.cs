using UnityEngine;

public class ObjectRotator : MonoBehaviour
{
    public float rotationSpeed = 100f; // 旋转速度
    public float zoomSpeed = 2f;       // 缩放速度
    public float minZoom = 0.5f;        // 最小缩放比例
    public float maxZoom = 2f;       // 最大缩放比例

    private bool isDragging = false;   // 鼠标是否在拖拽
    private float currentZoom = 1f;    // 当前缩放值，初始值由你设置

    void Update()
    {
        // 检测鼠标按下开始拖拽
        if (Input.GetMouseButtonDown(1))
        {
            isDragging = true;
        }

        // 检测鼠标松开停止拖拽
        if (Input.GetMouseButtonUp(1))
        {
            isDragging = false;
        }

        // 如果正在拖拽，控制物体旋转
        if (isDragging)
        {
            RotateObject();
        }

        // 控制缩放
        ZoomObject();
    }

    private void RotateObject()
    {
        // 获取鼠标的移动量
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // 根据鼠标移动量计算旋转
        Vector3 rotation = new Vector3(mouseY, -mouseX, 0) * rotationSpeed * Time.deltaTime;

        // 应用旋转到物体
        transform.Rotate(rotation, Space.World);
    }

    private void ZoomObject()
    {
        // 获取鼠标滚轮的输入值
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        // 根据滚轮输入调整当前缩放值
        currentZoom -= -scroll * zoomSpeed;

        // 限制缩放值范围
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

        // 应用缩放值
        transform.localScale = Vector3.one * currentZoom;
    }
}
