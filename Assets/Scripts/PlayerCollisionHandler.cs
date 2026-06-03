using UnityEngine;
using System.Collections.Generic;

public class PlayerCollisionHandler : MonoBehaviour
{
    [Header("Collision Cooldown")]
    [Tooltip("Khoảng thời gian tối thiểu giữa các lần tính collision với cùng một object (giây)")]
    public float collisionCooldown = 0.5f;

    // Lưu thời điểm va chạm gần nhất với từng GameObject
    private Dictionary<GameObject, float> lastCollisionTime = new Dictionary<GameObject, float>();

    private void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject other = collision.gameObject;

        // Kiểm tra cooldown
        if (lastCollisionTime.ContainsKey(other))
        {
            float timeSinceLast = Time.time - lastCollisionTime[other];
            if (timeSinceLast < collisionCooldown)
            {
                // Bỏ qua, không tính collision này
                Debug.Log($"[COLLISION DEBUG] Ignored {other.name} (only {timeSinceLast:F2}s since last)");
                return;
            }
        }

        // Cập nhật thời gian va chạm
        lastCollisionTime[other] = Time.time;

        // Log chi tiết (có thể bỏ sau khi kiểm tra)
        Debug.Log($"[COLLISION DEBUG] Counted! Time={Time.time} | Object={other.name} | ContactCount={collision.contactCount}");

        // Gọi hệ thống Wanted
        WantedSystem.Instance?.OnPlayerCollision();
    }

    // Dọn dẹp dictionary khi object bị hủy (tránh rò rỉ bộ nhớ)
    private void OnDestroy()
    {
        lastCollisionTime.Clear();
    }
}