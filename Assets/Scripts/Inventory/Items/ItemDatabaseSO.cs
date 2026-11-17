using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ItemCategoryWeight
{
    [Min(0)] public int categoryID;
    [Min(0f)] public float probability;

    public ItemCategoryWeight(int categoryID, float probability)
    {
        this.categoryID = categoryID;
        this.probability = probability;
    }
}

[Serializable]
public class ItemDropChance
{
    public ItemSO item;
    [Range(0f, 1f)] public float probability;
    [Min(1)] public int count = 1;
}

[Serializable]
public class ItemCategoryDropChance
{
    [Min(0)] public int categoryID;
    [Range(0f, 1f)] public float probability;
    public List<ItemTag> tags = new List<ItemTag>();
}

public interface IItemDatabase
{
    ItemSO GetRandomItem(IReadOnlyList<ItemCategoryWeight> categories, IReadOnlyList<ItemTag> tags = null);
    ItemSO GetRandomFromCategory(int categoryID, IReadOnlyList<ItemTag> tags = null);
}

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "ScriptableObjects/ItemDatabase")]
public class ItemDatabaseSO : ScriptableObject, IItemDatabase
{
    [Header("Categorized Subsets")]
    public List<ItemCategory> categories = new List<ItemCategory>();

    // Absolute chances: unused probability means no drop. Invalid entries retain
    // their chance of producing nothing rather than increasing other chances.
    public ItemSO GetRandomDrop(IReadOnlyList<ItemDropChance> items,
        IReadOnlyList<ItemCategoryDropChance> categoryDrops)
        => GetRandomDrop(items, categoryDrops, out _);

    public ItemSO GetRandomDrop(IReadOnlyList<ItemDropChance> items,
        IReadOnlyList<ItemCategoryDropChance> categoryDrops, out int count)
    {
        count = 0;
        double total = 0;
        if (items != null)
            foreach (var entry in items)
                if (entry != null && !AddDropChance(ref total, entry.probability)) return null;
        if (categoryDrops != null)
            foreach (var entry in categoryDrops)
                if (entry != null && !AddDropChance(ref total, entry.probability)) return null;
        if (total > 1.0000001)
        {
            Debug.LogWarning("Enemy drop probabilities must total no more than 1.", this);
            return null;
        }

        double roll = UnityEngine.Random.value;
        if (items != null)
            foreach (var entry in items)
            {
                if (entry == null) continue;
                if (roll < entry.probability)
                {
                    if (entry.item == null) return null;
                    count = Math.Max(1, entry.count);
                    return entry.item;
                }
                roll -= entry.probability;
            }
        if (categoryDrops != null)
            foreach (var entry in categoryDrops)
            {
                if (entry == null) continue;
                if (roll < entry.probability)
                {
                    ItemSO item = GetRandomFromCategory(entry.categoryID, entry.tags);
                    count = item != null ? 1 : 0;
                    return item;
                }
                roll -= entry.probability;
            }
        return null;
    }

    private bool AddDropChance(ref double total, float probability)
    {
        if (float.IsNaN(probability) || float.IsInfinity(probability) || probability < 0f || probability > 1f)
        {
            Debug.LogWarning("Enemy drop probabilities must be finite values between 0 and 1.", this);
            return false;
        }
        total += probability;
        return true;
    }

    // Category IDs are zero-based indices into categories. Empty tags allow all items.
    public ItemSO GetRandomFromCategory(int categoryID, IReadOnlyList<ItemTag> tags = null)
    {
        if (categories == null || categoryID < 0 || categoryID >= categories.Count) return null;
        return PickItem(categories[categoryID], tags);
    }

    public ItemSO GetRandomFromCategory(string categoryName)
    {
        if (categories != null)
            foreach (ItemCategory category in categories)
                if (category != null && category.categoryName == categoryName)
                    return PickItem(category, null);
        Debug.LogWarning($"Category '{categoryName}' not found!");
        return null;
    }

    public ItemSO GetRandomItem(IReadOnlyList<ItemCategoryWeight> selections, IReadOnlyList<ItemTag> tags = null)
    {
        if (selections == null || categories == null) return null;
        double total = 0;
        for (int i = 0; i < selections.Count; i++)
            if (IsEligible(selections[i], tags)) total += selections[i].probability;
        if (total <= 0) return null;

        double roll = UnityEngine.Random.value * total;
        ItemCategory fallback = null;
        for (int i = 0; i < selections.Count; i++)
        {
            ItemCategoryWeight selection = selections[i];
            if (!IsEligible(selection, tags)) continue;
            fallback = categories[selection.categoryID];
            roll -= selection.probability;
            if (roll < 0) return PickItem(fallback, tags);
        }
        // Random.value can be exactly 1.
        return PickItem(fallback, tags);
    }

    private bool IsEligible(ItemCategoryWeight selection, IReadOnlyList<ItemTag> tags)
    {
        return selection.categoryID >= 0 && selection.categoryID < categories.Count &&
            selection.probability > 0 && !float.IsInfinity(selection.probability) && !float.IsNaN(selection.probability) &&
            CountItems(categories[selection.categoryID], tags) > 0;
    }

    private static bool Matches(ItemSO item, IReadOnlyList<ItemTag> tags)
    {
        if (item == null) return false;
        if (tags == null || tags.Count == 0) return true;
        for (int i = 0; i < tags.Count; i++)
            if (tags[i] == ItemTag.None || item.tag == tags[i]) return true;
        return false;
    }

    private static int CountItems(ItemCategory category, IReadOnlyList<ItemTag> tags)
    {
        if (category == null || category.items == null) return 0;
        int count = 0;
        foreach (ItemSO item in category.items) if (Matches(item, tags)) count++;
        return count;
    }

    private static ItemSO PickItem(ItemCategory category, IReadOnlyList<ItemTag> tags)
    {
        int count = CountItems(category, tags);
        if (count == 0) return null;
        int index = UnityEngine.Random.Range(0, count);
        foreach (ItemSO item in category.items)
            if (Matches(item, tags) && index-- == 0) return item;
        return null;
    }
}
