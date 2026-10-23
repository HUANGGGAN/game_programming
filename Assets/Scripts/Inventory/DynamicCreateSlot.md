## Goal

Dynamically create inventory slot at runtime.

## Detail

In InventoryUIController.cs, slot's are find under SlotGrids.

Make it create the slot at runtime, with count declared in InventorySO.cs(private InventorySO inventoryData)

```
public const int universalSlotCount = 18;
    public const int weaponSlotCount = 3;
    public const int armorSlotCount = 4;
    public const int consumableSlotCount = 3;
```

I'll provide a prefab for each slot
```
Frame
Frame/Slot
Frame/Slot/Text
Frame/Border
```

Remember to change the slot type(In InventorySlot.cs) for different slot grid.
