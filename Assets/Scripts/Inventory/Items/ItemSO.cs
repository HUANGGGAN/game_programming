using UnityEngine;

public enum ItemRarity
{
    Common = 1,
    Uncommon = 2,
    Rare = 3,
    Epic = 4,
    Legendary = 5,
    Mythic = 6
}

public enum ItemTag
{
    None = 1,
    Consumable = 2,
    QuestItem = 3,
    CraftingMaterial = 4,
    Equipment = 5,
    Weapon = 6,
    Armor = 7
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Item/Normal")]
public class ItemSO : ScriptableObject
{
    [Header("Basic Stats")]
    public string itemName = "New Item";
    public int itemID; // Unique identifier for the item
    public ItemRarity rarity; // e.g., Common:1, Uncommon:2, Rare:3, Epic:4, Legendary:5, Mythic:6
    public Sprite icon;
    [Min(1)] public int maxStackSize = 1; // Maximum number of this item that can be stacked in one inventory slot
    public ItemTag tag; // Tag for categorizing the item
    [TextArea] public string description;
}
