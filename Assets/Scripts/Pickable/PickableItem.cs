using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PickableItem : MonoBehaviour
{
    [SerializeField] private ItemSO item;
    [SerializeField] private int count = 1;
    [SerializeField] private int ammoCount = 0;
    [SerializeField] private InventoryUIController inventoryUI;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (item == null)
            return;

        if (inventoryUI == null)
            return;

        InventorySlotType slotType = InventorySlotType.Universal;
        if (item is WeaponSO)
            slotType = InventorySlotType.Weapon;
        else if (item is ArmorSO)
            slotType = InventorySlotType.Armor;
        else if (item is ConsumableSO)
            slotType = InventorySlotType.Consumable;

        Debug.Log($"Picked up {item.itemName} x{count} (Ammo: {ammoCount})");
        if (inventoryUI.GiveItem(item, slotType, count, ammoCount))
        {
            Debug.Log($"Picked up {item.itemName} x{count} (Ammo: {ammoCount})");
            Destroy(gameObject);
        }
        
    }
}