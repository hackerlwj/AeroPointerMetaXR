using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class TrajectoryTracker : MonoBehaviour
{
    private Transform headTransform;//ͷ��λ�÷�����Ϣ
    public GameObject redDotPrefab; // ������Ԥ����
    private Queue<GameObject> dotsQueue = new Queue<GameObject>();
    private const int MaxDots = 60; // ������

    void Start()
    {
        headTransform = GameObject.Find("CenterEyeAnchor").transform;
        // ����Э�̣�ÿ��1/60������һ����
        StartCoroutine(SpawnDots());
    }

    IEnumerator SpawnDots()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f / 60f); // ��ȷ���
            AddDot();
        }
    }

    void AddDot()
    {
        // ���ɺ�㲢��¼λ��
        GameObject dot = Instantiate(redDotPrefab, headTransform.position, Quaternion.identity);
        dotsQueue.Enqueue(dot);

        // ����60����ʱ�Ƴ�����ĵ�
        if (dotsQueue.Count > MaxDots)
        {
            Destroy(dotsQueue.Dequeue());
        }
    }
}