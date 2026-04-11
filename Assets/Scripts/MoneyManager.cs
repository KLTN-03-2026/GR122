using UnityEngine;
using System;

public class MoneyManager : MonoBehaviour
{
    const string PREF_KEY = "Game_Cash_v1";

    // Singleton
    static MoneyManager _instance;
    public static MoneyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("MoneyManager");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<MoneyManager>();
            }
            return _instance;
        }
    }

    public event Action<int> OnMoneyChanged;

    [SerializeField]
    int currentMoney = 0;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Load()
    {
        currentMoney = PlayerPrefs.GetInt(PREF_KEY, 0);
        OnMoneyChanged?.Invoke(currentMoney);
    }

    void Save()
    {
        PlayerPrefs.SetInt(PREF_KEY, currentMoney);
        PlayerPrefs.Save();
    }

    public int GetMoney() => currentMoney;

    public void SetMoney(int value)
    {
        currentMoney = Mathf.Max(0, value);
        Save();
        OnMoneyChanged?.Invoke(currentMoney);
    }

    public void AddMoney(int delta)
    {
        if (delta == 0) return;
        currentMoney = Mathf.Max(0, currentMoney + delta);
        Save();
        OnMoneyChanged?.Invoke(currentMoney);
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0) return true;
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            Save();
            OnMoneyChanged?.Invoke(currentMoney);
            return true;
        }
        return false;
    }

    // debug helper
    [ContextMenu("Reset Money (Editor)")]
    public void ResetMoneyEditor()
    {
        SetMoney(0);
    }
}
