# Goal

- Implement Enemy ranged attack logic
- Separate Enemy attack module
- Create EnemyBulletSO.cs
- Create EnemyBullet.cs

# Detail

## Attack Type

Enemies could have different attack types
- Melee: Same as Enemy.cs now
- Ranged: Fire EnemyBullet to player when there is no obstacle between player and enemy.

## Enemy Base Class

Different enemies would have different script, make Enemy.cs a base class that handle damage, or dying process

## EnemyBulletSO.cs and EnemyBullet.cs

Almost same as BulletSO.cs, but remove critical attack

## Ranged Attack

Ranged attack enemy will stop at stop distance if no obstacles between enemy and player, otherwise keep targeting.

## Enemy Explosion

We already have Explosion.cs for player. Make enemy type explosion that damage player, you might want to reuse some code from Explosion.cs

For both player and enemy type explosion, add a particleId to play particle effect from ParticlePool