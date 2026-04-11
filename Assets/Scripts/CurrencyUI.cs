using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class CurrencyUI : MonoBehaviour
{
    TextMeshProUGUI tmp;

    void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        if (MoneyManager.Instance != null)
            MoneyManager.Instance.OnMoneyChanged += OnMoneyChanged;
        // update immediately
        tmp.text = FormatMoney(MoneyManager.Instance.GetMoney());
    }

    void OnDisable()
    {
        if (MoneyManager.Instance != null)
            MoneyManager.Instance.OnMoneyChanged -= OnMoneyChanged;
    }

    void OnMoneyChanged(int newAmount)
    {
        tmp.text = FormatMoney(newAmount);
    }

    string FormatMoney(int amount)
    {
        // change format as you like, e.g. add currency symbol or separators
        return amount.ToString(); // or $"${amount:N0}"
    }
}
