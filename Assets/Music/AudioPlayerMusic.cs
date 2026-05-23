using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AudioPlayerMusic : MonoBehaviour
{
    public static AudioPlayerMusic Instance;

    [Header("Database")]
    public MusicDatabase musicDatabase;

    [Header("Audio Source")]
    public AudioSource musicSource;

    [Header("UI Text")]
    public TMP_Text songNameText;
    public TMP_Text artistNameText;

    [Header("Settings")]
    public bool playOnStart = true;
    public bool loopPlaylist = true;
    public bool shuffle = false;

    private int currentIndex = 0;
    private bool isPaused = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (playOnStart)
    {
    currentIndex = Random.Range(0, musicDatabase.Count);
    PlayTrack(currentIndex);
    }
        else
        {
            UpdateMusicText();
        }
    }

    private void Update()
    {
        if (musicSource == null || musicDatabase == null) return;

        if (!musicSource.isPlaying && musicSource.clip != null && !isPaused)
        {
            PlayNext();
        }
    }

    public void PlayTrack(int index)
    {
        if (musicDatabase == null || musicDatabase.Count == 0) return;

        MusicTrack track = musicDatabase.GetTrack(index);
        if (track == null || track.clip == null) return;

        currentIndex = index;
        isPaused = false;

        musicSource.clip = track.clip;
        musicSource.Play();

        UpdateMusicText();

        Debug.Log("Now Playing: " + track.trackName);
    }

    public void PlayNext()
{
    if (musicDatabase == null || musicDatabase.Count == 0) return;

    int newIndex = currentIndex;

    if (musicDatabase.Count > 1)
    {
        while (newIndex == currentIndex)
        {
            newIndex = Random.Range(0, musicDatabase.Count);
        }
    }

    currentIndex = newIndex;

    PlayTrack(currentIndex);
}

    public void PlayPrevious()
    {
        if (musicDatabase == null || musicDatabase.Count == 0) return;

        currentIndex--;

        if (currentIndex < 0)
            currentIndex = musicDatabase.Count - 1;

        PlayTrack(currentIndex);
    }

    public void TogglePauseMusic()
    {
        if (musicSource == null) return;

        if (musicSource.isPlaying)
        {
            musicSource.Pause();
            isPaused = true;
        }
        else
        {
            musicSource.UnPause();
            isPaused = false;
        }
    }

    public void PauseMusic()
    {
        if (musicSource == null) return;

        musicSource.Pause();
        isPaused = true;
    }

    public void ResumeMusic()
    {
        if (musicSource == null) return;

        musicSource.UnPause();
        isPaused = false;
    }

    public void StopMusic()
    {
        if (musicSource == null) return;

        musicSource.Stop();
        isPaused = false;
    }

    private void UpdateMusicText()
{
    if (musicDatabase == null || musicDatabase.Count == 0)
    {
        if (songNameText != null) songNameText.text = "No Song";
        if (artistNameText != null) artistNameText.text = "Unknown Artist";
        return;
    }

    MusicTrack track = musicDatabase.GetTrack(currentIndex);

    if (track == null)
    {
        if (songNameText != null) songNameText.text = "No Song";
        if (artistNameText != null) artistNameText.text = "Unknown Artist";
        return;
    }

    if (songNameText != null)
    {
        if (!string.IsNullOrEmpty(track.trackName))
            songNameText.text = track.trackName;
        else if (track.clip != null)
            songNameText.text = track.clip.name;
        else
            songNameText.text = "No Song";
    }

    if (artistNameText != null)
    {
        if (!string.IsNullOrEmpty(track.artist))
            artistNameText.text = track.artist;
        else
            artistNameText.text = "Unknown Artist";
    }
}

    public string GetCurrentTrackName()
    {
        if (musicDatabase == null || musicDatabase.Count == 0) return "";

        MusicTrack track = musicDatabase.GetTrack(currentIndex);
        return track != null ? track.trackName : "";
    }

    public string GetCurrentArtistName()
    {
        if (musicDatabase == null || musicDatabase.Count == 0) return "";

        MusicTrack track = musicDatabase.GetTrack(currentIndex);
        return track != null ? track.artist : "";
    }
}