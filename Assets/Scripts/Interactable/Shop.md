# Shop

## Goal

Implement more detail on Shop panel

- Introduce Purchase state
- Implement purchase panel
- Refine Shop panel

## Detail

Currently buying process involves
- TMP_InputField quantityInput;
- descriptionText;
- statusText;
by clicking the button, change it to showing up a purchase panel

### Purchase Panel

Once player click the button to buy something
- show a purchase panel(don't pop up immediately, fade in smoothly instead and make it configurable)
- change state to purchase state
- hold a variable purchase_quantity = 1

the purchase panel(outside of shop panel) has structure
```
PurchasePanel

PurchasePanel/ItemFrame
PurchasePanel/ItemFrame/ItemImage -> Set it to offer's item image
PurchasePanel/ItemFrame/ItemQuantity -> Set it to $"\u00d7 {purchase_quantity}"

PurchasePanel/ItemText -> Set it to SellingObjectSO.objectName
PurchasePanel/DescriptionText -> Set it to SellingObjectSO.description

PurchasePanel/PurchaseCloseButton -> Cancel purchase

PurchasePanel/PlayerOwnPanel/
PurchasePanel/PlayerOwnPanel/PriceFrame
PurchasePanel/PlayerOwnPanel/PriceFrame/PriceImage -> Set it to offer's price image
PurchasePanel/PlayerOwnPanel/PriceFrame/PriceText -> Set it to how many money or the currency player has // Get it from InventorySO once

PurchasePanel/CounterPanel // If buyOneAtOnce == true, disable this counter panel
PurchasePanel/CounterPanel/DecreaseButton // When press it, purchase_quantity = min(1, purchase_quantity-1)
PurchasePanel/CounterPanel/IncreaseButton // purchase_quantity += 1
PurchasePanel/CounterPanel/QuantityText // Set it to $"{purchase_quantity}"

PurchasePanel/PurchaseButton -> Buy the item if enough money or currency, and show up GainItem Panel, you only need to care about randomItemGroupID == 0 for now(not random item), keep in mind if randomItemGroupID > 0, it should be other event.
PurchasePanel/PurchaseButton/PriceFrame
PurchasePanel/PurchaseButton/PriceFrame/PriceImage -> Set it to offer's price image
PurchasePanel/PurchaseButton/PriceFrame/PriceText -> Set it to $"{offer.price * purchase_quantity}, if player has not enough money or currency, set the text to red, otherwise black(check this every time purchase_quantity changes)
```
Keyboard Input:
player can increase/decrease purchase_quantity by pressing D/A or RightArrow/LeftArrow
player can buy the item by pressing enter.

### GainItem Panel
When Player but the item(click button or pressing enter), show the GainItemPanel(fade in smoothly)

The whole panel is an button, click it will close this panel

Structure
```
GainItemPanel // Button, click will close this panel
```

We will do more later.

### State transition

Once player click the offer button
Set Shop panel's alpha to 0.5(make it configurable) in "canvas group"
State: Merchant state -> Purchase state


Player can cancel purchase and get back to shop panel by
- Click PurchaseCloseButton
- Press E/Esc
Set Shop panel's alpha back to 1(make it configurable) in "canvas group"
State: Purchase state -> Merchant state

Player can buy item by pressing PurchaseButton or hit enter, then disable purchase panel, show up GainItemPanel
Set Shop panel's alpha back to 1(make it configurable) in "canvas group"
State: Purchase state -> Merchant state