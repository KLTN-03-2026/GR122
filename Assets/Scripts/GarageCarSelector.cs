using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GarageCarSelector (fixed UI update):
/// - Hỗ trợ 3 trạng thái: originalPlayer, committedPlayer, previewInstance.
/// - Khi mua (unlock) một xe, UI sẽ RefreshSlots() và UpdateSelectedUI() ngay lập tức.
/// - Khi ConfirmSelection(), UI cũng được cập nhật ngay.
/// - Nếu bạn dùng TextMeshPro cho selectButtonText, đổi kiểu tương ứng.
/// </summary>
public class GarageCarSelector : MonoBehaviour
{
    [Header("Catalog")]
    public CarCatalog catalog;

    [Header("Player")]
    public string playerTag = "Player Racer";

    [Header("UI - slots (4)")]
    public Image[] slotImages;          // 4 images showing thumbnails (UI Image)
    public GameObject[] slotLockImages; // overlay lock images for each slot (activate if locked)
    public Button[] slotButtons;        // clickable buttons for each slot (index in visible window)

    [Header("UI - controls")]
    public Button leftButton;
    public Button rightButton;
    public Button selectButton;
    public Text selectButtonText;       // SINGLE text used for both "Chọn" and "$<price>"

    [Header("Behavior")]
    public int visibleCount = 4;
    public string selectedKey = "selected_car_id";

    // runtime state
    int displayStart = 0;               // index in catalog that maps to slotImages[0]
    int selectedAbsoluteIndex = -1;     // absolute index in catalog that is highlighted

    // Players & preview management
    [HideInInspector] public GameObject previewInstance = null;   // instantiated preview while in garage (transient)
    [HideInInspector] public GameObject originalPlayer = null;    // the player that was active when entering garage (may be hidden while previewing)
    [HideInInspector] public GameObject committedPlayer = null;   // the player that was created by ConfirmSelection (persist after exit)
    GameObject previewOriginal = null; // the player GameObject that was hidden when creating preview (could be original or committed)

    bool selectionConfirmed = false;    // true if user pressed Select and a committedPlayer exists

    void OnEnable()
    {
        if (leftButton != null) leftButton.onClick.RemoveAllListeners();
        if (rightButton != null) rightButton.onClick.RemoveAllListeners();
        if (selectButton != null) selectButton.onClick.RemoveAllListeners();

        if (leftButton != null) leftButton.onClick.AddListener(OnLeft);
        if (rightButton != null) rightButton.onClick.AddListener(OnRight);
        if (selectButton != null) selectButton.onClick.AddListener(OnSelectPressed);

        if (slotButtons != null)
        {
            for (int i = 0; i < slotButtons.Length; i++)
            {
                int closureI = i;
                if (slotButtons[i] != null)
                {
                    slotButtons[i].onClick.RemoveAllListeners();
                    slotButtons[i].onClick.AddListener(() => OnSlotClicked(closureI));
                }
            }
        }

        // record the currently active player when opening the garage (if any)
        originalPlayer = GameObject.FindGameObjectWithTag(playerTag);

        displayStart = 0;
        RefreshSlots();
        selectedAbsoluteIndex = displayStart;
        UpdateSelectedUI();
    }

    void OnDisable()
    {
        // if user leaves the garage UI while a preview exists and didn't confirm, revert preview
        if (!selectionConfirmed)
        {
            RevertPreview();
        }
        else
        {
            // if selectionConfirmed and a preview exists (user previewed after commit but didn't confirm),
            // ensure preview is destroyed and committedPlayer remains active
            if (previewInstance != null)
            {
                Destroy(previewInstance);
                previewInstance = null;
            }
            // If committedPlayer got hidden during preview, re-activate it
            if (committedPlayer != null && !committedPlayer.activeInHierarchy)
            {
                committedPlayer.SetActive(true);
                // ensure camera follows committedPlayer
                CinemachineTargetBinder.SetTargetStatic(committedPlayer.transform);
            }
        }

        if (leftButton != null) leftButton.onClick.RemoveAllListeners();
        if (rightButton != null) rightButton.onClick.RemoveAllListeners();
        if (selectButton != null) selectButton.onClick.RemoveAllListeners();
        if (slotButtons != null)
            foreach (var b in slotButtons) if (b != null) b.onClick.RemoveAllListeners();

        // do not reset committedPlayer here — it persists across sessions until next ConfirmSelection overrides it
        originalPlayer = null;
        previewOriginal = null;
    }

    void OnLeft()
    {
        if (catalog == null || catalog.Count == 0) return;
        displayStart = Mathf.Max(0, displayStart - 1);
        RefreshSlots();
    }

    void OnRight()
    {
        if (catalog == null || catalog.Count == 0) return;
        displayStart = Mathf.Min(Math.Max(0, catalog.Count - visibleCount), displayStart + 1);
        RefreshSlots();
    }

    void RefreshSlots()
    {
        for (int s = 0; s < visibleCount; s++)
        {
            int absIdx = displayStart + s;
            Image img = (s < slotImages.Length) ? slotImages[s] : null;
            GameObject lockGO = (s < slotLockImages.Length) ? slotLockImages[s] : null;

            if (img == null) continue;

            if (catalog == null || absIdx >= catalog.Count)
            {
                img.sprite = null;
                img.color = new Color(1,1,1,0);
                if (lockGO != null) lockGO.SetActive(false);
            }
            else
            {
                var info = catalog.Get(absIdx);
                Sprite thumb = info.thumbnail;
                if ((thumb == null) && (info.prefab != null))
                {
                    var sr = info.prefab.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null) thumb = sr.sprite;
                }

                img.sprite = thumb;
                img.color = Color.white;

                bool unlocked = IsUnlocked(info);
                if (lockGO != null) lockGO.SetActive(!unlocked);
            }
        }
        UpdateSelectedUI();
    }

    bool IsUnlocked(CarInfo info)
    {
        if (info == null) return false;
        if (info.unlockedByDefault) return true;
        string key = "car_unlocked_" + info.id;
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    void SetUnlocked(CarInfo info, bool unlocked)
    {
        if (info == null) return;
        string key = "car_unlocked_" + info.id;
        PlayerPrefs.SetInt(key, unlocked ? 1 : 0);
        PlayerPrefs.Save();
    }

    void OnSlotClicked(int slotIndex)
    {
        int absIdx = displayStart + slotIndex;
        if (absIdx < 0 || catalog == null || absIdx >= catalog.Count) return;

        selectedAbsoluteIndex = absIdx;
        UpdateSelectedUI();
        CreatePreviewForIndex(absIdx);
    }

    void UpdateSelectedUI()
    {
        if (catalog == null) return;
        if (selectedAbsoluteIndex < 0 || selectedAbsoluteIndex >= catalog.Count)
        {
            if (selectButtonText != null) selectButtonText.text = "Chọn";
            return;
        }

        var info = catalog.Get(selectedAbsoluteIndex);
        bool unlocked = IsUnlocked(info);
        if (selectButtonText != null)
        {
            if (unlocked)
                selectButtonText.text = "Chọn";
            else
                selectButtonText.text = "$" + info.price.ToString("N0");
        }
    }

    /// <summary>
    /// Create preview vehicle for the given absolute index.
    /// If selectionConfirmed==false: preview hides originalPlayer (the player that existed when entering the garage).
    /// If selectionConfirmed==true: preview hides committedPlayer (the currently committed vehicle).
    /// previewOriginal stores whichever was hidden so we can restore it later.
    /// </summary>
    void CreatePreviewForIndex(int absIdx)
    {
        // destroy any existing previewInstance (we will recreate)
        RevertPreview_Internal(false);

        var info = catalog.Get(absIdx);
        if (info == null || info.prefab == null) return;

        // Decide which player to hide when previewing:
        GameObject toHide = null;

        if (selectionConfirmed && committedPlayer != null)
        {
            // If a committed player exists, preview should hide the committed player while previewing
            toHide = committedPlayer;
        }
        else
        {
            // Otherwise, hide the original player (the one present when entering garage)
            // If originalPlayer is null, try find the current tag (fallback)
            if (originalPlayer == null)
            {
                originalPlayer = GameObject.FindGameObjectWithTag(playerTag);
            }
            toHide = originalPlayer;
        }

        // If there's nothing to hide (edge-case), just pick the first found player by tag (if any)
        if (toHide == null)
        {
            toHide = GameObject.FindGameObjectWithTag(playerTag);
        }

        if (toHide == null)
        {
            Debug.LogWarning("[GarageCarSelector] No player found to use as reference for preview.");
            return;
        }

        // Instantiate preview at the position/rotation/parent of the hidden object
        previewInstance = Instantiate(info.prefab, toHide.transform.position, toHide.transform.rotation, toHide.transform.parent);

        // Ensure preview physics/AI disabled
        var rbs = previewInstance.GetComponentsInChildren<Rigidbody2D>();
        foreach (var rb in rbs) rb.simulated = false;
        var ai = previewInstance.GetComponentInChildren<CarAIHandler>();
        if (ai != null) ai.enabled = false;

        previewInstance.tag = playerTag;

        // hide the original/committed player while previewing
        toHide.SetActive(false);
        previewOriginal = toHide;

        // ensure camera follows preview while previewing
        CinemachineTargetBinder.SetTargetStatic(previewInstance.transform);
    }

    /// <summary>
    /// Restore original/committed player and destroy previewInstance (if any).
    /// Returns the GameObject that should be regarded as the active player after restore.
    /// Priority when restoring:
    /// 1) If preview existed, re-activate previewOriginal (the object hidden during preview) and return it.
    /// 2) Else if committedPlayer exists, return committedPlayer.
    /// 3) Else if originalPlayer exists, return originalPlayer.
    /// 4) Else fallback to finding object by tag (active).
    /// Also ensures Cinemachine follows the returned player.
    /// </summary>
    public GameObject RestoreOriginalPlayerAndDestroyPreview()
    {
        GameObject returned = null;

        // Destroy previewInstance first (we don't want preview to become the persistent player)
        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
        }

        // If a preview had hidden a player, restore that hidden player first.
        if (previewOriginal != null)
        {
            try
            {
                previewOriginal.SetActive(true);
                returned = previewOriginal;
            }
            catch { returned = previewOriginal; } // still return reference even if SetActive failed
            previewOriginal = null;

            // ensure camera follows returned
            if (returned != null) CinemachineTargetBinder.SetTargetStatic(returned.transform);
            return returned;
        }

        // If committed player exists (user had pressed Select earlier), that is the authoritative player.
        if (committedPlayer != null)
        {
            // ensure it's active
            if (!committedPlayer.activeInHierarchy) committedPlayer.SetActive(true);
            returned = committedPlayer;
            if (returned != null) CinemachineTargetBinder.SetTargetStatic(returned.transform);
            return returned;
        }

        // If original player exists (no commit happened), restore it
        if (originalPlayer != null)
        {
            if (!originalPlayer.activeInHierarchy) originalPlayer.SetActive(true);
            returned = originalPlayer;
            if (returned != null) CinemachineTargetBinder.SetTargetStatic(returned.transform);
            return returned;
        }

        // fallback: try find by tag (active)
        var found = GameObject.FindGameObjectWithTag(playerTag);
        if (found != null)
        {
            returned = found;
            if (returned != null) CinemachineTargetBinder.SetTargetStatic(returned.transform);
        }

        return returned;
    }

    void RevertPreview()
    {
        RevertPreview_Internal(true);
    }

    /// <summary>
    /// Internal revert: destroy preview and optionally restore previously hidden player.
    /// </summary>
    void RevertPreview_Internal(bool restoreOriginal)
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
        }
        if (restoreOriginal && previewOriginal != null)
        {
            // restore the object that was hidden while previewing
            try { previewOriginal.SetActive(true); }
            catch { /* ignore */ }
            // if previewOriginal was the committedPlayer, make sure camera follows it
            if (previewOriginal == committedPlayer)
            {
                CinemachineTargetBinder.SetTargetStatic(committedPlayer.transform);
            }
            previewOriginal = null;
        }
    }

    void OnSelectPressed()
    {
        if (catalog == null || selectedAbsoluteIndex < 0 || selectedAbsoluteIndex >= catalog.Count) return;
        var info = catalog.Get(selectedAbsoluteIndex);
        if (info == null) return;

        bool unlocked = IsUnlocked(info);
        if (!unlocked)
        {
            bool ok = MoneyManager.Instance.TrySpend(info.price);
            if (!ok)
            {
                Debug.Log("[GarageCarSelector] Not enough money to buy " + info.displayName);
                return;
            }
            // Mark unlocked in PlayerPrefs
            SetUnlocked(info, true);

            // IMMEDIATE UI UPDATE: refresh slots & selected UI so lock icon and button text update now
            RefreshSlots();
            UpdateSelectedUI();
        }

        ConfirmSelection(info);
    }

    /// <summary>
    /// Confirm selection: instantiate chosen prefab permanently (committedPlayer).
    /// Transfer disabled-controls from the old player (if any) to the committed one via GarageIcon.TransferDisabledControls.
    /// </summary>
    void ConfirmSelection(CarInfo info)
    {
        // mark selection confirmed
        selectionConfirmed = true;

        // Determine the currently active player to be replaced (could be originalPlayer or a found object)
        GameObject playerObj = originalPlayer ?? GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj == null)
        {
            Debug.LogWarning("[GarageCarSelector] Player object not found for confirming car selection");
            return;
        }

        Vector3 pos = playerObj.transform.position;
        Quaternion rot = playerObj.transform.rotation;
        Transform parent = playerObj.transform.parent;

        // keep reference to old player before destroy
        GameObject oldPlayer = playerObj;

        // Destroy old player (may be original or previously committed)
        try
        {
            Destroy(playerObj);
        }
        catch { /* ignore */ }

        // instantiate chosen prefab at same location (remain inside garage)
        var newPlayer = Instantiate(info.prefab, pos, rot, parent);
        newPlayer.tag = playerTag;

        // set committedPlayer reference
        committedPlayer = newPlayer;

        // Make sure controls remain disabled: transfer disabled state via GarageIcon if possible
        var garageIcons = FindObjectsOfType<GarageIcon>();
        foreach (var gi in garageIcons)
        {
            if (gi != null && gi.garageUI != null && gi.garageUI.activeInHierarchy)
            {
                gi.TransferDisabledControls(oldPlayer, newPlayer);
                break;
            }
        }

        // ensure physics disabled until exit
        var rb = newPlayer.GetComponent<Rigidbody2D>() ?? newPlayer.GetComponentInChildren<Rigidbody2D>();
        if (rb != null) rb.simulated = false;

        // cleanup any previous preview
        if (previewInstance != null) Destroy(previewInstance);
        previewInstance = null;

        // clear originalPlayer and previewOriginal (we now have a committed player)
        originalPlayer = null;
        previewOriginal = null;

        // Save selection persistently
        PlayerPrefs.SetString(selectedKey, info.id);
        PlayerPrefs.Save();

        // Make Cinemachine follow new player right away
        CinemachineTargetBinder.SetTargetStatic(newPlayer.transform);

        // UI refresh: ensure slot lock icons and select button reflect new state right away
        RefreshSlots();
        UpdateSelectedUI();

        Debug.Log("[GarageCarSelector] Selected car: " + info.displayName);
    }
}
