using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DynamicButton : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    private Image image;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(0.85f, 0.85f, 0.85f);
    public Color pressedColor = new Color(0.6f, 0.6f, 0.6f);

    [Header("Animation")]
    public float speed = 8f;

    [Header("Scale")]
    public bool useScale = true;
    public Vector3 normalScale = Vector3.one;
    public Vector3 hoverScale = new Vector3(1.05f, 1.05f, 1.05f);

    private Color targetColor;
    private Vector3 targetScale;

    private bool isHover;
    private bool isPressed;

    void Start()
    {
        image = GetComponent<Image>();

        targetColor = normalColor;
        targetScale = normalScale;
    }

    void Update()
    {
        image.color = Color.Lerp(image.color, targetColor, Time.deltaTime * speed);

        if (useScale)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHover = true;
        UpdateVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHover = false;
        isPressed = false;
        UpdateVisual();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        UpdateVisual();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (isPressed)
        {
            targetColor = pressedColor;
            if (useScale) targetScale = normalScale * 0.95f;
        }
        else if (isHover)
        {
            targetColor = hoverColor;
            if (useScale) targetScale = hoverScale;
        }
        else
        {
            targetColor = normalColor;
            if (useScale) targetScale = normalScale;
        }
    }
}