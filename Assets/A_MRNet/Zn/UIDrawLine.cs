/****************************************************
	文件名：		NewBehaviourScript.cs
	功能：		Nothing
*****************************************************/
using System.Collections.Generic;
using UnityEngine;

public class UIDrawLine : MonoBehaviour
{
    // 用于绘制线条的预制体
    public LineRenderer linePrefab;

    // 画布对象，用于确定线条的绘制范围
    public Canvas canvas;

    // 背景的 RectTransform，用于捕捉鼠标位置
    public RectTransform bg;

    // 主摄像机，用于将屏幕坐标转换为世界坐标
    public Camera camera;

    // 画布的缩放比例，用于适配不同分辨率
    private float canvasScaler;

    // 存储所有线条的 LineRenderer 组件
    private List<LineRenderer> lineRenderers = new List<LineRenderer>();

    // 存储所有线条的点集合，每个线条的点是一个 List<Vector3>
    private List<List<Vector3>> allLinePositions = new List<List<Vector3>>();

    // 当前正在绘制的线条对应的 LineRenderer
    private LineRenderer currentLineRenderer;

    // 当前正在绘制的线条的点集合
    private List<Vector3> currentLinePositions;

    // 公开所有线条的点集合，用于外部访问
    public List<List<Vector3>> AllLinePositions => allLinePositions;

    private void Start()
    {
        // 初始化画布缩放比例并添加第一条线条
        Init();
    }

    /// <summary>
    /// 初始化方法
    /// </summary>
    private void Init()
    {
        // 获取画布的缩放比例
        canvasScaler = canvas.transform.localScale.x;

        // 添加第一条线条
        AddNewLine();
    }

    /// <summary>
    /// 添加一条新的线条
    /// </summary>
    public void AddNewLine()
    {
        // 实例化一个新的 LineRenderer 并设置其属性
        currentLineRenderer = Instantiate(linePrefab, transform);
        currentLineRenderer.startWidth = 0.04f; // 设置起始宽度
        currentLineRenderer.endWidth = 0.04f;   // 设置结束宽度
        currentLineRenderer.gameObject.SetActive(true); // 激活线条对象

        // 将新的 LineRenderer 添加到线条列表
        lineRenderers.Add(currentLineRenderer);

        // 初始化当前线条的点集合并添加到所有点集合
        currentLinePositions = new List<Vector3>();
        allLinePositions.Add(currentLinePositions);
    }

    /// <summary>
    /// 绘制线条，根据鼠标位置添加点
    /// </summary>
    public void DrawLine()
    {
        // 如果当前没有正在绘制的线条，则直接返回
        if (currentLineRenderer == null) return;

        // 定义一个变量存储转换后的世界坐标
        Vector3 worldPoint;

        // 将鼠标的屏幕坐标转换为世界坐标
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(bg, Input.mousePosition, camera, out worldPoint))
        {
            // 调整世界坐标的 Z 值，使其位于画布平面内
            worldPoint = new Vector3(worldPoint.x, worldPoint.y, canvas.planeDistance - 0.1f);

            // 将新点添加到当前线条的点集合中
            currentLinePositions.Add(worldPoint);

            // 更新当前线条的点数和位置
            currentLineRenderer.positionCount = currentLinePositions.Count;
            currentLineRenderer.SetPositions(currentLinePositions.ToArray());
        }
    }

    /// <summary>
    /// 清除所有线条并重新添加一条空的线条
    /// </summary>
    public void ClearAllLines()
    {
        // 遍历并销毁所有现有的线条对象
        foreach (var line in lineRenderers)
        {
            Destroy(line.gameObject);
        }

        // 清空线条列表和所有点集合
        lineRenderers.Clear();
        allLinePositions.Clear();

        // 添加一条新的线条
        AddNewLine();
    }

    /// <summary>
    /// 撤销当前线条的最后一个点
    /// </summary>
    public void Undo()
    {
        // 如果当前线条的点集合不为空，则删除最后一个点
        if (currentLinePositions != null && currentLinePositions.Count > 0)
        {
            currentLinePositions.RemoveAt(currentLinePositions.Count - 1);

            // 更新当前线条的点数和位置
            currentLineRenderer.positionCount = currentLinePositions.Count;
            currentLineRenderer.SetPositions(currentLinePositions.ToArray());
        }
    }
}
