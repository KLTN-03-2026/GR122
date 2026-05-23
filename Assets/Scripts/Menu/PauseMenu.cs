using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PauseMenu with ESC toggle.
/// - Press ESC to toggle pause/resume.
/// - Methods Pause(), Resume(), Home(), Restart() kept public for Inspector OnClick() usage.
/// - Uses RaceEventIcon.GlobalFreezeRequestedByRace to avoid Resume unfreezing race countdown/result.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] GameObject pauseMenu;

    // Public static flag để các hệ thống khác (vd: countdown) biết game đang Pause.
    public static bool IsPaused { get; private set; } = false;
    void Awake()
    {
        // Reset static pause flag khi scene được load (tránh treo countdown)
        IsPaused = false;
        // Đảm bảo thời gian game bình thường nếu không có race yêu cầu đóng băng
        if (!RaceEventIcon.GlobalFreezeRequestedByRace)
            Time.timeScale = 1f;
    }

    void OnDestroy()
    {
        // Reset static flag khi object bị hủy (khi rời scene)
        IsPaused = false;
        // Phục hồi time scale nếu cần
        if (!RaceEventIcon.GlobalFreezeRequestedByRace)
            Time.timeScale = 1f;
    }
    void Update()
    {
        // Toggle pause when pressing ESC (once per keydown)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    /// <summary>
    /// Toggle between Pause and Resume.
    /// Keeps existing semantics: Pause() will set IsPaused=true and Time.timeScale=0.
    /// Resume() will clear IsPaused and restore Time.timeScale only if no race freeze requested.
    /// </summary>
    public void TogglePause()
    {
        if (IsPaused)
            Resume();
        else
            Pause();
    }

    /// <summary>
    /// Called by UI button or TogglePause().
    /// </summary>
    public void Pause()
    {
        // set flag first so coroutines check immediately
        IsPaused = true;

        if (pauseMenu != null) pauseMenu.SetActive(true);

        // freeze world simulation / physics / Update-time behaviour
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Called by UI button or TogglePause().
    /// Resume will not unfreeze if RaceEventIcon requests a global freeze.
    /// </summary>
    public void Resume()
    {
        // clear pause flag first
        IsPaused = false;

        if (pauseMenu != null) pauseMenu.SetActive(false);

        // Only restore timescale to 1 if no other system currently requests a global freeze.
        bool raceFreezeActive = false;
        try
        {
            raceFreezeActive = RaceEventIcon.GlobalFreezeRequestedByRace;
        }
        catch
        {
            // ignore if RaceEventIcon type not present
            raceFreezeActive = false;
        }

        if (!raceFreezeActive)
            Time.timeScale = 1f;
        else
            Time.timeScale = 0f; // keep it frozen (race-controlled)
    }

    /// <summary>
    /// Return to main menu (public for OnClick)
    /// </summary>
    public void Home()
    {
        if (WantedSystem.Instance != null && WantedSystem.Instance.IsChaseActive)
        {
            WantedSystem.Instance.OnTryExitToMenu();
            return;
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main Menu");
    }

    /// <summary>
    /// Restart current scene (public for OnClick)
    /// </summary>
    public void Restart()
    {
        // clear pause flag
        IsPaused = false;

        // restore time before reload
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
