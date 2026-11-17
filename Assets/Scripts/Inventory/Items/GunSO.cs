using UnityEngine;

[CreateAssetMenu(fileName = "NewGun", menuName = "Weapon/Gun")]
public class GunSO : WeaponSO, IProjectileWeapon, IProjectilePatternWeapon, IReloadableWeapon
{
    [Header("Gun Stats")]
    public int capacity = 30; // Number of bullets per magazine
    public float reloadTime = 2.0f; // Time to reload in seconds
    [Tooltip("Independent random deviation per bullet, in degrees (+/- this angle).")]
    [Min(0f)] public float spread = 5.0f;
    [Tooltip("Total angle of the evenly spaced burst fan, centered on the aim direction.")]
    [Range(0f, 360f)] public float burstAngle = 10f;
    public int burstCount = 1; // Number of bullets fired in a burst
    public BulletSO bulletData; // Reference to the bullet data used by this gun
    public BulletSO ProjectileData => bulletData;
    public float ProjectileSpread => spread;
    public float ProjectileBurstAngle => burstAngle;
    public int ProjectileBurstCount => burstCount;
    public int AmmoCapacity => capacity;
    public float ReloadDuration => reloadTime;
}
