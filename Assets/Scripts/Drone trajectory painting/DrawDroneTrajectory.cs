using UnityEngine;
using UnityEngine.UI;

public class DrawDroneTragectory : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform targetObject;
    [SerializeField] private Transform headTransform;
    [SerializeField] private float startWidth = 0.05f;
    [SerializeField] private float endWidth = 0.05f;
    [SerializeField] private float pointDistanceThreshold = 0.1f;
    [SerializeField] private int maxPoints = 1000;

    private bool isDrawing = false;

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
    }

    //重置画球位置
    public void ResetPosition()
    {
        targetObject.rotation = Quaternion.identity;// 重置旋转
        targetObject.position = headTransform.position + headTransform.forward * 0.4f;// 计算新位置：头显前方0.4米处
    }
    public void StartEndDrawing()
    {
        if (isDrawing)
        {
            isDrawing = false;
        }
        else if (!isDrawing)
        {
            isDrawing = true;
            if (lineRenderer.positionCount > 0)
            {
                lineRenderer.positionCount = 0;
            }
            AddPoint();
        }
    }

    public void ClearLine()
    {
        lineRenderer.positionCount = 0;
        isDrawing = false;
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
        if (lineRenderer.positionCount >= maxPoints)
        {
            // 移除最早的点并移动其他点
            for (int i = 1; i < lineRenderer.positionCount; i++)
            {
                lineRenderer.SetPosition(i - 1, lineRenderer.GetPosition(i));
            }
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, targetObject.position);
        }
        else
        {
            lineRenderer.positionCount++;
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, targetObject.position);
        }
    }
}