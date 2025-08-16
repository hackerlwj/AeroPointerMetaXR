using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;

public class UdpBroadcaster : MonoBehaviour
{
    [Header("设置")]
    public int port = 30000;
    public float interval = 0.5f; // 发送间隔（秒）

    private UdpClient udpClient;
    private Coroutine broadcastCoroutine;

    void Start()
    {
        // 初始化UDP客户端
        udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;

        // 启动协程
        broadcastCoroutine = StartCoroutine(BroadcastData());
    }

    IEnumerator BroadcastData()
    {
        Debug.Log($"开始向 255.255.255.255:{port} 广播数据...");

        while (true)
        {
            // 构建数据（改用Unity的JsonUtility）
            var data = new
            {
                timestamp = DateTime.UtcNow.ToString("o"),
                randomValue = UnityEngine.Random.Range(0, 1000),
                message = "Hello from Unity"
            };

            string json = JsonUtility.ToJson(data);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            // 发送数据
            udpClient.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, port));
            Debug.Log($"已发送: {json}");

            // 使用Unity的协程等待代替Thread.Sleep
            yield return new WaitForSeconds(interval);
        }
    }

    void OnDestroy()
    {
        // 停止协程并释放资源
        if (broadcastCoroutine != null) StopCoroutine(broadcastCoroutine);
        udpClient?.Close();
    }
}