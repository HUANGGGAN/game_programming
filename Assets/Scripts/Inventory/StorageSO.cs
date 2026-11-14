using UnityEngine;

[CreateAssetMenu(fileName = "Storage", menuName = "Inventory/Storage")]
public class StorageSO : ScriptableObject
{
    public InventorySlotData[] slots = System.Array.Empty<InventorySlotData>();
    public int SlotCount => slots != null ? slots.Length : 0;

    private void OnEnable() => EnsureDefaults();

    // Expanding never removes existing items or previously unlocked slots.
    public void EnsureDefaults(int minimumSlots = 0)
    {
        minimumSlots = Mathf.Max(0, minimumSlots);
        if (slots == null)
            slots = new InventorySlotData[minimumSlots];
        else if (slots.Length < minimumSlots)
            System.Array.Resize(ref slots, minimumSlots);
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null)
                slots[i] = new InventorySlotData();
    }

    public bool AddSlots(int count)
    {
        if (count <= 0 || count > int.MaxValue - SlotCount)
            return false;
        EnsureDefaults(SlotCount + count);
        return true;
    }

    public bool CanMove(InventorySO inventory, bool fromStorage, int from, bool toStorage, int to)
    {
        if (inventory == null || (fromStorage == toStorage && from == to))
            return false;
        EnsureDefaults();
        inventory.EnsureDefaults();
        InventorySlotData[] source = fromStorage ? slots : inventory.slots;
        InventorySlotData[] destination = toStorage ? slots : inventory.slots;
        return from >= 0 && from < source.Length && to >= 0 && to < destination.Length &&
            source[from].item != null && source[from].count > 0;
    }

    public bool TryMove(InventorySO inventory, bool fromStorage, int from, bool toStorage, int to)
    {
        if (!CanMove(inventory, fromStorage, from, toStorage, to))
            return false;
        InventorySlotData[] source = fromStorage ? slots : inventory.slots;
        InventorySlotData[] destination = toStorage ? slots : inventory.slots;
        InventorySlotData a = source[from];
        InventorySlotData b = destination[to];
        int maxStack = Mathf.Max(1, a.item.maxStackSize);
        if (a.item == b.item && a.count < maxStack && b.count < maxStack &&
            (a.item is not MultiWeaponSO || (a.ammoCount == b.ammoCount && a.attackModeIndex == b.attackModeIndex)))
        {
            int transferred = Mathf.Min(a.count, maxStack - b.count);
            b.count += transferred;
            a.count -= transferred;
            if (a.count == 0)
                source[from] = new InventorySlotData();
        }
        else
        {
            source[from] = b;
            destination[to] = a;
        }
        return true;
    }

}
