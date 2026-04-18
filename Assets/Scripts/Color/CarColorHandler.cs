using UnityEngine;

/// <summary>
/// CarColorHandler - Xử lý đổi màu cho xe (thân xe)
/// Gắn component này lên prefab xe
/// </summary>
public class CarColorHandler : MonoBehaviour
{
    [Header("Body Parts")]
    [Tooltip("SpriteRenderer của phần thân xe (sẽ đổi màu)")]
    public SpriteRenderer bodyRenderer;
    
    [Header("Other Parts (không đổi màu)")]
    [Tooltip("Các phần khác của xe như viền, đèn, kính... (giữ nguyên màu)")]
    public SpriteRenderer outlineRenderer;
    public SpriteRenderer headlightRenderer;
    public SpriteRenderer windowRenderer;
    
    [Header("Settings")]
    public Color defaultColor = Color.red;
    
    // Runtime
    private Color currentColor;
    
    void Awake()
    {
        // Tự động tìm bodyRenderer nếu chưa gán
        if (bodyRenderer == null)
        {
            bodyRenderer = GetComponent<SpriteRenderer>();
            
            // Nếu vẫn chưa có, tìm trong children
            if (bodyRenderer == null)
                bodyRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }
    
    void Start()
    {
        currentColor = defaultColor;
        SetBodyColor(currentColor);
    }
    
    /// <summary>
    /// Đổi màu thân xe
    /// </summary>
    public void SetBodyColor(Color newColor)
    {
        currentColor = newColor;
        
        if (bodyRenderer != null)
            bodyRenderer.color = newColor;
    }
    
    /// <summary>
    /// Lấy màu hiện tại của xe
    /// </summary>
    public Color GetBodyColor()
    {
        return currentColor;
    }
    
    /// <summary>
    /// Reset về màu mặc định
    /// </summary>
    public void ResetColor()
    {
        SetBodyColor(defaultColor);
    }
}