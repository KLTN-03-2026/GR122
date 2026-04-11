using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LapTrigger : MonoBehaviour
{
    [Tooltip("Owner RaceEventIcon that owns this finish line (set in Inspector)")]
    public RaceEventIcon owner;

    void Reset()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c != null) c.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (owner == null) return;

        // Try to find RaceParticipant on collider or parent
        var rp = other.GetComponent<RaceParticipant>();
        if (rp == null) rp = other.GetComponentInParent<RaceParticipant>();
        if (rp != null)
        {
            owner.OnLapTriggerHit(rp);
        }
    }
}
