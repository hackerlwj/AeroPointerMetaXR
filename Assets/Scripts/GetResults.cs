using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Oculus.Interaction.Locomotion;
using UnityEngine.UI;
using TMPro;

// ���紫�����ݽṹ���壨������Ϊ�����л���
[System.Serializable]
public class HeadData
{
    public long timestamp;       // ʱ�����Unix����ʱ�䣩
    public SerializableVector3 position; // ͷ��λ�ã����Ŀ������ľֲ�����ϵ��
    public SerializableQuaternion rotation; // ͷ����ת����Ԫ����
    public SerializableVector3 eulerAngles; // ͷ��ŷ����
}

// �����л���Vector3����ṹ��Unity��Vector3�޷�ֱ�����л���
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

// �����л���Quaternion����ṹ
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
    // ---------- VR������� ----------
    [Header("VR Settings")]
    public TeleportArcGravity rightTeleportArcGravity; // ���ִ������߷������
    public TeleportArcGravity leftTeleportArcGravity;  // ���ִ������߷������
    public Transform targetObject;                     // ����ϵԭ��Ŀ������
    public TMP_Text showText;                      // ͷ��λ�úͷ������Ϣ��ʾ�ı�
    //public TMP_Text headDirectionText;                     // ͷ��������ʾ�ı�
    //public TMP_Text rightHandDirectionText;                // ���ַ�����ʾ�ı�
    //public TMP_Text leftHandDirectionText;                 // ���ַ�����ʾ�ı�
    public Canvas vrCanvas;                            // VR���滭��
    public float uiDistance = 0.5f;                    // UI����ͷ�Ե�Ĭ�Ͼ��루�ף�
    public Vector3 uiOffset = new Vector3(-0.5f, 0.2f, 0); // UI���ͷ�Ե�ƫ����

    // ---------- �������� ----------
    [Header("Network Settings")]
    public int port = 30001;                  // ����˿ں�
    public float sendInterval = 0.1f;         // ���ݷ��ͼ�����룩

    // ---------- ˽�б��� ----------
    private Transform headTransform;          // ͷ��λ����Ϣ
    private UdpClient _client;                // UDP�ͻ���
    private IPEndPoint _broadcastEndPoint;    // �㲥�˵�
    private float _lastSendTime;              // �ϴη���ʱ���¼

    // ---------- ��ʼ�� ----------
    void Start()
    {
        // ����ͷ������ê��
        headTransform = GameObject.Find("CenterEyeAnchor").transform;

        // ��ʼ���������С��λ��
        InitializeCubeSize();
        Reset();

        // ������������
        InitializeNetwork();
    }

    // ��ʼ����������
    void InitializeNetwork()
    {
        try
        {
            _client = new UdpClient();
            _client.EnableBroadcast = true;
            // ����ʹ�þ���Ĺ㲥��ַ
            IPAddress broadcastAddress = IPAddress.Parse("255.255.255.255");
            _broadcastEndPoint = new IPEndPoint(broadcastAddress, port);
            Debug.Log($"׼���ڶ˿� {port} �Ͻ��й㲥����");
        }
        catch (Exception e)
        {
            Debug.LogError($"�����ʼ��ʧ��: {e.Message}����ϸ��Ϣ: {e.StackTrace}");
        }
    }

    // --------- �����߼� ----------
    void Update()
    {
        // ����Ŀ���������Y����ת
        float currentY = targetObject.transform.eulerAngles.y;
        targetObject.transform.rotation = Quaternion.Euler(0, currentY, 0);

        if (targetObject != null)
        {
            // ����ϵת������ͷ��λ��/����ת��ΪĿ������ľֲ�����ϵ
            Vector3 headLocalPosition = targetObject.InverseTransformPoint(headTransform.position);
            Vector3 headLocalDirection = targetObject.InverseTransformDirection(headTransform.forward);

            // �ֲ�������㣨ת�����ֲ�����ϵ��
            Vector3 rightDir = targetObject.InverseTransformDirection(rightTeleportArcGravity.GetRayDirection());
            Vector3 leftDir = targetObject.InverseTransformDirection(leftTeleportArcGravity.GetRayDirection());

            // ����UI��ʾ
            UpdateUI(
                headLocalPosition,
                Quaternion.LookRotation(headLocalDirection), // ͷ������תΪ��Ԫ��
                Quaternion.LookRotation(rightDir),            // ���ַ���
                Quaternion.LookRotation(leftDir)              // ���ַ���
            );

            // ����UIλ��
            UpdateUIPosition();

            // �������������
            if (Time.time - _lastSendTime > sendInterval)
            {
                // �����������ݰ�
                HeadData data = new HeadData
                {
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    position = new SerializableVector3(headLocalPosition),
                    rotation = new SerializableQuaternion(Quaternion.LookRotation(headLocalDirection)),
                    eulerAngles = new SerializableVector3(Quaternion.LookRotation(headLocalDirection).eulerAngles)
                };

                // ��������
                //SendHeadData(data);
                _lastSendTime = Time.time;
            }
        }
    }

    // ---------- ���ܷ��� ----------
    // ����ͷ�����ݵ�����
    void SendHeadData(HeadData data)
    {
        if (_client != null && _client.Client != null && _client.Client.Connected)
        {
            try
            {
                // ���л�ΪJSON����ӻ��з�
                string json = JsonUtility.ToJson(data);
                byte[] bytes = Encoding.UTF8.GetBytes(json + "\n");

                // ��������
                _client.Send(bytes, bytes.Length, _broadcastEndPoint);
            }
            catch (Exception e)
            {
                Debug.LogError($"����ʧ��: {e.Message}");
            }
        }
        else
        {
            Debug.LogError("UdpClient δ���ӻ���Ч");
        }
    }

    // ����UI��ʾ����
    void UpdateUI(Vector3 pos, Quaternion headRot, Quaternion rightRot, Quaternion leftRot)
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

    // ����UIλ�ã�ʼ��λ��ͷ����ǰ����
    void UpdateUIPosition()
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

    // ����Ŀ������λ��
    public void Reset()
    {
        // ������ת
        targetObject.rotation = Quaternion.identity;

        // ������λ�ã�ͷ��ǰ��0.4�״�
        targetObject.position = headTransform.position + headTransform.forward * 0.4f;
    }

    // ��ʼ��������ߴ磨����Ϊ8cm�߳���
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
        // �ر���������
        if (_client != null)
        {
            _client.Close();
            _client = null;
        }
    }
}