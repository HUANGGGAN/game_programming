using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Item/Normal")]
public class ItemSO : ScriptableObject
{
    [Header("Basic Stats")]
    public string itemName = "New Item";
    public int itemID; // Unique identifier for the item
    public string rarity; // e.g., Common, Rare, Epic, Legendary
    public Sprite icon;
    [TextArea] public string description;
}