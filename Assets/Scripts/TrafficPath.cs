using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Quản lý toàn bộ waypoint traffic trong thế giới mở.
/// Mỗi waypoint có thể có nhiều next waypoint (cho phép rẽ).
/// </summary>
public class TrafficPath : MonoBehaviour
{
    [Header("Waypoint nodes (children có component WaypointNode)")]
    public Transform waypointsRoot; // GameObject chứa tất cả waypoint traffic

    [Header("Spawn settings")]
    public float spawnCheckInterval = 0.5f;      // tần suất kiểm tra spawn/despawn
    public float minSpawnDistanceFromPlayer = 15f; // khoảng cách tối thiểu từ player để spawn (phải > camera range)
    public float maxSpawnDistanceFromPlayer = 30f; // spawn trong vòng này
    public float despawnDistance = 40f;            // despawn khi xa hơn

    [Header("Runtime")]
    public List<WaypointNode> allTrafficWaypoints = new List<WaypointNode>();

    void Awake()
    {
        CollectWaypoints();
    }

    void CollectWaypoints()
    {
        allTrafficWaypoints.Clear();
        if (waypointsRoot != null)
        {
            allTrafficWaypoints.AddRange(waypointsRoot.GetComponentsInChildren<WaypointNode>());
        }
        else
        {
            // Fallback: tìm tất cả WaypointNode có tag "Traffic" (gán tag)
            allTrafficWaypoints.AddRange(FindObjectsOfType<WaypointNode>().Where(w => w.CompareTag("Traffic")));
        }
        Debug.Log($"[TrafficPath] Collected {allTrafficWaypoints.Count} traffic waypoints.");
    }

    /// <summary>
    /// Lấy một waypoint ngẫu nhiên trong phạm vi quanh player, thích hợp để spawn.
    /// </summary>
    public WaypointNode GetRandomSpawnPoint(Vector3 playerPos, float minRange, float maxRange)
    {
        if (allTrafficWaypoints.Count == 0) return null;
        var candidates = allTrafficWaypoints.Where(w =>
        {
            float dist = Vector3.Distance(w.transform.position, playerPos);
            return dist >= minRange && dist <= maxRange;
        }).ToList();
        if (candidates.Count == 0)
        {
            // fallback: lấy waypoint xa nhất nhưng vẫn trong maxRange
            candidates = allTrafficWaypoints.Where(w => Vector3.Distance(w.transform.position, playerPos) <= maxRange).ToList();
            if (candidates.Count == 0) candidates = allTrafficWaypoints;
        }
        return candidates[Random.Range(0, candidates.Count)];
    }

    /// <summary>
    /// Lấy danh sách waypoint trong phạm vi quanh player (dùng để kiểm tra tồn tại)
    /// </summary>
    public List<WaypointNode> GetWaypointsInRange(Vector3 center, float radius)
    {
        return allTrafficWaypoints.Where(w => Vector3.Distance(w.transform.position, center) <= radius).ToList();
    }
}