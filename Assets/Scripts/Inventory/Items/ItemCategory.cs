using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemCategory", menuName = "ScriptableObjects/ItemCategory")]
public class ItemCategory : ScriptableObject
{
    public string categoryName; // e.g., "Tier 1 - Common", "Funny Items", "Boss Drops"
    public List<ItemSO> items = new List<ItemSO>();

    public ItemSO GetRandomItem()
    {
        if (items == null || items.Count == 0) return null;
        return items[Random.Range(0, items.Count)];
    }
}