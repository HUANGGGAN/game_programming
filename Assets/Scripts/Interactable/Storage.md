# Goal

Make a storage system

- Create Storage.cs for interactable object
- Create StorageManager.cs for storage logic
- Create StorageSO.cs to store the item inside
- Create StorageSlot.cs for storage UI's slot
- New state: Storage state

# Detail

## Structure

```
StoragePanel
StoragePanel/InventorySlotGrid -> This shows 12 universal slots of player's inventory(just like DecompositionManager.cs)
StoragePanel/"Scroll View"
StoragePanel/"Scroll View"/Viewport
StoragePanel/"Scroll View"/Viewport/Content
StoragePanel/"Scroll View"/Viewport/Content/StorageSlotGrid -> This shows universal slots from StorageSO.cs
```

## Storage.cs

An Interactable object(from Interactable.cs), interact with it to open the panel and get into Storage state

## StorageManager.cs

Manage storage system

- Create 21 slots when initialize(make it configurable), as we may let player unlock slots from merchant during game, make a function to dynamically add slots during runtime.
- Player can exit the panel and Storage state by pressing E/Esc

## StorageSO.cs

This should store items in the storage

## StorageSlot.cs

Just like DecompositionSlot.cs we add before, but the slot can contain any item. I'll provide prefab, you only need to create script.