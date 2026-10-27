# Merchant

## Goal

- Create a new file `Merchant.cs` for handling interaction
- Create a new file `ShopManager.cs` for handling transaction in Shop panel
- Create a new file `SellingObjectSO.cs` as a scriptable object, to add new item in shop

## Detail

### Merchant(Merchant.cs)

A game object, when player interact with it(see Interactable.cs, press F), open shop panel and set state to Merchant state(StateManager.cs)

### Shop Panel(ShopManager.cs)

I'll manually create these in Shop Panel
- A Close Button
- Button slot grid(empty initially)
- Button prefab
- Tab Slot Grid // with 3 button inside

#### Close Button

Player can close the shop panel(Merchant state to Playing state) by
- Press Close Button
- Press E or Esc

#### Button Prefab

I will manually create button prefab.
Each registered SellingObjectSO in Shop.cs should create a new button at the beginning(Under Button slot grid)

Structure
```
Button
Button/ItemImage  ->  Set to SellingObjectSO.objectIcon
Button/ItemNameText  ->  Set to SellingObjectSO.objectName;
Button/PricePanel
Button/PricePanel/CurrencyImage -> Set to SellingObjectSO.currencyIcon
Button/PricePanel/PriceText  -> Set to SellingObjectSO.price
```

SellingObjectSO
```
int type; // to decide which tab this button belongs to
ItemSO currency; // the item player needs to pay for, if null, player will have to pay PlayerData.money
Sprite currencyIcon;
int price;
ItemSO object; // the item player wants to buy, can be null
Sprite objectIcon; // show the icon on the button, cannot be null
string objectName; // show the name on the button, cannot be null
bool buyOneAtOnce; // false to let player buy multiple at once
string description; // show in descripton panel
int randomItemGroupID; // 0:not random item, give player ItemSO directly. Other:Give player random item, implement it later.
```

To give item to player, check InventorySO.cs TryGiveItem()
Right now you don't need to implement random item, but keep this in mind.

ShopManager.cs should have a configurable SellingObjectSO SellingObjects[];

If player press the button(buying the stuff), do
1. Check if player have enough currency(or money)
2. Check if player will have enough space after taking the item.
3. If both succeed, give player the item.


#### Tab Slot Grid

If player press i-th tab button, list all the buttons with SellingObjectSO.type == i
If player press first button(all), then show all buttons no matter the SellingObjectSO.type.
(To do this, you only need to enable those buttons that needed to be show, the slot grid will place them to correct place)