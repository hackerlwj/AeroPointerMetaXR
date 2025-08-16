using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;

public class UdpBroadcaster : MonoBehaviour
{
    [Header("����")]
    public int port = 30000;
    public float interval = 0.5f; // ���ͼ�����룩

    private UdpClient udpClient;
    private Coroutine broadcastCoroutine;

    void Start()
    {
        // ��ʼ��UDP�ͻ���
        udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;

        // ����Э��
        broadcastCoroutine = StartCoroutine(BroadcastData());
    }

    IEnumerator BroadcastData()
    {
        Debug.Log($"��ʼ�� 255.255.255.255:{port} �㲥����...");

        while (true)
        {
            // �������ݣ�����Unity��JsonUtility��
            var data = new
            {
                timestamp = DateTime.UtcNow.ToString("o"),
                randomValue = UnityEngine.Random.Range(0, 1000),
                message = "Hello from Unity"
            };

            string json = JsonUtility.ToJson(data);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            // ��������
            udpClient.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, port));
            Debug.Log($"�ѷ���: {json}");

            // ʹ��Unity��Э�̵ȴ�����Thread.Sleep
            yield return new WaitForSeconds(interval);
        }
    }

    void OnDestroy()
    {
        // ֹͣЭ�̲��ͷ���Դ
        if (broadcastCoroutine != null) StopCoroutine(broadcastCoroutine);
        udpClient?.Close();
    }
}