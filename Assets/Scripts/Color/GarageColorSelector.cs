using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GarageColorSelector : MonoBehaviour
{
    [Header("Catalog")]
    public ColorCatalog catalog;

    [Header("UI - slots")]
    public Image[] slotImages;
    public GameObject[] slotLockImages;
    public Button[] slotButtons;

    [Header("UI - controls")]
    public Button leftButton;
    public Button rightButton;
    public Button selectButton;
    public Button closeButton;
    public Text selectButtonText;

    [Header("Settings")]
    public int visibleCount = 8;
    public string selectedColorKey = "car_selected_color_";

    [Header("References")]
    public GameObject carSelectorPanel;

    // Runtime
    int displayStart = 0;
    int selectedAbsoluteIndex = -1;
    public string currentCarId = "";
    public GameObject currentPreviewCar = null;
    Color currentPreviewColor = Color.white;
    Color savedColor = Color.white;
    Color originalColor = Color.white;
    bool hasChanges = false;

    void OnEnable()
    {
        if (carSelectorPanel != null)
            carSelectorPanel.SetActive(false);

        if (leftButton != null) leftButton.onClick.AddListener(OnLeft);
        if (rightButton != null) rightButton.onClick.AddListener(OnRight);
        if (selectButton != null) selectButton.onClick.AddListener(OnSelectPressed);
        if (closeButton != null) closeButton.onClick.AddListener(OnClosePressed);

        for (int i = 0; i < slotButtons.Length; i++)
        {
            int idx = i;
            if (slotButtons[idx] != null)
            {
                slotButtons[idx].onClick.RemoveAllListeners();
                slotButtons[idx].onClick.AddListener(() => OnSlotClicked(idx));
            }
        }

        displayStart = 0;
        RefreshSlots();
        selectedAbsoluteIndex = displayStart;
        UpdateSelectedUI();
    }

    void OnDisable()
    {
        // Khôi phục màu nếu có thay đổi chưa được xác nhận (chưa bấm Select)
        if (hasChanges && currentPreviewCar != null)
        {
            var handler = currentPreviewCar.GetComponentInChildren<CarColorHandler>();
            if (handler != null)
            {
                handler.SetBodyColor(originalColor);
                Debug.Log("[GarageColorSelector] Khôi phục màu gốc (chưa xác nhận): " + originalColor);
            }
        }
        if (carSelectorPanel != null)
            carSelectorPanel.SetActive(true);
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
        displayStart = Mathf.Min(Mathf.Max(0, catalog.Count - visibleCount), displayStart + 1);
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
                img.color = new Color(1, 1, 1, 0);
                if (lockGO != null) lockGO.SetActive(false);
            }
            else
            {
                var color = catalog.Get(absIdx);
                img.color = color.colorValue;
                
                if (color.thumbnail != null)
                    img.sprite = color.thumbnail;
                else
                    img.sprite = null;

                bool unlocked = IsColorUnlocked(color);
                if (lockGO != null) lockGO.SetActive(!unlocked);
            }
        }
        UpdateSelectedUI();
    }

    bool IsColorUnlocked(ColorInfo color)
    {
        if (color == null) return false;
        if (color.unlockedByDefault) return true;
        string key = "color_unlocked_" + currentCarId + "_" + color.id;
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    void SetColorUnlocked(ColorInfo color, bool unlocked)
    {
        if (color == null) return;
        string key = "color_unlocked_" + currentCarId + "_" + color.id;
        PlayerPrefs.SetInt(key, unlocked ? 1 : 0);
        PlayerPrefs.Save();
    }

    void OnSlotClicked(int slotIndex)
    {
        // Tìm lại xe nếu bị null
        if (currentPreviewCar == null || !currentPreviewCar.activeInHierarchy)
        {
            currentPreviewCar = GameObject.FindGameObjectWithTag("Player Racer");
            if (currentPreviewCar == null) return;
        }
        
        int absIdx = displayStart + slotIndex;
        if (absIdx < 0 || catalog == null || absIdx >= catalog.Count) return;

        selectedAbsoluteIndex = absIdx;
        UpdateSelectedUI();
        
        var color = catalog.Get(selectedAbsoluteIndex);
        if (color == null) return;
        
        // Tìm CarColorHandler trong children
        var handler = currentPreviewCar.GetComponentInChildren<CarColorHandler>();
        
        if (handler != null)
        {
            currentPreviewColor = color.colorValue;
            handler.SetBodyColor(currentPreviewColor);
            hasChanges = true;
            Debug.Log("[GarageColorSelector] Preview màu: " + color.displayName);
        }
        else
        {
            Debug.LogError("[GarageColorSelector] Không tìm thấy CarColorHandler!");
        }
    }

    void UpdateSelectedUI()
    {
        if (catalog == null || selectedAbsoluteIndex < 0 || selectedAbsoluteIndex >= catalog.Count)
        {
            if (selectButtonText != null) selectButtonText.text = "Chọn";
            return;
        }

        var color = catalog.Get(selectedAbsoluteIndex);
        bool unlocked = IsColorUnlocked(color);
        
        if (selectButtonText != null)
        {
            if (unlocked)
                selectButtonText.text = "Chọn";
            else
                selectButtonText.text = "$" + color.price.ToString("N0");
        }
    }

    public void OnSelectPressed()
    {
        if (selectedAbsoluteIndex < 0 || selectedAbsoluteIndex >= catalog.Count) return;
        
        var color = catalog.Get(selectedAbsoluteIndex);
        if (color == null) return;

        bool unlocked = IsColorUnlocked(color);
        
        if (!unlocked)
        {
            bool ok = MoneyManager.Instance.TrySpend(color.price);
            if (!ok)
            {
                Debug.Log("[GarageColorSelector] Không đủ tiền");
                return;
            }
            SetColorUnlocked(color, true);
            RefreshSlots();
            UpdateSelectedUI();
        }
        
        // Lưu màu
        string key = selectedColorKey + currentCarId;
        PlayerPrefs.SetString(key, color.id);
        PlayerPrefs.Save();
        
        // Cập nhật màu gốc và đánh dấu đã thay đổi
        originalColor = color.colorValue;
        hasChanges = false;   // Đánh dấu đã xác nhận, không còn thay đổi chưa lưu
        
        // Preview màu lên xe
        if (currentPreviewCar != null)
        {
            var handler = currentPreviewCar.GetComponentInChildren<CarColorHandler>();
            if (handler != null)
            {
                handler.SetBodyColor(color.colorValue);
            }
        }
        
        Debug.Log("[GarageColorSelector] Đã chọn và lưu màu: " + color.displayName);
    }

    void OnClosePressed()
    {
        gameObject.SetActive(false);
        
        if (carSelectorPanel != null)
            carSelectorPanel.SetActive(true);
        
        Debug.Log("[GarageColorSelector] Đóng garage");
    }

    public void OpenPanel(string carId, GameObject previewCar)
    {
        currentCarId = carId;
        currentPreviewCar = previewCar;
        hasChanges = false;
        
        // Lưu màu gốc của xe
        if (currentPreviewCar != null)
        {
            var handler = currentPreviewCar.GetComponentInChildren<CarColorHandler>();
            if (handler != null)
            {
                originalColor = handler.GetBodyColor();
                Debug.Log("[GarageColorSelector] Màu gốc của xe: " + originalColor);
            }
            else
            {
                Debug.LogError("[GarageColorSelector] Không tìm thấy CarColorHandler!");
            }
        }
        hasChanges = false;   // Đặt lại trạng thái chưa có thay đổi khi mở panel
        // Ẩn panel chọn xe
        if (carSelectorPanel != null)
            carSelectorPanel.SetActive(false);
        
        // Load màu đã lưu
        string key = selectedColorKey + carId;
        string savedColorId = PlayerPrefs.GetString(key, "");
        
        if (!string.IsNullOrEmpty(savedColorId))
        {
            int idx = catalog.IndexOfId(savedColorId);
            if (idx >= 0) 
            {
                selectedAbsoluteIndex = idx;
                var color = catalog.Get(idx);
                savedColor = color.colorValue;
                currentPreviewColor = savedColor;
            }
            else 
            {
                selectedAbsoluteIndex = 0;
                savedColor = catalog.Get(0).colorValue;
                currentPreviewColor = savedColor;
            }
        }
        else
        {
            selectedAbsoluteIndex = 0;
            savedColor = catalog.Get(0).colorValue;
            currentPreviewColor = savedColor;
        }
        
        if (selectedAbsoluteIndex < displayStart)
            displayStart = selectedAbsoluteIndex;
        else if (selectedAbsoluteIndex >= displayStart + visibleCount)
            displayStart = Mathf.Max(0, selectedAbsoluteIndex - visibleCount + 1);
        
        RefreshSlots();
        
        // Preview màu đã lưu lên xe
        if (previewCar != null)
        {
            var handler = previewCar.GetComponentInChildren<CarColorHandler>();
            if (handler != null)
            {
                handler.SetBodyColor(savedColor);
            }
        }
        
        gameObject.SetActive(true);
    }
}