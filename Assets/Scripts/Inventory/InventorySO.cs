using UnityEngine;

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
    public ItemSO[] slots = new ItemSO[18];
    public ItemSO[] weaponSlots = new ItemSO[3];
    public ItemSO[] armorSlots = new ItemSO[4];
    public ItemSO[] consumableSlots = new ItemSO[2];
    public int[] slotCounts;
    public int[] weaponSlotCounts;
    public int[] armorSlotCounts;
    public int[] consumableSlotCounts;
    public int[] slotAmmoCounts;
    public int[] weaponSlotAmmoCounts;
    public int[] armorSlotAmmoCounts;
    public int[] consumableSlotAmmoCounts;

    private void OnEnable()
    {
        EnsureDefaults();
    }

    public void EnsureDefaults()
    {
        if (slots == null) slots = new ItemSO[18];
        if (weaponSlots == null) weaponSlots = new ItemSO[3];
        if (armorSlots == null) armorSlots = new ItemSO[4];
        if (consumableSlots == null) consumableSlots = new ItemSO[2];

        EnsureSlotState(slots, ref slotCounts, ref slotAmmoCounts);
        EnsureSlotState(weaponSlots, ref weaponSlotCounts, ref weaponSlotAmmoCounts);
        EnsureSlotState(armorSlots, ref armorSlotCounts, ref armorSlotAmmoCounts);
        EnsureSlotState(consumableSlots, ref consumableSlotCounts, ref consumableSlotAmmoCounts);
    }

    private static void EnsureSlotState(ItemSO[] items, ref int[] counts, ref int[] ammoCounts)
    {
        if (counts == null)
        {
            counts = new int[items.Length];
            for (int i = 0; i < items.Length; i++)
                counts[i] = items[i] != null ? 1 : 0;
        }
        else if (counts.Length != items.Length)
        {
            System.Array.Resize(ref counts, items.Length);
        }

        if (ammoCounts == null)
        {
            ammoCounts = new int[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] is GunSO weapon)
                    ammoCounts[i] = Mathf.Max(0, weapon.capacity);
            }
        }
        else if (ammoCounts.Length != items.Length)
        {
            System.Array.Resize(ref ammoCounts, items.Length);
        }
    }

    public ItemSO[] GetSlotsForType(InventorySlotType slotType)
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
        ItemSO[] slotsA = GetSlotsForType(typeA);
        ItemSO[] slotsB = GetSlotsForType(typeB);
        int[] countsA = GetCountsForType(typeA);
        int[] countsB = GetCountsForType(typeB);
        int[] ammoCountsA = GetAmmoCountsForType(typeA);
        int[] ammoCountsB = GetAmmoCountsForType(typeB);
        if (slotsA == null || slotsB == null)
            return false;

        if (indexA < 0 || indexA >= slotsA.Length || indexB < 0 || indexB >= slotsB.Length)
            return false;

        ItemSO itemA = slotsA[indexA];
        ItemSO itemB = slotsB[indexB];
        if (!CanPlaceItem(typeB, itemA) || !CanPlaceItem(typeA, itemB))
            return false;

        int countA = countsA[indexA];
        int countB = countsB[indexB];
        int ammoCountA = ammoCountsA[indexA];
        int ammoCountB = ammoCountsB[indexB];
        slotsA[indexA] = itemB;
        slotsB[indexB] = itemA;
        countsA[indexA] = countB;
        countsB[indexB] = countA;
        ammoCountsA[indexA] = ammoCountB;
        ammoCountsB[indexB] = ammoCountA;
        return true;
    }

    public int[] GetCountsForType(InventorySlotType slotType)
    {
        switch (slotType)
        {
            case InventorySlotType.Universal: return slotCounts;
            case InventorySlotType.Weapon: return weaponSlotCounts;
            case InventorySlotType.Armor: return armorSlotCounts;
            case InventorySlotType.Consumable: return consumableSlotCounts;
            default: return slotCounts;
        }
    }

    public int[] GetAmmoCountsForType(InventorySlotType slotType)
    {
        switch (slotType)
        {
            case InventorySlotType.Universal: return slotAmmoCounts;
            case InventorySlotType.Weapon: return weaponSlotAmmoCounts;
            case InventorySlotType.Armor: return armorSlotAmmoCounts;
            case InventorySlotType.Consumable: return consumableSlotAmmoCounts;
            default: return slotAmmoCounts;
        }
    }

    public bool TryConsumeWeaponAmmo(int slotIndex)
    {
        EnsureDefaults();
        if (slotIndex < 0 || slotIndex >= weaponSlotAmmoCounts.Length || weaponSlotAmmoCounts[slotIndex] <= 0)
            return false;

        weaponSlotAmmoCounts[slotIndex]--;
        return true;
    }

    public void RemoveItemsWithNoCount()
    {
        RemoveItemsWithNoCount(slots, slotCounts, slotAmmoCounts);
        RemoveItemsWithNoCount(weaponSlots, weaponSlotCounts, weaponSlotAmmoCounts);
        RemoveItemsWithNoCount(armorSlots, armorSlotCounts, armorSlotAmmoCounts);
        RemoveItemsWithNoCount(consumableSlots, consumableSlotCounts, consumableSlotAmmoCounts);
    }

    private static void RemoveItemsWithNoCount(ItemSO[] items, int[] counts, int[] ammoCounts)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
            {
                counts[i] = 0;
                ammoCounts[i] = 0;
            }
            else if (counts[i] <= 0)
            {
                items[i] = null;
                counts[i] = 0;
                ammoCounts[i] = 0;
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