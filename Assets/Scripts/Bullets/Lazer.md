# Goal

Add lazer attack mode in game
Lazer for Bullet type == 2


#### Type 2

Create LazerSO.cs over WeaponSO.cs

- Color // keep it simple now, we may change later
- Thickness
- Endpoint particle(id, request from ParticlePool)
- damage
- knockbackForce
- public EffectDefinition[] effects;
- public int[] attackAttributes;

Mode:
- 0: Normal

## Implementation
Use Line renderer and Physics2D.Raycast

Draw the laser between playershooter and target, the laser stop at obstacle or enemies.

It enemies hit, do damage, knockbackforce, effects to target every shootInterval

As for Endpoint particle, play the particle for each shootInterval(same time as damage apply)

After implementation, tell me what should I create and attach in Unity.