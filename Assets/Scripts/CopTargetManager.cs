using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CopTargetManager : MonoBehaviour
{
    public static CopTargetManager Instance { get; private set; }

    List<RacerTargetable> racers = new List<RacerTargetable>();
    List<CarAIHandler> cops = new List<CarAIHandler>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    // ---------- Racers ----------
    public void RegisterRacer(RacerTargetable r)
    {
        if (r == null) return;
        if (!racers.Contains(r)) racers.Add(r);
    }

    public void UnregisterRacer(RacerTargetable r)
    {
        if (r == null) return;
        if (racers.Contains(r)) racers.Remove(r);
    }

    // ---------- Cops (đăng ký để manager biết có bao nhiêu cop) ----------
    public void RegisterCop(CarAIHandler cop)
    {
        if (cop == null) return;
        if (!cops.Contains(cop)) cops.Add(cop);
    }

    public void UnregisterCop(CarAIHandler cop)
    {
        if (cop == null) return;
        if (cops.Contains(cop)) cops.Remove(cop);
    }

    // ---------- Assign target ----------
    public Transform AssignTarget(CarAIHandler caller)
    {
        if (caller == null) return null;

        // Lọc danh sách racer hợp lệ
        var valid = racers.Where(r => r != null && r.gameObject.activeInHierarchy).ToList();
        // tránh target chính mình (nếu caller cũng có component RacerTargetable)
        valid = valid.Where(r => r.transform != caller.transform).ToList();

        if (valid.Count == 0) return null;

        // SPECIAL CASE: nếu chỉ có 1 cop trong scene -> ưu tiên player (nếu tồn tại)
        if (cops.Count == 1)
        {
            var player = valid.FirstOrDefault(r => r.isPlayer);
            if (player != null)
            {
                player.AddChaser(caller);
                return player.transform;
            }
            // nếu không có player thì fallback xuống logic bình thường
        }

        // Thông thường: chọn racer có ChaseCount thấp nhất (min), ưu tiên isPlayer nếu tie, sau đó gần nhất
        // Tìm giá trị chaseCount nhỏ nhất hiện có
        int minCount = valid.Min(r => r.ChaseCount);

        // Lấy tập racer có chaseCount == minCount
        var candidates = valid.Where(r => r.ChaseCount == minCount).ToList();

        // Nếu không có (không thể xảy ra vì minCount lấy từ valid), fallback
        if (candidates.Count == 0)
            candidates = valid;

        // Trong candidates, ưu tiên player nếu có
        var playerCandidates = candidates.Where(r => r.isPlayer).ToList();
        RacerTargetable chosen = null;
        if (playerCandidates.Count > 0)
        {
            // nếu có nhiều player (hiếm) chọn gần nhất
            chosen = playerCandidates.OrderBy(r => Vector3.Distance(caller.transform.position, r.transform.position)).First();
        }
        else
        {
            // không có player, chọn nearest among candidates
            chosen = candidates.OrderBy(r => Vector3.Distance(caller.transform.position, r.transform.position)).First();
        }

        if (chosen != null)
        {
            chosen.AddChaser(caller);
            return chosen.transform;
        }

        return null;
    }

    // ---------- Release ----------
    // Khi cop bỏ target (vd: disable/destroy/đổi mục tiêu)
    public void ReleaseTarget(RacerTargetable racer, CarAIHandler caller)
    {
        if (racer == null || caller == null) return;
        racer.RemoveChaser(caller);
    }

    public void ReleaseTarget(Transform targetTransform, CarAIHandler caller)
    {
        if (targetTransform == null || caller == null) return;
        var r = targetTransform.GetComponent<RacerTargetable>();
        if (r != null) ReleaseTarget(r, caller);
    }
}
