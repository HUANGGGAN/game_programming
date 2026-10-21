# Consumable

## Goal

Create a script under `Player/` for handling consumable items.

- Implement comsumable item usage
- Implement the effection when using the consumable item.

## Details

- Consumable items vary, use id to decide handler function.
- 3 consumable item slots, press z,x,c to use the item and decrease by 1, if count <= 0, clear it from the slot.


## IDs

0: preserved
1: Call Heal(20.0) in PlayerData.cs
2: Clear all effect from player.(You might want to see `public static class EffectRuntime` in EnemySO.cs)
3: Call RestoreMP(20) in PlayerData.cs


## Other things to do

- OnPlayerStatsChanged in PlayerData.cs has no receiver, remove it.