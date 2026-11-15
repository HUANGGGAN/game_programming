# Goal

Implement double click and right click on slots, including

- InventorySlot.cs
- DecompositionSlot.cs
- StorageSlot.cs

# Double click

## InventorySlot.cs

When double click on a slot with item in it

- If it is armor/weapon/consumable slot, and there is a empty universal slot. Move the item(remember to unequip if needed)
- If it is universal slot, and the item is WeaponSO/ConsumableSO, equip it to weapon/consumable slot
(We haven't implement ArmorSO, ignore it for now)

## DecompositionSlot.cs and StorageSlot.cs

Move the item to the other side if available

# Right click

## InventorySlot.cs
If (is universal slot) && (count > 1) && (exist another empty universal slot), then move half of the item to it

## DecompositionSlot.cs and StorageSlot.cs

If (count > 1) && (exist another empty slot on the same side), then move half of the item to it