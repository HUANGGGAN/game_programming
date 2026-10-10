## Target Files

- PlayerShooter.cs
- ProjectileAttack.cs
- WeaponSO.cs
- Player.cs
- PlayerData.cs


## Goal 
Implement WandSO type weapon

## Details

Currently PlayerShooter.cs handles different WeaponSO type and their corresponding attack handler(MeleeSO, GunSO, ChargedSO)

Now we add WandSO in it. Below are attributes of wand.

public int MPCost = 0; // Mana cost per shot
public BulletSO bulletData; // Reference to the bullet data used by this gun
public int attackMode;

### MPCost

Consume currentMP in PlayerData.cs

### attackMode
we have mode = 0 for now, it shoots bullet just like GunSO
but we will have other mode later, make sure you modulize it(maybe use handler)