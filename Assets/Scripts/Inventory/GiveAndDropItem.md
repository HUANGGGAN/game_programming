# Inventory interaction

Implement inventory interfaces.

Check
```
InventoryUIController.cs
InventorySO.cs
ItemSO.cs
InventorySlot.cs
```

## Give Item

In InventoryUIController.cs, implement a function as API to give item to inventory.
If it is WeaponSO/ArmorSO/ConsumableSO, place at corresponding slots first, otherwise place it to universal slot.

The function should contain following parameters(Item, type, count, ammo count)

Remember to Refresh.

## remove Item

two functions
- drop from slot, clear the item in the slot
- remove a specific number of an item(like remove 10 wood)

Remember to Refresh.