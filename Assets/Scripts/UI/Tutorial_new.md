Below are the tutoral used now, remove them, we will add new one

# Old
./Enemies/WolfBoss.cs:                Tutorial.Instance?.ShowTutorial("shield", "The boss has gained a shield! You must break the shield by melee attacking.\nThe boss won't take damage from ranged attacks while the shield is active.", bossShieldSprite);
./UI/Tutorial.cs:    public bool ShowTutorial(string tutorialId, string text, Sprite image = null)
./AttackHandler/PlayerShooter.cs:                Tutorial.Instance?.ShowTutorial("reload", "You are out of ammo! Press R to reload your weapon. Don't worry, ammo is unlimited");
./Inventory/InventoryManager.cs:            Tutorial.Instance?.ShowTutorial("inventory", "In the inventory, you can view your weapons and other items.\nThe slots at the left are for your weapons. You can move them around or discard them by dragging and dropping.\n");
./Inventory/PanelManager.md:event called by any script(Tutorial.Instance?.ShowTutorial(...)): Any state to Tutorial state
./Player/Player.cs:        tutorial.ShowTutorial("start", "Welcome to the tutorial level, don't worry about dying. Pick up various weapons and try to defeat the boss. The game will get harder over time until the boss is defeated, so keep an eye on the time.");
./Player/Player.cs:        tutorial.ShowTutorial("movement", "WASD to move, left click to shoot\nF to interact with objects\n1-3 or mouse wheel to switch weapons\nE to open inventory.");


# New

### When game start

request 2 tutorials

Movement: WASD/arrow keys
Interact: F
Inventory: E
Weapon: Scroll/1/2/3
Potion: Z/X/C

Map: M
Teleport to Home:P
Add 10 coins:=


### First obtain multi-weapon

Press T to switch attack mode(A/B)

### First open inventory

This is your inventory.
Double Click: Equip/Unequip
Right Click: Split

### First open decomposite panel

You can decomposite weapons here.
Move weapons to the right side.
Get random weapon from weapon pieces! 

### First open Storage

This is your storage box.

### First leave home

Find the tree boss and kill it.
Feel free to explore the world and arm yourself.
Return home to restore HP/MP!

### First Obtain Weapon with Fire or Frost tag

Enemies has resistance/weakness.
Find suitable weapons to attack them.

### First respawn

After death, you will lose 20% of money

### First obtain ItemSO with Chest Tag

Some of the chests are locked
Find merchant at home to open