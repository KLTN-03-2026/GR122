using UnityEngine;
using System;

public class RaceParticipant : MonoBehaviour
{
    [HideInInspector] public RaceEventIcon raceController; // set by RaceEventIcon when spawning
    [HideInInspector] public bool isPlayer = false;

    [Tooltip("Current lap count (0-based internally).")]
    public int currentLap = 0;

    public event Action<RaceParticipant> OnLapCompleted;

    // --- debounce / safety ---
    [Tooltip("Seconds to ignore repeated finish crosses (prevents multiple counts while inside trigger).")]
    public float crossCooldown = 0.8f;

    [Tooltip("Minimum speed (units/s) required to count a crossing. Helps avoid counting when stopped on the line.")]
    public float minSpeedToCount = 0.5f;

    // Per-participant checkpoint progression:
    // direction = 0 -> not started; +1 forward; -1 backward
    [HideInInspector] public int direction = 0;
    [HideInInspector] public int nextCheckpointIndex = 0;

    float lastCrossTime = -Mathf.Infinity;

    /// <summary>
    /// Reset checkpoint progress when race starts / after finishing a lap.
    /// </summary>
    public void ResetCheckpointProgress()
    {
        direction = 0;
        nextCheckpointIndex = 0;
        lastCrossTime = -Mathf.Infinity;
    }

    /// <summary>
    /// Called by RaceEventIcon when this participant successfully hits the finish line and requirements met.
    /// This method ensures debounce & speed checks and increments lap.
    /// Returns true if lap was counted.
    /// </summary>
    public bool TryCompleteLap()
    {
        // debounce
        if (Time.time - lastCrossTime < crossCooldown) return false;

        // check speed
        Rigidbody2D rb = GetComponent<Rigidbody2D>() ?? GetComponentInParent<Rigidbody2D>();
        if (rb != null)
        {
            if (rb.linearVelocity.magnitude < minSpeedToCount) return false;
        }
        else
        {
            var tdc = GetComponent<TopDownCarController>() ?? GetComponentInParent<TopDownCarController>();
            if (tdc != null)
            {
                // attempt to call GetVelocityMagnitude if exists
                try
                {
                    float vm = tdc.GetVelocityMagnitude();
                    if (vm < minSpeedToCount) return false;
                }
                catch { }
            }
        }

        // accept lap
        lastCrossTime = Time.time;
        currentLap++;
        OnLapCompleted?.Invoke(this);

        // Reset checkpoint progression for next lap
        ResetCheckpointProgress();

        // notify controller if present
        if (raceController != null && currentLap >= raceController.totalLaps)
        {
            raceController.OnParticipantFinished(this);
        }

        return true;
    }

    /// <summary>
    /// Convenience method to compute progress score used for ordering position.
    /// </summary>
    public float GetProgressScore()
    {
        if (raceController == null || raceController.racePath == null) return currentLap;
        float distance = raceController.racePath.GetDistanceAlongPath(transform.position);
        // use path length + distance to combine
        return currentLap * (raceController.racePath.GetPathLength() + 1f) + distance;
    }

    public void ClearAllSubscribers()
    {
        OnLapCompleted = null;
    }
}
