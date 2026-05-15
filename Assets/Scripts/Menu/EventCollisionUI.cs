using UnityEngine;

/// <summary>
/// Robust EventCollisionUI:
/// - Chỉ player có tag "Player Racer" mới kích hoạt canvas.
/// - AI traffic dù có TopDownCarController cũng không ảnh hưởng.
/// </summary>
public class EventCollisionUI : MonoBehaviour
{
    [Tooltip("Canvas (or panel) to show when player is in trigger")]
    public GameObject eventCanvas;

    [Header("Optional: override tag check (default = \"Player Racer\")")]
    public string playerTag = "Player Racer";

    void Start()
    {
        if (eventCanvas != null)
            eventCanvas.SetActive(false);

        var col = GetComponent<Collider2D>();
        if (col == null)
            Debug.LogWarning("[EventCollisionUI] No Collider2D found on this GameObject. Add one and set Is Trigger = true.");
        else if (!col.isTrigger)
            Debug.LogWarning("[EventCollisionUI] Collider2D.isTrigger is false. Set it to true to receive trigger events.");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayerCollider(other))
        {
            if (eventCanvas != null)
                eventCanvas.SetActive(true);
            else
                Debug.LogWarning("[EventCollisionUI] eventCanvas not assigned.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayerCollider(other))
        {
            if (eventCanvas != null)
                eventCanvas.SetActive(false);
        }
    }

    bool IsPlayerCollider(Collider2D other)
    {
        if (other == null) return false;

        // Kiểm tra tag trực tiếp trên GameObject của collider
        if (other.gameObject.CompareTag(playerTag)) return true;

        // Kiểm tra trên rigidbody (nếu collider là con của player nhưng rigidbody ở parent)
        if (other.attachedRigidbody != null && other.attachedRigidbody.gameObject.CompareTag(playerTag)) return true;

        // Kiểm tra trên root transform (phòng trường hợp tag được gán ở prefab root)
        if (other.transform.root != null && other.transform.root.gameObject.CompareTag(playerTag)) return true;

        // KHÔNG dùng TopDownCarController để nhận diện, vì AI traffic cũng có component này
        return false;
    }
}