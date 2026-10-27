using TMPro;
using UnityEngine;

public static class CountDisplayFormatter
{
    public static string Format(int count)
    {
        if (count >= 1_000_000_000)
            return "-1";
        if (count >= 1_000_000)
            return $"{count / 1_000_000}.{count / 100_000 % 10}m";
        if (count >= 1_000)
            return $"{count / 1_000}.{count / 100 % 10}k";
        return count.ToString();
    }
}

public class ItemCountUI : MonoBehaviour
{
    [SerializeField] private InventorySO inventoryData;
    [SerializeField] private ItemSO item;
    [SerializeField] private TMP_Text countText;

    private int lastCount = int.MinValue;

    private void OnEnable()
    {
        if (countText == null)
            countText = GetComponent<TMP_Text>();
        lastCount = int.MinValue;
        Refresh();
    }

    private void LateUpdate()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (countText == null)
            return;
        int count = inventoryData != null ? inventoryData.GetItemCount(item) : 0;
        if (count == lastCount)
            return;
        lastCount = count;
        countText.text = CountDisplayFormatter.Format(count);
    }
}
