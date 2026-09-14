using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemSO : ScriptableObject
{
    public string itemName = "New Item";
    public Sprite icon;
    [TextArea] public string description;
}