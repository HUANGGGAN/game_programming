# Goal

Implement full TreeBoss.cs

# Detail

TreeBoss does not use EnemySO.cs. For those data needed, make it configurable under TreeBoss.cs

## MoveType

No move, and won't be knockback
- Health

## Attack

Uses skills, each skill has CD time.

### Skill 1

I will provide a CircleAttackIndicator Prefab, make pool under Treeboss(initial poolsize=5)

```
CircleAttackIndicator
CircleAttackIndicator/FillPart
CircleAttackIndicator/PokeHalfImage
CircleAttackIndicator/PokeFullImage
```

CD time 5s(PokeAttackCDTime)

Summon 5(PokeAttackCount) CircleAttackIndicator in a sequence at player's position, with a 0.5s(PokeAttackInterval) interval
Set FillPart scale to 0 at first, and increase the scale(both x and y) to 1 in 0.7s(PokeAttackDuration)
When the scale between 0.5-1, Show PokeHalfImage
when reaches 1, show PokeFullImage, and for later 1.5 second(PokeAttackRemainTime)
If Player is in the CircleAttackIndicator(use circle collider 2D), then do damage to player
- PokeAttackDamage
- PokeAttackKnockbackForce
- PokeAttackEffect
Then finish this attack.

### Skill2

Summon Slime in arena, spawnpoint should not be too close to player(SpawnMinDistance) , I will provide the prefab

x from -140 to -125
y from 7 to 21

CD time: 16s(SummonCDTime)

Summon 3 slimes(SummonPoint)

### Skill3

Vine entangle.

I will provide two type of vine(image), use Tiled draw mode + size.width to achieve growing.
Also I will provide VineAttackIndicator(rectangle) prefab, make pool under TreeBoss(initial poolsize=5)
Implement Vine.cs, and I will make it a prefab(and make a pool too)


CD time: 11s(VineCDTime)


Attack process:
Summon VineAttackIndicator at x = -145.8, y from 7 to 22
Set FillPart scale(x only) from 0 to 1 in 4s(VineHintDuration)
Once scale = 1, disable the indicator, summon Vine at the same place and start growing
To grow, set vine's image to one of the provided image, tiled mode size.width from 0 to 40 in 3s(VineAttackDuration), to handle collider, scale the collider size with the sprite

box collider.size.x = tile.size.width-1
box collider.offset.x = (tile.size.width-1) / 2

If player hit the vine(collider), stop player's movement and knockback until player kills the vine. Vines won't be knockback
- VineHealth:36

Vines cannot be hit before reaching player.


## Damaged

No resistance or weakness. Take damage normally.

For handling the effect, bound the effect level to at most 3.

Damage won't stop the boss from attacking.


## Boss Health Bar

Uses BossHealthBar.cs, in BossHealthBar.cs handle treeboss for now is enough, but we will add more bosses in future.

## Health CheckPoint

Once reaches one third or two third of the health. Pause attack and CD time count down and do camera shake for 1 second.

Increase boss' stats

PokeAttackCDTime - 0.5/1.0 s
PokeAttackDuration - 0.1/0.2s
PokeAttackCount + 1/2
PokeAttackInterval -0.05s/0.1s
PokeAttackDamage +1/2

SummonCDTime -2/4s
SummonPoint +0/1

## Boss Manager

When trigger boss, enter Boss state, stop spawning enemy from RegionEnemySpawner.

Leave Boss state only when boss dead or player dead.
If player dead, open the seal door and disable the boss.
