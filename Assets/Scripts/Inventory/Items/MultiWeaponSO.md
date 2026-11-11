# Goal

- Create a MultiWeaponSO.cs to allow player to switch attack mode from a same weapon

## Details

### Inventory

From InventorySlotData, we have something like item instance

```
public class InventorySlotData
{
    public ItemSO item;
    public int count;
    public int ammoCount;
}
```

Add a attackModeIndex to it, as the current weapon's attack mode. -1 to be unused.

### MultiWeaponSO

Create a new class, this is considered to be WeaponSO too -> It could be equipped in weapon slot

MultiWeaponSO {WeaponSO [] } // Stores different type of WeaponSO(GunSO, MeleeSO, ...) to let player switch attack mode.

When PlayerShooter.cs tries to equip MultiWeaponSO, try equip WeaponSO[attackModeIndex]

When Player press T, switch to next attack mode -> attackModeIndex++

### Weapon switching

In MultiWeaponSO, it is possible to switch from MeleeSO to GunSO(actually any type of WeaponSO)

- For InventorySlotData.ammoCount, set it to 0 when player tries to switch weapon(only when switch by pressing T, unequip won't unload ammo)
- When player first equip MultiWeaponSO, set attackModeIndex = 0 by default