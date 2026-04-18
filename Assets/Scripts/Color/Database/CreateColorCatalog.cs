
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class CreateColorCatalog : EditorWindow
{
    [MenuItem("Assets/Create/ColorCatalog", false, 100)]
    public static void CreateCatalog()
    {
        var catalog = ScriptableObject.CreateInstance<ColorCatalog>();
        
        // Tạo sẵn 20 màu
        catalog.colors = new System.Collections.Generic.List<ColorInfo>
        {
            // Màu cơ bản (miễn phí)
            new ColorInfo { id = "red", displayName = "Đỏ", colorValue = new Color(0.9f, 0.2f, 0.2f), price = 0, unlockedByDefault = true },
            new ColorInfo { id = "blue", displayName = "Xanh dương", colorValue = new Color(0.2f, 0.4f, 0.9f), price = 0, unlockedByDefault = true },
            new ColorInfo { id = "green", displayName = "Xanh lá", colorValue = new Color(0.2f, 0.7f, 0.2f), price = 0, unlockedByDefault = true },
            new ColorInfo { id = "yellow", displayName = "Vàng", colorValue = new Color(0.95f, 0.85f, 0.2f), price = 0, unlockedByDefault = true },
            new ColorInfo { id = "black", displayName = "Đen", colorValue = new Color(0.1f, 0.1f, 0.1f), price = 0, unlockedByDefault = true },
            new ColorInfo { id = "white", displayName = "Trắng", colorValue = Color.white, price = 0, unlockedByDefault = true },
            
            // Màu pastel (3,000)
            new ColorInfo { id = "pink", displayName = "Hồng", colorValue = new Color(1f, 0.6f, 0.8f), price = 3000 },
            new ColorInfo { id = "pastel_blue", displayName = "Xanh nhạt", colorValue = new Color(0.6f, 0.8f, 1f), price = 3000 },
            new ColorInfo { id = "pastel_green", displayName = "Xanh lá nhạt", colorValue = new Color(0.7f, 0.9f, 0.7f), price = 3000 },
            
            // Màu neon (8,000)
            new ColorInfo { id = "neon_red", displayName = "Neon đỏ", colorValue = new Color(1f, 0.2f, 0.2f), price = 8000 },
            new ColorInfo { id = "neon_green", displayName = "Neon xanh", colorValue = new Color(0.2f, 1f, 0.2f), price = 8000 },
            new ColorInfo { id = "neon_blue", displayName = "Neon dương", colorValue = new Color(0.2f, 0.5f, 1f), price = 8000 },
            new ColorInfo { id = "neon_pink", displayName = "Neon hồng", colorValue = new Color(1f, 0.2f, 0.8f), price = 8000 },
            
            // Màu metallic (12,000)
            new ColorInfo { id = "metallic_silver", displayName = "Bạc", colorValue = new Color(0.75f, 0.75f, 0.8f), price = 12000 },
            new ColorInfo { id = "metallic_gold", displayName = "Vàng gold", colorValue = new Color(0.9f, 0.75f, 0.2f), price = 15000 },
            new ColorInfo { id = "metallic_bronze", displayName = "Đồng", colorValue = new Color(0.8f, 0.5f, 0.2f), price = 13000 },
            
            // Màu dark (5,000)
            new ColorInfo { id = "dark_red", displayName = "Đỏ đô", colorValue = new Color(0.5f, 0.1f, 0.1f), price = 5000 },
            new ColorInfo { id = "navy", displayName = "Xanh navy", colorValue = new Color(0.1f, 0.1f, 0.5f), price = 5000 },
            new ColorInfo { id = "dark_gray", displayName = "Xám đậm", colorValue = new Color(0.3f, 0.3f, 0.35f), price = 4000 },
        };
        
        string path = "Assets/ColorCatalog.asset";
        AssetDatabase.CreateAsset(catalog, path);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"✅ Đã tạo ColorCatalog tại {path} với {catalog.colors.Count} màu");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = catalog;
    }
}
#endif