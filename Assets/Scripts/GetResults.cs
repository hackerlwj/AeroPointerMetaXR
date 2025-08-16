using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Oculus.Interaction.Locomotion;
using TMPro;

// ==============================
// ���紫�����ݽṹ���壨������Ϊ�����л���
// ==============================
[System.Serializable]
public class HeadData
{
    /// <summary>Unixʱ��������룩</summary>
    public long timestamp;

    /// <summary>�����Ŀ������ľֲ�����ϵλ��</summary>
    public SerializableVector3 position;

    /// <summary>ͷ����ת��Ԫ����ʾ</summary>
    public SerializableQuaternion rotation;

    /// <summary>ͷ��ŷ���Ǳ�ʾ</summary>
    public SerializableVector3 eulerAngles; // ע��ԭ����˴�ƴд������
}

// ==============================
// �����л���Vector3����ṹ
// ��Unityԭ��Vector3�޷�ֱ�����л���
// ==============================
[System.Serializable]
public struct SerializableVector3
{
    public float x;
    public float y;
    public float z;

    /// <summary>��Unity Vector3ת��</summary>
    public SerializableVector3(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }
}

// ==============================
// �����л���Quaternion����ṹ
// ==============================
[System.Serializable]
public struct SerializableQuaternion
{
    public float x;
    public float y;
    public float z;
    public float w;

    /// <summary>��Unity Quaternionת��</summary>
    public SerializableQuaternion(Quaternion q)
    {
        x = q.x;
        y = q.y;
        z = q.z;
        w = q.w;
    }
}

// ==============================
// �������ࣺͷ�����ݲɼ������紫��
// ==============================
public class GetResults : MonoBehaviour
{
    // ---------- VR��������� ----------
    [Header("VR Settings")]
    [Tooltip("���ִ������߷������")]
    public TeleportArcGravity rightTeleportArcGravity;

    [Tooltip("���ִ������߷������")]
    public TeleportArcGravity leftTeleportArcGravity;

    [Tooltip("����ϵԭ��Ŀ������")]
    public Transform targetObject;

    [Tooltip("��Ϣ��ʾ�ı����")]
    public TMP_Text showText;

    [Tooltip("VR���滭��")]
    public Canvas vrCanvas;

    [Tooltip("UIĬ����ʾ���루�ף�")]
    public float uiDistance = 0.5f;

    [Tooltip("UI���ƫ����")]
    public Vector3 uiOffset = new Vector3(-0.5f, 0.2f, 0);

    // ---------- ���������� ----------
    [Header("Network Settings")]
    [Tooltip("����˿ں�")]
    public int port = 30001;

    [Tooltip("�㲥��ַ���Ƽ�ʹ�������㲥��ַ��")]
    public string broadcastIP = "192.168.1.255";

    [Tooltip("���ݷ��ͼ�����룩")]
    [Range(0.001f, 1f)]
    public float sendInterval = 0.1f;

    // ---------- ˽�б����� ----------
    private Transform headTransform;          // ͷ��λ����Ϣ
    private UdpClient _client;                // UDP�ͻ���ʵ��
    private IPEndPoint _broadcastEndPoint;    // �㲥�˵���Ϣ
    private float _lastSendTime;              // �ϴη���ʱ���


    // ---------- ��ʼ������ ----------
    void Start()
    {
        // ��������ê��
        headTransform = GameObject.Find("CenterEyeAnchor").transform;

        // ��ʼ��Ŀ������
        InitializeCubeSize();
        ResetPosition();

        // �����ʼ��
        SetupNetworkConnection();
    }

    /// <summary>
    /// ��ʼ����������
    /// </summary>
    void SetupNetworkConnection()
    {
        try
        {
            _client = new UdpClient();
            _client.EnableBroadcast = true;

            // ʹ�ø��ɿ��������㲥��ַ
            IPAddress broadcastAddress = IPAddress.Parse(broadcastIP);
            _broadcastEndPoint = new IPEndPoint(broadcastAddress, port);

            Debug.Log($"UDP�㲥��ʼ����ɣ�Ŀ���ַ��{broadcastIP}:{port}");
        }
        catch (Exception e)
        {
            Debug.LogError($"�����ʼ��ʧ�ܣ�{e.GetType().Name}\n{e.Message}\n{e.StackTrace}");
        }
    }

    // ---------- ������ѭ�� ----------
    void Update()
    {
        // ����Ŀ���������Y����ת
        MaintainYAxisRotation();

        if (targetObject != null)
        {
            // ����ϵת��
            Vector3 headLocalPos = targetObject.InverseTransformPoint(headTransform.position);
            Vector3 headLocalDir = targetObject.InverseTransformDirection(headTransform.forward);

            // �ֲ��������
            Vector3 rightDir = targetObject.InverseTransformDirection(rightTeleportArcGravity.GetRayDirection());
            Vector3 leftDir = targetObject.InverseTransformDirection(leftTeleportArcGravity.GetRayDirection());

            // ���½���
            UpdateDisplay(headLocalPos,
                Quaternion.LookRotation(headLocalDir),
                Quaternion.LookRotation(rightDir),
                Quaternion.LookRotation(leftDir));

            AdjustCanvasPosition();

            // ��ʱ��������
            if (Time.realtimeSinceStartup - _lastSendTime >= sendInterval)
            {
                SendTrackingData(CreateDataPackage(headLocalPos, headLocalDir));
                _lastSendTime = Time.realtimeSinceStartup;
            }
        }
    }

    // ---------- ���Ĺ��ܷ��� ----------

    /// <summary>
    /// �������ݰ�
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
    /// ���͸������ݣ��첽��ʽ��
    /// </summary>
    void SendTrackingData(HeadData data)
    {
        if (_client == null)
        {
            Debug.LogWarning("UDP�ͻ���δ��ʼ��");
            return;
        }

        try
        {
            // ���л�����
            string json = JsonUtility.ToJson(data);
            byte[] buffer = Encoding.UTF8.GetBytes(json + "\n"); // ��ӷָ���

            // �첽���ͱ����������߳�
            _client.BeginSend(buffer, buffer.Length, _broadcastEndPoint, SendCallback, null);
        }
        catch (Exception e)
        {
            Debug.LogError($"�������л�ʧ�ܣ�{e.Message}");
        }
    }

    /// <summary>
    /// �첽���ͻص�
    /// </summary>
    private void SendCallback(IAsyncResult result)
    {
        try
        {
            int sentBytes = _client.EndSend(result);
            if (sentBytes == 0)
            {
                Debug.LogWarning("�����ݰ��ѷ���");
            }
        }
        catch (SocketException e)
        {
            Debug.LogError($"�������[{e.ErrorCode}]{e.SocketErrorCode}\n{e.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"�����쳣��{e.GetType().Name}\n{e.Message}");
        }
    }

    // ---------- �������� ----------

    /// <summary>
    /// ά��Y����תԼ��
    /// </summary>
    void MaintainYAxisRotation()
    {
        float currentY = targetObject.eulerAngles.y;
        targetObject.rotation = Quaternion.Euler(0, currentY, 0);
    }

    /// <summary>
    /// ������ʾ����
    /// </summary>
    void UpdateDisplay(Vector3 pos, Quaternion headRot, Quaternion rightRot, Quaternion leftRot)
    {
        // ��ɫ��������
        const string X_COLOR = "#FF6666"; // ��ɫϵ
        const string Y_COLOR = "#66FF66"; // ��ɫϵ 
        const string Z_COLOR = "#6699FF"; // ��ɫϵ
        const string W_COLOR = "#AAAAAA"; // ���Ի�

        var sb = new System.Text.StringBuilder();

        // ===== λ������ =====
        sb.AppendLine("<b><color=#4DA6FF>=== POSITION ===</color></b>");
        sb.AppendLine($"<color={X_COLOR}>X:</color> <pos=25%>{pos.x,8:F2}</pos>");
        sb.AppendLine($"<color={Y_COLOR}>Y:</color> <pos=25%>{pos.y,8:F2}</pos>");
        sb.AppendLine($"<color={Z_COLOR}>Z:</color> <pos=25%>{pos.z,8:F2}</pos>");
        sb.AppendLine("---------------------");

        // ===== ŷ���� =====
        Vector3 euler = headRot.eulerAngles;
        sb.AppendLine("<b><color=#FFB84D>=== ROTATION ===</color></b>");
        sb.AppendLine("<color=#AAAAAA>Euler Angles:</color>");
        sb.AppendLine($"<color={X_COLOR}>Pitch(X):</color> <pos=35%>{euler.x,7:F1}��</pos>");
        sb.AppendLine($"<color={Y_COLOR}>Yaw(Y):</color>   <pos=35%>{euler.y,7:F1}��</pos>");
        sb.AppendLine($"<color={Z_COLOR}>Roll(Z):</color>  <pos=35%>{euler.z,7:F1}��</pos>");
        sb.AppendLine("---------------------");

        // ===== ��Ԫ�� =====
        sb.AppendLine("<b><color=#FF80FF>=== QUATERNION ===</color></b>");
        sb.AppendLine($"<color={X_COLOR}>X:</color> <pos=15%>{headRot.x,+9:F4}</pos>");
        sb.AppendLine($"<color={Y_COLOR}>Y:</color> <pos=15%>{headRot.y,+9:F4}</pos>");
        sb.AppendLine($"<color={Z_COLOR}>Z:</color> <pos=15%>{headRot.z,+9:F4}</pos>");
        sb.AppendLine($"<color={W_COLOR}>W:</color> <pos=15%>{headRot.w,+9:F4}</pos>");

        showText.text = sb.ToString();
    }

    /// <summary>
    /// ��������λ��
    /// </summary>
    void AdjustCanvasPosition()
    {
        if (vrCanvas == null || headTransform == null) return;

        // ����Ŀ��λ�ã�ͷ��ǰ��ָ������ + ƫ����
        Vector3 targetPos = headTransform.position +
                          headTransform.forward * uiDistance +
                          headTransform.right * uiOffset.x +
                          headTransform.up * uiOffset.y;

        // ƽ���ƶ�UIλ��
        vrCanvas.transform.position = Vector3.Lerp(
            vrCanvas.transform.position,
            targetPos,
            Time.deltaTime * 8f // ƽ��ϵ��
        );

        // ����UI�����û�
        vrCanvas.transform.rotation = Quaternion.LookRotation(
            vrCanvas.transform.position - headTransform.position
        );
    }

    /// <summary>
    /// ����Ŀ������λ��
    /// </summary>
    public void ResetPosition()
    {
        targetObject.rotation = Quaternion.identity;// ������ת
        targetObject.position = headTransform.position + headTransform.forward * 0.4f;// ������λ�ã�ͷ��ǰ��0.4�״�
    }

    /// <summary>
    /// ��ʼ��������ߴ�
    /// </summary>
    void InitializeCubeSize()
    {
        MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();
        if (meshFilter == null) return;

        // ���������Ա����޸�ԭʼ��Դ
        Mesh mesh = Instantiate(meshFilter.sharedMesh);
        Vector3 targetSize = new Vector3(0.08f, 0.08f, 0.08f);

        // ������������
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = new Vector3(
                vertices[i].x * targetSize.x,
                vertices[i].y * targetSize.y,
                vertices[i].z * targetSize.z
            );
        }

        // Ӧ�������޸�
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        meshFilter.sharedMesh = mesh;

        // ͬ��������ײ��
        BoxCollider collider = targetObject.GetComponent<BoxCollider>();
        if (collider != null) collider.size = targetSize;
    }

    // ---------- ������Դ ----------
    void OnDestroy()
    {
        if (_client != null)
        {
            _client.Close();
            _client = null;
            Debug.Log("UDP�����ѹر�");
        }
    }
}