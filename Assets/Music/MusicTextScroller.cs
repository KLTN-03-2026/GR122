using UnityEngine;
using TMPro;

public class MusicTextScroller : MonoBehaviour
{
    public float speed = 60f;
    public float waitTime = 1.5f;

    private RectTransform rect;
    private TMP_Text textMesh;

    private float startX;
    private float textWidth;
    private bool waiting;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        textMesh = GetComponent<TMP_Text>();

        startX = rect.anchoredPosition.x;
    }

    private void OnEnable()
    {
        ResetScroll();
    }

    private void Update()
    {
        if (waiting) return;

        textWidth = textMesh.preferredWidth;

        rect.anchoredPosition += Vector2.left * speed * Time.deltaTime;

        if (rect.anchoredPosition.x <= -textWidth)
        {
            StartCoroutine(ResetAfterWait());
        }
    }

    private System.Collections.IEnumerator ResetAfterWait()
    {
        waiting = true;

        yield return new WaitForSeconds(waitTime);

        ResetScroll();

        waiting = false;
    }

    public void ResetScroll()
    {
        rect.anchoredPosition =
            new Vector2(startX, rect.anchoredPosition.y);
    }
}