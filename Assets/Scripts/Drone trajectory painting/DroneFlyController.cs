using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneFlyController : MonoBehaviour
{
    [Header("轨迹设置")]
    public GameObject penObject; // 包含LineRenderer的Pen物体
    public float moveSpeed = 0.5f; // 移动速度
    public float rotationSpeed = 0.5f; // 旋转速度
    public bool loop = false; // 是否循环运动
    public bool startOnPlay = false; // 是否在游戏开始时自动开始移动
    public bool smoothStart = true; // 是否平滑移动到起点
    
    private LineRenderer lineRenderer;
    private Vector3[] pathPoints;
    private int currentPointIndex = 0;
    private bool isMoving = false;
    private bool isInitializing = false; // 标记是否正在初始化移动到起点
    
    void Start()
    {
        // 获取Pen物体上的LineRenderer组件
        if (penObject != null)
        {
            lineRenderer = penObject.GetComponent<LineRenderer>();
        }
        else
        {
            // 如果没有指定Pen物体，尝试在当前场景中查找名为"Pen"的物体
            penObject = GameObject.Find("Pen");
            if (penObject != null)
            {
                lineRenderer = penObject.GetComponent<LineRenderer>();
            }
        }
        
        if (lineRenderer == null)
        {
            Debug.LogError("未找到LineRenderer组件！请确保penObject已赋值或场景中有名为'Pen'的物体且包含LineRenderer组件。");
            return;
        }
        
        // 初始化路径点
        InitializePath();
        
        // 如果设置为游戏开始时自动移动
        if (startOnPlay)
        {
            StartMovement();
        }
    }
    
    void Update()
    {
        
    }
    
    // 初始化路径点
    void InitializePath()
    {
        if (lineRenderer == null) return;
        
        // 获取LineRenderer的所有位置点
        pathPoints = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(pathPoints);
        
        if (pathPoints.Length > 0)
        {
            if (smoothStart)
            {
                // 平滑移动到起点
                StartCoroutine(SmoothMoveToStart());
            }
            else
            {
                // 直接设置到起点
                transform.position = pathPoints[0];
                currentPointIndex = 0;
            }
        }
    }
    
    // 平滑移动到起点的协程
    IEnumerator SmoothMoveToStart()
    {
        if (pathPoints.Length == 0) yield break;
        
        isInitializing = true;
        
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = pathPoints[0];
        float distance = Vector3.Distance(startPosition, targetPosition);
        float duration = distance / moveSpeed;
        float elapsedTime = 0f;
        
        // 计算朝向起点的旋转
        Vector3 direction = (targetPosition - transform.position).normalized;
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = direction != Vector3.zero ? 
        Quaternion.LookRotation(direction) : startRotation;
        
        while (elapsedTime < duration && isInitializing)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // 位置插值
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            
            // 旋转插值（使物体朝向起点方向）
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t * rotationSpeed);
            
            yield return null;
        }
        
        // 确保精确到达起点
        transform.position = targetPosition;
        currentPointIndex = 0;
        isInitializing = false;
    }
    
    // 开始移动
    public void StartMovement()
    {
        // if (pathPoints == null || pathPoints.Length == 0)
        // {
        //     InitializePath();
        // }
        InitializePath();
        
        if (pathPoints.Length > 1 && !isMoving)
        {
            // 如果正在初始化移动到起点，等待完成
            if (isInitializing)
            {
                StartCoroutine(WaitForInitialization());
            }
            else
            {
                isMoving = true;
                StartCoroutine(FollowPath());
            }
        }
    }
    
    // 等待初始化完成的协程
    IEnumerator WaitForInitialization()
    {
        while (isInitializing)
        {
            yield return null;
        }
        
        isMoving = true;
        StartCoroutine(FollowPath());
    }
    
    // 停止移动
    public void StopMovement()
    {
        isMoving = false;
        isInitializing = false;
        StopAllCoroutines();
    }
    
    // 重置路径点
    public void ResetPath()
    {
        StopMovement(); // 停止移动
        pathPoints = new Vector3[0]; // 清空路径点
        currentPointIndex = 0;
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0; // 清空 LineRenderer 的点
        }
    }

    // 重新开始移动
    public void RestartMovement()
    {
        StopMovement();
        currentPointIndex = 0;
        
        if (smoothStart && pathPoints.Length > 0)
        {
            // 平滑移动到起点
            StartCoroutine(SmoothMoveToStart());
            
            // 等待初始化完成后开始移动
            StartCoroutine(WaitForInitializationAndStart());
        }
        else
        {
            if (pathPoints.Length > 0)
            {
                transform.position = pathPoints[0];
            }
            StartMovement();
        }
    }
    
    // 等待初始化完成并开始移动的协程
    IEnumerator WaitForInitializationAndStart()
    {
        while (isInitializing)
        {
            yield return null;
        }
        
        isMoving = true;
        StartCoroutine(FollowPath());
    }
    
    // 跟随路径的协程
    IEnumerator FollowPath()
    {
        while (isMoving && currentPointIndex < pathPoints.Length - 1)
        {
            Vector3 targetPoint = pathPoints[currentPointIndex + 1];
            
            // 移动到下一个点
            yield return StartCoroutine(MoveToPoint(targetPoint));
            
            currentPointIndex++;
            
            // 如果到达终点且启用循环，重新开始
            if (currentPointIndex >= pathPoints.Length - 1 && loop)
            {
                currentPointIndex = 0;
                
                if (smoothStart)
                {
                    // 平滑移动到起点
                    yield return StartCoroutine(SmoothMoveToStart());
                }
                else
                {
                    transform.position = pathPoints[0];
                }
            }
            else if (currentPointIndex >= pathPoints.Length - 1 && !loop)
            {
                currentPointIndex = -1;
                break;
            }
        }
        isMoving = false;
    }
    
    // 移动到指定点的协程
    IEnumerator MoveToPoint(Vector3 targetPoint)
    {
        float distance = Vector3.Distance(transform.position, targetPoint);
        float duration = distance / moveSpeed;
        float elapsedTime = 0f;
        
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        
        // 计算朝向目标点的旋转
        Vector3 direction = (targetPoint - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            
            while (elapsedTime < duration && isMoving)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                
                // 位置插值
                transform.position = Vector3.Lerp(startPosition, targetPoint, t);
                
                // 旋转插值（使物体朝向移动方向）
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t * rotationSpeed);
                
                yield return null;
            }
        }
        else
        {
            // 如果方向为零向量，只进行位置移动
            while (elapsedTime < duration && isMoving)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                transform.position = Vector3.Lerp(startPosition, targetPoint, t);
                yield return null;
            }
        }
        
        // 确保精确到达目标点
        if (isMoving)
        {
            transform.position = targetPoint;
        }
    }
    
    // 在Scene视图中绘制路径和当前目标点（便于调试）
    void OnDrawGizmos()
    {
        if (pathPoints != null && pathPoints.Length > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < pathPoints.Length - 1; i++)
            {
                Gizmos.DrawLine(pathPoints[i], pathPoints[i + 1]);
                Gizmos.DrawSphere(pathPoints[i], 0.05f);
            }
            Gizmos.DrawSphere(pathPoints[pathPoints.Length - 1], 0.05f);
            
            // 绘制当前目标点
            if ((isMoving || isInitializing) && currentPointIndex < pathPoints.Length - 1)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(pathPoints[currentPointIndex + 1], 0.1f);
            }
            
            // 绘制起点（黄色）
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pathPoints[0], 0.15f);
        }
    }
    
    // 公开方法：获取当前移动状态
    public bool IsMoving()
    {
        return isMoving || isInitializing;
    }
    
    // 公开方法：获取路径进度（0-1）
    public float GetProgress()
    {
        if (pathPoints == null || pathPoints.Length <= 1) return 0f;
        return (float)currentPointIndex / (pathPoints.Length - 1);
    }
    
    // 公开方法：设置是否平滑移动到起点
    public void SetSmoothStart(bool smooth)
    {
        smoothStart = smooth;
    }
}