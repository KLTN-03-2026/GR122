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
    public Image[] slotImages;
    public GameObject[] slotLockImages;
    public Button[] slotButtons;

    [Header("UI - controls")]
    public Button leftButton;
    public Button rightButton;
    public Button selectButton;
    public Text selectButtonText;

    [Header("Behavior")]
    public int visibleCount = 4;
    public string selectedKey = "selected_car_id";
    
    [Header("Layout Groups")]
    public GameObject carLayoutGroup;
    public GameObject colorLayoutGroup;

    [Header("Switch Buttons")]
    public Button switchToCarButton;
    public Button switchToColorButton;
    
    [Header("Color Selector")]
    public GarageColorSelector colorSelector;

    // runtime state
    int displayStart = 0;
    int selectedAbsoluteIndex = -1;

    // Players & preview management
    [HideInInspector] public GameObject previewInstance = null;
    [HideInInspector] public GameObject originalPlayer = null;
    [HideInInspector] public GameObject committedPlayer = null;
    GameObject previewOriginal = null;
    bool selectionConfirmed = false;

    void Start()
    {
        if (switchToCarButton != null)
            switchToCarButton.onClick.AddListener(ShowCarLayout);
        
        if (switchToColorButton != null)
            switchToColorButton.onClick.AddListener(ShowColorLayout);
        
        // Bắt đầu với car layout
        ShowCarLayout();
    }

    void ShowCarLayout()
    {
        if (carLayoutGroup != null) carLayoutGroup.SetActive(true);
        if (colorLayoutGroup != null) colorLayoutGroup.SetActive(false);
    }

    void ShowColorLayout()
    {
        if (carLayoutGroup != null) carLayoutGroup.SetActive(false);
        if (colorLayoutGroup != null) colorLayoutGroup.SetActive(true);
        
        // Cập nhật currentPreviewCar cho color selector
        if (colorSelector != null && previewInstance != null)
        {
            colorSelector.currentPreviewCar = previewInstance;
            if (selectedAbsoluteIndex >= 0 && selectedAbsoluteIndex < catalog.Count)
            {
                var info = catalog.Get(selectedAbsoluteIndex);
                if (info != null) colorSelector.currentCarId = info.id;
            }
        }
    }

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

        originalPlayer = GameObject.FindGameObjectWithTag(playerTag);

        displayStart = 0;
        RefreshSlots();
        selectedAbsoluteIndex = displayStart;
        UpdateSelectedUI();
    }

    void OnDisable()
    {
        if (!selectionConfirmed)
        {
            RevertPreview();
        }
        else
        {
            if (previewInstance != null)
            {
                Destroy(previewInstance);
                previewInstance = null;
            }
            if (committedPlayer != null && !committedPlayer.activeInHierarchy)
            {
                committedPlayer.SetActive(true);
                CinemachineTargetBinder.SetTargetStatic(committedPlayer.transform);
            }
        }

        if (leftButton != null) leftButton.onClick.RemoveAllListeners();
        if (rightButton != null) rightButton.onClick.RemoveAllListeners();
        if (selectButton != null) selectButton.onClick.RemoveAllListeners();
        if (slotButtons != null)
            foreach (var b in slotButtons) if (b != null) b.onClick.RemoveAllListeners();

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

    void CreatePreviewForIndex(int absIdx)
    {
        RevertPreview_Internal(false);

        var info = catalog.Get(absIdx);
        if (info == null || info.prefab == null) return;

        GameObject toHide = null;

        if (selectionConfirmed && committedPlayer != null)
        {
            toHide = committedPlayer;
        }
        else
        {
            if (originalPlayer == null)
            {
                originalPlayer = GameObject.FindGameObjectWithTag(playerTag);
            }
            toHide = originalPlayer;
        }

        if (toHide == null)
        {
            toHide = GameObject.FindGameObjectWithTag(playerTag);
        }

        if (toHide == null)
        {
            Debug.LogWarning("[GarageCarSelector] No player found to use as reference for preview.");
            return;
        }

        previewInstance = Instantiate(info.prefab, toHide.transform.position, toHide.transform.rotation, toHide.transform.parent);

        var rbs = previewInstance.GetComponentsInChildren<Rigidbody2D>();
        foreach (var rb in rbs) rb.simulated = false;
        var ai = previewInstance.GetComponentInChildren<CarAIHandler>();
        if (ai != null) ai.enabled = false;

        previewInstance.tag = playerTag;

        toHide.SetActive(false);
        previewOriginal = toHide;

        CinemachineTargetBinder.SetTargetStatic(previewInstance.transform);
    }

    public GameObject RestoreOriginalPlayerAndDestroyPreview()
    {
        GameObject returned = null;

        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
        }

        if (previewOriginal != null)
        {
            try
            {
                previewOriginal.SetActive(true);
                returned = previewOriginal;
            }
            catch { returned = previewOriginal; }
            previewOriginal = null;

            if (returned != null) CinemachineTargetBinder.SetTargetStatic(returned.transform);
            return returned;
        }

        if (committedPlayer != null)
        {
            if (!committedPlayer.activeInHierarchy) committedPlayer.SetActive(true);
            returned = committedPlayer;
            if (returned != null) CinemachineTargetBinder.SetTargetStatic(returned.transform);
            return returned;
        }

        if (originalPlayer != null)
        {
            if (!originalPlayer.activeInHierarchy) originalPlayer.SetActive(true);
            returned = originalPlayer;
            if (returned != null) CinemachineTargetBinder.SetTargetStatic(returned.transform);
            return returned;
        }

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

    void RevertPreview_Internal(bool restoreOriginal)
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
        }
        if (restoreOriginal && previewOriginal != null)
        {
            try { previewOriginal.SetActive(true); }
            catch { }
            if (previewOriginal == committedPlayer)
            {
                CinemachineTargetBinder.SetTargetStatic(committedPlayer.transform);
            }
            previewOriginal = null;
        }
    }

    void OnSelectPressed()
    {
        if (carLayoutGroup != null && carLayoutGroup.activeInHierarchy)
        {
            // XỬ LÝ CHỌN XE
            Debug.Log("Đang xử lý chọn xe");
            
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
                SetUnlocked(info, true);
                RefreshSlots();
                UpdateSelectedUI();
            }

            ConfirmSelection(info);
        }
        else if (colorLayoutGroup != null && colorLayoutGroup.activeInHierarchy)
        {
            // XỬ LÝ CHỌN MÀU
            Debug.Log("Đang xử lý chọn màu");
            
            if (colorSelector != null)
            {
                colorSelector.OnSelectPressed();
            }
            else
            {
                Debug.LogWarning("[GarageCarSelector] colorSelector chưa được gán!");
            }
        }
    }

    void ConfirmSelection(CarInfo info)
    {
        selectionConfirmed = true;

        GameObject playerObj = originalPlayer ?? GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj == null)
        {
            Debug.LogWarning("[GarageCarSelector] Player object not found for confirming car selection");
            return;
        }

        Vector3 pos = playerObj.transform.position;
        Quaternion rot = playerObj.transform.rotation;
        Transform parent = playerObj.transform.parent;

        GameObject oldPlayer = playerObj;

        try
        {
            Destroy(playerObj);
        }
        catch { }

        var newPlayer = Instantiate(info.prefab, pos, rot, parent);
        newPlayer.tag = playerTag;

        committedPlayer = newPlayer;

        var garageIcons = FindObjectsOfType<GarageIcon>();
        foreach (var gi in garageIcons)
        {
            if (gi != null && gi.garageUI != null && gi.garageUI.activeInHierarchy)
            {
                gi.TransferDisabledControls(oldPlayer, newPlayer);
                break;
            }
        }

        var rb = newPlayer.GetComponent<Rigidbody2D>() ?? newPlayer.GetComponentInChildren<Rigidbody2D>();
        if (rb != null) rb.simulated = false;

        if (previewInstance != null) Destroy(previewInstance);
        previewInstance = null;

        originalPlayer = null;
        previewOriginal = null;

        PlayerPrefs.SetString(selectedKey, info.id);
        PlayerPrefs.Save();

        CinemachineTargetBinder.SetTargetStatic(newPlayer.transform);

        RefreshSlots();
        UpdateSelectedUI();

        Debug.Log("[GarageCarSelector] Selected car: " + info.displayName);
    }
}