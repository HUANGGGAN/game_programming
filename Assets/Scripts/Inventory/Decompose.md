# Goal

Create an UI to let player decompose the items from inventory

- A DecompositionManager.cs
- An UI with left 12 slots(from inventory), right 9 slots(to place item and return weapon pieces)
- A button to confirm decompositon
- A button to close the UI
- An entrance from ShopManager.cs tab
- A new state `Decomposition`
- Add 2 integer (minPiece, maxPiece)in ItemSO, to randomly give player weapon pieces randomly between these two number.

# Detail

## Entrance

To open the decomposition panel, player can press the tab from Shop(see ShopManager.cs)
Currently there are 3 tabs(All(type0), type1, type2) -> (Check it from SelectTab())
I will manually add 4th tab in the slot grid, when player press the tab

- Change state from Merchant -> Decomposition
- Change shopGroup.alpha to purchaseShopAlpha(0.5 by default), just like purchasing
- Open Decomposition Panel

## UI structure

```
DecompositionPanel
DecompositionPanel/CloseButton
DecompositionPanel/InventorySlotGrid
DecompositionPanel/DecompositonSlotGrid
DecompositionPanel/ConfirmButton
```

In DecompositionPanel/InventorySlotGrid, you should place `const int universalSlotCount = 12`(from InventorySO.cs) slot prefabs(as in InventoryUIController.cs) under DecompositionPanel/InventorySlotGrid. This is same as player's inventory

In DecompositionPanel/DecompositonSlotGrid, you will place 9 slot prefabs. You need to create a scriptable object to store the items in these 9 slots, the item inside should still exists even player close the panel

DecompositionPanel/CloseButton closes the panel(the item inside decomposition slot grid should remain)

DecompositionPanel/ConfirmButton triggers the decomposition process, see Decompositon section.

## Slot

I will provide slot prefab, but there could be slighly different.
The original InventorySlot.cs handles slot dragging and dropping in player's own inventory, we should give the slot's in decomposition panel different script.

You need to create new script for these slots(they should have access to InventorySO and other)

The 12 + 9 slots are all universal slot(can place anything).
Moving from player's slot to decompose slot will move the exact item from player's inventory to the new decomposition inventory(the new scriptable object)

The 9 slots from decomposition inventory cannot hold items that cannot be decomposed(i.e. minPiece == maxPiece == 0)

Other remains the same.

## Decomposition

I will provide 6 ItemSO as different rarity of weapon pieces.

Once press decomposition button, decompose all items in decomposition inventory(the 9 slots)
Each item gives minPiece~maxPiece of weapon pieces with same rarity(default normal)

The returned weapon pieces will be in decomposition inventory(weapon pieces item will have minPiece == maxPiece == 1)
There are only 6 rarity and 9 slots, it is guarenteed to fit inside.

When decompose, skip the weapon pieces inside decomposition inventory if player forget to take out.

## Exit

When player press E/Esc or close button, exit the decomposition panel and return Merchant state(and set alpha back)