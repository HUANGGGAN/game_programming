## Chest

Implement a interactable chest.
- Chest.cs

## Details

Chests may need keys, add a required ItemSO field(it can be null if no key is required)
Open chest need time
A chest hold an item, but we will use PickableItem prefab

- ItemSO requiredKey;
- float openTime;

Chest state:
- Locked(require a key to unlock)
- Closed(unlocked, still need time to open)
- Opening(chest is now opening, player should keep in the same distance to continue opening, if player leave, set back to Closed state)
(when done open, destroy the chest and spawn the selected item prefab at the same place)


### Show text:
- Press F to open(when Closed state)
- "You need {ItemSO.itemName} to open ({count}/1)" (If count == 0, set the text to red, otherwise green)(when Locked state)
- empty string(when Opening state)

### Progress bar
I have installed AnnulusGames/UnityProgressBar and make a prefab.

when opening, use the progress bar to show the progress, you need to write another ProgressBar.cs