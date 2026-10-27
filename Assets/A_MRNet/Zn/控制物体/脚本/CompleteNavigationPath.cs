using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CompleteNavigationPath : MonoBehaviour
{
    public static CompleteNavigationPath self;

    public Transform target; // 目标位置
    public NavMeshAgent agent; // 角色的 NavMeshAgent
    public LineRenderer lineRenderer; // 用于绘制路径的 LineRenderer
    public int interpolationSteps = 10; // 插值步长，用于增加路径的平滑度

    private void Awake()
    {
        self = this;
    }

    void Start()
    {
        if (!lineRenderer)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.green;
            lineRenderer.endColor = Color.green;
        }
    }

    void Update()
    {
        if (agent == null || target == null)
        {
            return;
        }
        // 计算路径
        NavMeshPath path = new NavMeshPath();
        if (agent.CalculatePath(target.position, path))
        {
            DrawPath(path);
            // 处理路径点（如显示路径线）
        }
    }

    /// <summary>
    /// 设置点位
    /// </summary>
    public void SetTarget(Transform tr)
    {
        target = tr;
    }

    void DrawPath(NavMeshPath path)
    {
        // 如果路径没有角点，则不绘制
        if (path.corners.Length < 2)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        // 计算更多的插值点
        List<Vector3> smoothedCorners = new List<Vector3>();

        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            smoothedCorners.Add(path.corners[i]);

            // 在每两个路径点之间插值
            Vector3 start = path.corners[i];
            Vector3 end = path.corners[i + 1];
            for (int j = 1; j <= interpolationSteps; j++)
            {
                float t = j / (float)(interpolationSteps + 1);
                Vector3 interpolatedPoint = Vector3.Lerp(start, end, t);
                smoothedCorners.Add(interpolatedPoint);
            }
        }

        // 添加路径的最后一个点
        smoothedCorners.Add(path.corners[path.corners.Length - 1]);

        // 设置 LineRenderer 的路径点
        Vector3[] adjustedCorners = new Vector3[smoothedCorners.Count];
        for (int i = 0; i < smoothedCorners.Count; i++)
        {
            adjustedCorners[i] = smoothedCorners[i] + Vector3.up * 0.3f; // 增加 0.1f 高度偏移
        }

        lineRenderer.positionCount = adjustedCorners.Length;
        lineRenderer.SetPositions(adjustedCorners);
    }
}
