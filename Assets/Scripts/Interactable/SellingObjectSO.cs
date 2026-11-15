using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SellingObject", menuName = "Shop/Selling Object")]
public class SellingObjectSO : ScriptableObject
{
    public int type;
    public ItemSO currency;
    public Sprite currencyIcon;
    [Min(0)] public int price;
    public InventorySlotData @object = new InventorySlotData { count = 1 };
    public Sprite objectIcon;
    public string objectName;
    public bool buyOneAtOnce = true;
    [TextArea] public string description;
    public bool isRandom;
    [Tooltip("Used for random offers. IDs are zero-based ItemDatabase category indices; positive weights are normalized.")]
    public List<ItemCategoryWeight> randomCategories = new List<ItemCategoryWeight>();
    [Tooltip("Allowed item tags for random offers. An empty list allows every tag.")]
    public List<ItemTag> randomTags = new List<ItemTag>();

}
