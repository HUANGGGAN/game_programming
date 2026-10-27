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
    public int randomItemGroupID;
}
