# Goal

- Implement random bullet

# Detail

We have already implemented MultiWeaponSO.cs, now make a new type of bullet

class RandomBulletSO { BulletSO [] , float prob []}

In PlayerShooter.cs, make sure to handle this type of bullet when equip.
In ProjectileAttack.cs, handle the random bullet type

## Shoot

When fire RandomBulletSO, fire a random bullet from the given list with given probability
For burst count = 1, fire exactly one bullet, for burst count > 1, each bullet might be different.