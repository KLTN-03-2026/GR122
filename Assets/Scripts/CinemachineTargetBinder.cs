using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using UnityEngine;

/// <summary>
/// Robust Cinemachine binder:
/// - dùng reflection để tìm Cinemachine types (CinemachineVirtualCamera, CinemachineFreeLook...)
/// - nếu SetTargetStatic được gọi quá sớm (trước khi assembly Cinemachine load), sẽ giữ target
///   và thử apply lại mỗi 0.2s trong vòng một vài lần cho tới khi thành công.
/// - không cần define scripting symbol; hoạt động nếu Cinemachine package đã được import.
/// </summary>
public class CinemachineTargetBinder : MonoBehaviour
{
    public string playerTag = "Player Racer";
    public bool autoFindOnStart = true;
    public bool setFollow = true;
    public bool setLookAt = true;

    [Tooltip("Số lần thử gán lại khi Cinemachine chưa sẵn sàng.")]
    public int maxRetryAttempts = 25;

    [Tooltip("Khoảng cách giữa các lần thử (giây, realtime).")]
    public float retryDelay = 0.15f;

    Transform currentTarget;
    static CinemachineTargetBinder _instance;

    // reflection caches
    static Type vcamType = null;
    static Type freelookType = null;
    static PropertyInfo followProp = null;
    static PropertyInfo lookAtProp = null;
    static bool attemptedDetect = false;
    static bool cinemachineAvailable = false;
    static bool loggedNotFound = false;
    static bool appliedOnce = false; // only log success once

    void Awake()
    {
        if (_instance == null) _instance = this;
    }

    void Start()
    {
        if (autoFindOnStart) TryFindAndAssign();
    }

    void TryFindAndAssign()
    {
        var go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null)
        {
            SetTarget(go.transform);
        }
    }

    /// <summary>
    /// Set target and try to apply immediately or schedule retries if needed.
    /// Use this after you instantiate/replace the player.
    /// </summary>
    public void SetTarget(Transform t)
    {
        if (t == null) return;
        currentTarget = t;
        // Try immediate apply synchronously first
        DetectCinemachineTypes();
        if (cinemachineAvailable)
        {
            bool ok = TryApplyToAllCameras();
            if (ok)
            {
                if (!appliedOnce)
                {
                    Debug.Log("[CinemachineTargetBinder] Applied target to Cinemachine cameras.");
                    appliedOnce = true;
                }
                return;
            }
        }
        // if not applied, start coroutine retries
        StartCoroutine(RetryApplyCoroutine());
    }

    IEnumerator RetryApplyCoroutine()
    {
        int attempts = 0;
        while (attempts < maxRetryAttempts)
        {
            attempts++;
            DetectCinemachineTypes();
            if (cinemachineAvailable)
            {
                bool ok = TryApplyToAllCameras();
                if (ok)
                {
                    if (!appliedOnce)
                    {
                        Debug.Log("[CinemachineTargetBinder] Applied target to Cinemachine cameras (after retry).");
                        appliedOnce = true;
                    }
                    yield break;
                }
            }
            yield return new WaitForSecondsRealtime(retryDelay);
        }

        // After retries, if still not available -> warn once
        if (!cinemachineAvailable && !loggedNotFound)
        {
            Debug.LogWarning("[CinemachineTargetBinder] Cinemachine types not found in project after retries. Please ensure Cinemachine package is installed and compiled.");
            loggedNotFound = true;
        }
    }

    /// <summary>
    /// Public static helper. Creates instance if necessary.
    /// </summary>
    public static void SetTargetStatic(Transform t)
    {
        if (t == null) return;

        if (_instance == null)
        {
            _instance = FindObjectOfType<CinemachineTargetBinder>();
            if (_instance == null)
            {
                var go = new GameObject("CinemachineTargetBinder");
                _instance = go.AddComponent<CinemachineTargetBinder>();
                DontDestroyOnLoad(go);
            }
        }

        _instance.SetTarget(t);
    }

    /// <summary>
    /// Try to apply currentTarget to all Cinemachine cameras -- returns true if at least one assignment made.
    /// </summary>
    bool TryApplyToAllCameras()
    {
        if (currentTarget == null) return false;

        DetectCinemachineTypes();
        if (!cinemachineAvailable) return false;

        bool anyAssigned = false;

        // iterate all GameObjects and GetComponent by type (works with reflection)
        var allGOs = GameObject.FindObjectsOfType<GameObject>();
        foreach (var go in allGOs)
        {
            if (vcamType != null)
            {
                var comp = go.GetComponent(vcamType);
                if (comp != null)
                {
                    try
                    {
                        if (setFollow && followProp != null) followProp.SetValue(comp, currentTarget, null);
                        if (setLookAt && lookAtProp != null) lookAtProp.SetValue(comp, currentTarget, null);
                        anyAssigned = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("[CinemachineTargetBinder] Failed to set properties on vcam: " + ex.Message);
                    }
                }
            }

            if (freelookType != null)
            {
                var comp2 = go.GetComponent(freelookType);
                if (comp2 != null)
                {
                    try
                    {
                        if (setFollow && followProp != null) followProp.SetValue(comp2, currentTarget, null);
                        if (setLookAt && lookAtProp != null) lookAtProp.SetValue(comp2, currentTarget, null);
                        anyAssigned = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("[CinemachineTargetBinder] Failed to set properties on freelook: " + ex.Message);
                    }
                }
            }
        }

        return anyAssigned;
    }

    /// <summary>
    /// Reflection detection for Cinemachine types/properties.
    /// </summary>
    static void DetectCinemachineTypes()
    {
        if (attemptedDetect) return;
        attemptedDetect = true;

        try
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                Type[] types = null;
                try { types = asm.GetTypes(); }
                catch { continue; }
                foreach (var t in types)
                {
                    if (vcamType == null && t.Name.IndexOf("CinemachineVirtualCamera", StringComparison.OrdinalIgnoreCase) >= 0)
                        vcamType = t;
                    if (freelookType == null && t.Name.IndexOf("CinemachineFreeLook", StringComparison.OrdinalIgnoreCase) >= 0)
                        freelookType = t;

                    if (vcamType != null && freelookType != null) break;
                }
                if (vcamType != null && freelookType != null) break;
            }

            // determine property infos
            Type sample = vcamType ?? freelookType;
            if (sample != null)
            {
                followProp = sample.GetProperty("Follow", BindingFlags.Public | BindingFlags.Instance);
                lookAtProp = sample.GetProperty("LookAt", BindingFlags.Public | BindingFlags.Instance);
                cinemachineAvailable = (followProp != null || lookAtProp != null);
            }
            else
            {
                cinemachineAvailable = false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[CinemachineTargetBinder] Detection error: " + ex.Message);
            cinemachineAvailable = false;
        }
    }
}
