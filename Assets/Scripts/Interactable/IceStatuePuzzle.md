```
FiveStatue/
FiveStatue/IceStatue1
FiveStatue/IceStatue2
FiveStatue/IceStatue3
FiveStatue/IceStatue4
FiveStatue/IceStatue5
FiveStatue/Treasure
```

Write an IceStatue.cs, that moves IcsStatue up and down.(Initial position is at the lowest place, make the height and speed configurable)

Write a IceStatuePuzzle.cs for handling the puzzle.(If player is too far from the puzzle, disable the IceStatue to save resource)

Each IceStatue is an Interactable object, player to follow the order to deactivate those Statues(from 1 to 5)

In correct order, once player try to deactivate the statue(press F), the statue moves to the highest place, player should stay inside the range for a short period(make radius and time configurable). Summon enemies from Region enemy spawner(make points configurable)

When time ends, the statue moves to the lowest place, and change to second icon(I will provide)

If player leaves before end, the statue continue moving in active state.


If player tries to deactivate in wrong order, once interact(press F), all the deactivated statue revive(become active), and summon enemies with punishment(a higher enemy points)


# End

Once 5 statues deactivated, set Treasure to active.