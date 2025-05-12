using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class TrajectoryTracker : MonoBehaviour
{
    [Header("Tracking Settings")]
    private Transform headTransform;
    public GameObject redDotPrefab;
    private Queue<GameObject> dotsQueue = new Queue<GameObject>();
    public int MaxDots = 60;

    private bool isAutoCleanEnabled = true; // 默认自动清理旧点

    void Start()
    {
        headTransform = GameObject.Find("CenterEyeAnchor").transform;
        StartCoroutine(SpawnDots());
    }

    IEnumerator SpawnDots()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f / 60f);
            AddDot();
        }
    }

    void AddDot()
    {
        GameObject dot = Instantiate(redDotPrefab, headTransform.position, Quaternion.identity);
        dotsQueue.Enqueue(dot);

        // 仅在自动清理模式时限制数量
        if (isAutoCleanEnabled && dotsQueue.Count > MaxDots)
        {
            DestroyOldestDot();
        }
    }

    public void TogglePersistence()
    {
        isAutoCleanEnabled = !isAutoCleanEnabled;
        Debug.Log($"Red dot persistence: {!isAutoCleanEnabled}");
    }

    void DestroyOldestDot()
    {
        GameObject oldDot = dotsQueue.Dequeue();
        if (oldDot != null)
        {
            Destroy(oldDot);
        }
    }

    public void ClearAllDots()
    {
        while (dotsQueue.Count > 0)
        {
            DestroyOldestDot();
        }
    }
}