using UnityEngine;

/// <summary>
/// Robust EventCollisionUI:
/// - Accept trigger on Event Icon (this script should be on Event Icon with Collider2D (isTrigger = true))
/// - Detect player even when tag is on root or on Rigidbody object or on collider child.
/// - Safe null checks and debug logs to help trace problems.
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

        // sanity warning
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
            {
                eventCanvas.SetActive(true);
            }
            else Debug.LogWarning("[EventCollisionUI] eventCanvas not assigned.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayerCollider(other))
        {
            if (eventCanvas != null)
            {
                eventCanvas.SetActive(false);
            }
        }
    }

    bool IsPlayerCollider(Collider2D other)
    {
        if (other == null) return false;

        // 1) check the collider's GameObject directly
        if (other.gameObject.CompareTag(playerTag)) return true;

        // 2) check attachedRigidbody's GameObject (if collider is part of child and rigidbody on parent)
        if (other.attachedRigidbody != null && other.attachedRigidbody.gameObject.CompareTag(playerTag)) return true;

        // 3) check root transform (if tag is on top-level prefab root)
        if (other.transform.root != null && other.transform.root.gameObject.CompareTag(playerTag)) return true;

        // 4) fallback: try to detect player by common component name (TopDownCarController)
        var comp = other.GetComponentInParent<MonoBehaviour>();
        if (comp != null)
        {
            // crude check: if root has TopDownCarController component
            var tdc = other.GetComponentInParent<TopDownCarController>();
            if (tdc != null) return true;
        }

        // optional debug (uncomment if you need more info)
        // Debug.Log("[EventCollisionUI] Trigger hit by: " + other.gameObject.name + " (root: " + other.transform.root.name + ")");

        return false;
    }
}
