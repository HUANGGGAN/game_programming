using System;
using UnityEngine;

public interface IChargedAttackHandler
{
    bool TryPerformCharged(WeaponSO weapon, Vector2 origin, Vector2 direction, float chargeRatio, Func<bool> tryConsumeAmmo, Vector2? targetPosition = null, Player attacker = null);
}

public sealed class ProjectileAttack : IWeaponAttackHandler
{
    public bool CanHandle(WeaponSO weapon)
    {
        return weapon is not WandSO && weapon is IProjectileWeapon projectileWeapon && projectileWeapon.ProjectileData != null;
    }

    public bool TryPerform(WeaponSO weapon, Vector2 origin, Vector2 direction, Func<bool> tryConsumeAmmo, Vector2? targetPosition = null, Player attacker = null)
    {
        return ProjectileAttackExecutor.TryPerform(weapon, origin, direction, null, tryConsumeAmmo, targetPosition, attacker);
    }
}

public sealed class WandProjectileAttack : IWeaponAttackHandler
{
    public bool CanHandle(WeaponSO weapon)
    {
        return weapon is WandSO wand && wand.attackMode == 0 && wand.bulletData != null;
    }

    public bool TryPerform(WeaponSO weapon, Vector2 origin, Vector2 direction, Func<bool> tryConsumeAmmo, Vector2? targetPosition = null, Player attacker = null)
    {
        if (!CanHandle(weapon))
            return false;

        WandSO wand = (WandSO)weapon;
        return ProjectileAttackExecutor.TryPerform(
            wand, origin, direction, null, tryConsumeAmmo, targetPosition, attacker);
    }
}

public sealed class ChargedAttack : IWeaponAttackHandler, IChargedAttackHandler
{
    public bool CanHandle(WeaponSO weapon)
    {
        return weapon is IChargedProjectileWeapon &&
            weapon is IProjectileWeapon projectileWeapon && projectileWeapon.ProjectileData != null;
    }

    public bool TryPerform(WeaponSO weapon, Vector2 origin, Vector2 direction, Func<bool> tryConsumeAmmo, Vector2? targetPosition = null, Player attacker = null)
    {
        return TryPerformCharged(weapon, origin, direction, 0f, tryConsumeAmmo, targetPosition, attacker);
    }

    public bool TryPerformCharged(WeaponSO weapon, Vector2 origin, Vector2 direction, float chargeRatio, Func<bool> tryConsumeAmmo, Vector2? targetPosition = null, Player attacker = null)
    {
        if (weapon is not IChargedProjectileWeapon)
            return false;

        return ProjectileAttackExecutor.TryPerform(weapon, origin, direction, Mathf.Clamp01(chargeRatio), tryConsumeAmmo, targetPosition, attacker);
    }
}

internal static class ProjectileAttackExecutor
{
    public static bool TryPerform(WeaponSO weapon, Vector2 origin, Vector2 direction, float? chargeRatio, Func<bool> tryConsumeResource, Vector2? targetPosition = null, Player attacker = null)
    {
        if (weapon is not IProjectileWeapon projectileWeapon || projectileWeapon.ProjectileData == null || BulletPool.Instance == null)
            return false;

        BulletSO configuredBullet = projectileWeapon.ProjectileData;

        int burstCount = 1;
        float spread = 0f;
        if (weapon is IProjectilePatternWeapon patternWeapon)
        {
            burstCount = Mathf.Max(1, patternWeapon.ProjectileBurstCount);
            spread = Mathf.Max(0f, patternWeapon.ProjectileSpread);
        }

        bool resourceConsumed = false;
        bool firedAny = false;
        for (int i = 0; i < burstCount; i++)
        {
            BulletSO bulletData = configuredBullet;
            if (configuredBullet is RandomBulletSO random && !random.TryPickBullet(out bulletData))
                break;
            float damage = bulletData.damage;
            float speed = bulletData.speed;
            float effectProbabilityMultiplier = 1f;
            if (chargeRatio.HasValue && weapon is IChargedProjectileWeapon chargedWeapon)
            {
                float charge = chargeRatio.Value;
                damage = chargedWeapon.BaseDamage * Mathf.Lerp(1f, chargedWeapon.AttackMultiplier, charge);
                speed = chargedWeapon.BaseSpeed * Mathf.Lerp(1f, chargedWeapon.SpeedMultiplier, charge);
                effectProbabilityMultiplier = Mathf.Lerp(1f, chargedWeapon.ProbabilityMultiplier, charge);
            }
            Bullet2D bullet = BulletPool.Instance.GetBullet(bulletData);
            if (bullet == null)
                break;

            // Pay the attack's MP/ammo cost once, even for a multi-projectile burst.
            if (!resourceConsumed)
            {
                if (!tryConsumeResource())
                    return false;
                resourceConsumed = true;
            }

            float spreadOffset = burstCount == 1
                ? UnityEngine.Random.Range(-spread, spread)
                : -spread + (2f * spread * i / (burstCount - 1));
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, spreadOffset) * direction.normalized;
            if (targetPosition.HasValue && ((bulletData.bulletMode == 1 && bulletData.isExplosive) ||
                (bulletData.bulletMode == 0 && bulletData.bulletId == "1")))
                shotDirection = (targetPosition.Value - origin).normalized;
            float shotDamage = CriticalAttack.RollDamage(
                damage,
                bulletData.critRate,
                bulletData.critMul);
            bullet.transform.position = origin;
            bullet.Attacker = attacker;
            bullet.Launch(shotDirection, shotDamage, speed, effectProbabilityMultiplier, targetPosition);
            firedAny = true;
        }

        return firedAny;
    }
}
