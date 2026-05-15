using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using System.Collections;

public class AudioSettingsManager : MonoBehaviour
{
    [Header("Audio Mixer")]
    public AudioMixer audioMixer;

    [Header("Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Slider Canvas Groups")]
    public CanvasGroup musicSliderGroup;
    public CanvasGroup sfxSliderGroup;

    [Header("Fade Settings")]
    public float fadeDuration = 0.25f;
    public float fadedAlpha = 0.4f;

    private Coroutine musicFadeCoroutine;
    private Coroutine sfxFadeCoroutine;

    private void Start()
    {
        // Load saved values
        masterSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);

        // Listeners
        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        // Apply values
        SetMasterVolume(masterSlider.value);
        SetMusicVolume(musicSlider.value);
        SetSFXVolume(sfxSlider.value);
    }

    public void SetMasterVolume(float value)
    {
        float volume = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;

        audioMixer.SetFloat("MasterVolume", volume);

        PlayerPrefs.SetFloat("MasterVolume", value);

        bool muted = value <= 0.001f;

        musicSlider.interactable = !muted;
        sfxSlider.interactable = !muted;

        float targetAlpha = muted ? fadedAlpha : 1f;

        if (musicFadeCoroutine != null)
            StopCoroutine(musicFadeCoroutine);

        if (sfxFadeCoroutine != null)
            StopCoroutine(sfxFadeCoroutine);

        musicFadeCoroutine =
            StartCoroutine(FadeCanvasGroup(musicSliderGroup, targetAlpha));

        sfxFadeCoroutine =
            StartCoroutine(FadeCanvasGroup(sfxSliderGroup, targetAlpha));
    }

    public void SetMusicVolume(float value)
    {
        float volume = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;

        audioMixer.SetFloat("MusicVolume", volume);

        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    public void SetSFXVolume(float value)
    {
        float volume = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;

        audioMixer.SetFloat("SFXVolume", volume);

        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float targetAlpha)
    {
        float startAlpha = group.alpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;

            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);

            yield return null;
        }

        group.alpha = targetAlpha;
    }
}