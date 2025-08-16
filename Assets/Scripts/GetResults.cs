using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Oculus.Interaction.Locomotion;
using TMPro;

// ==============================
// 网络传输数据结构定义（必须标记为可序列化）
// ==============================
[System.Serializable]
public class HeadData
{
    /// <summary>Unix时间戳（毫秒）</summary>
    public long timestamp;

    /// <summary>相对于目标物体的局部坐标系位置</summary>
    public SerializableVector3 position;

    /// <summary>头部旋转四元数表示</summary>
    public SerializableQuaternion rotation;

    /// <summary>头部欧拉角表示</summary>
    public SerializableVector3 eulerAngles; // 注意原代码此处拼写错误保留
}

// ==============================
// 可序列化的Vector3替代结构
// （Unity原生Vector3无法直接序列化）
// ==============================
[System.Serializable]
public struct SerializableVector3
{
    public float x;
    public float y;
    public float z;

    /// <summary>从Unity Vector3转换</summary>
    public SerializableVector3(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }
}

// ==============================
// 可序列化的Quaternion替代结构
// ==============================
[System.Serializable]
public struct SerializableQuaternion
{
    public float x;
    public float y;
    public float z;
    public float w;

    /// <summary>从Unity Quaternion转换</summary>
    public SerializableQuaternion(Quaternion q)
    {
        x = q.x;
        y = q.y;
        z = q.z;
        w = q.w;
    }
}

// ==============================
// 主功能类：头部数据采集与网络传输
// ==============================
public class GetResults : MonoBehaviour
{
    // ---------- VR组件配置区 ----------
    [Header("VR Settings")]
    [Tooltip("右手传送射线方向组件")]
    public TeleportArcGravity rightTeleportArcGravity;

    [Tooltip("左手传送射线方向组件")]
    public TeleportArcGravity leftTeleportArcGravity;

    [Tooltip("坐标系原点目标物体")]
    public Transform targetObject;

    [Tooltip("信息显示文本组件")]
    public TMP_Text showText;

    [Tooltip("VR界面画布")]
    public Canvas vrCanvas;

    [Tooltip("UI默认显示距离（米）")]
    public float uiDistance = 0.5f;

    [Tooltip("UI相对偏移量")]
    public Vector3 uiOffset = new Vector3(-0.5f, 0.2f, 0);

    // ---------- 网络配置区 ----------
    [Header("Network Settings")]
    [Tooltip("网络端口号")]
    public int port = 30001;

    [Tooltip("广播地址（推荐使用子网广播地址）")]
    public string broadcastIP = "192.168.1.255";

    [Tooltip("数据发送间隔（秒）")]
    [Range(0.001f, 1f)]
    public float sendInterval = 0.1f;

    // ---------- 私有变量区 ----------
    private Transform headTransform;          // 头显位置信息
    private UdpClient _client;                // UDP客户端实例
    private IPEndPoint _broadcastEndPoint;    // 广播端点信息
    private float _lastSendTime;              // 上次发送时间戳


    // ---------- 初始化方法 ----------
    void Start()
    {
        // 查找中心锚点
        headTransform = GameObject.Find("CenterEyeAnchor").transform;

        // 初始化目标物体
        InitializeCubeSize();
        ResetPosition();

        // 网络初始化
        SetupNetworkConnection();
    }

    /// <summary>
    /// 初始化网络连接
    /// </summary>
    void SetupNetworkConnection()
    {
        try
        {
            _client = new UdpClient();
            _client.EnableBroadcast = true;

            // 使用更可靠的子网广播地址
            IPAddress broadcastAddress = IPAddress.Parse(broadcastIP);
            _broadcastEndPoint = new IPEndPoint(broadcastAddress, port);

            Debug.Log($"UDP广播初始化完成，目标地址：{broadcastIP}:{port}");
        }
        catch (Exception e)
        {
            Debug.LogError($"网络初始化失败：{e.GetType().Name}\n{e.Message}\n{e.StackTrace}");
        }
    }

    // ---------- 主更新循环 ----------
    void Update()
    {
        // 保持目标物体仅绕Y轴旋转
        MaintainYAxisRotation();

        if (targetObject != null)
        {
            // 坐标系转换
            Vector3 headLocalPos = targetObject.InverseTransformPoint(headTransform.position);
            Vector3 headLocalDir = targetObject.InverseTransformDirection(headTransform.forward);

            // 手部方向计算
            Vector3 rightDir = targetObject.InverseTransformDirection(rightTeleportArcGravity.GetRayDirection());
            Vector3 leftDir = targetObject.InverseTransformDirection(leftTeleportArcGravity.GetRayDirection());

            // 更新界面
            UpdateDisplay(headLocalPos,
                Quaternion.LookRotation(headLocalDir),
                Quaternion.LookRotation(rightDir),
                Quaternion.LookRotation(leftDir));

            AdjustCanvasPosition();

            // 定时发送数据
            if (Time.realtimeSinceStartup - _lastSendTime >= sendInterval)
            {
                SendTrackingData(CreateDataPackage(headLocalPos, headLocalDir));
                _lastSendTime = Time.realtimeSinceStartup;
            }
        }
    }

    // ---------- 核心功能方法 ----------

    /// <summary>
    /// 创建数据包
    /// </summary>
    HeadData CreateDataPackage(Vector3 position, Vector3 direction)
    {
        Quaternion rotation = Quaternion.LookRotation(direction);

        return new HeadData
        {
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            position = new SerializableVector3(position),
            rotation = new SerializableQuaternion(rotation),
            eulerAngles = new SerializableVector3(rotation.eulerAngles)
        };
    }

    /// <summary>
    /// 发送跟踪数据（异步方式）
    /// </summary>
    void SendTrackingData(HeadData data)
    {
        if (_client == null)
        {
            Debug.LogWarning("UDP客户端未初始化");
            return;
        }

        try
        {
            // 序列化数据
            string json = JsonUtility.ToJson(data);
            byte[] buffer = Encoding.UTF8.GetBytes(json + "\n"); // 添加分隔符

            // 异步发送避免阻塞主线程
            _client.BeginSend(buffer, buffer.Length, _broadcastEndPoint, SendCallback, null);
        }
        catch (Exception e)
        {
            Debug.LogError($"数据序列化失败：{e.Message}");
        }
    }

    /// <summary>
    /// 异步发送回调
    /// </summary>
    private void SendCallback(IAsyncResult result)
    {
        try
        {
            int sentBytes = _client.EndSend(result);
            if (sentBytes == 0)
            {
                Debug.LogWarning("空数据包已发送");
            }
        }
        catch (SocketException e)
        {
            Debug.LogError($"网络错误：[{e.ErrorCode}]{e.SocketErrorCode}\n{e.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"发送异常：{e.GetType().Name}\n{e.Message}");
        }
    }

    // ---------- 辅助方法 ----------

    /// <summary>
    /// 维持Y轴旋转约束
    /// </summary>
    void MaintainYAxisRotation()
    {
        float currentY = targetObject.eulerAngles.y;
        targetObject.rotation = Quaternion.Euler(0, currentY, 0);
    }

    /// <summary>
    /// 更新显示界面
    /// </summary>
    void UpdateDisplay(Vector3 pos, Quaternion headRot, Quaternion rightRot, Quaternion leftRot)
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

    /// <summary>
    /// 调整画布位置
    /// </summary>
    void AdjustCanvasPosition()
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

    /// <summary>
    /// 重置目标物体位置
    /// </summary>
    public void ResetPosition()
    {
        targetObject.rotation = Quaternion.identity;// 重置旋转
        targetObject.position = headTransform.position + headTransform.forward * 0.4f;// 计算新位置：头显前方0.4米处
    }

    /// <summary>
    /// 初始化立方体尺寸
    /// </summary>
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
        if (_client != null)
        {
            _client.Close();
            _client = null;
            Debug.Log("UDP连接已关闭");
        }
    }
}