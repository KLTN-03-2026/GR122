using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingManager : MonoBehaviour
{
    // 👇 THÊM DÒNG NÀY (singleton instance)
    public static LoadingManager Instance { get; private set; }

    public GameObject loadingPanel;

    void Awake()
    {
        // 👇 THÊM CÁC DÒNG NÀY VÀO ĐẦU HÀM Awake (hoặc tạo hàm Awake nếu chưa có)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadRoutine(sceneName));
    }

    // 👇 THÊM PHƯƠNG THỨC NÀY (overload nhận build index)
    public void LoadScene(int sceneBuildIndex)
    {
        StartCoroutine(LoadRoutine(sceneBuildIndex));
    }

    IEnumerator LoadRoutine(string sceneName)
    {
        Debug.Log("START LOADING");
        loadingPanel.SetActive(true);
        yield return null;
        yield return new WaitForSeconds(5f);
        Debug.Log("LOAD SCENE NOW");
        SceneManager.LoadScene(sceneName);
    }

    // 👇 THÊM COROUTINE MỚI CHO BUILD INDEX
    IEnumerator LoadRoutine(int sceneBuildIndex)
    {
        Debug.Log("START LOADING");
        loadingPanel.SetActive(true);
        yield return null;
        yield return new WaitForSeconds(5f);
        Debug.Log("LOAD SCENE NOW");
        SceneManager.LoadScene(sceneBuildIndex);
    }
}