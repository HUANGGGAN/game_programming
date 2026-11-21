# Reset

Make Tutorial.cs record those tutorial already read in Scriptable Object(TutorialSO), so it does not appear again in different scene.

So, for those scriptable objects, we might need to save them when Return to Main Menu, but reset them for a new game.

- TutorialSO
- DecompositionInventorySO
- InventorySO
- PlayerData

For player state
- Handle quitting during boss fight, simply end the boss fight
- Player's position, player will respawn at home(we will do this first)

For thing we will do later(we might give them ID or even stages)
- Puzzles
- PickableItems or Chests
- Boss defeated

For other Enemy or DropItems in pool, just let them disappear

Create another scriptable objects that save data in gameplay, but for now, only a isSaved is needed.

# Resume game button

This should load MainGame and map_0_0 and Main again.

Mention above, some data should be resumed.

For now, focus on the 5 scriptable objects.

If game is saved, show this button, otherwise hide it