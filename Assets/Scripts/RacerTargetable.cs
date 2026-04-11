using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class RacerTargetable : MonoBehaviour
{
    [Tooltip("Tick nếu đây là player")]
    public bool isPlayer = false;

    // Danh sách CarAIHandler hiện đang chase racer này
    List<CarAIHandler> chasers = new List<CarAIHandler>();

    public int ChaseCount { get { return chasers.Count; } }

    public void AddChaser(CarAIHandler cop)
    {
        if (cop == null) return;
        if (!chasers.Contains(cop)) chasers.Add(cop);
    }

    public void RemoveChaser(CarAIHandler cop)
    {
        if (cop == null) return;
        if (chasers.Contains(cop)) chasers.Remove(cop);
    }

    void OnEnable()
    {
        CopTargetManager.Instance?.RegisterRacer(this);
    }

    void OnDisable()
    {
        // Nếu racer disable, giải phóng tất cả claims của các cops đang chase nó.
        if (CopTargetManager.Instance != null)
        {
            // copy list để tránh modify-while-iterate
            var copy = new List<CarAIHandler>(chasers);
            foreach (var c in copy)
                CopTargetManager.Instance.ReleaseTarget(this, c);
        }

        CopTargetManager.Instance?.UnregisterRacer(this);
    }

    void OnDestroy()
    {
        if (CopTargetManager.Instance != null)
        {
            var copy = new List<CarAIHandler>(chasers);
            foreach (var c in copy)
                CopTargetManager.Instance.ReleaseTarget(this, c);
        }

        CopTargetManager.Instance?.UnregisterRacer(this);
    }
}
