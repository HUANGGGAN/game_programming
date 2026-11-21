using UnityEngine;

[CreateAssetMenu(fileName = "NewWand", menuName = "Weapon/Wand")]
public class WandSO : WeaponSO, IProjectileWeapon
{
    [Header("Wand Stats")]
    public BulletSO bulletData;
    public int attackMode; // 0: projectile, 1: meteorite, 2: tornado (summons need no BulletSO)
    [Min(0f)] public float meteoriteScatterRadius = 4f;
    public BulletSO ProjectileData => bulletData;
    // Additional wand-specific stats can be added here
}
