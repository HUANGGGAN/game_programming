## Target Files

PlayerShooter.cs
ProjectileAttack.cs
BulletSO.cs
WeaponSO.cs

## Goal

You are going to implement charged weapon, which means

The attack and velocity of the bullet increase as the player holding left click.(to max damage/velocity)
We will use bow and arrow for example.

You may inherit this type of weapon from GunSO(ChargedSO:GunSO)

## Details

shootInterval(after release and shoot, player must wait this time before start charging another attack)

we will add
- baseDamage: 10
- baseSpeed: 10
- maxChargeTime(sec): 1

If the player simply click and shoot, the damage will be baseDamage

Multipliers
- attackMul: 3
- speedMul: 2
- probMul: 2(this probability is the chance of applying effect on target, see EnemySO.cs)

Multipliers will apply on bullet if the weapon fully charged, otherwise apply partial charge
For instance, damage

$final_damage = baseDamage \times  (1+\frac{(attackMul-1) * chargeTime}{maxChargeTime})$ (a.k.a linearly)
remember to clip chargeTime to maxChargeTime.

Eventually, pass the finalDamage, finalSpeed, probability to the bullet to update.(we will use bulletID:1)