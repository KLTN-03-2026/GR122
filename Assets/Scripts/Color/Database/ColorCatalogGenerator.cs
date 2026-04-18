#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class ColorCatalogGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Color Catalog")]
    public static void ShowWindow()
    {
        GetWindow<ColorCatalogGenerator>("Tạo Color Catalog");
    }
    
    private string catalogName = "ColorCatalog";
    
    void OnGUI()
    {
        GUILayout.Label("Tạo Color Catalog với màu có sẵn", EditorStyles.boldLabel);
        
        catalogName = EditorGUILayout.TextField("Tên catalog:", catalogName);
        
        if (GUILayout.Button("Tạo Catalog", GUILayout.Height(40)))
        {
            GenerateCatalog();
        }
    }
    
    void GenerateCatalog()
    {
        // Tạo database mới
        var catalog = ScriptableObject.CreateInstance<ColorCatalog>();
        catalog.colors = new List<ColorInfo>();
        
        // === TỰ ĐỘNG THÊM 20+ MÀU VÀO ĐÂY ===
        
        // Màu cơ bản (miễn phí)
        AddColor(catalog, "red", "Đỏ", new Color(0.9f, 0.2f, 0.2f), 0, true);
        AddColor(catalog, "blue", "Xanh dương", new Color(0.2f, 0.4f, 0.9f), 0, true);
        AddColor(catalog, "green", "Xanh lá", new Color(0.2f, 0.7f, 0.2f), 0, true);
        AddColor(catalog, "yellow", "Vàng", new Color(0.95f, 0.85f, 0.2f), 0, true);
        AddColor(catalog, "black", "Đen", new Color(0.1f, 0.1f, 0.1f), 0, true);
        AddColor(catalog, "white", "Trắng", Color.white, 0, true);
        
        // Màu pastel
        AddColor(catalog, "pink", "Hồng", new Color(1f, 0.6f, 0.8f), 3000);
        AddColor(catalog, "pastel_blue", "Xanh nhạt", new Color(0.6f, 0.8f, 1f), 3000);
        AddColor(catalog, "pastel_green", "Xanh lá nhạt", new Color(0.7f, 0.9f, 0.7f), 3000);
        AddColor(catalog, "pastel_purple", "Tím nhạt", new Color(0.85f, 0.7f, 0.95f), 3000);
        AddColor(catalog, "peach", "Đào", new Color(1f, 0.8f, 0.6f), 3000);
        
        // Màu neon
        AddColor(catalog, "neon_red", "Neon đỏ", new Color(1f, 0.2f, 0.2f), 8000);
        AddColor(catalog, "neon_green", "Neon xanh", new Color(0.2f, 1f, 0.2f), 8000);
        AddColor(catalog, "neon_blue", "Neon dương", new Color(0.2f, 0.5f, 1f), 8000);
        AddColor(catalog, "neon_pink", "Neon hồng", new Color(1f, 0.2f, 0.8f), 8000);
        AddColor(catalog, "neon_yellow", "Neon vàng", new Color(1f, 0.9f, 0.1f), 8000);
        
        // Màu metallic
        AddColor(catalog, "metallic_red", "Đỏ metallic", new Color(0.85f, 0.25f, 0.25f), 12000);
        AddColor(catalog, "metallic_blue", "Xanh metallic", new Color(0.2f, 0.4f, 0.85f), 12000);
        AddColor(catalog, "metallic_silver", "Bạc", new Color(0.75f, 0.75f, 0.8f), 10000);
        AddColor(catalog, "metallic_gold", "Vàng gold", new Color(0.9f, 0.75f, 0.2f), 15000);
        AddColor(catalog, "metallic_bronze", "Đồng", new Color(0.8f, 0.5f, 0.2f), 13000);
        
        // Màu dark
        AddColor(catalog, "dark_red", "Đỏ đô", new Color(0.5f, 0.1f, 0.1f), 5000);
        AddColor(catalog, "navy", "Xanh navy", new Color(0.1f, 0.1f, 0.5f), 5000);
        AddColor(catalog, "dark_green", "Xanh rêu", new Color(0.1f, 0.3f, 0.1f), 5000);
        AddColor(catalog, "purple", "Tím than", new Color(0.3f, 0.1f, 0.4f), 5000);
        AddColor(catalog, "dark_gray", "Xám đậm", new Color(0.3f, 0.3f, 0.35f), 4000);
        
        // Màu đặc biệt
        AddColor(catalog, "orange", "Cam", new Color(1f, 0.55f, 0f), 6000);
        AddColor(catalog, "cyan", "Xanh ngọc", new Color(0f, 0.8f, 0.9f), 6000);
        AddColor(catalog, "champagne", "Vàng champagne", new Color(0.85f, 0.75f, 0.55f), 10000);
        
        // Lưu file
        string path = $"Assets/{catalogName}.asset";
        AssetDatabase.CreateAsset(catalog, path);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"✅ Đã tạo {catalog.colors.Count} màu tại {path}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = catalog;
    }
    
    void AddColor(ColorCatalog catalog, string id, string name, Color color, int price, bool unlocked = false)
    {
        catalog.colors.Add(new ColorInfo
        {
            id = id,
            displayName = name,
            colorValue = color,
            price = price,
            unlockedByDefault = unlocked
        });
    }
}
#endif