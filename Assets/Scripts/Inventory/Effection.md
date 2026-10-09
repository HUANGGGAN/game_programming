## Target Files

WeaponSO.cs
MeleeAttack.cs
BulletSO.cs
Bullet.cs
Enemy.cs
EnemySO.cs

## Goal

To apply effection on attacked target.

Currently EnemySO.cs, BulletSO.cs, MeleeSO has 
- int [] effect
- int [] effectDurations (change it to float, this is effection apply duration(sec))

wrap it into a class array(class effect{int effectID, float duration, float probability, int level})

The two array should have same length

When attacked, (melee, bullet collision, enemy attack), apply effection based on ID with probability.
There could be many IDs, make sure you can handle multiple.


## Countdown
Player and enemies should record how many seconds remain for each effections.
`array of {effectID, remainTime, level}`
If a higher level effection with same effectID, replace it.
If same level effection, set remainTime to max(remainTime, apply duration).

## EffectID

There are only 3 now, handle them separately, as we will implement resistance in future.
You may write mutual function to avoid repeating writing same code.

- ID:0, Fire, drop `{level}` HP per second
- ID:1, Faint, set speed to 0
- ID:2, Bleed, drop `{level}` HP per second