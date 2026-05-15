using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CarAIHandler : MonoBehaviour
{
    public enum AIMode { followPlayer, followWaypoints, followMouse, trafficMode };
    public enum PathMode { Normal, Recover }
    [Header("Cop Detection (for Patrol mode)")]
    public float detectionRadius = 10f;      // Khoảng cách phát hiện Player
    private bool hasReportedDetection = false;    

    [Header("AI settings")]
    public AIMode aiMode;
    public float maxSpeed = 16;
    public float originalMaxSpeed;    
    public bool isAvoidingCars = true;
    [Range(0.0f, 1.0f)]
    public float skillLevel = 1.0f;

    [Header("Cop settings")]
    public bool isCop = false;
    

    [Header("Recovery settings")]
    public RacePath racePath;
    public float recoverBackwardDuration = 2.5f;
    public float recoverPathDuration = 5.0f;

    [Header("Stuck detection")]
    public float stuckCheckInterval = 1.0f;
    public float stuckPositionThreshold = 0.5f;
    public float stuckRotationThreshold = 5.0f;
    public int stuckRequiredCount = 3;

    [Header("Traffic Mode")]
    public WaypointNode trafficStartNode;     // waypoint spawn lần đầu
    public float trafficMinSpeed = 5f;
    public float trafficMaxSpeed = 12f;
    public float trafficTurnSpeed = 2.5f;

    // Local variables
    Vector3 targetPosition = Vector3.zero;
    public Transform targetTransform = null;
    float originalMaximumSpeed = 0;

    // Stuck handling mới
    Vector3 lastPosition;
    float lastRotationZ;
    float stuckTimer = 0f;
    int stuckCount = 0;
    bool isCheckingStuck = true;

    // Temporary waypoints (cho stuck cũ - giữ để tương thích)
    bool isRunningStuckCheck = false;
    bool isFirstTemporaryWaypoint = false;
    List<Vector2> temporaryWaypoints = new List<Vector2>();
    float angleToTarget = 0;

    // Avoidance
    Vector2 avoidanceVectorLerped = Vector3.zero;

    // Waypoints
    PathMode currentPathMode = PathMode.Normal;
    WaypointNode[] normalWaypoints;
    WaypointNode[] recoverWaypoints;
    WaypointNode currentWaypoint = null;
    WaypointNode previousWaypoint = null;

    // Recovery state
    bool isRecovering = false;
    float recoverTimer = 0f;
    bool isBackingUp = false;
    float backupTimer = 0f;

    // Traffic specific
    bool isTrafficModeInitialized = false;

    // Components
    PolygonCollider2D polygonCollider2D;
    TopDownCarController topDownCarController;
    AStarLite aStarLite;

    void Awake()
    {
        originalMaxSpeed = maxSpeed;
        topDownCarController = GetComponent<TopDownCarController>();
        aStarLite = GetComponent<AStarLite>();
        polygonCollider2D = GetComponentInChildren<PolygonCollider2D>();
        originalMaximumSpeed = maxSpeed;

        if (racePath == null && aiMode != AIMode.trafficMode)
            racePath = FindObjectOfType<RacePath>();

        if (racePath != null && aiMode != AIMode.trafficMode)
        {
            normalWaypoints = racePath.NormalWaypointNodes;
            recoverWaypoints = racePath.RecoverWaypointNodes;
        }
        else
        {
            normalWaypoints = FindObjectsOfType<WaypointNode>();
            recoverWaypoints = new WaypointNode[0];
        }
    }

    void Start()
    {
        SetMaxSpeedBasedOnSkillLevel(maxSpeed);
        if (aiMode == AIMode.trafficMode)
        {
            InitializeTrafficMode();
        }
        else
        {
            if (normalWaypoints != null && normalWaypoints.Length > 0)
                currentWaypoint = FindClosestWayPoint(normalWaypoints);
        }
        lastPosition = transform.position;
        lastRotationZ = transform.rotation.eulerAngles.z;
        stuckTimer = stuckCheckInterval;
    }

    void InitializeTrafficMode()
    {
        if (trafficStartNode != null)
        {
            currentWaypoint = trafficStartNode;
            previousWaypoint = currentWaypoint;
            // Tốc độ ngẫu nhiên
            maxSpeed = Random.Range(trafficMinSpeed, trafficMaxSpeed);
            originalMaximumSpeed = maxSpeed;
            // Điều chỉnh turn factor (tuỳ chọn)
            if (topDownCarController != null)
                topDownCarController.turnFactor = trafficTurnSpeed;
        }
        else
        {
            Debug.LogWarning("[CarAIHandler] Traffic mode but no trafficStartNode assigned!");
            enabled = false;
        }
        isTrafficModeInitialized = true;
    }


    void FixedUpdate()
    {
        Vector2 inputVector = Vector2.zero;
        if (aiMode == AIMode.trafficMode && isCop && !hasReportedDetection && WantedSystem.Instance != null && !WantedSystem.Instance.IsChaseActive)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player Racer");
            if (player != null)
            {
                float dist = Vector3.Distance(transform.position, player.transform.position);
                if (dist <= detectionRadius)
                {
                    hasReportedDetection = true;
                    Debug.Log($"[CarAIHandler] Patrol cop detected player! Distance={dist}. Calling OnPatrolDetectedPlayer.");
                    WantedSystem.Instance.OnPatrolDetectedPlayer();
                }
            }
        }
        if (!isRecovering && isCheckingStuck && aiMode != AIMode.trafficMode)
            CheckStuckByTransform();

        if (isRecovering)
        {
            HandleRecovery();
            if (currentWaypoint != null)
                targetPosition = currentWaypoint.transform.position;
        }
        else
        {
            switch (aiMode)
            {
                case AIMode.followPlayer:
                    FollowPlayer();
                    break;
                case AIMode.followWaypoints:
                    if (temporaryWaypoints.Count == 0)
                        FollowWaypoints();
                    else
                        FollowTemporaryWayPoints();
                    break;
                case AIMode.followMouse:
                    FollowMousePosition();
                    break;
                case AIMode.trafficMode:
                    FollowTrafficPath();
                    break;
            }
        }

        inputVector.x = TurnTowardTarget();
        inputVector.y = ApplyThrottleOrBrake(inputVector.x);

        topDownCarController.SetInputVector(inputVector);
    }

    #region Stuck Detection
    void CheckStuckByTransform()
    {
        stuckTimer -= Time.fixedDeltaTime;
        if (stuckTimer <= 0f)
        {
            stuckTimer = stuckCheckInterval;

            Vector3 currentPos = transform.position;
            float currentRotZ = transform.rotation.eulerAngles.z;
            float deltaPos = Vector3.Distance(currentPos, lastPosition);
            float deltaRot = Mathf.Abs(Mathf.DeltaAngle(currentRotZ, lastRotationZ));

            if (deltaPos < stuckPositionThreshold && deltaRot < stuckRotationThreshold)
            {
                stuckCount++;
                if (stuckCount >= stuckRequiredCount && !isRecovering)
                {
                    StartRecovery();
                    stuckCount = 0;
                }
            }
            else
            {
                stuckCount = 0;
            }

            lastPosition = currentPos;
            lastRotationZ = currentRotZ;
        }
    }
    #endregion

    #region Recovery Logic
    void HandleRecovery()
    {
        if (isBackingUp)
        {
            backupTimer -= Time.fixedDeltaTime;
            if (backupTimer <= 0f)
            {
                isBackingUp = false;
                SwitchToRecoverPath();
            }
            else
            {
                Vector3 backwardTarget = transform.position - transform.up * 3f;
                targetPosition = backwardTarget;
            }
            return;
        }

        recoverTimer -= Time.fixedDeltaTime;
        if (recoverTimer <= 0f)
        {
            ExitRecovery();
        }
        else
        {
            if (currentWaypoint == null && recoverWaypoints.Length > 0)
                currentWaypoint = FindClosestWayPoint(recoverWaypoints);
            FollowWaypoints(); // dùng logic waypoint bình thường
        }
    }

    void StartRecovery()
    {
        if (isRecovering) return;
        isRecovering = true;
        isBackingUp = true;
        backupTimer = recoverBackwardDuration;
        temporaryWaypoints.Clear();
        if (isRunningStuckCheck) StopCoroutine(StuckCheckCO());
        isRunningStuckCheck = false;
        stuckCount = 0;
    }

    void SwitchToRecoverPath()
    {
        currentPathMode = PathMode.Recover;
        if (recoverWaypoints != null && recoverWaypoints.Length > 0)
        {
            currentWaypoint = FindClosestWayPoint(recoverWaypoints);
            previousWaypoint = currentWaypoint;
        }
        recoverTimer = recoverPathDuration;
    }

    void ExitRecovery()
    {
        isRecovering = false;
        isBackingUp = false;
        currentPathMode = PathMode.Normal;

        if (normalWaypoints != null && normalWaypoints.Length > 0 && racePath != null)
        {
            WaypointNode next = racePath.GetNextNormalWaypoint(transform.position);
            if (next != null)
            {
                currentWaypoint = next;
                previousWaypoint = currentWaypoint;
            }
            else
            {
                currentWaypoint = FindClosestWayPoint(normalWaypoints);
                previousWaypoint = currentWaypoint;
            }
        }
        else if (normalWaypoints != null && normalWaypoints.Length > 0)
        {
            currentWaypoint = FindClosestWayPoint(normalWaypoints);
            previousWaypoint = currentWaypoint;
        }
        else
        {
            currentWaypoint = null;
        }

        stuckCount = 0;
        lastPosition = transform.position;
        lastRotationZ = transform.rotation.eulerAngles.z;
        stuckTimer = stuckCheckInterval;
    }
    #endregion

    #region AI Behaviors
    void FollowPlayer()
    {
        if (isCop)
        {
            if (targetTransform == null)
            {
                if (CopTargetManager.Instance != null)
                {
                    Transform assigned = CopTargetManager.Instance.AssignTarget(this);
                    if (assigned != null)
                    {
                        targetTransform = assigned;
                        targetPosition = targetTransform.position;
                    }
                }
                else
                {
                    var p = GameObject.FindGameObjectWithTag("Player");
                    if (p != null) targetTransform = p.transform;
                }
            }
            else
            {
                if (!targetTransform.gameObject.activeInHierarchy)
                {
                    CopTargetManager.Instance?.ReleaseTarget(targetTransform, this);
                    targetTransform = null;
                }
                else
                {
                    targetPosition = targetTransform.position;
                }
            }
            return;
        }

        if (targetTransform == null)
            targetTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (targetTransform != null)
            targetPosition = targetTransform.position;
    }

    void FollowWaypoints()
    {
        WaypointNode[] activeWaypoints = (currentPathMode == PathMode.Normal) ? normalWaypoints : recoverWaypoints;
        if (activeWaypoints == null || activeWaypoints.Length == 0) return;

        if (currentWaypoint == null)
        {
            currentWaypoint = FindClosestWayPoint(activeWaypoints);
            previousWaypoint = currentWaypoint;
        }

        if (currentWaypoint != null)
        {
            targetPosition = currentWaypoint.transform.position;
            float distanceToWayPoint = (targetPosition - transform.position).magnitude;

            if (distanceToWayPoint <= currentWaypoint.minDistanceToReachWaypoint)
            {
                if (currentWaypoint.maxSpeed > 0)
                    SetMaxSpeedBasedOnSkillLevel(currentWaypoint.maxSpeed);
                else
                    SetMaxSpeedBasedOnSkillLevel(1000);

                previousWaypoint = currentWaypoint;
                if (currentWaypoint.nextWaypointNode != null && currentWaypoint.nextWaypointNode.Length > 0)
                    currentWaypoint = currentWaypoint.nextWaypointNode[Random.Range(0, currentWaypoint.nextWaypointNode.Length)];
                else
                    currentWaypoint = null;
            }
        }
    }

    void FollowTemporaryWayPoints()
    {
        if (temporaryWaypoints.Count == 0) return;
        targetPosition = temporaryWaypoints[0];
        float distanceToWayPoint = (targetPosition - transform.position).magnitude;
        SetMaxSpeedBasedOnSkillLevel(5);
        float minDistanceToReachWaypoint = isFirstTemporaryWaypoint ? 3.0f : 1.5f;
        if (distanceToWayPoint <= minDistanceToReachWaypoint)
        {
            temporaryWaypoints.RemoveAt(0);
            isFirstTemporaryWaypoint = false;
        }
    }

    void FollowMousePosition()
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        targetPosition = worldPosition;
    }

    void FollowTrafficPath()
    {
        if (!isTrafficModeInitialized || currentWaypoint == null) return;

        targetPosition = currentWaypoint.transform.position;
        float distance = (targetPosition - transform.position).magnitude;

        if (distance <= currentWaypoint.minDistanceToReachWaypoint)
        {
            if (currentWaypoint.nextWaypointNode != null && currentWaypoint.nextWaypointNode.Length > 0)
            {
                // Chọn ngẫu nhiên một next waypoint
                int r = Random.Range(0, currentWaypoint.nextWaypointNode.Length);
                WaypointNode next = currentWaypoint.nextWaypointNode[r];
                if (next != null)
                {
                    previousWaypoint = currentWaypoint;
                    currentWaypoint = next;
                }
                else
                {
                    // Fallback: giữ waypoint hiện tại (sẽ bị kẹt, nhưng hiếm)
                    Debug.LogWarning($"[Traffic] Null next waypoint at {currentWaypoint.name}");
                }
            }
            else
            {
                // Waypoint cụt: tìm waypoint khác gần đó để tiếp tục (tránh kẹt)
                var nearby = FindObjectsOfType<WaypointNode>()
                    .Where(w => w != currentWaypoint && Vector3.Distance(w.transform.position, transform.position) < 20f)
                    .ToList();
                if (nearby.Count > 0)
                {
                    currentWaypoint = nearby[Random.Range(0, nearby.Count)];
                }
            }
        }
    }
    #endregion

    #region Helpers
    WaypointNode FindClosestWayPoint(WaypointNode[] waypointsArray)
    {
        if (waypointsArray == null || waypointsArray.Length == 0) return null;
        return waypointsArray.OrderBy(t => Vector3.Distance(transform.position, t.transform.position)).FirstOrDefault();
    }

    float TurnTowardTarget()
    {
        Vector2 vectorToTarget = targetPosition - transform.position;
        vectorToTarget.Normalize();

        if (isAvoidingCars && !topDownCarController.IsJumping())
            AvoidCars(vectorToTarget, out vectorToTarget);

        angleToTarget = Vector2.SignedAngle(transform.up, vectorToTarget) * -1;
        float steerAmount = angleToTarget / 45.0f;
        steerAmount = Mathf.Clamp(steerAmount, -1.0f, 1.0f);
        return steerAmount;
    }

    float ApplyThrottleOrBrake(float inputX)
    {
        if (topDownCarController.GetVelocityMagnitude() > maxSpeed)
            return 0;

        if (isBackingUp)
            return -0.8f;

        float reduceSpeedDueToCornering = Mathf.Abs(inputX) / 1.0f;
        float throttle = 1.05f - reduceSpeedDueToCornering * skillLevel;

        if (temporaryWaypoints.Count != 0)
        {
            if (angleToTarget > 70 || angleToTarget < -70)
                throttle = throttle * -1;
        }

        return throttle;
    }

    void SetMaxSpeedBasedOnSkillLevel(float newSpeed)
    {
        maxSpeed = Mathf.Clamp(newSpeed, 0, originalMaximumSpeed);
        float skillbasedMaxiumSpeed = Mathf.Clamp(skillLevel, 0.3f, 1.0f);
        maxSpeed = maxSpeed * skillbasedMaxiumSpeed;
    }
    #endregion

    #region Avoidance
    Vector2 FindNearestPointOnLine(Vector2 lineStartPosition, Vector2 lineEndPosition, Vector2 point)
    {
        Vector2 lineHeadingVector = (lineEndPosition - lineStartPosition);
        float maxDistance = lineHeadingVector.magnitude;
        lineHeadingVector.Normalize();
        Vector2 lineVectorStartToPoint = point - lineStartPosition;
        float dotProduct = Vector2.Dot(lineVectorStartToPoint, lineHeadingVector);
        dotProduct = Mathf.Clamp(dotProduct, 0f, maxDistance);
        return lineStartPosition + lineHeadingVector * dotProduct;
    }

    bool IsCarsInFrontOfAICar(out Vector3 position, out Vector3 otherCarRightVector)
    {
        polygonCollider2D.enabled = false;
        RaycastHit2D raycastHit2d = Physics2D.CircleCast(transform.position + transform.up * 0.5f, 1.2f, transform.up, 12, 1 << LayerMask.NameToLayer("Car"));
        polygonCollider2D.enabled = true;

        if (raycastHit2d.collider != null)
        {
            Debug.DrawRay(transform.position, transform.up * 12, Color.red);
            position = raycastHit2d.collider.transform.position;
            otherCarRightVector = raycastHit2d.collider.transform.right;
            return true;
        }
        else
        {
            Debug.DrawRay(transform.position, transform.up * 12, Color.black);
            position = Vector3.zero;
            otherCarRightVector = Vector3.zero;
            return false;
        }
    }

    void AvoidCars(Vector2 vectorToTarget, out Vector2 newVectorToTarget)
    {
        if (IsCarsInFrontOfAICar(out Vector3 otherCarPosition, out Vector3 otherCarRightVector))
        {
            Vector2 avoidanceVector = Vector2.Reflect((otherCarPosition - transform.position).normalized, otherCarRightVector);
            float distanceToTarget = (targetPosition - transform.position).magnitude;
            float driveToTargetInfluence = 6.0f / distanceToTarget;
            driveToTargetInfluence = Mathf.Clamp(driveToTargetInfluence, 0.30f, 1.0f);
            float avoidanceInfluence = 1.0f - driveToTargetInfluence;
            avoidanceVectorLerped = Vector2.Lerp(avoidanceVectorLerped, avoidanceVector, Time.fixedDeltaTime * 4);
            newVectorToTarget = (vectorToTarget * driveToTargetInfluence + avoidanceVector * avoidanceInfluence);
            newVectorToTarget.Normalize();
            Debug.DrawRay(transform.position, avoidanceVector * 10, Color.green);
            Debug.DrawRay(transform.position, newVectorToTarget * 10, Color.yellow);
            return;
        }
        newVectorToTarget = vectorToTarget;
    }

    IEnumerator StuckCheckCO()
    {
        // Giữ lại nhưng không dùng
        yield return null;
    }
    #endregion

    #region Cop Registration
    void OnEnable()
    {
        if (isCop)
        {
            if (CopTargetManager.Instance == null)
            {
                GameObject go = new GameObject("CopTargetManager");
                go.AddComponent<CopTargetManager>();
            }
            CopTargetManager.Instance.RegisterCop(this);
        }
    }

    void OnDisable()
    {
        if (isCop)
        {
            if (targetTransform != null)
            {
                CopTargetManager.Instance?.ReleaseTarget(targetTransform, this);
                targetTransform = null;
            }
            CopTargetManager.Instance?.UnregisterCop(this);
        }
    }

    void OnDestroy()
    {
        if (isCop)
        {
            if (targetTransform != null)
            {
                CopTargetManager.Instance?.ReleaseTarget(targetTransform, this);
                targetTransform = null;
            }
            CopTargetManager.Instance?.UnregisterCop(this);
        }
    }
    #endregion
}