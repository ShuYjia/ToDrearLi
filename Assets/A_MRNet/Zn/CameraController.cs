/****************************************************
	文件名：		CameraController.cs

	功能：		Nothing
*****************************************************/
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public LayerMask mask;
    public float rotationSpeed = 5.0f; // 控制旋转速度
    public float mouseSensitivity = 0.5f; // 鼠标灵敏度
    public float minYaw = -45f; // 左侧水平旋转角度的限制
    public float maxYaw = 45f;  // 右侧水平旋转角度的限制

    public float minVer = -50f;
    public float maxVer = 50f;
    [Space]

    public float minFieldOfView = 30f; // 视角最小值
    public float maxFieldOfView = 60f; // 视角最大值
    public float minRotationSpeed = 1.0f; // 旋转速度最小值
    public float maxRotationSpeed = 5.0f; // 旋转速度最大值
    public float minMouseSensitivity = 0.1f; // 鼠标灵敏度最小值
    public float maxMouseSensitivity = 0.5f; // 鼠标灵敏度最大值

    private Camera cam;
    private Vector3 lastMousePosition; // 上次鼠标位置
    private bool isRotating = false; // 是否按下右键
    private float pitch = 0.0f; // 垂直角度
    private float yaw = 0.0f;   // 水平角度

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        HandleMouseInput(); // 处理鼠标输入
        HandleScrollInput(); // 处理滚轮输入
        HandleRaycast();
    }

    void HandleMouseInput()
    {
        // 按下右键，开始旋转
        if (Input.GetMouseButtonDown(1))
        {
            lastMousePosition = Input.mousePosition;
            isRotating = true;
        }

        // 松开右键，停止旋转
        if (Input.GetMouseButtonUp(1))
        {
            isRotating = false;
        }

        if (isRotating)
        {
            Vector3 deltaMousePosition = Input.mousePosition - lastMousePosition;

            // 计算水平旋转，限制在 minYaw 和 maxYaw 之间
            float deltaX = deltaMousePosition.x * mouseSensitivity * rotationSpeed * Time.deltaTime;
            yaw = Mathf.Clamp(yaw + deltaX, minYaw, maxYaw);

            // 计算垂直旋转，限制在 -80 到 80 度之间
            float deltaY = deltaMousePosition.y * mouseSensitivity * rotationSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch - deltaY, minVer, maxVer);

            // 更新摄像机旋转
            transform.localEulerAngles = new Vector3(pitch, yaw, 0);

            // 更新鼠标位置
            lastMousePosition = Input.mousePosition;
        }
    }

    // 处理滚轮输入的接口，可在此添加功能
    void HandleScrollInput()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            // 更新摄像机视角，限制在 minFieldOfView 和 maxFieldOfView 范围内
            float targetFieldOfView = cam.fieldOfView - scrollInput * 5;
            cam.fieldOfView = Mathf.Clamp(targetFieldOfView, minFieldOfView, maxFieldOfView);

            // 计算插值比例（0 到 1），用于基于 fieldOfView 调整其他属性
            float lerpFactor = (cam.fieldOfView - minFieldOfView) / (maxFieldOfView - minFieldOfView);

            // 使用 lerpFactor 插值计算旋转速度和鼠标灵敏度
            rotationSpeed = Mathf.Lerp(minRotationSpeed, maxRotationSpeed, lerpFactor);
            mouseSensitivity = Mathf.Lerp(minMouseSensitivity, maxMouseSensitivity, lerpFactor);
        }
    }

    void HandleRaycast()
    {
        // 定义射线，从摄像机的位置沿着前方发射
        Ray ray = cam.ViewportPointToRay(new Vector2(0.5f,0.5f));
        RaycastHit hit;
        // 检测射线是否与任何碰撞体相交
        if (Physics.Raycast(ray, out hit, 1000, mask)) 
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                switch (hit.collider.name)
                {
                    case "镜片":
                    case "定位点":
                        TexCore_BusinessManager.OnClick(hit.collider.gameObject);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}


