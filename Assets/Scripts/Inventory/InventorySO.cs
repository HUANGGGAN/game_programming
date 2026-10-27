using UnityEngine;

[System.Serializable]
public class InventorySlotData
{
    public ItemSO item;
    public int count;
    public int ammoCount;
}

public enum InventorySlotType
{
    Universal,
    Weapon,
    Armor,
    Consumable
}




[CreateAssetMenu(fileName = "NewInventory", menuName = "Inventory/Inventory Storage")]
public class InventorySO : ScriptableObject
{
    public const int universalSlotCount = 18;
    public const int weaponSlotCount = 3;
    public const int armorSlotCount = 4;
    public const int consumableSlotCount = 3;
    public InventorySlotData[] slots = new InventorySlotData[universalSlotCount];
    public InventorySlotData[] weaponSlots = new InventorySlotData[weaponSlotCount];
    public InventorySlotData[] armorSlots = new InventorySlotData[armorSlotCount];
    public InventorySlotData[] consumableSlots = new InventorySlotData[consumableSlotCount];

    private void OnEnable()
    {
        EnsureDefaults();
    }

    public void EnsureDefaults()
    {
        EnsureSlotState(ref slots, universalSlotCount);
        EnsureSlotState(ref weaponSlots, weaponSlotCount);
        EnsureSlotState(ref armorSlots, armorSlotCount);
        EnsureSlotState(ref consumableSlots, consumableSlotCount);
    }

    private static void EnsureSlotState(ref InventorySlotData[] slotData, int capacity)
    {
        if (slotData == null)
        {
            slotData = new InventorySlotData[capacity];
        }
        else if (slotData.Length != capacity)
        {
            System.Array.Resize(ref slotData, capacity);
        }

        for (int i = 0; i < slotData.Length; i++)
        {
            if (slotData[i] == null)
                slotData[i] = new InventorySlotData();
        }
    }

    public InventorySlotData[] GetSlotsForType(InventorySlotType slotType)
    {
        switch (slotType)
        {
            case InventorySlotType.Universal:
                return slots;
            case InventorySlotType.Weapon:
                return weaponSlots;
            case InventorySlotType.Armor:
                return armorSlots;
            case InventorySlotType.Consumable:
                return consumableSlots;
            default:
                return slots;
        }
    }

    public bool TrySwapSlots(InventorySlotType typeA, int indexA, InventorySlotType typeB, int indexB)
    {
        EnsureDefaults();
        InventorySlotData[] slotsA = GetSlotsForType(typeA);
        InventorySlotData[] slotsB = GetSlotsForType(typeB);
        if (slotsA == null || slotsB == null)
            return false;

        if (indexA < 0 || indexA >= slotsA.Length || indexB < 0 || indexB >= slotsB.Length)
            return false;

        InventorySlotData slotA = slotsA[indexA];
        InventorySlotData slotB = slotsB[indexB];
        if (slotA == slotB)
            return false;

        if (slotA.item != null && slotA.item == slotB.item)
        {
            int maxStack = Mathf.Max(1, slotA.item.maxStackSize);
            int room = Mathf.Max(0, maxStack - slotB.count);
            bool eitherFull = slotA.count == maxStack || slotB.count == maxStack;
            if (!eitherFull && room > 0)
            {
                int transferred = Mathf.Min(Mathf.Max(0, slotA.count), room);
                if (transferred == 0)
                    return false;
                slotB.count += transferred;
                slotA.count -= transferred;
                if (slotA.count == 0)
                {
                    slotA.item = null;
                    slotA.ammoCount = 0;
                }
                return true;
            }
            if (!eitherFull)
                return false;
        }
        if (!CanPlaceItem(typeB, slotA.item) || !CanPlaceItem(typeA, slotB.item))
            return false;

        slotsA[indexA] = slotB;
        slotsB[indexB] = slotA;
        return true;
    }

    public bool TryConsumeConsumable(int slotIndex)
    {
        EnsureDefaults();
        if (slotIndex < 0 || slotIndex >= consumableSlots.Length)
            return false;
        InventorySlotData slot = consumableSlots[slotIndex];
        if (slot.item is not ConsumableSO || slot.count <= 0)
            return false;
        slot.count--;
        if (slot.count == 0)
        {
            slot.item = null;
            slot.ammoCount = 0;
        }
        return true;
    }

    public bool TryConsumeWeaponAmmo(int slotIndex)
    {
        EnsureDefaults();
        if (slotIndex < 0 || slotIndex >= weaponSlots.Length)
            return false;
        InventorySlotData slot = weaponSlots[slotIndex];
        if (slot.item is not WeaponSO weapon || slot.count <= 0)
            return false;
        if (!weapon.consumeAmmo)
            return true;
        if (weapon.hasLimitedAmmo)
        {
            slot.count--;
            if (slot.count == 0)
            {
                slot.item = null;
                slot.ammoCount = 0;
            }
            return true;
        }
        if (slot.ammoCount <= 0)
            return false;
        slot.ammoCount--;
        return true;
    }

    public InventorySlotType GetPreferredSlotType(ItemSO item, InventorySlotType fallbackType)
    {
        if (item == null)
            return fallbackType;

        if (item is WeaponSO)
            return InventorySlotType.Weapon;
        if (item is ArmorSO)
            return InventorySlotType.Armor;
        if (item is ConsumableSO)
            return InventorySlotType.Consumable;

        return InventorySlotType.Universal;
    }

    public bool CanGiveItem(ItemSO item, InventorySlotType preferredType, int count = 1)
    {
        return FindSlotForItem(item, preferredType, count) != null;
    }

    private InventorySlotData FindSlotForItem(ItemSO item, InventorySlotType preferredType, int count)
    {
        EnsureDefaults();
        if (item == null || count <= 0)
            return null;

        InventorySlotType[] allSlotTypes =
        {
            InventorySlotType.Universal,
            InventorySlotType.Weapon,
            InventorySlotType.Armor,
            InventorySlotType.Consumable
        };
        foreach (InventorySlotType type in allSlotTypes)
        {
            foreach (InventorySlotData slot in GetSlotsForType(type))
            {
                if (slot.item == item && slot.count < Mathf.Max(1, item.maxStackSize))
                    return slot;
            }
        }

        InventorySlotType[] slotPriority =
        {
            GetPreferredSlotType(item, preferredType),
            InventorySlotType.Universal
        };

        foreach (InventorySlotType type in slotPriority)
        {
            if (!CanPlaceItem(type, item))
                continue;

            InventorySlotData[] targetSlots = GetSlotsForType(type);
            if (targetSlots == null)
                continue;

            foreach (InventorySlotData slot in targetSlots)
            {
                if (slot.item == null)
                    return slot;
            }
        }

        return null;
    }

    public bool TryGiveItem(ItemSO item, InventorySlotType preferredType, int count = 1, int ammoCount = 0)
    {
        return TryGiveItem(item, preferredType, out _, count, ammoCount);
    }

    public bool TryGiveItem(ItemSO item, InventorySlotType preferredType, out int givenCount, int count = 1, int ammoCount = 0)
    {
        givenCount = 0;
        if (item == null || count <= 0)
            return false;

        int maxStack = Mathf.Max(1, item.maxStackSize);
        while (givenCount < count)
        {
            InventorySlotData slot = FindSlotForItem(item, preferredType, count - givenCount);
            if (slot == null)
                break;

            bool isExistingStack = slot.item == item;
            int existingCount = isExistingStack ? Mathf.Max(0, slot.count) : 0;
            int added = Mathf.Min(count - givenCount, maxStack - existingCount);
            slot.item = item;
            slot.count = existingCount + added;
            if (!isExistingStack || ammoCount > 0)
                slot.ammoCount = ammoCount;
            givenCount += added;
        }
        return givenCount > 0;
    }

    // Validate payment and the entire delivery on a separate inventory before committing.
    public bool TryExchangeItems(ItemSO currency, int currencyCount, ItemSO purchasedItem, int purchasedCount)
    {
        if (currencyCount < 0 || purchasedCount <= 0 || (currency == null && currencyCount > 0))
            return false;
        EnsureDefaults();
        InventorySO preview = CreateInstance<InventorySO>();
        try
        {
            preview.EnsureDefaults();
            CopySlotContents(slots, preview.slots);
            CopySlotContents(weaponSlots, preview.weaponSlots);
            CopySlotContents(armorSlots, preview.armorSlots);
            CopySlotContents(consumableSlots, preview.consumableSlots);
            if (currency != null && currencyCount > 0 && !preview.TryRemoveItemAmount(currency, currencyCount))
                return false;
            if (purchasedItem != null &&
                (!preview.TryGiveItem(purchasedItem, InventorySlotType.Universal, out int givenCount, purchasedCount) ||
                 givenCount != purchasedCount))
                return false;

            CopySlotContents(preview.slots, slots);
            CopySlotContents(preview.weaponSlots, weaponSlots);
            CopySlotContents(preview.armorSlots, armorSlots);
            CopySlotContents(preview.consumableSlots, consumableSlots);
            return true;
        }
        finally
        {
            Destroy(preview);
        }
    }

    private static void CopySlotContents(InventorySlotData[] source, InventorySlotData[] destination)
    {
        for (int i = 0; i < source.Length; i++)
        {
            destination[i].item = source[i].item;
            destination[i].count = source[i].count;
            destination[i].ammoCount = source[i].ammoCount;
        }
    }

    public bool TryDropFromSlot(InventorySlotType slotType, int index)
    {
        EnsureDefaults();

        InventorySlotData[] targetSlots = GetSlotsForType(slotType);

        if (targetSlots == null)
            return false;

        if (index < 0 || index >= targetSlots.Length)
            return false;

        targetSlots[index].item = null;
        targetSlots[index].count = 0;
        targetSlots[index].ammoCount = 0;
        return true;
    }

    public int GetItemCount(ItemSO item)
    {
        EnsureDefaults();
        if (item == null)
            return 0;

        int total = 0;
        InventorySlotData[][] groups = { slots, weaponSlots, armorSlots, consumableSlots };
        foreach (InventorySlotData[] group in groups)
        {
            foreach (InventorySlotData slot in group)
            {
                if (slot.item == item)
                    total += Mathf.Max(0, slot.count);
            }
        }
        return total;
    }

    public bool TryRemoveItemAmount(ItemSO item, int amount)
    {
        if (item == null || amount <= 0 || GetItemCount(item) < amount)
            return false;

        int remaining = amount;
        InventorySlotType[] slotTypes =
        {
            InventorySlotType.Universal,
            InventorySlotType.Weapon,
            InventorySlotType.Armor,
            InventorySlotType.Consumable
        };

        for (int s = 0; s < slotTypes.Length; s++)
        {
            InventorySlotData[] items = GetSlotsForType(slotTypes[s]);

            if (items == null)
                continue;

            for (int i = 0; i < items.Length && remaining > 0; i++)
            {
                if (items[i].item != item)
                    continue;

                int removeCount = Mathf.Min(items[i].count, remaining);
                items[i].count -= removeCount;
                remaining -= removeCount;

                if (items[i].count <= 0)
                {
                    items[i].item = null;
                    items[i].count = 0;
                    items[i].ammoCount = 0;
                }
            }

            if (remaining <= 0)
                break;
        }

        return remaining <= 0;
    }

    public void RemoveItemsWithNoCount()
    {
        RemoveItemsWithNoCount(slots);
        RemoveItemsWithNoCount(weaponSlots);
        RemoveItemsWithNoCount(armorSlots);
        RemoveItemsWithNoCount(consumableSlots);
    }

    private static void RemoveItemsWithNoCount(InventorySlotData[] slots)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].item == null)
            {
                slots[i].count = 0;
                slots[i].ammoCount = 0;
            }
            else if (slots[i].count <= 0)
            {
                slots[i].item = null;
                slots[i].count = 0;
                slots[i].ammoCount = 0;
            }
        }
    }

    public bool CanPlaceItem(InventorySlotType slotType, ItemSO item)
    {
        if (item == null)
            return true;

        switch (slotType)
        {
            case InventorySlotType.Universal:
                return true;
            case InventorySlotType.Weapon:
                return item is WeaponSO;
            case InventorySlotType.Armor:
                return item is ArmorSO;
            case InventorySlotType.Consumable:
                return item is ConsumableSO;
            default:
                return true;
        }
    }
}
