using UnityEngine;

public interface IProjectileWeapon
{
    BulletSO ProjectileData { get; }
}

public interface IProjectilePatternWeapon
{
    float ProjectileSpread { get; }
    int ProjectileBurstCount { get; }
}

public interface IReloadableWeapon
{
    int AmmoCapacity { get; }
    float ReloadDuration { get; }
}

public interface IChargedProjectileWeapon
{
    float BaseDamage { get; }
    float BaseSpeed { get; }
    float MaxChargeTime { get; }
    float AttackMultiplier { get; }
    float SpeedMultiplier { get; }
    float ProbabilityMultiplier { get; }
}

public class WeaponSO : ItemSO
{
    [Header("Weapon Stats")]
    public float shootInterval = 1.0f; // Time between shots
    public bool consumeAmmo = true; // Whether the weapon consumes ammo
    public bool hasLimitedAmmo = false; // Whether the weapon has limited ammo
    public Vector2 firePointOffset = new Vector2(0.5f, 0.1f); // Offset for the fire point relative to the weapon's position
}