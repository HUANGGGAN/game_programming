# Goal

To let player do some setting in pause menu

# Structure

PauseMenu should be opened by Esc during playing mode(other panel still remains closing when pressing esc)

```
PauseMenu
PauseMenu/ContinueButton  // This should leave pause menu
PauseMenu/Options
PauseMenu/Options/DeathPenalty
PauseMenu/Options/DeathPenalty/Button // A toggle button, write a script for this, I will provide 2 icons(On/Off), when on, player death will drop 20% money. Default on
PauseMenu/Options/ShowMeleeRange
PauseMenu/Options/ShowMeleeRange/Button // A toggle button too, use the same script, when off, disable drawing melee range(MeleeAttack.cs) no matter the MeleeSO's setting.
PauseMenu/MovementKeyboardBinding
PauseMenu/MovementKeyboardBinding/Up/Button  // A normal button, when click it, highlight it(yellow), then capture the next keyboard input, allow
- [A-Z][0-9]
- `-=[]\\;',./~`
- Esc/Caps/Shift/Ctrl/Alt/Space
// Then the movement is bind to this input, if player press continue button and leave, stop the key selection.
PauseMenu/MovementKeyboardBinding/Up/Button/Text (TMP)    // After player decide the input, set this Text to the input
```

# Detail

Add a new state "Pause", can come from any state and return to them.

You need to write a PauseManager.cs and let the ContinueButton to toggle the panel

As for input selection
- Up/Left/Down/Right, default WASD(keep the arrow key)
- Inventory, default E
- Leave, default Esc
- Switch Weapon1/2/3, default 1/2/3
- Use Consumable1/2/3, default Z/X/C
- Reload, default R
- Change Attack Mode, default T
- Teleport, default P
- Open Map, default M
- Add Coin, default =
- God Mode, default ;
- Run, default Shift

Note that Inventory and Leave still work for leaving panels(e.g. player change it to A/B, then A/B should leave panel)

Write a script for those button that works as I mentioned above.
Add a field for me to select for different type.(for example, enum UP/Left/Down/...)

Write a script for toggle button, also a field for me to select for different type(DeathPenalty/ShowMeleeRange)
