using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Oculus.Interaction.Locomotion;
using UnityEngine.UI;
using TMPro;

// 网络传输数据结构定义（必须标记为可序列化）
[System.Serializable]
public class HeadData
{
    public long timestamp;       // 时间戳（Unix毫秒时间）
    public SerializableVector3 position; // 头部位置（相对目标物体的局部坐标系）
    public SerializableQuaternion rotation; // 头部旋转（四元数）
    public SerializableVector3 eulerAngles; // 头部欧拉角
}

// 可序列化的Vector3替代结构（Unity的Vector3无法直接序列化）
[System.Serializable]
public struct SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }
}

// 可序列化的Quaternion替代结构
[System.Serializable]
public struct SerializableQuaternion
{
    public float x;
    public float y;
    public float z;
    public float w;

    public SerializableQuaternion(Quaternion q)
    {
        x = q.x;
        y = q.y;
        z = q.z;
        w = q.w;
    }
}

public class GetResults : MonoBehaviour
{
    // ---------- VR组件引用 ----------
    [Header("VR Settings")]
    public TeleportArcGravity rightTeleportArcGravity; // 右手传送射线方向组件
    public TeleportArcGravity leftTeleportArcGravity;  // 左手传送射线方向组件
    public Transform targetObject;                     // 坐标系原点目标物体
    public TMP_Text showText;                      // 头部位置和方向等信息显示文本
    //public TMP_Text headDirectionText;                     // 头部方向显示文本
    //public TMP_Text rightHandDirectionText;                // 右手方向显示文本
    //public TMP_Text leftHandDirectionText;                 // 左手方向显示文本
    public Canvas vrCanvas;                            // VR界面画布
    public float uiDistance = 0.5f;                    // UI距离头显的默认距离（米）
    public Vector3 uiOffset = new Vector3(-0.5f, 0.2f, 0); // UI相对头显的偏移量

    // ---------- 网络配置 ----------
    [Header("Network Settings")]
    public int port = 30001;                  // 网络端口号
    public float sendInterval = 0.1f;         // 数据发送间隔（秒）

    // ---------- 私有变量 ----------
    private Transform headTransform;          // 头显位置信息
    private UdpClient _client;                // UDP客户端
    private IPEndPoint _broadcastEndPoint;    // 广播端点
    private float _lastSendTime;              // 上次发送时间记录

    // ---------- 初始化 ----------
    void Start()
    {
        // 查找头显中心锚点
        headTransform = GameObject.Find("CenterEyeAnchor").transform;

        // 初始化立方体大小和位置
        InitializeCubeSize();
        Reset();

        // 建立网络连接
        InitializeNetwork();
    }

    // 初始化网络连接
    void InitializeNetwork()
    {
        try
        {
            _client = new UdpClient();
            _client.EnableBroadcast = true;
            // 尝试使用具体的广播地址
            IPAddress broadcastAddress = IPAddress.Parse("255.255.255.255");
            _broadcastEndPoint = new IPEndPoint(broadcastAddress, port);
            Debug.Log($"准备在端口 {port} 上进行广播发送");
        }
        catch (Exception e)
        {
            Debug.LogError($"网络初始化失败: {e.Message}，详细信息: {e.StackTrace}");
        }
    }

    // --------- 核心逻辑 ----------
    void Update()
    {
        // 保持目标物体仅绕Y轴旋转
        float currentY = targetObject.transform.eulerAngles.y;
        targetObject.transform.rotation = Quaternion.Euler(0, currentY, 0);

        if (targetObject != null)
        {
            // 坐标系转换：将头显位置/方向转换为目标物体的局部坐标系
            Vector3 headLocalPosition = targetObject.InverseTransformPoint(headTransform.position);
            Vector3 headLocalDirection = targetObject.InverseTransformDirection(headTransform.forward);

            // 手部方向计算（转换到局部坐标系）
            Vector3 rightDir = targetObject.InverseTransformDirection(rightTeleportArcGravity.GetRayDirection());
            Vector3 leftDir = targetObject.InverseTransformDirection(leftTeleportArcGravity.GetRayDirection());

            // 更新UI显示
            UpdateUI(
                headLocalPosition,
                Quaternion.LookRotation(headLocalDirection), // 头部方向转为四元数
                Quaternion.LookRotation(rightDir),            // 右手方向
                Quaternion.LookRotation(leftDir)              // 左手方向
            );

            // 调整UI位置
            UpdateUIPosition();

            // 按间隔发送数据
            if (Time.time - _lastSendTime > sendInterval)
            {
                // 构造网络数据包
                HeadData data = new HeadData
                {
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    position = new SerializableVector3(headLocalPosition),
                    rotation = new SerializableQuaternion(Quaternion.LookRotation(headLocalDirection)),
                    eulerAngles = new SerializableVector3(Quaternion.LookRotation(headLocalDirection).eulerAngles)
                };

                // 发送数据
                //SendHeadData(data);
                _lastSendTime = Time.time;
            }
        }
    }

    // ---------- 功能方法 ----------
    // 发送头部数据到网络
    void SendHeadData(HeadData data)
    {
        if (_client != null && _client.Client != null && _client.Client.Connected)
        {
            try
            {
                // 序列化为JSON并添加换行符
                string json = JsonUtility.ToJson(data);
                byte[] bytes = Encoding.UTF8.GetBytes(json + "\n");

                // 发送数据
                _client.Send(bytes, bytes.Length, _broadcastEndPoint);
            }
            catch (Exception e)
            {
                Debug.LogError($"发送失败: {e.Message}");
            }
        }
        else
        {
            Debug.LogError("UdpClient 未连接或无效");
        }
    }

    // 更新UI显示内容
    void UpdateUI(Vector3 pos, Quaternion headRot, Quaternion rightRot, Quaternion leftRot)
    {
        // 颜色常量定义
        const string X_COLOR = "#FF6666"; // 红色系
        const string Y_COLOR = "#66FF66"; // 绿色系 
        const string Z_COLOR = "#6699FF"; // 蓝色系
        const string W_COLOR = "#AAAAAA"; // 中性灰

        var sb = new System.Text.StringBuilder();

        // ===== 位置坐标 =====
        sb.AppendLine("<b><color=#4DA6FF>=== POSITION ===</color></b>");
        sb.AppendLine($"<color={X_COLOR}>X:</color> <pos=25%>{pos.x,8:F2}</pos>");
        sb.AppendLine($"<color={Y_COLOR}>Y:</color> <pos=25%>{pos.y,8:F2}</pos>");
        sb.AppendLine($"<color={Z_COLOR}>Z:</color> <pos=25%>{pos.z,8:F2}</pos>");
        sb.AppendLine("---------------------");

        // ===== 欧拉角 =====
        Vector3 euler = headRot.eulerAngles;
        sb.AppendLine("<b><color=#FFB84D>=== ROTATION ===</color></b>");
        sb.AppendLine("<color=#AAAAAA>Euler Angles:</color>");
        sb.AppendLine($"<color={X_COLOR}>Pitch(X):</color> <pos=35%>{euler.x,7:F1}°</pos>");
        sb.AppendLine($"<color={Y_COLOR}>Yaw(Y):</color>   <pos=35%>{euler.y,7:F1}°</pos>");
        sb.AppendLine($"<color={Z_COLOR}>Roll(Z):</color>  <pos=35%>{euler.z,7:F1}°</pos>");
        sb.AppendLine("---------------------");

        // ===== 四元数 =====
        sb.AppendLine("<b><color=#FF80FF>=== QUATERNION ===</color></b>");
        sb.AppendLine($"<color={X_COLOR}>X:</color> <pos=15%>{headRot.x,+9:F4}</pos>");
        sb.AppendLine($"<color={Y_COLOR}>Y:</color> <pos=15%>{headRot.y,+9:F4}</pos>");
        sb.AppendLine($"<color={Z_COLOR}>Z:</color> <pos=15%>{headRot.z,+9:F4}</pos>");
        sb.AppendLine($"<color={W_COLOR}>W:</color> <pos=15%>{headRot.w,+9:F4}</pos>");

        showText.text = sb.ToString();
    }

    // 调整UI位置（始终位于头显左前方）
    void UpdateUIPosition()
    {
        if (vrCanvas == null || headTransform == null) return;

        // 计算目标位置：头显前方指定距离 + 偏移量
        Vector3 targetPos = headTransform.position +
                          headTransform.forward * uiDistance +
                          headTransform.right * uiOffset.x +
                          headTransform.up * uiOffset.y;

        // 平滑移动UI位置
        vrCanvas.transform.position = Vector3.Lerp(
            vrCanvas.transform.position,
            targetPos,
            Time.deltaTime * 8f // 平滑系数
        );

        // 保持UI面向用户
        vrCanvas.transform.rotation = Quaternion.LookRotation(
            vrCanvas.transform.position - headTransform.position
        );
    }

    // 重置目标物体位置
    public void Reset()
    {
        // 重置旋转
        targetObject.rotation = Quaternion.identity;

        // 计算新位置：头显前方0.4米处
        targetObject.position = headTransform.position + headTransform.forward * 0.4f;
    }

    // 初始化立方体尺寸（调整为8cm边长）
    void InitializeCubeSize()
    {
        MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();
        if (meshFilter == null) return;

        // 复制网格以避免修改原始资源
        Mesh mesh = Instantiate(meshFilter.sharedMesh);
        Vector3 targetSize = new Vector3(0.08f, 0.08f, 0.08f);

        // 调整顶点坐标
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = new Vector3(
                vertices[i].x * targetSize.x,
                vertices[i].y * targetSize.y,
                vertices[i].z * targetSize.z
            );
        }

        // 应用网格修改
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        meshFilter.sharedMesh = mesh;

        // 同步调整碰撞体
        BoxCollider collider = targetObject.GetComponent<BoxCollider>();
        if (collider != null) collider.size = targetSize;
    }

    // ---------- 清理资源 ----------
    void OnDestroy()
    {
        // 关闭网络连接
        if (_client != null)
        {
            _client.Close();
            _client = null;
        }
    }
}