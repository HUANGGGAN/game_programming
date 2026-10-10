using System;
using UnityEngine;

public interface IChargedAttackHandler
{
    bool TryPerformCharged(WeaponSO weapon, Vector2 origin, Vector2 direction, float chargeRatio, Func<bool> tryConsumeAmmo);
}

public sealed class ProjectileAttack : IWeaponAttackHandler
{
    public bool CanHandle(WeaponSO weapon)
    {
        return weapon is not WandSO && weapon is IProjectileWeapon projectileWeapon && projectileWeapon.ProjectileData != null;
    }

    public bool TryPerform(WeaponSO weapon, Vector2 origin, Vector2 direction, Func<bool> tryConsumeAmmo)
    {
        return ProjectileAttackExecutor.TryPerform(weapon, origin, direction, null, tryConsumeAmmo);
    }
}

public sealed class WandProjectileAttack : IWeaponAttackHandler
{
    private readonly Func<int, bool> tryConsumeMP;

    public WandProjectileAttack(Func<int, bool> tryConsumeMP)
    {
        this.tryConsumeMP = tryConsumeMP;
    }

    public bool CanHandle(WeaponSO weapon)
    {
        return weapon is WandSO wand && wand.attackMode == 0 && wand.bulletData != null;
    }

    public bool TryPerform(WeaponSO weapon, Vector2 origin, Vector2 direction, Func<bool> tryConsumeAmmo)
    {
        if (!CanHandle(weapon))
            return false;

        WandSO wand = (WandSO)weapon;
        return ProjectileAttackExecutor.TryPerform(
            wand, origin, direction, null, () => tryConsumeMP != null && tryConsumeMP(Mathf.Max(0, wand.MPCost)));
    }
}

public sealed class ChargedAttack : IWeaponAttackHandler, IChargedAttackHandler
{
    public bool CanHandle(WeaponSO weapon)
    {
        return weapon is IChargedProjectileWeapon &&
            weapon is IProjectileWeapon projectileWeapon && projectileWeapon.ProjectileData != null;
    }

    public bool TryPerform(WeaponSO weapon, Vector2 origin, Vector2 direction, Func<bool> tryConsumeAmmo)
    {
        return TryPerformCharged(weapon, origin, direction, 0f, tryConsumeAmmo);
    }

    public bool TryPerformCharged(WeaponSO weapon, Vector2 origin, Vector2 direction, float chargeRatio, Func<bool> tryConsumeAmmo)
    {
        if (weapon is not IChargedProjectileWeapon)
            return false;

        return ProjectileAttackExecutor.TryPerform(weapon, origin, direction, Mathf.Clamp01(chargeRatio), tryConsumeAmmo);
    }
}

internal static class ProjectileAttackExecutor
{
    public static bool TryPerform(WeaponSO weapon, Vector2 origin, Vector2 direction, float? chargeRatio, Func<bool> tryConsumeResource)
    {
        if (weapon is not IProjectileWeapon projectileWeapon || projectileWeapon.ProjectileData == null || BulletPool.Instance == null)
            return false;

        BulletSO bulletData = projectileWeapon.ProjectileData;
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
            Bullet2D bullet = BulletPool.Instance.GetBullet(bulletData);
            if (bullet == null)
                break;

            // Wand handlers supply mana consumption here instead of inventory ammo.
            if ((weapon is WandSO || weapon.consumeAmmo) && !resourceConsumed)
            {
                if (!tryConsumeResource())
                    return false;
                resourceConsumed = true;
            }

            float spreadOffset = burstCount == 1
                ? UnityEngine.Random.Range(-spread, spread)
                : -spread + (2f * spread * i / (burstCount - 1));
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, spreadOffset) * direction.normalized;
            float shotDamage = CriticalAttack.RollDamage(
                damage,
                bulletData.critRate,
                bulletData.critMul);
            bullet.transform.position = origin;
            bullet.Launch(shotDirection, shotDamage, speed, effectProbabilityMultiplier);
            firedAny = true;
        }

        return firedAny;
    }
}
