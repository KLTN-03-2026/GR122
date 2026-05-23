using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioSettingsLoader : MonoBehaviour
{
    [Header("Audio Mixer")]
    public AudioMixer audioMixer;

    private void Awake()
    {
        ApplySavedAudioSettings();
    }

    private void Start()
    {
        ApplySavedAudioSettings();
        StartCoroutine(ApplyAfterOneFrame());
    }

    private IEnumerator ApplyAfterOneFrame()
    {
        yield return null;
        ApplySavedAudioSettings();
    }

    private void ApplySavedAudioSettings()
    {
        if (audioMixer == null)
        {
            Debug.LogError("AudioMixer chưa được gán!");
            return;
        }

        SetMixerVolume("MasterVolume", PlayerPrefs.GetFloat("MasterVolume", 1f));
        SetMixerVolume("MusicVolume", PlayerPrefs.GetFloat("MusicVolume", 1f));
        SetMixerVolume("SFXVolume", PlayerPrefs.GetFloat("SFXVolume", 1f));
    }

    private void SetMixerVolume(string parameterName, float value)
    {
        float db = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;

        bool success = audioMixer.SetFloat(parameterName, db);

        Debug.Log(parameterName + " | value: " + value + " | db: " + db + " | success: " + success);
    }
}