# Item Count UI

## Goal

Write a script that change a text based on item count.
- `ItemCountUI.cs`

## Detail

Given ItemSO and a TextMeshPro, change the text to the item count.(Check GetItemCount(ItemSO item) in InventorySO)

Let c = item count

if c < 1000, show $"{c}" directly
if 1e6 > c >= 1000, show $"{c/1000}.{c/100%10}k", for example, 75622 shows 75.6k
if 1e9 > c >= 1e6, show $"{c/1e6}.{c/1e5%10}m"
if c >= 1e9, player might be cheating or something wrong, show -1


## MoneyUI.cs

Do the number showing logic in MoneyUI.cs too, although it uses playerData.money instead of item count.
