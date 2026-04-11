using UnityEngine;

[DisallowMultipleComponent]
public class SpawnPoint : MonoBehaviour
{
    [Tooltip("Danh sách prefab AI có thể spawn (chọn 1 hoặc nhiều). Nếu để 0 phần tử thì không spawn.")]
    public GameObject[] aiPrefabs;

    [Tooltip("Nếu true sẽ chọn ngẫu nhiên 1 prefab từ aiPrefabs, nếu false sẽ dùng indexPrefabToUse.")]
    public bool randomizePrefab = true;

    [Tooltip("Index prefab sử dụng khi randomizePrefab = false")]
    public int indexPrefabToUse = 0;

    [Tooltip("Góc Z (degrees) muốn áp dụng cho xe spawn (top-down): 0 = up, 90 = right, 180 = down, 270 = left")]
    public float zRotation = 0f;

    [Tooltip("Offset nếu muốn spawn hơi lệch khỏi transform.position")]
    public Vector3 localOffset = Vector3.zero;

    /// <summary>
    /// Instantiate an AI car at this spawn point. Returns the spawned GameObject or null.
    /// </summary>
    public GameObject SpawnAI()
    {
        if (aiPrefabs == null || aiPrefabs.Length == 0) return null;

        GameObject prefab = null;
        if (randomizePrefab)
        {
            prefab = aiPrefabs[Random.Range(0, aiPrefabs.Length)];
        }
        else
        {
            int idx = Mathf.Clamp(indexPrefabToUse, 0, aiPrefabs.Length - 1);
            prefab = aiPrefabs[idx];
        }

        if (prefab == null) return null;

        Vector3 worldPos = transform.position + transform.TransformVector(localOffset);
        Quaternion rot = Quaternion.Euler(0f, 0f, zRotation);

        GameObject go = GameObject.Instantiate(prefab, worldPos, rot);
        go.name = prefab.name + "_spawned";

        return go;
    }
}
