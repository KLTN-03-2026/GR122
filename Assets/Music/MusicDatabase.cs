using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "MusicDatabase", menuName = "Audio/Music Database")]
public class MusicDatabase : ScriptableObject
{
    public List<MusicTrack> tracks = new List<MusicTrack>();

    public MusicTrack GetTrack(int index)
    {
        if (tracks == null || tracks.Count == 0) return null;
        if (index < 0 || index >= tracks.Count) return null;

        return tracks[index];
    }

    public int Count => tracks != null ? tracks.Count : 0;

#if UNITY_EDITOR
    [ContextMenu("Auto Fill Track Names")]
    private void AutoFillTrackNames()
    {
        foreach (var track in tracks)
        {
            track.AutoFill();
        }

        EditorUtility.SetDirty(this);
        Debug.Log("Auto filled track names.");
    }
#endif
}