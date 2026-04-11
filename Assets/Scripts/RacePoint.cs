using UnityEngine;

[DisallowMultipleComponent]
public class RacePoint : MonoBehaviour
{
    [Tooltip("Tag dùng để tìm player (mặc định: Player)")]
    public string playerTag = "Player";

    [Tooltip("Góc Z (degrees) muốn áp dụng cho player khi teleport tới đây")]
    public float zRotation = 0f;

    [Tooltip("Offset vị trí so với transform (nếu cần)")]
    public Vector3 localOffset = Vector3.zero;

    public Vector3 WorldPosition => transform.position + transform.TransformVector(localOffset);
    public Quaternion RotationQuaternion => Quaternion.Euler(0f, 0f, zRotation);
}
