using System.Linq;
using UnityEngine;

/// <summary>
/// RacePath: quản lý hai đường đi cho AI:
/// - waypoints: đường đua chính (rẽ sớm, dùng cho cạnh tranh)
/// - recoverWaypoints: đường đua phụ (rẽ đúng tại ngã rẽ, dùng khi AI cần hồi phục)
/// Cả hai đều là mảng Transform chứa các GameObject có component WaypointNode.
/// </summary>
public class RacePath : MonoBehaviour
{
    [Tooltip("Ordered list of waypoints for the main racing line (early turning).")]
    public Transform[] waypoints;

    [Tooltip("Ordered list of waypoints for the recovery line (turn exactly at intersection).")]
    public Transform[] recoverWaypoints;

    [Tooltip("If true, treat the path as a closed loop (adds a segment from last waypoint back to first).")]
    public bool closedLoop = true;

    // cumulative distances for normal path
    float[] cumulativeNormal;
    float cachedNormalLength = 0f;

    // cumulative distances for recover path
    float[] cumulativeRecover;
    float cachedRecoverLength = 0f;

    bool cacheValid = false;

    void OnValidate() => Recalculate();
    void Awake() => Recalculate();

    public void Recalculate()
    {
        cacheValid = false;

        // Normal path
        if (waypoints != null && waypoints.Length > 0)
        {
            cumulativeNormal = new float[waypoints.Length];
            cumulativeNormal[0] = 0f;
            for (int i = 1; i < waypoints.Length; i++)
            {
                cumulativeNormal[i] = cumulativeNormal[i - 1] + Vector3.Distance(waypoints[i - 1].position, waypoints[i].position);
            }
            if (waypoints.Length >= 2 && closedLoop)
            {
                float closing = Vector3.Distance(waypoints[waypoints.Length - 1].position, waypoints[0].position);
                cachedNormalLength = cumulativeNormal[cumulativeNormal.Length - 1] + closing;
            }
            else
            {
                cachedNormalLength = cumulativeNormal[cumulativeNormal.Length - 1];
            }
        }
        else
        {
            cumulativeNormal = new float[0];
            cachedNormalLength = 0f;
        }

        // Recover path
        if (recoverWaypoints != null && recoverWaypoints.Length > 0)
        {
            cumulativeRecover = new float[recoverWaypoints.Length];
            cumulativeRecover[0] = 0f;
            for (int i = 1; i < recoverWaypoints.Length; i++)
            {
                cumulativeRecover[i] = cumulativeRecover[i - 1] + Vector3.Distance(recoverWaypoints[i - 1].position, recoverWaypoints[i].position);
            }
            if (recoverWaypoints.Length >= 2 && closedLoop)
            {
                float closing = Vector3.Distance(recoverWaypoints[recoverWaypoints.Length - 1].position, recoverWaypoints[0].position);
                cachedRecoverLength = cumulativeRecover[cumulativeRecover.Length - 1] + closing;
            }
            else
            {
                cachedRecoverLength = cumulativeRecover[cumulativeRecover.Length - 1];
            }
        }
        else
        {
            cumulativeRecover = new float[0];
            cachedRecoverLength = 0f;
        }

        cacheValid = true;
    }

    /// <summary>Lấy danh sách WaypointNode từ mảng Transform (normal hoặc recover).</summary>
    public WaypointNode[] GetWaypointNodes(Transform[] source)
    {
        if (source == null || source.Length == 0) return new WaypointNode[0];
        return source.Select(t => t != null ? t.GetComponent<WaypointNode>() : null)
                     .Where(w => w != null)
                     .ToArray();
    }

    public WaypointNode[] NormalWaypointNodes => GetWaypointNodes(waypoints);
    public WaypointNode[] RecoverWaypointNodes => GetWaypointNodes(recoverWaypoints);

    public float GetPathLength(bool useRecover = false)
    {
        if (!cacheValid) Recalculate();
        return useRecover ? cachedRecoverLength : cachedNormalLength;
    }

    public float GetDistanceAlongPath(Vector3 worldPos, bool useRecover = false)
    {
        Transform[] points = useRecover ? recoverWaypoints : waypoints;
        float[] cum = useRecover ? cumulativeRecover : cumulativeNormal;
        float totalLen = useRecover ? cachedRecoverLength : cachedNormalLength;

        if (points == null || points.Length == 0) return 0f;
        if (!cacheValid || cum == null || cum.Length != points.Length) Recalculate();

        float bestDistance = 0f;
        float bestSqr = float.MaxValue;
        int lastIndex = points.Length - 1;

        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 a = points[i].position;
            Vector3 b = points[i + 1].position;
            Vector3 proj = NearestPointOnSegment(a, b, worldPos);
            float sq = (proj - worldPos).sqrMagnitude;
            if (sq < bestSqr)
            {
                bestSqr = sq;
                float segLen = Vector3.Distance(a, proj);
                bestDistance = cum[i] + segLen;
            }
        }

        if (closedLoop && points.Length >= 2)
        {
            Vector3 a = points[lastIndex].position;
            Vector3 b = points[0].position;
            Vector3 proj = NearestPointOnSegment(a, b, worldPos);
            float sq = (proj - worldPos).sqrMagnitude;
            if (sq < bestSqr)
            {
                bestSqr = sq;
                float segLen = Vector3.Distance(a, proj);
                bestDistance = cum[lastIndex] + segLen;
            }
        }

        if (points.Length == 1) bestDistance = 0f;

        if (closedLoop && totalLen > 0f && bestDistance >= totalLen) bestDistance = 0f;
        return bestDistance;
    }

    static Vector3 NearestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector3 ab = b - a;
        float ab2 = Vector3.Dot(ab, ab);
        if (ab2 == 0f) return a;
        float t = Vector3.Dot(p - a, ab) / ab2;
        t = Mathf.Clamp01(t);
        return a + ab * t;
    }

    /// <summary>
    /// Tìm index của waypoint tiếp theo trên normal path dựa vào vị trí hiện tại.
    /// Trả về -1 nếu không tìm thấy (khi xe đã ở sau waypoint cuối).
    /// </summary>
    public int GetNextWaypointIndex(Vector3 worldPos)
    {
        if (waypoints == null || waypoints.Length == 0) return -1;
        float currentDist = GetDistanceAlongPath(worldPos, false);
        // Tìm waypoint đầu tiên có cumulative distance > currentDist (chặn trước)
        for (int i = 0; i < cumulativeNormal.Length; i++)
        {
            if (cumulativeNormal[i] > currentDist + 0.1f)
                return i;
        }
        // Nếu không tìm thấy (ở gần cuối hoặc cuối path), trả về waypoint cuối cùng
        return waypoints.Length - 1;
    }

    /// <summary>
    /// Lấy WaypointNode tiếp theo trên normal path, dựa vào vị trí hiện tại.
    /// </summary>
    public WaypointNode GetNextNormalWaypoint(Vector3 worldPos)
    {
        int idx = GetNextWaypointIndex(worldPos);
        if (idx < 0 || idx >= waypoints.Length) return null;
        return waypoints[idx]?.GetComponent<WaypointNode>();
    }
}