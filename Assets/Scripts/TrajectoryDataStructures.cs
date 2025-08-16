using UnityEngine; // �����ҪVector3��Unity���ͣ�������Բ�����
using System.Collections.Generic; // �����ҪList

// Ϊ����Python��Vector3���֣���������Ϊ Vector3_Serializable �� TrajectoryVector3
[System.Serializable]
public struct PositionData // ��ӦPython��Vector3����position
{
    public float x;
    public float y;
    public float z;
}

[System.Serializable]
public struct QuaternionData // ��ӦPython��Quaternion
{
    public float x;
    public float y;
    public float z;
    public float w;
}

[System.Serializable]
public struct EulerAnglesData // ��ӦPython��EulerAngles
{
    public float yaw;
    public float pitch;
    public float roll;
}

[System.Serializable]
public struct VelocityData // ��ӦPython��Vector3����velocity (���ʹ��)
{
    public float x;
    public float y;
    public float z;
}

[System.Serializable]
public class DroneTrajectoryPoint_Serializable // ��ӦPython��DroneTrajectoryPoint
{
    public int sequence_number;
    public PositionData position;
    public QuaternionData quaternion;
    public EulerAnglesData euler_angles;
    public double timestamp; // double ��Ӧ Python float
    public VelocityData velocity; // ��������˴��ֶ�
    // public float accuracy_position; // �����Ҫ
    // public float accuracy_orientation; // �����Ҫ

    // ���������������յ�������ת��ΪUnity��Vector3 (ע������ϵת����)
    public Vector3 GetUnityPosition()
    {
        // ����Python���͵���ENU (East, North, Up)
        // Unity ʹ�� LHS (Left-Hand System), Y-up
        // Unity X = East (Python Y)
        // Unity Y = Up   (Python Z)
        // Unity Z = North(Python X)
        return new Vector3(position.y, position.z, position.x);
    }

    public Quaternion GetUnityQuaternion()
    {
        // ��Ԫ������������ϵ����������ϵ��ת��ͨ���ǣ�
        // (x, y, z, w)_rhs -> (-x, -y, z, w)_lhs for ENU to Unity if Unity forward is Z and up is Y
        // ���߸�׼ȷ�أ���Ҫ���ݾ�������������ת����
        // һ��������ת�� (���Python����̬�Ǳ�׼�ĺ���ENU�µ���̬):
        // Unity X (Roll)   -> Python X (Roll)
        // Unity Y (Pitch)  -> -Python Y (Pitch)
        // Unity Z (Yaw)    -> -Python Z (Yaw)
        // ������Ԫ�� (qx, qy, qz, qw) ��Ӧ (qx, -qy, -qz, qw) �����Ƶ�����
        // ��ʾ����ʵ��Ӧ������Ҫ��ȷ��֤��
        return new Quaternion(-quaternion.y, -quaternion.z, -quaternion.x, quaternion.w);
        // ���ߣ������̬������ȫ�ֵģ�������ֻ�ǽ����λ��ת���ˣ�
        // ��ʱ����ֱ��ʹ����Ԫ��������Ҫ���ԣ�
        // return new Quaternion(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
    }
}

[System.Serializable]
public class TrajectoryStartData_Serializable // ��Ӧtrajectory_start��Ϣ��data����
{
    public string drone_id;
    public string trajectory_id;
    public string mission_id;
    public double start_time;
    public string coordinate_system;
    public string euler_angles_unit;
    public string euler_angles_convention;
    public string description;
    public int total_points;
}

[System.Serializable]
public class TrajectoryEndData_Serializable // ��Ӧtrajectory_end��Ϣ��data����
{
    public string trajectory_id;
}

// ���ڳ�������JSON���ж���Ϣ����
[System.Serializable]
public class MessageWrapper
{
    public string type;
    public string data; // data��������ΪԭʼJSON�ַ����������پ������
}