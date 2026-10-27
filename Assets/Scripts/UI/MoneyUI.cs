using UnityEngine;
using TMPro;

public class MoneyUI : MonoBehaviour
{
    [Header("Data Reference")]
    [SerializeField] private PlayerData playerData;

    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI moneyText;

    [Header("Format Settings")]
    [SerializeField] private string prefix = "\u00d7 ";

    private void OnEnable()
    {
        if (playerData != null)
        {
            // 1. Subscribe to the change event
            playerData.OnMoneyChanged += UpdateMoneyDisplay;
        }

        // 2. Refresh display once on startup to show initial money
        UpdateMoneyDisplay();
    }

    private void OnDisable()
    {
        if (playerData != null)
        {
            // 3. ALWAYS unsubscribe to prevent memory leaks / null references!
            playerData.OnMoneyChanged -= UpdateMoneyDisplay;
        }
    }

    // This method ONLY executes when OnMoneyChanged is fired
    private void UpdateMoneyDisplay()
    {
        if (playerData != null && moneyText != null)
        {
            moneyText.text = $"{prefix}{CountDisplayFormatter.Format(playerData.money)}";
        }
    }
}
