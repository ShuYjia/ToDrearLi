using Bpf.Diagnostics;
/****************************************************
	文件名：		CameraMove.cs

	功能：		Nothing
*****************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMove : MonoBehaviour
{
    // 摄像机移动速度
    public float moveSpeed = 10f;
    // 摄像机上下移动速度（Q/E）
    public float verticalMoveSpeed = 5f;

    public bool isMove = true;
    public bool isRot = true;

    // 摄像机的垂直移动（前后方向）
    private Vector3 moveDirection;

    void Update()
    {
        if (isMove)
        {
            // 处理摄像机移动
            HandleMovement();
            // 处理摄像机上下移动（Q/E）
            HandleVerticalMovement();
        }
        
        if(isRot)
        {
            // 处理摄像机旋转
            HandleRotation();
        }
    }

    public float rotationSpeed = 5.0f; // 控制旋转速度的变量，数值越大旋转越快
    public float mouseSensitivity = 0.5f; // 鼠标灵敏度变量，调整鼠标移动时的反应速度
    private Vector3 lastMousePosition; // 记录上一次的鼠标位置，用于计算鼠标移动的差值
    private bool isRotating = false;   // 判断右键是否按下，控制是否进入旋转模式
    public float pitch = 0.0f;        // 记录当前摄像机的俯仰角度，用于垂直旋转时的限制

    void HandleRotation()
    {
        // 当右键按下时，开始记录鼠标位置并进入旋转模式
        if (Input.GetMouseButtonDown(1))
        {
            lastMousePosition = Input.mousePosition; // 记录当前的鼠标初始位置
            pitch = transform.localEulerAngles.x;
            isRotating = true; // 设置为旋转状态
        }

        // 当右键松开时，停止旋转
        if (Input.GetMouseButtonUp(1))
        {
            isRotating = false; // 结束旋转状态
        }

        // 如果正在旋转
        if (isRotating)
        {
            // 计算鼠标自上次记录以来的移动量（差值）
            Vector3 deltaMousePosition = Input.mousePosition - lastMousePosition;

            // 根据鼠标水平移动量，计算水平方向的旋转角度
            float deltaX = deltaMousePosition.x * mouseSensitivity * rotationSpeed * Time.deltaTime;

            // 绕着世界空间的Y轴（竖直方向）进行水平旋转
            transform.Rotate(Vector3.up, deltaX, Space.World);

            // 根据鼠标垂直移动量，计算垂直方向的旋转角度
            float deltaY = deltaMousePosition.y * mouseSensitivity * rotationSpeed * Time.deltaTime;

            // 更新俯仰角度，减去鼠标垂直移动对应的角度
            pitch -= deltaY;

            // 使用Mathf.Clamp来限制俯仰角度，确保其在 -80 到 80 度之间
            pitch = Mathf.Clamp(pitch, -80f, 80f);

            BpfLog.Info(BpfLogCategory.XR, "camera_move.handle_rotation", deltaY + "，" + pitch);
            // 将俯仰角度应用到摄像机的局部旋转，保持Z轴方向不变
            transform.localEulerAngles = new Vector3(pitch, transform.localEulerAngles.y, 0);

            // 更新鼠标位置，准备下次旋转使用
            lastMousePosition = Input.mousePosition;
        }
    }

    void HandleMovement()
    {
        // 获取输入的移动方向（WASD 控制）
        float horizontal = Input.GetAxis("Horizontal"); // A/D 左右
        float vertical = Input.GetAxis("Vertical");     // W/S 前后

        // 使用摄像机的局部坐标系进行移动
        moveDirection = transform.forward * vertical + transform.right * horizontal;

        // 移动摄像机
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }

    void HandleVerticalMovement()
    {
        // 使用 Q 和 E 键来上下移动
        if (Input.GetKey(KeyCode.Q))
        {
            transform.position += Vector3.down * verticalMoveSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.E))
        {
            transform.position += Vector3.up * verticalMoveSpeed * Time.deltaTime;
        }
    }
}



