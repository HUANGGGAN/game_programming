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

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapon/Weapon")]
public class WeaponSO : ItemSO
{
    [Header("Weapon Stats")]
    public float shootInterval = 1.0f; // Time between shots
    public bool consumeAmmo = true; // Whether the weapon consumes ammo
    public bool hasLimitedAmmo = false; // Whether the weapon has limited ammo
    public Vector2 firePointOffset = new Vector2(0.5f, 0.1f); // Offset for the fire point relative to the weapon's position
}

[CreateAssetMenu(fileName = "NewGun", menuName = "Weapon/Gun")]
public class GunSO : WeaponSO, IProjectileWeapon, IProjectilePatternWeapon, IReloadableWeapon
{
    [Header("Gun Stats")]
    public int capacity = 30; // Number of bullets per magazine
    public float reloadTime = 2.0f; // Time to reload in seconds
    public float spread = 5.0f; // Bullet spread in degrees
    public int burstCount = 1; // Number of bullets fired in a burst
    public BulletSO bulletData; // Reference to the bullet data used by this gun
    public BulletSO ProjectileData => bulletData;
    public float ProjectileSpread => spread;
    public int ProjectileBurstCount => burstCount;
    public int AmmoCapacity => capacity;
    public float ReloadDuration => reloadTime;
}

[CreateAssetMenu(fileName = "NewChargedWeapon", menuName = "Weapon/Charged")]
public class ChargedSO : GunSO, IChargedProjectileWeapon
{
    [Header("Charge Stats")]
    public float baseDamage = 10f;
    public float baseSpeed = 10f;
    [Min(0f)] public float maxChargeTime = 1f;
    public float attackMul = 3f;
    public float speedMul = 2f;
    public float probMul = 2f;

    public float BaseDamage => baseDamage;
    public float BaseSpeed => baseSpeed;
    public float MaxChargeTime => maxChargeTime;
    public float AttackMultiplier => attackMul;
    public float SpeedMultiplier => speedMul;
    public float ProbabilityMultiplier => probMul;
}

[CreateAssetMenu(fileName = "NewMelee", menuName = "Weapon/Melee")]
public class MeleeSO : WeaponSO
{
    [Header("Melee Stats")]
    public float attackRange = 2.0f; // Range of the melee attack
    public float attackAngle = 90.0f; // Angle of the melee attack in degrees
    public float attackDamage = 10.0f; // Damage value for the melee attack
    public float attackDuration = 0.1f; // Duration of the melee attack
    public float knockbackForce = 1.0f; // Force applied to enemies on hit
    [Range(0f, 1f)] public float critRate = 0.05f;
    public float critMul = 3f;
    public bool drawAttackRange;
    public EffectDefinition[] effects;
    public int[] attackAttributes; // Attribute IDs for the bullet's attack type (e.g., fire, ice, poison)
}

[CreateAssetMenu(fileName = "NewWand", menuName = "Weapon/Wand")]
public class WandSO : WeaponSO, IProjectileWeapon
{
    [Header("Wand Stats")]
    [Min(0)] public int MPCost = 0; // Mana cost per shot
    public BulletSO bulletData;
    public int attackMode; // 0: projectile; other modes require their own attack handler
    public BulletSO ProjectileData => bulletData;
    // Additional wand-specific stats can be added here
}

[CreateAssetMenu(fileName = "NewArmor", menuName = "Weapon/Armor")]
public class ArmorSO : ItemSO
{
    [Header("Armor Stats")]
    public float defense = 10.0f; // Amount of damage reduction
    public float healthBonus = 0.0f; // Bonus health provided by the armor
    public float speedPenalty = 0.0f; // Penalty to movement speed when wearing the armor
    public float knockbackResistance = 0.0f; // Resistance to knockback effects
    public int[] resistances; // Array of resistance IDs that this armor provides (e.g., fire, ice, poison)
    public int[] weaknesses;
}

[CreateAssetMenu(fileName = "NewConsumable", menuName = "Item/Consumable")]
public class ConsumableSO : ItemSO
{
    [Header("Consumable Stats")]
    public int[] effects; // Array of effect IDs that this consumable applies when used (e.g., heal, buff, debuff)
    public int[] effectDurations; // Array of durations for each effect in seconds
}
