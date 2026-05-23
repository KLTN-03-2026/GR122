using UnityEngine;

[System.Serializable]
public class MusicTrack
{
    [HideInInspector]
    public string trackName;

    public string artist;

    public AudioClip clip;

    [TextArea]
    public string note;

#if UNITY_EDITOR
    public void AutoFill()
    {
        if (clip != null)
        {
            trackName = clip.name;
        }
    }
#endif
}