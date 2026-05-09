using UnityEngine;
using System.Collections;

public class CarSelectionManager : MonoBehaviour
{
    public CarCatalog catalog;
    public string playerTag = "Player Racer";
    public string selectedKey = "selected_car_id";

    IEnumerator Start()
    {
        yield return null;

        if (catalog == null) yield break;

        string id = PlayerPrefs.GetString(selectedKey, "");
        if (string.IsNullOrEmpty(id))
        {
            var existing = GameObject.FindGameObjectWithTag(playerTag);
            if (existing != null) CinemachineTargetBinder.SetTargetStatic(existing.transform);
            yield break;
        }

        int idx = catalog.IndexOfId(id);
        if (idx < 0)
        {
            var fallback = GameObject.FindGameObjectWithTag(playerTag);
            if (fallback != null) CinemachineTargetBinder.SetTargetStatic(fallback.transform);
            yield break;
        }

        var info = catalog.Get(idx);
        if (info == null || info.prefab == null)
        {
            var fallback2 = GameObject.FindGameObjectWithTag(playerTag);
            if (fallback2 != null) CinemachineTargetBinder.SetTargetStatic(fallback2.transform);
            yield break;
        }

        var playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj == null)
        {
            var newPlayer = Instantiate(info.prefab, Vector3.zero, Quaternion.identity);
            newPlayer.tag = playerTag;
            ApplySavedColor(newPlayer, info.id);  // THÊM DÒNG NÀY
            CinemachineTargetBinder.SetTargetStatic(newPlayer.transform);
            yield break;
        }

        Vector3 pos = playerObj.transform.position;
        Quaternion rot = playerObj.transform.rotation;
        Transform parent = playerObj.transform.parent;

        Destroy(playerObj);

        var newPlayerInstance = Instantiate(info.prefab, pos, rot, parent);
        newPlayerInstance.tag = playerTag;
        ApplySavedColor(newPlayerInstance, info.id);  // THÊM DÒNG NÀY
        CinemachineTargetBinder.SetTargetStatic(newPlayerInstance.transform);
    }

    // HÀM MỚI: Áp dụng màu đã lưu cho xe
    void ApplySavedColor(GameObject car, string carId)
    {
        var colorHandler = car.GetComponentInChildren<CarColorHandler>();
        if (colorHandler == null) return;

        string colorKey = "car_selected_color_" + carId;
        string savedColorId = PlayerPrefs.GetString(colorKey, "");
        if (string.IsNullOrEmpty(savedColorId)) return;

        // Tìm ColorCatalog (có thể load từ Resources hoặc tham chiếu)
        ColorCatalog colorCatalog = Resources.Load<ColorCatalog>("ColorCatalog");
        if (colorCatalog == null)
        {
            Debug.LogWarning("Không tìm thấy ColorCatalog trong Resources!");
            return;
        }

        int colorIdx = colorCatalog.IndexOfId(savedColorId);
        if (colorIdx >= 0)
        {
            var colorInfo = colorCatalog.Get(colorIdx);
            colorHandler.SetBodyColor(colorInfo.colorValue);
            Debug.Log("[CarSelectionManager] Đã áp dụng màu " + colorInfo.displayName + " cho xe " + carId);
        }
    }
}