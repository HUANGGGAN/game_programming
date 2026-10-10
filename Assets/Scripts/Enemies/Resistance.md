## Target Files

EnemySO.cs
WeaponSO.cs
Enemy.cs

## Goal

Implement resistance in Enemy.

In EnemySO, we have
public int[] resistances;
public int[] weaknesses;

In EnemySO, MeleeSO, BulletSO(attack media), we have
public int[] attackAttributes;
public EffectDefinition[] effects; // already implemented handler

currently, there are 3 types of Effect
- Fire, ID:1
- Faint, ID:2
- Bleed, ID:3

And could apply on target when attack with probability(already implemented in EnemySO.cs)

Now If EnemySO has same type of resistance/weakness(Let's say fire, ID:1)
For the attack media (EnemySO, MeleeSO, BulletSO)

resistance will avoid applying effect on itself, and decrease the damage by 70% with attack media holding same attackAttribute.

weakness will double the damage when hit by attack media holding same attackAttribute.

You can ignore ArmorSO for now, as it will be implemented differently.

## Example

BulletA:
- attackAttribute = {1, 2}
- effects = {1} // Fire, lv:1, 100%
- damage = 10

EnemyA:
- resistance = {1}
(will avoid being apply fire, and take only 3 damage point(10*0.3))

EnemyB:
- weakness = {1}
(will take 20 damage point)