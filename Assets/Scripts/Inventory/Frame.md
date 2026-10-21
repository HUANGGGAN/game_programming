# Frame

## Description

Frames are for showing the position of inventory slots, it is created under a gridslot just like InventorySlot.cs
(No logic here, so currently it has no code.)

Hierarchy
- Slotgrid
- Slotgrid/Frame
- Slotgrid/Frame/Slot

## Goal

Create Frame.cs for each frame

1. To show the item rarity by color
2. To highlight the frame when player drag an item that can placed in the slot.
3. To highlight the frame(much lighter) if the item will be drop to the slot.

## Details

1. Item has rarity(check ItemSO.cs), assign color for the frame and make it configurable(RGB and alpha). When an item is in the slot, change the frame's color, if the item is dragged away(on drag), or the slot is empty, change to default color.