# Goal

Show some animation and status for enemies.
These will work under a world space canvas

- Add health bar for each enemy and a health bar pool
- Add coin drop animation for when enemy dies, use pool
- Add a pop up damage number when enemy is damaged, manage the text by a pool

# Detail

## Health Bar

Enemy count could be large ~30, create a pool with initialized 30 health bar, you may manage it with EnemyPoolSpawner.cs

Each enemy would have a health bar rendered in world space canvas, I will provide the prefab

```
EnemyHealthBarBG -> Just a static rectangle
EnemyHealthBarBackground/EnemyHealthBarFill -> The health bar, use horizontal fill type 
```

When Enemy Initialize, it should have a full health bar.
When damaged(once health drop), change the fill amount(immeidiately, no need something like smoothspeed)

Clean up(restore to pool) when enemy despawn

## Coin animation

Every enemy drops coin, currently we only add the amount(addMoney by Player), we need a coin animation

1. The coin jumps out from enemy's dead body, popping out, scattering, and falling to the ground using simple lightweight velocity
2. Smoothing straight to the top-left UI corner(I will assign the position(transform))

Player don't need to collect the coins by themselves, so no collider needed.

Use a pool to manage each coin.
I will assign the coin's image, we will create a prefab first(not yet), then assign to the pool

## Damage number

When Enemy damaged, make a damage number(I will create a text mesh pro prefab) jump out from enemy.

We will manage the text with pool too.