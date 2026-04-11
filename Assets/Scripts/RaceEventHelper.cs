using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class RaceEventHelper : MonoBehaviour
{
    static RaceEventHelper _instance;
    public static RaceEventHelper Instance
    {
        get
        {
            if (_instance == null)
            {
                // create GameObject to host helper if not present
                var go = new GameObject("RaceEventHelper");
                _instance = go.AddComponent<RaceEventHelper>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // Invoke immediate cleanup on the given RaceEventIcon instance (safe even if owner is inactive).
    public void InvokeCleanup(RaceEventIcon owner)
    {
        if (owner == null) return;
        owner.CleanupAfterRaceImmediate();
    }
}
