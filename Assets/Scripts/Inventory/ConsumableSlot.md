# Goal

- Manage consumable UI slot
- Add CD time for consumables

# Detail

## CD

In ConsumableSO.cs, add CD time.
In PlayerConsumables.cs, manage the time for consumable use, all of the 3 slots share same cooldown time.

## UI

I created a consumable UI slot just like WeaponSlot does(check WeaponSlot.cs)

```
LowerConsumablePanel
LowerConsumablePanel/ConsumableFrame -> radial 360 fill just like PlayerShooter.cs UpdateWeaponFrames(), but there is only one frame as they share same cooldown time
LowerConsumablePanel/ConsumableLayout -> Three image under this grid slot, show the image of the consumable item
LowerConsumablePanel/ConsumableCounts -> Three text under this grid slot, show the count
```

You can check how PlayerShooter.cs and InventoryUIController.cs manage the weapon slot. Do something similar, and I will wire them in Unity.