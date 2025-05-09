using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class TrajectoryTracker : MonoBehaviour
{
    private Transform headTransform;//头显位置方向信息
    public GameObject redDotPrefab; // 拖入红点预制体
    private Queue<GameObject> dotsQueue = new Queue<GameObject>();
    private const int MaxDots = 60; // 最大点数

    void Start()
    {
        headTransform = GameObject.Find("CenterEyeAnchor").transform;
        // 启动协程，每隔1/60秒生成一个点
        StartCoroutine(SpawnDots());
    }

    IEnumerator SpawnDots()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f / 60f); // 精确间隔
            AddDot();
        }
    }

    void AddDot()
    {
        // 生成红点并记录位置
        GameObject dot = Instantiate(redDotPrefab, headTransform.position, Quaternion.identity);
        dotsQueue.Enqueue(dot);

        // 超过60个点时移除最早的点
        if (dotsQueue.Count > MaxDots)
        {
            Destroy(dotsQueue.Dequeue());
        }
    }
}