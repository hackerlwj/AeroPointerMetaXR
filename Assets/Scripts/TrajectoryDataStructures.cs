using UnityEngine; // 如果需要Vector3等Unity类型，否则可以不引用
using System.Collections.Generic; // 如果需要List

// 为了与Python的Vector3区分，可以命名为 Vector3_Serializable 或 TrajectoryVector3
[System.Serializable]
public struct PositionData // 对应Python的Vector3用于position
{
    public float x;
    public float y;
    public float z;
}

[System.Serializable]
public struct QuaternionData // 对应Python的Quaternion
{
    public float x;
    public float y;
    public float z;
    public float w;
}

[System.Serializable]
public struct EulerAnglesData // 对应Python的EulerAngles
{
    public float yaw;
    public float pitch;
    public float roll;
}

[System.Serializable]
public struct VelocityData // 对应Python的Vector3用于velocity (如果使用)
{
    public float x;
    public float y;
    public float z;
}

[System.Serializable]
public class DroneTrajectoryPoint_Serializable // 对应Python的DroneTrajectoryPoint
{
    public int sequence_number;
    public PositionData position;
    public QuaternionData quaternion;
    public EulerAnglesData euler_angles;
    public double timestamp; // double 对应 Python float
    public VelocityData velocity; // 如果发送了此字段
    // public float accuracy_position; // 如果需要
    // public float accuracy_orientation; // 如果需要

    // 辅助方法：将接收到的数据转换为Unity的Vector3 (注意坐标系转换！)
    public Vector3 GetUnityPosition()
    {
        // 假设Python发送的是ENU (East, North, Up)
        // Unity 使用 LHS (Left-Hand System), Y-up
        // Unity X = East (Python Y)
        // Unity Y = Up   (Python Z)
        // Unity Z = North(Python X)
        return new Vector3(position.y, position.z, position.x);
    }

    public Quaternion GetUnityQuaternion()
    {
        // 四元数从右手坐标系到左手坐标系的转换通常是：
        // (x, y, z, w)_rhs -> (-x, -y, z, w)_lhs for ENU to Unity if Unity forward is Z and up is Y
        // 或者更准确地，需要根据具体的轴向定义进行转换。
        // 一个常见的转换 (如果Python的姿态是标准的航空ENU下的姿态):
        // Unity X (Roll)   -> Python X (Roll)
        // Unity Y (Pitch)  -> -Python Y (Pitch)
        // Unity Z (Yaw)    -> -Python Z (Yaw)
        // 所以四元数 (qx, qy, qz, qw) 对应 (qx, -qy, -qz, qw) 或类似调整。
        // 简单示例，实际应用中需要精确验证：
        return new Quaternion(-quaternion.y, -quaternion.z, -quaternion.x, quaternion.w);
        // 或者，如果姿态本身是全局的，并且你只是将点的位置转换了，
        // 有时可以直接使用四元数，但需要测试：
        // return new Quaternion(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
    }
}

[System.Serializable]
public class TrajectoryStartData_Serializable // 对应trajectory_start消息的data部分
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
public class TrajectoryEndData_Serializable // 对应trajectory_end消息的data部分
{
    public string trajectory_id;
}

// 用于初步解析JSON，判断消息类型
[System.Serializable]
public class MessageWrapper
{
    public string type;
    public string data; // data部分先作为原始JSON字符串，后续再具体解析
}