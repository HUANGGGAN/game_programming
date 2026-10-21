using UnityEngine;

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