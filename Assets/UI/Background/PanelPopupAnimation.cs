using UnityEngine;
using System.Collections;

public class PanelPopupAnimation : MonoBehaviour
{
    [Header("Animation")]
    public float duration = 0.2f;

    [Range(0f, 1f)]
    public float startScaleMultiplier = 0.75f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private Vector3 originalScale;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Save original scale
        originalScale = rectTransform.localScale;
    }

    private void OnEnable()
    {
        StartCoroutine(PlayAnimation());
    }
private IEnumerator PlayAnimation()
{
    yield return null;

    float time = 0f;

    Vector3 startScale = originalScale * startScaleMultiplier;
    Vector3 overshootScale = originalScale * 1.05f;

    rectTransform.localScale = startScale;
    canvasGroup.alpha = 0f;

    while (time < duration)
    {
        time += Time.unscaledDeltaTime;

        float t = time / duration;
        t = 1f - Mathf.Pow(1f - t, 3f);

        rectTransform.localScale = Vector3.Lerp(startScale, overshootScale, t);
        canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

        yield return null;
    }

    rectTransform.localScale = originalScale;
    canvasGroup.alpha = 1f;
}}