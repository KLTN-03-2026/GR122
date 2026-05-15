using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingManager : MonoBehaviour
{
    public GameObject loadingPanel;

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadRoutine(sceneName));
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
}