using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class WantedSystem : MonoBehaviour
{
    public static WantedSystem Instance { get; private set; }

    [Header("Wanted Triggers")]
    public int collisionToStartPatrol = 3;
    public int additionalCollisionsPerSpawn = 2;
    public int maxPatrolCops = 3;

    [Header("Chase Settings")]
    public int initialChasePool = 30;
    public int maxConcurrentChaseCops = 4;
    public int backupTriggerCount = 4;
    public float backupCountdownSeconds = 120f;
    public int backupAddAmount = 30;

    [Header("Escape & Busted")]
    public float escapeFillTime = 15f;
    public float escapeCheckRadius = 20f;
    public float bustedFillTime = 4f;
    public float bustedSpeedThreshold = 10f;
    public float bustedProximityDistance = 5f;

    [Header("Fine Settings")]
    public float finePerSecond = 50f;
    public int finePerCollision = 10;

    [Header("Cooldown Settings")]
    public float bustedCooldownSeconds = 60f;
    public float escapeCooldownSeconds = 60f;    

    [Header("UI - Cop Canvas")]
    public GameObject copCanvas;
    public TextMeshProUGUI bustedText_TMP;
    public Text bustedText_UI;
    public TextMeshProUGUI escapeText_TMP;
    public Text escapeText_UI;
    public TextMeshProUGUI copsCountText_TMP;
    public Text copsCountText_UI;
    public TextMeshProUGUI fineText_TMP;
    public Text fineText_UI;

    [Header("UI - Busted Canvas")]
    public GameObject bustedCanvas;
    public TextMeshProUGUI bustedCollisionText_TMP;
    public Text bustedCollisionText_UI;
    public TextMeshProUGUI bustedTimeText_TMP;
    public Text bustedTimeText_UI;
    public TextMeshProUGUI bustedFineText_TMP;
    public Text bustedFineText_UI;
    public Button bustedEndButton;

    [Header("UI - Warning")]
    public GameObject warningFineCanvas;
    public TextMeshProUGUI warningText_TMP;
    public Text warningText_UI;
    public Button warningYesButton;
    public Button warningNoButton;

    [Header("UI - Backup & Garage Text")]
    public GameObject backupAndGarageTextObject;
    public TextMeshProUGUI backupAndGarageText_TMP;
    public Text backupAndGarageText_UI;

    private int playerCollisions = 0;
    private bool isChaseActive = false;
    private float bustedPercent = 0f;
    private float escapePercent = 0f;
    private int currentChasePool;
    private float fineAccumulated = 0f;
    private float chaseStartTime = 0f;
    private int totalCollisionsDuringChase = 0;
    private bool isBackupCountdown = false;
    private float backupTimer = 0f;
    private bool escapeCompleted = false;
    private float escapeCooldownTimer = 0f;
    private bool bustedCooldownActive = false;
    private float bustedCooldownTimer = 0f;
    private bool spawnPatrolEnabled = true;

    private GameObject player;
    private TopDownCarController playerController;

    public bool IsChaseActive => isChaseActive;

    private void SetText(TextMeshProUGUI tmp, Text ui, string value)
    {
        if (tmp != null) tmp.text = value;
        else if (ui != null) ui.text = value;
    }

    void Awake() { Instance = this; }

    void Start()
    {
        Debug.Log($"[WantedSystem] Start called. copCanvas = {(copCanvas == null ? "NULL" : copCanvas.name)}");        
        if (bustedEndButton != null) bustedEndButton.onClick.AddListener(OnBustedEndPressed);
        if (warningYesButton != null) warningYesButton.onClick.AddListener(OnWarningYes);
        if (warningNoButton != null) warningNoButton.onClick.AddListener(OnWarningNo);
        copCanvas?.SetActive(false);
        bustedCanvas?.SetActive(false);
        warningFineCanvas?.SetActive(false);
        backupAndGarageTextObject?.SetActive(false);
        currentChasePool = initialChasePool;
        UpdateCopsCountUI();
        player = GameObject.FindGameObjectWithTag("Player Racer");
        playerController = player?.GetComponent<TopDownCarController>();
    }

    void Update()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player Racer");
        if (playerController == null && player != null) playerController = player.GetComponent<TopDownCarController>();
        if (isChaseActive && !escapeCompleted)
        {
            fineAccumulated += finePerSecond * Time.deltaTime;
            UpdateFineUI();
            
            // Cập nhật số lượng chase cop mong muốn TRƯỚC khi có thể kết thúc chase
            int desiredChase = Mathf.Min(maxConcurrentChaseCops, currentChasePool);
            TrafficSpawner.Instance?.SetDesiredChaseCount(desiredChase);
            UpdateCopsCountUI();
            
            // Gọi hàm này sau cùng, vì nó có thể đặt isChaseActive = false
            UpdateBustedAndEscape();
        }
        else if (escapeCompleted)
        {
            if (escapeCooldownTimer > 0)
            {
                escapeCooldownTimer -= Time.deltaTime;
                if (escapeCooldownTimer <= 0)
                {
                    spawnPatrolEnabled = true;
                    escapeCompleted = false;
                    playerCollisions = 0;
                    TrafficSpawner.Instance?.SetDesiredPatrolCount(0);
                    TrafficSpawner.Instance?.SetDesiredChaseCount(0);
                    TrafficSpawner.Instance?.SetDesiredChaseCount(0);  // Đảm bảo
                    Debug.Log("[WantedSystem] Escape cooldown ended, reset desired chase count.");
                }
            }
        }
        if (bustedCooldownActive)
        {
            bustedCooldownTimer -= Time.deltaTime;
            if (bustedCooldownTimer <= 0f)
            {
                bustedCooldownActive = false;
                spawnPatrolEnabled = true;
                Debug.Log("[WantedSystem] Busted cooldown ended, patrol spawning re-enabled.");
            }
        }
        // Debug: in trạng thái mỗi vài giây (tránh spam)
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[WantedSystem] Update: isChaseActive={isChaseActive}, escapeCompleted={escapeCompleted}, spawnPatrolEnabled={spawnPatrolEnabled}, bustedCooldownActive={bustedCooldownActive}, bustedCooldownTimer={bustedCooldownTimer}");
        }                
        if (!isChaseActive && !escapeCompleted && spawnPatrolEnabled)
        {
            int desiredPatrol = Mathf.FloorToInt(playerCollisions / additionalCollisionsPerSpawn);
            if (playerCollisions < collisionToStartPatrol) desiredPatrol = 0;
            desiredPatrol = Mathf.Clamp(desiredPatrol, 0, maxPatrolCops);
            TrafficSpawner.Instance?.SetDesiredPatrolCount(desiredPatrol);
        }

        if (isBackupCountdown)
        {
            backupTimer -= Time.deltaTime;
            UpdateBackupText();
            if (backupTimer <= 0f)
            {
                isBackupCountdown = false;
                currentChasePool += backupAddAmount;
                UpdateCopsCountUI();
                ShowBackupAndGarageText("Backup xuất hiện!", 4f);
            }
        }
    }

    public void OnPlayerCollision()
    {
        if (escapeCompleted) return;
        if (isChaseActive)
        {
            totalCollisionsDuringChase++;
            fineAccumulated += finePerCollision;
            UpdateFineUI();
        }
        else
        {
            playerCollisions++;
            // Chỉ set desired patrol nếu được phép spawn (không trong cooldown)
            if (spawnPatrolEnabled && playerCollisions >= collisionToStartPatrol && !isChaseActive)
            {
                int desired = Mathf.Clamp(Mathf.FloorToInt(playerCollisions / additionalCollisionsPerSpawn), 1, maxPatrolCops);
                TrafficSpawner.Instance?.SetDesiredPatrolCount(desired);
            }
        }
    }

    public void OnPatrolDetectedPlayer()
    {
        if (isChaseActive || escapeCompleted) return;
        StartChase();
    }

    void StartChase()
    {
        bustedCooldownActive = false;  // tắt cooldown nếu đang chạy        
        isChaseActive = true;
        escapeCompleted = false;
        spawnPatrolEnabled = false;
        chaseStartTime = Time.time;
        totalCollisionsDuringChase = 0;
        fineAccumulated = 0;
        bustedPercent = 0f;
        escapePercent = 0f;
        currentChasePool = initialChasePool;
        UpdateCopsCountUI();

        // 1. Chuyển toàn bộ patrol cop hiện có thành chase cop
        TrafficSpawner.Instance?.ConvertAllPatrolToChase();
        int currentChaseCount = TrafficSpawner.Instance?.GetCurrentChaseCount() ?? 0;

        // 2. Spawn thêm để đạt số lượng tối đa (nếu cần)
        if (currentChaseCount < maxConcurrentChaseCops)
        {
            int need = maxConcurrentChaseCops - currentChaseCount;
            int canSpawn = Mathf.Min(need, currentChasePool);
            if (canSpawn > 0)
            {
                int newDesired = currentChaseCount + canSpawn;
                TrafficSpawner.Instance?.SetDesiredChaseCount(newDesired);
                currentChasePool -= canSpawn;
            }
            else
            {
                TrafficSpawner.Instance?.SetDesiredChaseCount(currentChaseCount);
            }
        }
        else
        {
            TrafficSpawner.Instance?.SetDesiredChaseCount(currentChaseCount);
        }

        UpdateCopsCountUI();
        copCanvas?.SetActive(true);
        Debug.Log($"[WantedSystem] Chase started. Chase cops count: {currentChaseCount}, pool left: {currentChasePool}");
    }

    void UpdateBustedAndEscape()
    {
        if (player == null || playerController == null) return;
        float speed = playerController.GetVelocityMagnitude();
        bool copNear = IsAnyChaseCopNear(bustedProximityDistance);
        if (speed < bustedSpeedThreshold && copNear)
        {
            bustedPercent += Time.deltaTime / bustedFillTime * 100f;
            if (bustedPercent >= 100f) EndChase(true);
        }
        else bustedPercent -= Time.deltaTime / bustedFillTime * 100f;
        bustedPercent = Mathf.Clamp(bustedPercent, 0f, 100f);

        bool anyCopNearEscape = IsAnyChaseCopNear(escapeCheckRadius);
        if (!anyCopNearEscape)
        {
            escapePercent += Time.deltaTime / escapeFillTime * 100f;
            if (escapePercent >= 100f) EndChase(false);
        }
        else escapePercent -= Time.deltaTime / escapeFillTime * 100f;
        escapePercent = Mathf.Clamp(escapePercent, 0f, 100f);

        SetText(bustedText_TMP, bustedText_UI, $"Busted: {Mathf.FloorToInt(bustedPercent)}%");
        SetText(escapeText_TMP, escapeText_UI, $"Escape: {Mathf.FloorToInt(escapePercent)}%");
    }

    bool IsAnyChaseCopNear(float radius)
    {
        if (TrafficSpawner.Instance == null) return false;
        foreach (var cop in TrafficSpawner.Instance.ActiveChaseCops)
            if (cop != null && player != null && Vector3.Distance(cop.transform.position, player.transform.position) <= radius)
                return true;
        return false;
    }

    void EndChase(bool busted)
    {
        isChaseActive = false;
        if (busted)
        {
            if (bustedCanvas != null)
            {
                SetText(bustedCollisionText_TMP, bustedCollisionText_UI, $"Số va chạm: {totalCollisionsDuringChase}");
                SetText(bustedTimeText_TMP, bustedTimeText_UI, $"Thời gian: {FormatTime(Time.time - chaseStartTime)}");
                SetText(bustedFineText_TMP, bustedFineText_UI, $"Tiền phạt: {Mathf.FloorToInt(fineAccumulated):N0}$");
                bustedCanvas.SetActive(true);
            }
            MoneyManager.Instance?.TrySpend(Mathf.FloorToInt(fineAccumulated));
            if (player != null)
            {
                player.GetComponent<CarInputHandler>()?.SetEnabled(false);
                player.GetComponent<TopDownCarController>()?.SetEnabled(false);
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null) rb.simulated = false;
            }
            
            // BẮT ĐẦU COOLDOWN THAY VÌ CHO SPAWN NGAY
            bustedCooldownActive = true;
            bustedCooldownTimer = 60f;   // Có thể lấy từ Inspector nếu muốn
            spawnPatrolEnabled = false;
            
            // Despawn tất cả cops (giống như escape)
            TrafficSpawner.Instance?.ForceDespawnAllCops();
            playerCollisions = 0;  // Reset số lần va chạm
            TrafficSpawner.Instance?.SetDesiredPatrolCount(0);
            TrafficSpawner.Instance?.SetDesiredChaseCount(0);            
        }
        else // Escape thành công
        {
            escapeCompleted = true;
            escapeCooldownTimer = 60f;
            spawnPatrolEnabled = false;
            copCanvas?.SetActive(false);
            ShowBackupAndGarageText("Escaped!", 4f);
            playerCollisions = 0;
            fineAccumulated = 0;
            currentChasePool = initialChasePool;
            
            TrafficSpawner.Instance?.ForceDespawnAllCops();
            TrafficSpawner.Instance?.SetDesiredChaseCount(0); // THÊM DÒNG NÀY
        }
        // Despawn tất cả cops ngay lập tức
        TrafficSpawner.Instance?.ForceDespawnAllCops();
        Debug.Log("[WantedSystem] Chase ended, all cops despawned.");
    }

    void OnBustedEndPressed()
    {
        bustedCanvas?.SetActive(false);
        if (player != null)
        {
            player.GetComponent<CarInputHandler>()?.SetEnabled(true);
            player.GetComponent<TopDownCarController>()?.SetEnabled(true);
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.simulated = true;
        }
        isChaseActive = false;
        escapeCompleted = false;
        playerCollisions = 0;
        fineAccumulated = 0;
        currentChasePool = initialChasePool;
        TrafficSpawner.Instance?.ForceDespawnAllCops();
        if (copCanvas != null) copCanvas.SetActive(false);
        Debug.Log("[WantedSystem] Busted screen closed, state reset.");
    }

    void OnWarningYes()
    {
        MoneyManager.Instance?.TrySpend(Mathf.FloorToInt(fineAccumulated));
        warningFineCanvas?.SetActive(false);
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main Menu");
    }

    void OnWarningNo()
    {
        warningFineCanvas?.SetActive(false);
    }

    void UpdateCopsCountUI()
    {
        SetText(copsCountText_TMP, copsCountText_UI, $"Cops: {currentChasePool} cars");
    }

    void UpdateFineUI()
    {
        SetText(fineText_TMP, fineText_UI, $"Fine: {Mathf.FloorToInt(fineAccumulated):N0}$");
    }

    void UpdateBackupText()
    {
        if (isBackupCountdown && backupAndGarageTextObject != null)
        {
            string text = "Backup sẽ xuất hiện trong " + FormatBackupTime(backupTimer);
            SetText(backupAndGarageText_TMP, backupAndGarageText_UI, text);
            if (!backupAndGarageTextObject.activeSelf) backupAndGarageTextObject.SetActive(true);
        }
    }

    void ShowBackupAndGarageText(string msg, float duration)
    {
        if (backupAndGarageTextObject == null) return;
        SetText(backupAndGarageText_TMP, backupAndGarageText_UI, msg);
        backupAndGarageTextObject.SetActive(true);
        if (duration > 0) StartCoroutine(HideTextAfterDelay(duration));
    }
    /// <summary>
    /// Hiển thị một thông báo tạm thời lên BackupAndGarageText (dùng cho RaceEvent khóa, v.v.)
    /// </summary>
    public void ShowTemporaryMessage(string msg, float duration)
    {
        if (backupAndGarageTextObject != null)
        {
            SetText(backupAndGarageText_TMP, backupAndGarageText_UI, msg);
            backupAndGarageTextObject.SetActive(true);
            if (duration > 0)
            {
                // Dừng coroutine cũ để tránh chồng chéo và đảm bảo thời gian mới
                StopCoroutine("HideTextAfterDelay");
                StartCoroutine(HideTextAfterDelay(duration));
            }
        }
        else
        {
            Debug.LogWarning("[WantedSystem] backupAndGarageTextObject is null, cannot show message.");
        }
    }    

    IEnumerator HideTextAfterDelay(float d)
    {
        yield return new WaitForSeconds(d);
        if (backupAndGarageTextObject != null) backupAndGarageTextObject.SetActive(false);
    }

    public bool CanEnterGarage()
    {
        if (isChaseActive && !escapeCompleted)
        {
            ShowBackupAndGarageText("Bạn không thể vào Garage lúc này!", 4f);
            return false;
        }
        return true;
    }

    public void OnTryExitToMenu()
    {
        if (isChaseActive && !escapeCompleted && warningFineCanvas != null)
        {
            int fine = Mathf.FloorToInt(fineAccumulated);
            SetText(warningText_TMP, warningText_UI, $"Bạn sẽ phải nộp tiền phạt nếu chưa tẩu thoát khỏi cảnh sát thành công. Bạn có muốn nộp phạt {fine}$ và quay về menu không?");
            warningFineCanvas.SetActive(true);
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Main Menu");
        }
    }

    string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60);
        int s = Mathf.FloorToInt(seconds % 60);
        return $"{m:00}:{s:00}";
    }

    string FormatBackupTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60);
        int s = Mathf.FloorToInt(seconds % 60);
        return $"{m:00}:{s:00}";
    }
}