using UnityEngine;

[CreateAssetMenu(fileName = "NewWand", menuName = "Weapon/Wand")]
public class WandSO : WeaponSO, IProjectileWeapon
{
    [Header("Wand Stats")]
    public BulletSO bulletData;
    public int attackMode; // 0: projectile; other modes require their own attack handler
    public BulletSO ProjectileData => bulletData;
    // Additional wand-specific stats can be added here
}
