# Structure

```
InventoryMenu -> Has the script InventoryUIController
InventoryMenu/Stats
...
```

There are texts in Stats box inside InventoryMenu, I'll assign them in Unity

Can be found in PlayerData.cs
- HPText: Format $"{currentHealth/maxHealth}"
- MPText: Format $"{currentMP/maxMP}"
- CoinText: Format $"{money}"

Add kill count in PlayerData.cs, when enemy died, increase by 1(just like money)
- KillText: Format $"{killCount}"

Add unique weapon in PlayerData.cs, when gain a new WeaponSO, +1
- UniqueWeaponText: Format $"{uniqueWeapon/34}"
(You might want to see InventorySO to see how it get item)

I will assign the text to InventoryUIController

# Top Left Stats

I will assign it to InventoryUIController, just make sure to hide it when inventory is open.