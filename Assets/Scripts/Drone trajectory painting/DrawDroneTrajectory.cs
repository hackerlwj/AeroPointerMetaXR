using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // 添加对List的支持

public class DrawDroneTragectory : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform targetObject;
    [SerializeField] private Transform headTransform;
    [SerializeField] private float startWidth = 0.05f;
    [SerializeField] private float endWidth = 0.05f;
    [SerializeField] private float pointDistanceThreshold = 0.01f;
    [SerializeField] private int maxPoints = 10000;

    private bool isDrawing = false;

    private DroneFlyController droneFlyController; // 引用 DroneFlyController

    private List<Vector3> rawPositions = new List<Vector3>(); // 原始点列表

    private void Start()
    {
        if (lineRenderer == null)
        {
            //lineRenderer.startColor = Color.white;
            //lineRenderer.endColor = Color.white;
            lineRenderer.startWidth = startWidth;
            lineRenderer.endWidth = endWidth;
        }

        if (targetObject == null)
        {
            Debug.LogError("Target object not assigned!");
            enabled = false;
            return;
        }

        lineRenderer.positionCount = 0;
        // 获取 DroneFlyController
        droneFlyController = FindObjectOfType<DroneFlyController>();
        if (droneFlyController == null)
        {
            Debug.LogError("DroneFlyController not found in the scene!");
        }
    }

    //重置画球位置
    public void ResetPosition()
    {
        targetObject.rotation = Quaternion.identity;// 重置旋转
        targetObject.position = headTransform.position + headTransform.forward * 0.4f;// 计算新位置：头显前方0.4米处
        ClearLine();// 清空轨迹
    }
    public void StartEndDrawing()
    {
        if (isDrawing)
        {
            isDrawing = false;
            // 结束绘制时整体平滑
            SmoothTrajectory();
        }
        else if (!isDrawing)
        {
            isDrawing = true;
            rawPositions.Clear();
            lineRenderer.positionCount = 0;
            AddPoint();
        }
    }

    public void ClearLine()
    {
        lineRenderer.positionCount = 0;
        isDrawing = false;

        // 通知 DroneFlyController 清空轨迹
        if (droneFlyController != null)
        {
            droneFlyController.ResetPath();
        }

        // 通知 EditDroneTrajectory 清空所有小球
        EditDroneTrajectory editDroneTrajectory = FindObjectOfType<EditDroneTrajectory>();
        if (editDroneTrajectory != null)
        {
            editDroneTrajectory.ClearControlPoints();
        }
    }

    private void Update()
    {
        if (!isDrawing || targetObject == null) return;
        if (lineRenderer.positionCount == 0)
        {
            AddPoint();
        }
        else if (Vector3.Distance(lineRenderer.GetPosition(lineRenderer.positionCount - 1), targetObject.position) > pointDistanceThreshold)
        {
            AddPoint();
        }
    }

    private void AddPoint()
    {
        if (rawPositions.Count >= maxPoints)
        {
            rawPositions.RemoveAt(0);
        }
        rawPositions.Add(targetObject.position);

        // 只更新LineRenderer，不做平滑
        lineRenderer.positionCount = rawPositions.Count;
        for (int i = 0; i < rawPositions.Count; i++)
        {
            lineRenderer.SetPosition(i, rawPositions[i]);
        }
    }

    // 替换原有的平滑处理为整体Catmull-Rom插值
    private void SmoothTrajectory()
    {
        if (rawPositions.Count < 2) return;

        int smoothPoints = 5; // 每段插值点数
        List<Vector3> smoothPositions = new List<Vector3>();

        for (int i = 0; i < rawPositions.Count; i++)
        {
            smoothPositions.Add(rawPositions[i]);
            if (i < rawPositions.Count - 1)
            {
                Vector3 p0 = i == 0 ? rawPositions[i] : rawPositions[i - 1];
                Vector3 p1 = rawPositions[i];
                Vector3 p2 = rawPositions[i + 1];
                Vector3 p3 = (i + 2 < rawPositions.Count) ? rawPositions[i + 2] : rawPositions[i + 1];

                for (int j = 1; j < smoothPoints; j++)
                {
                    float t = j / (float)smoothPoints;
                    Vector3 smoothPos = CatmullRom(p0, p1, p2, p3, t);
                    smoothPositions.Add(smoothPos);
                }
            }
        }

        lineRenderer.positionCount = smoothPositions.Count;
        for (int i = 0; i < smoothPositions.Count; i++)
        {
            lineRenderer.SetPosition(i, smoothPositions[i]);
        }
    }

    // Catmull-Rom样条插值函数
    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t
        );
    }
}