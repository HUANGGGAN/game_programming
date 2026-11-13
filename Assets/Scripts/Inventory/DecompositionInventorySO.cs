using UnityEngine;

[CreateAssetMenu(fileName = "DecompositionInventory", menuName = "Inventory/Decomposition Storage")]
public class DecompositionInventorySO : ScriptableObject
{
    public const int SlotCount = 9;
    public InventorySlotData[] slots = new InventorySlotData[SlotCount];

    private void OnEnable() => EnsureDefaults();

    public void EnsureDefaults()
    {
        if (slots == null)
            slots = new InventorySlotData[SlotCount];
        else if (slots.Length != SlotCount)
            System.Array.Resize(ref slots, SlotCount);
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null)
                slots[i] = new InventorySlotData();
    }

    public static bool CanDecompose(ItemSO item) => item == null || item.minPiece > 0 || item.maxPiece > 0;

    public bool CanMove(InventorySO inventory, bool fromDecomposition, int from, bool toDecomposition, int to)
    {
        if (inventory == null || (fromDecomposition == toDecomposition && from == to))
            return false;
        EnsureDefaults();
        inventory.EnsureDefaults();
        InventorySlotData[] source = fromDecomposition ? slots : inventory.slots;
        InventorySlotData[] destination = toDecomposition ? slots : inventory.slots;
        return from >= 0 && from < source.Length && to >= 0 && to < destination.Length &&
            source[from].item != null && source[from].count > 0 &&
            (!toDecomposition || CanDecompose(source[from].item)) &&
            (!fromDecomposition || CanDecompose(destination[to].item));
    }

    public bool TryMove(InventorySO inventory, bool fromDecomposition, int from, bool toDecomposition, int to)
    {
        if (!CanMove(inventory, fromDecomposition, from, toDecomposition, to))
            return false;
        InventorySlotData[] source = fromDecomposition ? slots : inventory.slots;
        InventorySlotData[] destination = toDecomposition ? slots : inventory.slots;
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

    public bool TryDecompose(ItemSO[] piecesByRarity)
    {
        EnsureDefaults();
        if (piecesByRarity == null || piecesByRarity.Length != 6)
            return false;
        var preview = new InventorySlotData[SlotCount];
        var rewards = new long[6];
        bool decomposed = false;
        for (int i = 0; i < slots.Length; i++)
        {
            InventorySlotData slot = slots[i];
            preview[i] = new InventorySlotData
            {
                item = slot.item, count = slot.count, ammoCount = slot.ammoCount, attackModeIndex = slot.attackModeIndex
            };
            if (slot.item == null || slot.count <= 0 || System.Array.IndexOf(piecesByRarity, slot.item) >= 0 ||
                !CanDecompose(slot.item))
                continue;
            int rarity = (int)slot.item.rarity - 1;
            if (rarity < 0 || rarity >= 6)
                rarity = 0;
            ItemSO piece = piecesByRarity[rarity];
            if (piece == null || !CanDecompose(piece))
                return false;
            int min = Mathf.Max(0, slot.item.minPiece);
            int max = Mathf.Max(min, slot.item.maxPiece);
            if (min == max)
            {
                rewards[rarity] += (long)min * slot.count;
                if (rewards[rarity] > int.MaxValue)
                    return false;
            }
            else
            {
                for (int n = 0; n < slot.count; n++)
                {
                    long roll = min + System.Math.Min((long)max - min,
                        (long)(Random.value * ((double)max - min + 1)));
                    rewards[rarity] += roll;
                    if (rewards[rarity] > int.MaxValue)
                        return false;
                }
            }
            preview[i] = new InventorySlotData();
            decomposed = true;
        }
        if (!decomposed)
            return false;
        for (int rarity = 0; rarity < rewards.Length; rarity++)
        {
            int remaining = (int)rewards[rarity];
            if (remaining == 0)
                continue;
            ItemSO piece = piecesByRarity[rarity];
            int maxStack = Mathf.Max(1, piece.maxStackSize);
            // Fill existing stacks first, then the slots freed by decomposition.
            for (int pass = 0; pass < 2 && remaining > 0; pass++)
            {
                foreach (InventorySlotData slot in preview)
                {
                    if (pass == 0 ? slot.item != piece : slot.item != null)
                        continue;
                    int added = Mathf.Min(remaining, Mathf.Max(0, maxStack - slot.count));
                    if (added <= 0)
                        continue;
                    slot.item = piece;
                    slot.count += added;
                    remaining -= added;
                    if (remaining == 0)
                        break;
                }
            }
            if (remaining > 0)
                return false;
        }
        slots = preview;
        return true;
    }
}
