# Attack Mode

## Target Files

- EnemySO.cs
- AttackIndicator.cs(create a new file)
- Enemy.cs

## Goal

Draw the range and timing of enemies attack.

## Details

I will manually create a rectangle under enemy prefab, use this as attack indicator.
```
AttackIndicator -> the whole indicator
AttackIndicator/FillPart -> the fill part to show the timing
```

Currently enemy attack player when in attackRange, change it to:

1. When enemy enter attackRange, enable the indicator(the rectangle)
2. The rectangle should face toward player.
3. The rectangle will start filling(fillAmount from 0.0 to 1.0) in attackDuration seconds.
4. When the indicator is full and player is still inside the rectangle, do the damage to player(as usual)
5. Then wait for attackInterval seconds before next attack.

- During attack, stop the enemy's movement(chasing)
- If the enemy is hit during the attack, terminate this attack

These parameters are already in EnemySO.cs
```
public float attackRange = 1.5f;
public float attackInterval = 1.2f;
public float attackDuration = 0.4f;
```