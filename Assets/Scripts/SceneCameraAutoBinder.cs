using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Khi scene được load, sau 1 khoảng delay ngắn sẽ tìm player theo tag và gọi CinemachineTargetBinder.SetTargetStatic
/// Gắn component này lên GameManagers hoặc một GameObject persistent trong scene.
/// </summary>
public class SceneCameraAutoBinder : MonoBehaviour
{
    [Tooltip("Tag của player object")]
    public string playerTag = "Player Racer";

    [Tooltip("Delay (giây realtime) sau scene load để bind camera. Thử nghiệm: 0.05 - 0.2")]
    public float delayAfterLoad = 0.06f;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(DelayedBind());
    }

    IEnumerator DelayedBind()
    {
        // chờ 1 vài frame / thời gian nhỏ để mọi object spawn / assembly load xong
        yield return new WaitForSecondsRealtime(delayAfterLoad);

        var player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            CinemachineTargetBinder.SetTargetStatic(player.transform);
            yield break;
        }

        // Nếu chưa tìm thấy player, thử lại một lần sau 1 frame
        yield return null;
        player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            CinemachineTargetBinder.SetTargetStatic(player.transform);
        }
    }
}
