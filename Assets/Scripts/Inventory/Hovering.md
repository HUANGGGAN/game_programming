# Hovering and Clicking

## Target Files

- Frame.cs
- InventorySlot.cs
- ItemSO.cs

## Goal

- To show the description of an item in slot
- Change the frame's(Frame.cs) border color

## Details

### Description panel

Show the description of the item(ItemSO) when one of these happen.

- If the cursor hover over a slot(InventorySlot) for a short time(make it configurable)
- User click the slot.

When user start dragging the item, description panel should not appear.

I'll create a panel manually, reuse it by changing the text.
```
DescriptionPanel
DescriptionPanel/Text
```

- The text should be `public string description;` in ItemSO.

- The position of the panel: If InventorySlot.x < half_of_the_screen, the panel will be on the cursor's right side, otherwise left side.

### Border color

- when cursor hovers on a frame with mouse is not clicking, change the frame's border color temporarily
- when user click the frame, change the frame's border color until other frame be clicked or inventory closed(marked as selected), only one frame can be selected at once(this rule takes precedence over other rules)

make these 2 color configurable.