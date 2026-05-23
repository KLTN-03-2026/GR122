using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// RaceEventIcon: điều khiển toàn bộ lifecycle của 1 race event (spawn AI, teleport player, countdown, lap/checkpoint logic, UI, cleanup).
/// Cập nhật: hỗ trợ rewardAmount (gọi MoneyManager.Instance.AddMoney khi player hoàn thành race).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class RaceEventIcon : MonoBehaviour
{
    [Header("Spawn / Race points")]
    public SpawnPoint[] spawnPoints;
    public RacePoint racePoint;
    // public static flag to indicate the race system currently requests a global freeze (countdown/result)
    public static bool GlobalFreezeRequestedByRace = false;
    [Header("Activation Area (optional)")]
    public Collider2D activationCollider;
    public float activationDistance = 3.0f;
    public KeyCode activationKey = KeyCode.Return;
    public string playerTagFallback = "Player";

    [Header("Race Objects")]
    public GameObject raceBarrier;    // barrier object
    public GameObject finishLine;     // finish line GameObject (activate/deactivate like raceBarrier)
    public GameObject aiPathRoot;
    public RacePath racePath;

    [Header("Checkpoints (ordered)")]
    [Tooltip("List of checkpoint components in running order (first = index 0).")]
    public Checkpoint[] checkpoints;

    [Tooltip("Allow reverse-direction laps (start at last checkpoint)")]
    public bool allowReverse = true;

    [Header("UI (in-game)")]
    public GameObject raceUICanvasRoot;
    public Text countdownText;
    public Text lapText;
    public Text positionText;
    public Text timeText;

    [Header("Result UI")]
    public GameObject resultCanvasRoot;
    public Text resultTotalTimeText;
    public Text resultPositionText;
    public Button resultConfirmButton;
    public Text resultCashText;


    [Header("Race Settings")]
    public int totalLaps = 3;
    public float countdownSeconds = 3f;
    public bool singleUse = false;

    [Header("Reward")]
    [Tooltip("Amount of cash awarded to the player when they finish this race.")]
    public int rewardAmount = 0;

    [Header("Event Locking")]
    public string eventId;               // Ví dụ: "Street", "Performance", "Super", "Exotic", "Hyper"
    public string requiredEventId;       // ID của event cần hoàn thành trước (để trống nếu không cần)
    public string lockMessage = "Hãy chiến thắng Event trước đó để mở khóa Event này.";
    public float lockMessageDuration = 5f;    
    // runtime state
    bool hasActivated = false;
    bool playerInside = false;
    GameObject playerInsideObj = null;

    SpriteRenderer iconRenderer;
    List<GameObject> spawnedAIs = new List<GameObject>();
    List<RaceParticipant> participants = new List<RaceParticipant>();
    RaceParticipant playerParticipant = null;

    bool raceActive = false;
    float raceStartTime = 0f;
    float raceElapsed = 0f;

    bool temporarilyDisabled = false;

    // per-instance stored UnityAction for result button, so we can remove exactly this listener
    UnityAction onConfirmAction = null;

    // ensure we only give reward once per run
    bool rewardGivenForThisRun = false;

    void Reset()
    {
        if (activationCollider == null)
            activationCollider = GetComponent<Collider2D>();
    }

    void Awake()
    {
        // Reset global freeze flag mỗi khi một RaceEventIcon được tạo (tránh ảnh hưởng từ lần chơi trước)
        GlobalFreezeRequestedByRace = false;        
        iconRenderer = GetComponent<SpriteRenderer>();

        // register checkpoints owner & index if they are assigned
        if (checkpoints != null)
        {
            for (int i = 0; i < checkpoints.Length; i++)
            {
                if (checkpoints[i] != null)
                {
                    checkpoints[i].index = i;
                    checkpoints[i].owner = this;
                }
            }
        }

        // If finishLine GameObject assigned and it contains a LapTrigger, ensure owner is set
        if (finishLine != null)
        {
            var lt = finishLine.GetComponent<LapTrigger>();
            if (lt != null) lt.owner = this;
        }

        if (raceUICanvasRoot != null) raceUICanvasRoot.SetActive(false);
        if (resultCanvasRoot != null) resultCanvasRoot.SetActive(false);

        // do NOT assign permanent listeners for resultConfirmButton here;
        // we'll register a per-instance listener when showing result UI.
    }

    /// <summary>
    /// Temporarily disable visuals & activation (keeps component alive).
    /// </summary>
    public void SetTemporarilyDisabled(bool disabled)
    {
        temporarilyDisabled = disabled;
        if (iconRenderer != null) iconRenderer.enabled = !disabled;
        if (activationCollider != null) activationCollider.enabled = !disabled;

        // if being disabled, clear cached playerInside so it won't keep reacting
        if (disabled)
        {
            playerInside = false;
            playerInsideObj = null;
        }
    }

    void Update()
    {
        // 1) If race running -> always update timer and UI
        if (raceActive)
        {
            raceElapsed = Time.time - raceStartTime;
            if (timeText != null) timeText.text = FormatTime(raceElapsed);
            UpdatePositionsUI();
            UpdateLapUI();
            return;
        }

        // 2) Not racing: block activation if temporarily disabled or single-use activated
        if (temporarilyDisabled || (hasActivated && singleUse)) return;

        // if player is inside via trigger, re-validate and handle Enter
        if (playerInside && playerInsideObj != null)
        {
            // re-validate (in case player was teleported away)
            bool stillInRange = true;
            if (activationCollider != null)
            {
                if (activationCollider.enabled)
                    stillInRange = activationCollider.bounds.Contains(playerInsideObj.transform.position);
                else
                    stillInRange = Vector2.Distance(playerInsideObj.transform.position, transform.position) <= activationDistance;
            }
            else
            {
                stillInRange = Vector2.Distance(playerInsideObj.transform.position, transform.position) <= activationDistance;
            }

            if (!stillInRange)
            {
                playerInside = false;
                playerInsideObj = null;
            }
            else
            {
                if (Input.GetKeyDown(activationKey))
                    TryActivate(playerInsideObj);
                return;
            }
        }

        // fallback: activationCollider bounds contain
        if (activationCollider != null)
        {
            GameObject player = FindPlayerByTag();
            if (player != null)
            {
                if (activationCollider.bounds.Contains(player.transform.position))
                {
                    if (Input.GetKeyDown(activationKey))
                        TryActivate(player);
                    return;
                }
            }
        }

        // final fallback: use activationDistance
        GameObject fallbackPlayer = FindPlayerByTag();
        if (fallbackPlayer == null) return;
        float dist = Vector2.Distance(fallbackPlayer.transform.position, transform.position);
        if (dist <= activationDistance)
        {
            if (Input.GetKeyDown(activationKey))
                TryActivate(fallbackPlayer);
        }
    }

    GameObject FindPlayerByTag()
    {
        string tagToUse = (racePoint != null && !string.IsNullOrEmpty(racePoint.playerTag)) ? racePoint.playerTag : playerTagFallback;
        return GameObject.FindGameObjectWithTag(tagToUse);
    }

    void TryActivate(GameObject player)
    {
        if (hasActivated && singleUse) return;
        // Kiểm tra khóa event trước khi cho phép bắt đầu
        if (!IsUnlocked())
        {
            if (WantedSystem.Instance != null)
                WantedSystem.Instance.ShowTemporaryMessage(lockMessage, lockMessageDuration);
            else
                Debug.LogWarning("[RaceEventIcon] Cannot unlock race, but WantedSystem not found.");
            return;
        }        
        StartCoroutine(StartRaceRoutine(player));
    }

    private bool IsUnlocked()
    {
        if (string.IsNullOrEmpty(requiredEventId)) return true;
        return PlayerPrefs.GetInt("race_completed_" + requiredEventId, 0) == 1;
    }    

    void OnTriggerEnter2D(Collider2D other)
    {
        if (temporarilyDisabled || (hasActivated && singleUse)) return;
        string tagToUse = (racePoint != null && !string.IsNullOrEmpty(racePoint.playerTag)) ? racePoint.playerTag : playerTagFallback;
        if (other.gameObject.CompareTag(tagToUse))
        {
            playerInside = true;
            playerInsideObj = other.gameObject;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        string tagToUse = (racePoint != null && !string.IsNullOrEmpty(racePoint.playerTag)) ? racePoint.playerTag : playerTagFallback;
        if (other.gameObject.CompareTag(tagToUse))
        {
            playerInside = false;
            playerInsideObj = null;
        }
    }

    IEnumerator StartRaceRoutine(GameObject playerRoot)
    {
        hasActivated = true;
        rewardGivenForThisRun = false;

        // Disable other event icons & garage icons visuals (keep components alive)
        DisableOtherEvents();

        // Hide this icon visuals (keep component active)
        SetTemporarilyDisabled(true);

        // Activate race objects & UI
        if (raceBarrier != null) raceBarrier.SetActive(true);
        if (finishLine != null) finishLine.SetActive(true);
        if (aiPathRoot != null) aiPathRoot.SetActive(true);
        if (raceUICanvasRoot != null) raceUICanvasRoot.SetActive(true);

        // reset runtime lists
        spawnedAIs.Clear();
        participants.Clear();
        playerParticipant = null;
        raceActive = false;
        raceElapsed = 0f;

        // Spawn AIs at spawnPoints
        if (spawnPoints != null)
        {
            foreach (var sp in spawnPoints)
            {
                if (sp == null) continue;
                var go = sp.SpawnAI();
                if (go != null)
                {
                    spawnedAIs.Add(go);
                    var rp = go.GetComponent<RaceParticipant>() ?? go.AddComponent<RaceParticipant>();

                    rp.ClearAllSubscribers();
                    rp.currentLap = 0;
                    rp.isPlayer = false;
                    rp.raceController = this;
                    rp.ResetCheckpointProgress();

                    participants.Add(rp);

                    var ai = go.GetComponent<CarAIHandler>();
                    if (ai != null) ai.aiMode = CarAIHandler.AIMode.followWaypoints;
                }
            }
        }

        // Teleport player to racePoint
        if (playerRoot != null && racePoint != null)
        {
            PlayerTeleportHelper.TeleportPlayer(playerRoot, racePoint.WorldPosition, racePoint.zRotation);

            RaceParticipant pc = playerRoot.GetComponent<RaceParticipant>() ?? playerRoot.GetComponentInChildren<RaceParticipant>();
            if (pc == null) pc = playerRoot.AddComponent<RaceParticipant>();

            pc.ClearAllSubscribers();
            pc.currentLap = 0;
            pc.isPlayer = true;
            pc.raceController = this;
            pc.ResetCheckpointProgress();

            playerParticipant = pc;
            participants.Add(pc);
        }

        // allow spawned AIs to Awake/Start
        yield return null;

    // Freeze world for countdown (race requests frozen state)
    GlobalFreezeRequestedByRace = true;
    Time.timeScale = 0f;
    if (countdownText != null) countdownText.gameObject.SetActive(true);

    int show = Mathf.Max(0, Mathf.FloorToInt(countdownSeconds));
    for (int i = show; i > 0; i--)
    {
        if (countdownText != null) countdownText.text = i.ToString();

        // Wait realtime 1 second but pause the wait when the Pause menu is active.
        float remaining = 1f;
        while (remaining > 0f)
        {
            // If Pause menu not active, advance real-time timer (unscaled)
            if (!PauseMenu.IsPaused)
            {
                remaining -= Time.unscaledDeltaTime;
            }

            // yield return null so this coroutine respects PauseMenu.IsPaused toggles
            yield return null;
        }
    }

    // show "GO!" and short wait (also pausable)
    if (countdownText != null) countdownText.text = "GO!";
    float goWait = 0.6f;
    while (goWait > 0f)
    {
        if (!PauseMenu.IsPaused)
            goWait -= Time.unscaledDeltaTime;
        yield return null;
    }
    if (countdownText != null) countdownText.gameObject.SetActive(false);

    // End of countdown, race no longer requests freeze
    GlobalFreezeRequestedByRace = false;

    // If Pause is currently active (player pressed Pause during countdown and hasn't resumed),
    // let PauseMenu control the timescale; otherwise unfreeze to normal.
    if (!PauseMenu.IsPaused)
        Time.timeScale = 1f;
    else
        Time.timeScale = 0f;

    raceActive = true;
    raceStartTime = Time.time;
    raceElapsed = 0f;

    UpdateLapUI();
    UpdatePositionsUI();
    }

    #region Checkpoint handling
    public void OnCheckpointTriggered(Checkpoint checkpoint, RaceParticipant rp)
    {
        if (checkpoint == null || rp == null) return;
        if (!participants.Contains(rp)) return; // ignore outsiders

        int idx = checkpoint.index;
        int lastIdx = (checkpoints != null ? checkpoints.Length - 1 : -1);

        // If participant hasn't chosen direction yet
        if (rp.direction == 0)
        {
            if (idx == 0)
            {
                // start forward
                rp.direction = 1;
                rp.nextCheckpointIndex = 1; // expect next
                return;
            }
            else if (allowReverse && idx == lastIdx)
            {
                // start backward
                rp.direction = -1;
                rp.nextCheckpointIndex = lastIdx - 1; // expect previous
                return;
            }
            else
            {
                // not a valid start checkpoint -> ignore
                return;
            }
        }

        // forward progression
        if (rp.direction == 1)
        {
            if (idx == rp.nextCheckpointIndex)
            {
                rp.nextCheckpointIndex = idx + 1;
                if (rp.nextCheckpointIndex > lastIdx)
                {
                    rp.nextCheckpointIndex = checkpoints.Length; // ready for finish
                }
            }
            return;
        }

        // backward progression
        if (rp.direction == -1)
        {
            if (idx == rp.nextCheckpointIndex)
            {
                rp.nextCheckpointIndex = idx - 1;
                if (rp.nextCheckpointIndex < 0)
                {
                    rp.nextCheckpointIndex = -1; // ready for finish in reverse
                }
            }
            return;
        }
    }
    #endregion

    #region Lap handling
    public void OnLapTriggerHit(RaceParticipant rp)
    {
        if (rp == null) return;
        if (!participants.Contains(rp)) return;

        bool readyForFinish = (rp.direction == 1 && rp.nextCheckpointIndex == (checkpoints != null ? checkpoints.Length : 0))
                            || (rp.direction == -1 && rp.nextCheckpointIndex == -1);

        if (!readyForFinish)
        {
            // Not allowed to count lap (didn't pass full checkpoint sequence)
            return;
        }

        // Try to complete lap (this checks debounce + speed inside rp)
        bool counted = rp.TryCompleteLap();
        if (!counted) return;

        // update UI
        UpdateLapUI();
        UpdatePositionsUI();

        // If this was player finishing total laps, handle end flow
        if (rp.isPlayer && rp.currentLap >= totalLaps)
        {
            StartCoroutine(HandleRaceEndForPlayer());
        }
    }
    #endregion

    IEnumerator HandleRaceEndForPlayer()
    {
        raceActive = false;

        // disable AIs movement
        foreach (var go in spawnedAIs)
        {
            if (go == null) continue;
            var ai = go.GetComponent<CarAIHandler>();
            if (ai != null) ai.enabled = false;
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null) { rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f; rb.simulated = false; }
        }

        // freeze game time for result UI
        GlobalFreezeRequestedByRace = true;
        Time.timeScale = 0f;

        // Determine finishing order now
        var valid = participants.Where(p => p != null).ToList();
        var ordered = valid.OrderByDescending(p => p.GetProgressScore()).ToList();
        int total = ordered.Count;
        int posIndex = ordered.FindIndex(p => p == playerParticipant);
        if (posIndex < 0) posIndex = ordered.Count - 1;

        // Decide award: only give reward if player is 1st (posIndex == 0)
        int awardedAmount = 0;
        if (posIndex == 0 && !rewardGivenForThisRun && rewardAmount != 0)
        {
            try
            {
                MoneyManager.Instance.AddMoney(rewardAmount);
                awardedAmount = rewardAmount;
                rewardGivenForThisRun = true;
            }
            catch
            {
                Debug.LogWarning("[RaceEventIcon] MoneyManager not available or failed to add money.");
                awardedAmount = 0;
            }
            // Đánh dấu event này đã được hoàn thành (chiến thắng)
            if (!string.IsNullOrEmpty(eventId))
            {
                PlayerPrefs.SetInt("race_completed_" + eventId, 1);
                PlayerPrefs.Save();
                Debug.Log($"[RaceEventIcon] Event {eventId} completed (first place).");
            }            
        }
        else
        {
            // not first or already awarded -> no money this time
            awardedAmount = 0;
        }

        // Show result UI and populate texts
        if (resultCanvasRoot != null) resultCanvasRoot.SetActive(true);
        if (resultTotalTimeText != null) resultTotalTimeText.text = "Time: " + FormatTime(raceElapsed);

        // Set the cash text: "Tiền nhận: (lượng tiền)" or "Tiền nhận: 0"
        if (resultCashText != null)
        {
            resultCashText.text = "Tiền nhận: " + awardedAmount.ToString("N0");
        }

        if (resultPositionText != null) resultPositionText.text = "Position: " + (posIndex + 1) + "/" + total;

        // register instance-specific listener for confirm button
        if (resultConfirmButton != null)
        {
            if (onConfirmAction != null) resultConfirmButton.onClick.RemoveListener(onConfirmAction);

            onConfirmAction = new UnityEngine.Events.UnityAction(() =>
            {
                RaceEventHelper.Instance.InvokeCleanup(this);
            });

            resultConfirmButton.onClick.AddListener(onConfirmAction);
        }

        yield break;
    }



    /// <summary>
    /// Immediate cleanup called from RaceEventHelper - safe even if this GameObject becomes inactive.
    /// </summary>
    public void CleanupAfterRaceImmediate()
    {
        if (resultCanvasRoot != null) resultCanvasRoot.SetActive(false);
        if (raceUICanvasRoot != null) raceUICanvasRoot.SetActive(false);

        // Clear race freeze request
        GlobalFreezeRequestedByRace = false;

        // restore timescale only if not paused by PauseMenu
        Time.timeScale = PauseMenu.IsPaused ? 0f : 1f;


        // Despawn AIs
        foreach (var go in spawnedAIs)
        {
            if (go == null) continue;
            Destroy(go);
        }
        spawnedAIs.Clear();

        // Cleanup participants
        foreach (var p in participants)
        {
            if (p == null) continue;
            MethodInfo clear = p.GetType().GetMethod("ClearAllSubscribers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (clear != null) clear.Invoke(p, null);
            if (!p.isPlayer)
            {
                Destroy(p);
            }
        }
        participants.Clear();
        playerParticipant = null;

        // Deactivate race objects & UI
        if (raceBarrier != null) raceBarrier.SetActive(false);
        if (finishLine != null) finishLine.SetActive(false);
        if (aiPathRoot != null) aiPathRoot.SetActive(false);

        // Remove result button listener if exists
        if (resultConfirmButton != null && onConfirmAction != null)
        {
            resultConfirmButton.onClick.RemoveListener(onConfirmAction);
            onConfirmAction = null;
        }

        // Re-enable other event icons visuals
        EnableAllEvents();

        // Restore own visuals if not singleUse
        if (!singleUse)
            SetTemporarilyDisabled(false);

        // allow re-activation
        hasActivated = false;

        // Reset reward flag so next run can award again (if allowed by singleUse logic)
        rewardGivenForThisRun = false;
    }

    void DisableOtherEvents()
    {
        // disable other RaceEventIcon visuals (as before)
        var allEvents = FindObjectsOfType<RaceEventIcon>();
        foreach (var e in allEvents)
        {
            if (e == this) continue;
            e.SetTemporarilyDisabled(true);
        }

        // ALSO temporarily disable all GarageIcon instances while race runs
        var allGarages = FindObjectsOfType<GarageIcon>();
        foreach (var g in allGarages)
        {
            if (g == null) continue;
            g.SetTemporarilyDisabled(true);
        }
    }

    void EnableAllEvents()
    {
        // restore other RaceEventIcon visuals
        var allEvents = FindObjectsOfType<RaceEventIcon>();
        foreach (var e in allEvents)
        {
            if (e != null) e.SetTemporarilyDisabled(false);
        }

        // restore GarageIcon visuals
        var allGarages = FindObjectsOfType<GarageIcon>();
        foreach (var g in allGarages)
        {
            if (g != null) g.SetTemporarilyDisabled(false);
        }
    }

    #region UI helpers
    void UpdatePositionsUI()
    {
        if (positionText == null) return;
        var valid = participants.Where(p => p != null).ToList();
        if (valid.Count == 0) { positionText.text = "-/0"; return; }
        var ordered = valid.OrderByDescending(p => p.GetProgressScore()).ToList();
        int total = ordered.Count;
        int pos = ordered.FindIndex(p => p == playerParticipant);
        if (pos == -1) { positionText.text = "-/" + total; return; }
        string suffix = SuffixFromNumber(pos + 1);
        positionText.text = (pos + 1) + suffix + " / " + total;
    }

    void UpdateLapUI()
    {
        if (lapText == null || playerParticipant == null) return;
        int displayedLap = Mathf.Clamp(playerParticipant.currentLap + 1, 1, totalLaps);
        lapText.text = "Lap " + displayedLap + " / " + totalLaps;
    }
    #endregion

    public void OnParticipantFinished(RaceParticipant p)
    {
        // reserved for future use
    }

    string SuffixFromNumber(int n)
    {
        if (n % 100 >= 11 && n % 100 <= 13) return "th";
        switch (n % 10)
        {
            case 1: return "st";
            case 2: return "nd";
            case 3: return "rd";
            default: return "th";
        }
    }

    string FormatTime(float seconds)
    {
        int s = Mathf.FloorToInt(seconds % 60);
        int m = Mathf.FloorToInt(seconds / 60f);
        int ms = Mathf.FloorToInt((seconds - Mathf.Floor(seconds)) * 1000f);
        return string.Format("{0:00}:{1:00}.{2:000}", m, s, ms);
    }
}
