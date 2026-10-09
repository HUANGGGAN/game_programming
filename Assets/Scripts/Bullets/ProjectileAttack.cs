using System;
using UnityEngine;

public sealed class ProjectileAttack : IWeaponAttackHandler
{
    public bool CanHandle(WeaponSO weapon)
    {
        return weapon is IProjectileWeapon projectileWeapon && projectileWeapon.ProjectileData != null;
    }

    public bool TryPerform(WeaponSO weapon, Vector2 origin, Vector2 direction, Func<bool> tryConsumeAmmo)
    {
        if (weapon is not IProjectileWeapon projectileWeapon || projectileWeapon.ProjectileData == null || BulletPool.Instance == null)
            return false;

        int burstCount = 1;
        float spread = 0f;
        if (weapon is IProjectilePatternWeapon patternWeapon)
        {
            burstCount = Mathf.Max(1, patternWeapon.ProjectileBurstCount);
            spread = Mathf.Max(0f, patternWeapon.ProjectileSpread);
        }

        bool ammoConsumed = false;
        bool firedAny = false;
        for (int i = 0; i < burstCount; i++)
        {
            Bullet2D bullet = BulletPool.Instance.GetBullet(projectileWeapon.ProjectileData);
            if (bullet == null)
                break;

            if (weapon.consumeAmmo && !ammoConsumed)
            {
                if (!tryConsumeAmmo())
                    return false;
                ammoConsumed = true;
            }

            float spreadOffset = burstCount == 1
                ? UnityEngine.Random.Range(-spread, spread)
                : -spread + (2f * spread * i / (burstCount - 1));
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, spreadOffset) * direction.normalized;
            bullet.transform.position = origin;
            bullet.Launch(shotDirection);
            firedAny = true;
        }

        return firedAny;
    }
}