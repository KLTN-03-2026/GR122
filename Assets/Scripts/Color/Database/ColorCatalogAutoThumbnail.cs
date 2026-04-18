
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ColorCatalogHelper
{
    [MenuItem("Tools/Generate Color Thumbnails")]
    public static void GenerateThumbnails()
    {
        // Tìm ColorCatalog trong project
        string[] guids = AssetDatabase.FindAssets("t:ColorCatalog");
        if (guids.Length == 0)
        {
            Debug.LogWarning("Không tìm thấy ColorCatalog!");
            return;
        }
        
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        var catalog = AssetDatabase.LoadAssetAtPath<ColorCatalog>(path);
        
        if (catalog == null) return;
        
        foreach (var color in catalog.colors)
        {
            // Tạo thumbnail từ màu sắc
            color.thumbnail = CreateColorThumbnail(color.colorValue, color.displayName);
        }
        
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"Đã tạo thumbnail cho {catalog.colors.Count} màu");
    }
    
    static Sprite CreateColorThumbnail(Color color, string name)
    {
        // Tạo texture 64x64 hình tròn màu
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dx = x - size/2;
                float dy = y - size/2;
                float dist = Mathf.Sqrt(dx*dx + dy*dy);
                
                if (dist < size/2 - 2)
                    tex.SetPixel(x, y, color);
                else if (dist < size/2)
                    tex.SetPixel(x, y, Color.black);
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }
        tex.Apply();
        
        // Lưu texture thành file
        string path = $"Assets/ColorThumbnails/{name}.png";
        System.IO.Directory.CreateDirectory("Assets/ColorThumbnails");
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.Refresh();
        
        // Import thành sprite
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            importer.SaveAndReimport();
        }
        
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
#endif