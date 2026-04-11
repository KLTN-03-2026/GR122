using UnityEngine;

/// <summary>
/// Attach this to each checkpoint GameObject (with a Collider2D set as trigger).
/// The RaceEventIcon will register as owner and will handle the logic when a participant triggers the checkpoint.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Index in the race sequence (0-based). Order must match the RaceEventIcon.checkpoints array.")]
    public int index = 0;

    [HideInInspector] public RaceEventIcon owner;

    void Reset()
    {
        var c = GetComponent<Collider2D>();
        if (c != null) c.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (owner == null) return;

        // try get RaceParticipant from collider or parent
        RaceParticipant rp = other.GetComponent<RaceParticipant>() ?? other.GetComponentInParent<RaceParticipant>();
        if (rp != null)
        {
            owner.OnCheckpointTriggered(this, rp);
        }
    }
}
