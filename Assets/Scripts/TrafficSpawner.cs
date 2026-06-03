using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
public class TrafficSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject trafficPrefab;
    public TrafficPath trafficPath;

    public static TrafficSpawner Instance { get; private set; }    

    [Header("Camera Settings")]
    [Tooltip("Để trống, script sẽ tự động tìm Camera mà Cinemachine đang dùng để render.")]
    public Camera targetCamera;

    [Header("Spawn Settings")]
    public int maxTrafficCount = 10;
    public float idealSpawnRange = 35f;
    public float maxSpawnDistance = 50f;
    public float minSpawnDistance = 15f;
    public float despawnDistance = 60f;
    public float spawnCheckInterval = 0.5f;
    public int maxSpawnAttempts = 15;
    public List<GameObject> ActiveChaseCops => activeChaseCops;    

    [Header("Advanced")]
    public bool drawDebugGizmos = true;

    private List<GameObject> activeTraffic = new List<GameObject>();
    private float checkTimer = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }    
    void Start()
    {
        if (targetCamera == null)
            targetCamera = GetActiveCinemachineCamera();

        if (trafficPath == null) trafficPath = FindObjectOfType<TrafficPath>();
        if (trafficPrefab == null)
            Debug.LogError("[TrafficSpawner] trafficPrefab not assigned!");
        if (targetCamera == null)
            Debug.LogError("[TrafficSpawner] Could not find an active camera!");
    }

    void Update()
    {
        if (targetCamera == null) return;

        checkTimer += Time.deltaTime;
        if (checkTimer >= spawnCheckInterval)
        {
            checkTimer = 0f;
            ManageTraffic();
            ManagePatrolCops();
            ManageChaseCops();
        }
    }

    /// <summary>
    /// Lấy đúng Camera mà Cinemachine đang dùng để render.
    /// </summary>
    Camera GetActiveCinemachineCamera()
    {
        // Cách 1: Nếu có CinemachineBrain, lấy OutputCamera từ nó
        CinemachineBrain brain = Camera.main?.GetComponent<CinemachineBrain>();
        if (brain != null && brain.OutputCamera != null)
            return brain.OutputCamera;

        // Cách 2: Fallback, tìm camera có tag "MainCamera"
        return Camera.main;
    }

    void ManageTraffic()
    {
        if (targetCamera == null || trafficPath == null) return;

        // 1. Xóa những xe quá xa
        Vector3 cameraPos = targetCamera.transform.position;
        for (int i = activeTraffic.Count - 1; i >= 0; i--)
        {
            if (activeTraffic[i] == null)
            {
                activeTraffic.RemoveAt(i);
                continue;
            }
            float dist = Vector3.Distance(activeTraffic[i].transform.position, cameraPos);
            if (dist > despawnDistance)
            {
                Destroy(activeTraffic[i]);
                activeTraffic.RemoveAt(i);
            }
        }

        // 2. Spawn xe mới nếu chưa đủ số lượng
        if (activeTraffic.Count < maxTrafficCount)
            SpawnOneTraffic();
    }

    void SpawnOneTraffic()
    {
        if (targetCamera == null || trafficPath == null || trafficPrefab == null) return;

        Vector3 cameraPos = targetCamera.transform.position;
        WaypointNode spawnNode = null;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            // Lấy một waypoint ngẫu nhiên trong khoảng cách cho phép
            WaypointNode candidate = trafficPath.GetRandomSpawnPoint(cameraPos, minSpawnDistance, maxSpawnDistance);
            if (candidate == null) continue;

            // Kiểm tra vị trí này có an toàn để spawn không
            if (IsSpawnPositionValid(candidate.transform.position))
            {
                spawnNode = candidate;
                break;
            }
        }

        if (spawnNode == null) return;

        // Tiến hành spawn
        GameObject newTraffic = Instantiate(trafficPrefab, spawnNode.transform.position, spawnNode.transform.rotation);
        var ai = newTraffic.GetComponent<CarAIHandler>();
        if (ai != null)
        {
            ai.aiMode = CarAIHandler.AIMode.trafficMode;
            ai.trafficStartNode = spawnNode;
            // Xóa thành phần RaceParticipant nếu có, để tránh ảnh hưởng đến hệ thống đua
            var rp = newTraffic.GetComponent<RaceParticipant>();
            if (rp != null) Destroy(rp);
        }
        else
        {
            Debug.LogWarning("[TrafficSpawner] Traffic prefab is missing CarAIHandler component!");
        }

        activeTraffic.Add(newTraffic);
    }

    /// <summary>
    /// Kiểm tra tổng thể: vị trí spawn có nằm trong tầm nhìn camera không? Có bị trùng với xe khác không?
    /// </summary>
    bool IsSpawnPositionValid(Vector3 spawnPosition)
    {
        // 1. Kiểm tra có nằm trong khung hình camera không
        if (IsPointInsideCameraViewport(spawnPosition))
            return false;

        // 2. Kiểm tra có quá gần các xe traffic hiện có không
        foreach (var existingTraffic in activeTraffic)
        {
            if (existingTraffic != null && Vector3.Distance(existingTraffic.transform.position, spawnPosition) < 5f)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Sử dụng WorldToViewportPoint để xác định một điểm có nằm trong khung hình camera hay không.
    /// Điểm được xem là "trong khung hình" nếu tọa độ viewport của nó nằm trong khoảng [0,1].
    /// </summary>
    bool IsPointInsideCameraViewport(Vector3 worldPoint)
    {
        if (targetCamera == null) return false;

        // Chuyển đổi tọa độ thế giới sang tọa độ viewport (0 đến 1)
        Vector3 viewportPoint = targetCamera.WorldToViewportPoint(worldPoint);

        // Nếu điểm nằm phía sau camera (z < 0), thì cũng không thể nhìn thấy.
        if (viewportPoint.z < 0) return false;

        // Kiểm tra tọa độ x và y có nằm trong khoảng [0, 1] hay không.
        return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
               viewportPoint.y >= 0 && viewportPoint.y <= 1;
    }

    void OnDrawGizmos()
    {
        if (!drawDebugGizmos) return;
        if (targetCamera == null) return;

        Vector3 camPos = targetCamera.transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(camPos, idealSpawnRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(camPos, despawnDistance);
    }
    // ==================== COP SPAWNER INTEGRATION ====================
    [Header("Cop Spawner (Patrol & Chase)")]
    public GameObject copPatrolPrefab;
    public GameObject copChasePrefab;
    public int maxPatrolCops = 3;
    public int maxChaseCops = 4;
    public float patrolDespawnDistance = 50f;
    public float chaseDespawnDistance = 40f;
    public float patrolDetectRadius = 8f;

    private List<GameObject> activePatrolCops = new List<GameObject>();
    private List<GameObject> activeChaseCops = new List<GameObject>();
    private int desiredPatrolCount = 0;
    private int desiredChaseCount = 0;

    public void SetDesiredPatrolCount(int count)
    {
        desiredPatrolCount = Mathf.Clamp(count, 0, maxPatrolCops);
    }

    public void SetDesiredChaseCount(int count)
    {
        desiredChaseCount = Mathf.Clamp(count, 0, maxChaseCops);
    }
    public List<GameObject> GetActiveChaseCops() => activeChaseCops;

    public void DespawnAllCops()
    {
        foreach (var c in activePatrolCops) if (c != null) Destroy(c);
        foreach (var c in activeChaseCops) if (c != null) Destroy(c);
        activePatrolCops.Clear();
        activeChaseCops.Clear();
    }

    // Gọi các method này trong Update (cùng với ManageTraffic)
    private void ManagePatrolCops()
    {
        if (copPatrolPrefab == null) return;
        activePatrolCops.RemoveAll(c => c == null);
        Vector3 camPos = targetCamera.transform.position;

        // Despawn xa
        for (int i = activePatrolCops.Count - 1; i >= 0; i--)
        {
            if (Vector3.Distance(activePatrolCops[i].transform.position, camPos) > patrolDespawnDistance)
            {
                Destroy(activePatrolCops[i]);
                activePatrolCops.RemoveAt(i);
            }
        }

        // Spawn để đạt số lượng mong muốn
        while (activePatrolCops.Count < desiredPatrolCount)
            SpawnOnePatrolCop();

        while (activePatrolCops.Count > desiredPatrolCount)
        {
            var last = activePatrolCops[activePatrolCops.Count - 1];
            activePatrolCops.Remove(last);
            Destroy(last);
        }
    }

    private void SpawnOnePatrolCop()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player Racer");
        if (player == null) return;
        if (trafficPath == null || trafficPath.allTrafficWaypoints.Count == 0) return;

        WaypointNode spawnNode = null;
        for (int i = 0; i < 10; i++)
        {
            var candidate = trafficPath.GetRandomSpawnPoint(player.transform.position, minSpawnDistance, maxSpawnDistance);
            if (candidate != null) { spawnNode = candidate; break; }
        }
        if (spawnNode == null) return;

        GameObject newCop = Instantiate(copPatrolPrefab, spawnNode.transform.position, spawnNode.transform.rotation);
        var ai = newCop.GetComponent<CarAIHandler>();
        if (ai != null)
        {
            ai.aiMode = CarAIHandler.AIMode.trafficMode;
            ai.isCop = true;
            ai.detectionRadius = patrolDetectRadius;
            ai.trafficStartNode = spawnNode;
        }
        newCop.tag = "AI Cop";
        activePatrolCops.Add(newCop);
    }

    private void ManageChaseCops()
    {
        if (copChasePrefab == null) return;
        activeChaseCops.RemoveAll(c => c == null);
        GameObject player = GameObject.FindGameObjectWithTag("Player Racer");
        if (player == null) return;

        for (int i = activeChaseCops.Count - 1; i >= 0; i--)
        {
            if (Vector3.Distance(activeChaseCops[i].transform.position, player.transform.position) > chaseDespawnDistance)
            {
                // 👇 THÊM DÒNG NÀY TRƯỚC Destroy
                if (WantedSystem.Instance != null && WantedSystem.Instance.IsChaseActive)
                    WantedSystem.Instance.DecrementChasePool();

                Destroy(activeChaseCops[i]);
                activeChaseCops.RemoveAt(i);
            }
        }

        while (activeChaseCops.Count < desiredChaseCount)
            SpawnOneChaseCop();
    }

    private void SpawnOneChaseCop()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player Racer");
        if (player == null) return;
        if (trafficPath == null || trafficPath.allTrafficWaypoints.Count == 0) return;

        WaypointNode spawnNode = null;
        for (int i = 0; i < 10; i++)
        {
            var candidate = trafficPath.GetRandomSpawnPoint(player.transform.position, minSpawnDistance, maxSpawnDistance);
            if (candidate != null) { spawnNode = candidate; break; }
        }
        if (spawnNode == null) return;

        GameObject newCop = Instantiate(copChasePrefab, spawnNode.transform.position, spawnNode.transform.rotation);
        var ai = newCop.GetComponent<CarAIHandler>();
        if (ai != null)
        {
            ai.aiMode = CarAIHandler.AIMode.followPlayer;
            ai.isCop = true;
            // Có thể gán targetTransform để tránh tìm lại mỗi frame
            ai.targetTransform = player.transform;
        }
        newCop.tag = "AI Cop";
        activeChaseCops.Add(newCop);
    }
    public int GetCurrentChaseCount() => activeChaseCops.Count;

    public void ConvertAllPatrolToChase()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player Racer");
        for (int i = activePatrolCops.Count - 1; i >= 0; i--)
        {
            GameObject patrol = activePatrolCops[i];
            if (patrol == null) continue;
            CarAIHandler ai = patrol.GetComponent<CarAIHandler>();
            if (ai != null)
            {
                ai.aiMode = CarAIHandler.AIMode.followPlayer;
                ai.isCop = true;
                // Khôi phục tốc độ gốc của prefab (đã lưu trong originalMaxSpeed)
                ai.maxSpeed = ai.originalMaxSpeed;
                if (player != null) ai.targetTransform = player.transform;
            }
            activeChaseCops.Add(patrol);
            activePatrolCops.RemoveAt(i);
        }
        desiredPatrolCount = 0;
        desiredChaseCount = activeChaseCops.Count;
    }

    public void ForceDespawnAllCops()
    {
        // Xóa tất cả chase cop
        foreach (var cop in activeChaseCops)
            if (cop != null) Destroy(cop);
        activeChaseCops.Clear();
        // Xóa tất cả patrol cop
        foreach (var cop in activePatrolCops)
            if (cop != null) Destroy(cop);
        activePatrolCops.Clear();
        activeChaseCops.Clear();
        // Reset desired counts
        desiredChaseCount = 0;
        desiredPatrolCount = 0;
    }     
}