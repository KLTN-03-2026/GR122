using UnityEngine;
using System.Collections;

public class CarSelectionManager : MonoBehaviour
{
    public CarCatalog catalog;
    public string playerTag = "Player Racer";
    public string selectedKey = "selected_car_id";

    IEnumerator Start()
    {
        // đợi 1 frame để cho Unity hoàn tất khởi tạo các assembly / camera
        yield return null;

        if (catalog == null) yield break;

        string id = PlayerPrefs.GetString(selectedKey, "");
        if (string.IsNullOrEmpty(id))
        {
            // không có selection, cố gắng bind camera đến player hiện tại (nếu có)
            var existing = GameObject.FindGameObjectWithTag(playerTag);
            if (existing != null)
            {
                CinemachineTargetBinder.SetTargetStatic(existing.transform);
            }
            yield break;
        }

        int idx = catalog.IndexOfId(id);
        if (idx < 0) 
        {
            // không tìm thấy trong catalog -> bind existing
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

        // tìm player hiện tại để nhận vị trí / parent
        var playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj == null)
        {
            // nếu không có player, instantiate trực tiếp
            var newPlayer = Instantiate(info.prefab, Vector3.zero, Quaternion.identity);
            newPlayer.tag = playerTag;
            CinemachineTargetBinder.SetTargetStatic(newPlayer.transform);
            yield break;
        }

        Vector3 pos = playerObj.transform.position;
        Quaternion rot = playerObj.transform.rotation;
        Transform parent = playerObj.transform.parent;

        // destroy old object và instantiate new selected prefab
        Destroy(playerObj);

        var newPlayerInstance = Instantiate(info.prefab, pos, rot, parent);
        newPlayerInstance.tag = playerTag;

        // bind camera cho newPlayer (an toàn vì đã chờ 1 frame)
        CinemachineTargetBinder.SetTargetStatic(newPlayerInstance.transform);
    }
}
