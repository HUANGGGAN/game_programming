# Goal

Create a new script: StateManager.cs
To manage different panel shows up and pause the game.

## Detail

### InventoryManager

Now it only manage

- E: Toggle inventory panel and pause the game when open
- 1/2/3/scroll: switch equipped weapon

### Changes

To make it simple, i will use "states" to describe.

- Playing state: Player can move, shoot, with timescale = 1
- Inventory state: Time pauses, inventory panel shows up
- Tutorial state: Time pauses, tutorial panel shows up
- Merchant state: Time pauses, merchant panel shows up(haven't implemented yet)

The goal is to manage these states(there could be more later) ane pause the time correctly, instead of let each script handle them separately. You might want to check:

- InventoryManager.cs
- Tutorial.cs

### State transition

E: Playing state to Inventory state
E/esc: Inventory state to Playing state

event called by any script(Tutorial.Instance?.ShowTutorial(...)): Any state to Tutorial state
closed by player(Button or Tutorial.Close()): Tutorial state to previous state

(We will do this later)
Interact with F(Interactable.cs):Playing state to Merchant state
Close button/esc: Merchant state to Playing state.

## Final

Each script (InventoryManager, Tutorial, or other) will have to request StateManager.cs