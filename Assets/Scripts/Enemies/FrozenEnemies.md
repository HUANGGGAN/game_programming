# Structure

```
IceLake/ -> hold a manager
IceLake/FrozenEnemies1
IceLake/FrozenEnemies2
...
IceLake/FrozenEnemies6
IceLake/EnemyGroup1
...
IceLake/EnemyGroup6
IceLake/InvisibleWall1
IceLake/InvisibleWall2
IceLake/InvisibleWall3
```

For each IceLake/Treasure{i}

```
IceLake/Treasure{i}
IceLake/Treasure{i}/treasure2_{i}_1_0
IceLake/Treasure{i}/treasure2_{i}_2_0
IceLake/Treasure{i}/treasure2_{i}_3_0
IceLake/Treasure{i}/treasure2_{i}_4_0
```

# Implement

Implement FrozenEnemies1~FrozenEnemies6

Create FrozenEnemies.cs for each FrozenEnemies{i}, this act as a huge ice.(with a collider)
- No movement/knockback
- Can be damaged by player(make sure melee attack work)
- When damaged, flash
- Does not have effect handler
- Have weakness ID:5
- does not have a health bar
- Does not drop item
- 120HP
- 3 stages, i will provide 3 icons, each image for 40HP
- Link to a EnemyGroup{i}, when died, trigger the enemies

For each EnemyGroup{i}, there will be several enemies under it.
Those enemies cannot move/attack/do any action. But you cannot simply disable them, they should be rendered on screen.

Once triggered by FrozenEnemies, all enemies restore.

Implement a IceLakeManager.cs

If FrozenEnemies1 and 2 dies, deactivate invisible wall 1
3,4 -> 2
5,6 -> 3