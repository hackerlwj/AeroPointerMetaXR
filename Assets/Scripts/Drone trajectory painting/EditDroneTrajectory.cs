using System.Collections.Generic;
using UnityEngine;

public class EditDroneTrajectory : MonoBehaviour
{
    [Header("LineRenderer设置")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private GameObject pointPrefab; // 小球预制体
    [SerializeField] private int pointInterval = 10; // 每隔多少个点生成一个小球

    private List<GameObject> controlPoints = new List<GameObject>(); // 存储生成的小球
    private List<Vector3> linePoints = new List<Vector3>(); // 存储LineRenderer的点
    private bool pointsGenerated = false; // 标记是否已经生成小球

    // 点击按钮时调用，生成或清除小球
    public void ToggleControlPoints()
    {
        if (pointsGenerated)
        {
            ClearControlPoints();
        }
        else
        {
            GenerateControlPoints();
        }
    }

    // 生成控制点小球
    private void GenerateControlPoints()
    {
        if (lineRenderer == null || pointPrefab == null) return;

        // 获取LineRenderer的所有点
        linePoints.Clear();
        for (int i = 0; i < lineRenderer.positionCount; i++)
        {
            linePoints.Add(lineRenderer.GetPosition(i));
        }

        // 每隔一定数量的点生成小球
        for (int i = 0; i < linePoints.Count; i += pointInterval)
        {
            Vector3 position = linePoints[i];
            GameObject controlPoint = Instantiate(pointPrefab, position, Quaternion.identity);
            controlPoint.name = $"ControlPoint_{i}";
            controlPoints.Add(controlPoint);

            // 添加监听器，实时更新轨迹
            var draggable = controlPoint.AddComponent<DraggablePoint>();
            draggable.Initialize(i, UpdateLineRenderer);
        }

        pointsGenerated = true;
    }

    // 清除所有控制点小球
    public void ClearControlPoints()
    {
        foreach (var point in controlPoints)
        {
            Destroy(point);
        }
        controlPoints.Clear();
        pointsGenerated = false;
    }

    // 更新LineRenderer的轨迹点
    private void UpdateLineRenderer(int index, Vector3 newPosition)
    {
        if (index >= 0 && index < linePoints.Count)
        {
            // 更新当前点的位置
            linePoints[index] = newPosition;

            // 调整前后 pointInterval 个点的位置
            for (int i = 1; i <= pointInterval; i++)
            {
                float weight = 1f - (float)i / pointInterval; // 权重，距离越远影响越小

                // 调整前面的点
                if (index - i >= 0)
                {
                    linePoints[index - i] = Vector3.Lerp(linePoints[index - i], newPosition, weight);
                }

                // 调整后面的点
                if (index + i < linePoints.Count)
                {
                    linePoints[index + i] = Vector3.Lerp(linePoints[index + i], newPosition, weight);
                }
            }

            // 更新 LineRenderer 的点
            lineRenderer.positionCount = linePoints.Count;
            for (int i = 0; i < linePoints.Count; i++)
            {
                lineRenderer.SetPosition(i, linePoints[i]);
            }
        }
    }
}