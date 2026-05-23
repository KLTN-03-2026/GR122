using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ColorInfo
{
    public string id;              // unique id (ví dụ "color_red")
    public string displayName;     // Tên màu (ví dụ "Đỏ", "Xanh")
    public Color colorValue;       // Giá trị màu thực tế
    public Sprite thumbnail;       // Icon hiển thị (có thể là hình tròn màu)
    public int price = 0;          // Giá để mở khóa màu
    public bool unlockedByDefault = false;
}

[CreateAssetMenu(menuName = "Garage/ColorCatalog", fileName = "ColorCatalog")]
public class ColorCatalog : ScriptableObject
{
    public List<ColorInfo> colors = new List<ColorInfo>();
    
    public int Count => colors != null ? colors.Count : 0;
    
    public ColorInfo Get(int idx)
    {
        if (colors == null || idx < 0 || idx >= colors.Count) return null;
        return colors[idx];
    }
    
    public int IndexOfId(string id)
    {
        if (colors == null) return -1;
        for (int i = 0; i < colors.Count; i++)
            if (colors[i] != null && colors[i].id == id) return i;
        return -1;
    }
}