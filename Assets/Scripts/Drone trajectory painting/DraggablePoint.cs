using UnityEngine;

public class DraggablePoint : MonoBehaviour
{
    private int pointIndex;
    private System.Action<int, Vector3> onPositionChanged;

    public void Initialize(int index, System.Action<int, Vector3> callback)
    {
        pointIndex = index;
        onPositionChanged = callback;
    }

    private void Update()
    {
        if (transform.hasChanged) // 检测位置是否发生变化
        {
            onPositionChanged?.Invoke(pointIndex, transform.position);
            transform.hasChanged = false;
        }
    }
}