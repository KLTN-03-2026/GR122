using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CarInfo
{
    public string id;               // unique id (ví dụ "car_01")
    public string displayName;
    public GameObject prefab;       // prefab của xe
    public Sprite thumbnail;        // optional: nếu null sẽ lấy sprite từ prefab
    public int price = 0;
    public bool unlockedByDefault = false;
}

[CreateAssetMenu(menuName = "Garage/CarCatalog", fileName = "CarCatalog")]
public class CarCatalog : ScriptableObject
{
    public List<CarInfo> cars = new List<CarInfo>();

    public int Count => cars != null ? cars.Count : 0;

    public CarInfo Get(int idx)
    {
        if (cars == null || idx < 0 || idx >= cars.Count) return null;
        return cars[idx];
    }

    public int IndexOfId(string id)
    {
        if (cars == null) return -1;
        for (int i = 0; i < cars.Count; i++)
            if (cars[i] != null && cars[i].id == id) return i;
        return -1;
    }
}
