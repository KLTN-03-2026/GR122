using System.Linq;
using UnityEngine;

/// <summary>
/// Simple RacePath: gán các Waypoint (ordered) trong Inspector.
/// Cung cấp API GetDistanceAlongPath(worldPos) trả về distance (float) từ đầu path đến vị trí gần nhất trên path.
/// Hỗ trợ chế độ closedLoop (circuit) — nếu bật, sẽ coi đường là vòng kín (nối waypoint cuối -> waypoint đầu).
/// </summary>
public class RacePath : MonoBehaviour
{
    [Tooltip("Ordered list of waypoints for the racing line (first is start of path).")]
    public Transform[] waypoints;

    [Tooltip("If true, treat the path as a closed loop (adds a segment from last waypoint back to first).")]
    public bool closedLoop = true;

    // cumulative distances between waypoints (cumulative[i] = distance from waypoint[0] to waypoint[i])
    float[] cumulative;

    // cached total path length (includes closing segment if closedLoop)
    float cachedPathLength = 0f;
    bool cacheValid = false;

    void OnValidate()
    {
        Recalculate();
    }

    void Awake()
    {
        Recalculate();
    }

    public void Recalculate()
    {
        cacheValid = false;

        if (waypoints == null || waypoints.Length == 0)
        {
            cumulative = new float[0];
            cachedPathLength = 0f;
            cacheValid = true;
            return;
        }

        cumulative = new float[waypoints.Length];
        cumulative[0] = 0f;
        for (int i = 1; i < waypoints.Length; i++)
        {
            cumulative[i] = cumulative[i - 1] + Vector3.Distance(waypoints[i - 1].position, waypoints[i].position);
        }

        // compute cached total length (include closing segment if closedLoop and at least 2 waypoints)
        if (waypoints.Length >= 2 && closedLoop)
        {
            float closing = Vector3.Distance(waypoints[waypoints.Length - 1].position, waypoints[0].position);
            cachedPathLength = cumulative[cumulative.Length - 1] + closing;
        }
        else
        {
            cachedPathLength = cumulative[cumulative.Length - 1];
        }

        cacheValid = true;
    }

    /// <summary>
    /// Returns a continuous distance along the path to the nearest projection of worldPos on the polyline.
    /// If closedLoop==true the closing segment (last->first) is considered.
    /// If waypoints not set returns 0.
    /// </summary>
    public float GetDistanceAlongPath(Vector3 worldPos)
    {
        if (waypoints == null || waypoints.Length == 0) return 0f;
        if (!cacheValid || cumulative == null || cumulative.Length != waypoints.Length) Recalculate();

        float bestDistanceAlong = 0f;
        float bestSqr = float.MaxValue;

        int lastIndex = waypoints.Length - 1;

        // check segments i -> i+1
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            Vector3 a = waypoints[i].position;
            Vector3 b = waypoints[i + 1].position;
            Vector3 proj = NearestPointOnSegment(a, b, worldPos);
            float sq = (proj - worldPos).sqrMagnitude;
            if (sq < bestSqr)
            {
                bestSqr = sq;
                float segLen = Vector3.Distance(a, proj);
                bestDistanceAlong = cumulative[i] + segLen;
            }
        }

        // if closed loop, also check closing segment (last -> first)
        if (closedLoop && waypoints.Length >= 2)
        {
            Vector3 a = waypoints[lastIndex].position;
            Vector3 b = waypoints[0].position;
            Vector3 proj = NearestPointOnSegment(a, b, worldPos);
            float sq = (proj - worldPos).sqrMagnitude;
            if (sq < bestSqr)
            {
                bestSqr = sq;
                // segLen measured from last waypoint along closing segment
                float segLen = Vector3.Distance(a, proj);
                // distance along path = cumulative[lastIndex] + segLen
                bestDistanceAlong = cumulative[lastIndex] + segLen;
            }
        }

        // Edge cases: if single waypoint or only one point
        if (waypoints.Length == 1) bestDistanceAlong = 0f;

        // If closedLoop, it's possible bestDistanceAlong == cachedPathLength (when proj == first point),
        // we normalize to [0, pathLength) so that positions right at start map to 0.
        if (closedLoop && cachedPathLength > 0f)
        {
            if (bestDistanceAlong >= cachedPathLength) bestDistanceAlong = 0f;
        }

        return bestDistanceAlong;
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
    /// Full path length (distance from first to last waypoint along polyline).
    /// If closedLoop==true, includes closing segment (last -> first).
    /// </summary>
    public float GetPathLength()
    {
        if (!cacheValid || cumulative == null || cumulative.Length != waypoints.Length) Recalculate();
        return cachedPathLength;
    }
}
