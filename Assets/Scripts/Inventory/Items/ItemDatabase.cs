using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "ScriptableObjects/ItemDatabase")]
public class ItemDatabaseSO : ScriptableObject
{
    [Header("Categorized Subsets")]
    public List<ItemCategory> categories = new List<ItemCategory>();

    // Get a random item from a specific category by name
    public ItemSO GetRandomFromCategory(string categoryName)
    {
        ItemCategory category = categories.Find(c => c.categoryName == categoryName);
        if (category != null)
        {
            return category.GetRandomItem();
        }
        
        Debug.LogWarning($"Category '{categoryName}' not found!");
        return null;
    }

    // Implement cascading random selection across categories if needed
}