using UnityEngine;

public class PoliceLightBlink : MonoBehaviour
{
    public Sprite sprite1;
    public Sprite sprite2;
    public float blinkSpeed = 1f;

    private SpriteRenderer sr;
    private float timer;
    private bool toggle;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        sr.sortingOrder = 999;
        sr.sprite = sprite1;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= blinkSpeed)
        {
            timer = 0f;
            toggle = !toggle;

            sr.sprite = toggle ? sprite1 : sprite2;

            // test màu để dễ thấy
            sr.color = toggle ? Color.white : Color.yellow;
        }
    }
}