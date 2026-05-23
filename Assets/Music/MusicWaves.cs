using UnityEngine;

public class MusicWaves : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource musicSource;

    [Header("Bars")]
    public RectTransform[] bars;

    [Header("Settings")]
    public float minHeight = 6f;
    public float maxHeight = 45f;
    public float sensitivity = 900f;
    public float smoothSpeed = 18f;

    private float[] spectrum = new float[128];

    private void Update()
    {
        if (musicSource == null || bars == null || bars.Length == 0)
            return;

        if (!musicSource.isPlaying)
        {
            ResetBars();
            return;
        }

        musicSource.GetSpectrumData(
            spectrum,
            0,
            FFTWindow.BlackmanHarris
        );

        for (int i = 0; i < bars.Length; i++)
        {
            float normalized =
                Mathf.Abs(
                    (i - (bars.Length - 1) / 2f)
                    / (bars.Length / 2f)
                );

            int spectrumIndex =
                Mathf.RoundToInt(
                    Mathf.Lerp(35, 2, normalized)
                );

            spectrumIndex =
                Mathf.Clamp(
                    spectrumIndex,
                    0,
                    spectrum.Length - 1
                );

            float value =
                spectrum[spectrumIndex] * sensitivity;

            float targetHeight =
                Mathf.Clamp(
                    value,
                    minHeight,
                    maxHeight
                );

            Vector2 size = bars[i].sizeDelta;

            size.y = Mathf.Lerp(
                size.y,
                targetHeight,
                Time.unscaledDeltaTime * smoothSpeed
            );

            bars[i].sizeDelta = size;
        }
    }

    private void ResetBars()
    {
        foreach (RectTransform bar in bars)
        {
            Vector2 size = bar.sizeDelta;

            size.y = Mathf.Lerp(
                size.y,
                minHeight,
                Time.unscaledDeltaTime * smoothSpeed
            );

            bar.sizeDelta = size;
        }
    }
}