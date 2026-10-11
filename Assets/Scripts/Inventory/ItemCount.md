## Target Files

- ItemSO.cs
- InventorySlot.cs
- InventorySO.cs
- InventoryUIController.cs

## Goal

- Manage item count in UI and in inventory.

## Details

1. Show count in UI.
For each InventorySlot.cs, I will manually create a text mesh pro under it. InventorySlot should set up text for it and shows the count. Notice there is maxStackSize in ItemSO, if maxStackSize == 1, show empty string, otherwise shows count directly. If item_count === maxStackSize, change the text to green.

2. Handle GiveItem(), CanGiveItem() correctly.
Currently if we give item to inventory, it checks same type of item in slot first. Change the logic
- if found same item, stack the count, if reaches maxStackSize, find another slot again.(it is possible to find another slot with same item)
- if slots with same item are full, start trying empty slot with preferred type, then universal type.

As for PickableItem, if only part of Item can give to Inventory, decrease the count, then shows inventory full later.

3. DragAndDrop.
Drag and drop now switches item. If both are same item, do the logic
- If both are full(count == maxStackSize), switch item.
- If dropped slot is not full, give as much item from drag slot to drop slot as it can.