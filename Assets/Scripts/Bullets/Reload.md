# Reload

## Goal

Add a radial 360 degree fill method to weapon frame for showing reloading.
Add a vertical fill method to weapon frame for showing cooldown time(shoot interval)

## Details

I'll manually create 3 image(WeaponFrame1-3) under slot grid(WeaponFrameSlotGrid), different from those inventory slots.
"Just like InventoryUIController.cs(weaponGuiSlots = weaponSlotPanelTransform.GetComponentsInChildren<WeaponSlot>(true);)"


In PlayerShooter.cs, if player is reloading i-th weapon from weapon slot, then

1. Set i-th WeaponFrame fill method to radial 360 degree with fill amount = 1.0
2. Set fill amount to $\frac{\text{remaining_reload_time}}{\text{reload_time}}$

If the reload process abort or end, set fill amount to 0.0(0 is default)


If (not reloading) and ((player Shoot() with i-th weapon from weapon slot) or (equip i-th weapon)) , then

1. Set i-th WeaponFrame fill method to vertical with fill amount = 1.0
2. Set fill amount to $\frac{\text{next_shot_time} - \text{current_time}}{\text{shootInterval}}$

If the cooldown process abort or end, set fill amount to 0.0(0 is default)
