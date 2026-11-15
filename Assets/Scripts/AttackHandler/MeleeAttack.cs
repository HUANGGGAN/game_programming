using System;
using System.Collections.Generic;
using UnityEngine;

public interface IWeaponAttackHandler
{
    bool CanHandle(WeaponSO weapon);
    bool TryPerform(WeaponSO weapon, Vector2 origin, Vector2 direction, Func<bool> tryConsumeAmmo, Vector2? targetPosition = null, Player attacker = null);
}

public interface ITimedWeaponAttackHandler
{
    void Tick(float deltaTime);
}

public sealed class MeleeAttack : IWeaponAttackHandler, ITimedWeaponAttackHandler
{
    private MeleeSO activeWeapon;
    private Player activeAttacker;
    private Vector2 activeOrigin;
    private Vector2 activeDirection;
    private float activeDuration;
    private float elapsedDuration;
    private float activeDamage;
    private int activeDamageSourceId;
    private MeleeSO equippedWeapon;
    private WeaponHolder weaponHolder;
    private int completedAttacks;
    private Vector2 previousDashOrigin;
    private readonly HashSet<EnemyChase> hitEnemies = new HashSet<EnemyChase>();

    public void SetWeaponHolder(WeaponHolder holder) => weaponHolder = holder;

    public void CancelAttack()
    {
        if (activeWeapon != null && activeWeapon.attackType == 1 && activeAttacker != null)
            activeAttacker.EndMeleeDash();
        weaponHolder?.SetMeleeSpin(0f);
        activeWeapon = null;
        activeAttacker = null;
        hitEnemies.Clear();
    }

    private void BeginStrike()
    {
        activeDamage = CriticalAttack.RollDamage(activeWeapon.attackDamage, activeWeapon.critRate, activeWeapon.critMul);
        activeDamageSourceId = DamageSources.Next();
        hitEnemies.Clear();
        if (activeWeapon.attackType == 2 && activeWeapon.drawAttackRange)
            DrawAttackRange(activeOrigin, activeDirection, Mathf.Max(0f, activeWeapon.attackRange), 360f, 0.9f);
    }

    private LineRenderer rangeRenderer;
    private Material rangeMaterial;
    private Vector3[] rangePoints;

    public void SetEquippedWeapon(MeleeSO weapon)
    {
        if (equippedWeapon != weapon)
            CancelAttack();
        equippedWeapon = weapon;
        if (weapon == null || !weapon.drawAttackRange)
            HideRange();
    }

    public void HideRange()
    {
        if (rangeRenderer != null)
            rangeRenderer.enabled = false;
    }

    public void DisposeRange()
    {
        CancelAttack();
        if (rangeRenderer != null)
            UnityEngine.Object.Destroy(rangeRenderer.gameObject);
        if (rangeMaterial != null)
            UnityEngine.Object.Destroy(rangeMaterial);
        rangeRenderer = null;
        rangeMaterial = null;
    }

    public void UpdateRange(Vector2 origin, Vector2 direction)
    {
        if (equippedWeapon == null)
        {
            HideRange();
            return;
        }

        bool attacking = activeWeapon != null;
        MeleeSO weapon = attacking ? activeWeapon : equippedWeapon;
        if (!weapon.drawAttackRange)
        {
            HideRange();
            return;
        }
        if (attacking)
        {
            origin = weapon.attackType == 1 && activeAttacker != null ? (Vector2)activeAttacker.transform.position : activeOrigin;
            direction = activeDirection;
        }
        float opacity = attacking ? 0.9f : 0.3f;
        if (attacking && weapon.attackType == 2)
        {
            // Restart the highlight for every strike instead of showing one solid circle.
            float progress = Mathf.Clamp01(elapsedDuration / activeDuration);
            opacity = Mathf.Lerp(0.9f, 0.3f, progress);
        }
        DrawAttackRange(origin, direction,
            Mathf.Max(0f, weapon.attackType == 1 ? weapon.attackRadius : weapon.attackRange),
            weapon.attackType == 0 ? Mathf.Clamp(weapon.attackAngle, 0f, 360f) : 360f,
            opacity);
    }

    public bool CanHandle(WeaponSO weapon)
    {
        return weapon is MeleeSO;
    }

    public bool TryPerform(WeaponSO weapon, Vector2 origin, Vector2 direction, System.Func<bool> tryConsumeAmmo, Vector2? targetPosition = null, Player attacker = null)
    {
        if (weapon is not MeleeSO meleeWeapon || meleeWeapon.attackRange <= 0f || meleeWeapon.attackDamage <= 0f || direction.sqrMagnitude <= Mathf.Epsilon)
            return false;

        if (meleeWeapon.attackType == 1 && (attacker == null || !attacker.CanMeleeDash || meleeWeapon.attackRadius <= 0f))
            return false;
        if (activeWeapon != null)
            return false;

        if (!tryConsumeAmmo())
            return false;

        activeWeapon = meleeWeapon;
        activeAttacker = attacker;
        activeOrigin = meleeWeapon.attackType == 1 ? (Vector2)attacker.transform.position : origin;
        previousDashOrigin = activeOrigin;
        activeDirection = direction.normalized;
        activeDuration = meleeWeapon.attackType == 0 ? Mathf.Max(0f, meleeWeapon.attackDuration) : Mathf.Max(Time.fixedDeltaTime, meleeWeapon.attackDuration);
        elapsedDuration = 0f;
        completedAttacks = 0;
        BeginStrike();
        if (meleeWeapon.attackType == 1)
            attacker.BeginMeleeDash(activeDirection, meleeWeapon.attackRange, activeDuration);

        if (meleeWeapon.ParticleID > 0)
        {
            float angle = Mathf.Atan2(activeDirection.y, activeDirection.x) * Mathf.Rad2Deg - 90f;
            ParticlePool.Instance?.Play(
                meleeWeapon.ParticleID.ToString(System.Globalization.CultureInfo.InvariantCulture),
                activeOrigin, Quaternion.Euler(0f, 0f, angle) * Quaternion.Euler(-90f, 0f, 0f));
        }

        ApplyAttack();
        if (activeDuration <= 0f)
            CancelAttack();

        return true;
    }

    public void Tick(float deltaTime)
    {
        if (activeWeapon == null || deltaTime <= 0f) return;
        if (activeWeapon.attackType == 1)
        {
            if (activeAttacker == null || !activeAttacker.isActiveAndEnabled)
            {
                CancelAttack();
                return;
            }
            activeOrigin = activeAttacker.transform.position;
            ApplyAttack();
            previousDashOrigin = activeOrigin;
            if (!activeAttacker.IsMeleeDashing) CancelAttack();
            return;
        }

        // Split long frames at strike boundaries so every spin gets its own hit.
        while (activeWeapon != null && deltaTime > 0f)
        {
            ApplyAttack();
            float step = Mathf.Min(deltaTime, activeDuration - elapsedDuration);
            elapsedDuration += step;
            deltaTime -= step;
            if (activeWeapon.attackType == 2)
                weaponHolder?.SetMeleeSpin(360f * elapsedDuration / activeDuration);
            if (elapsedDuration < activeDuration) break;
            completedAttacks++;
            if (activeWeapon.attackType != 2 || completedAttacks >= Mathf.Max(1, activeWeapon.attackCount))
            {
                CancelAttack();
                break;
            }
            elapsedDuration = 0f;
            BeginStrike();
            ApplyAttack();
        }
    }

    private void ApplyAttack()
    {
        float halfAngle = activeWeapon.attackType == 0 ? Mathf.Clamp(activeWeapon.attackAngle, 0f, 360f) * 0.5f : 180f;
        Collider2D[] colliders;
        if (activeWeapon.attackType == 1)
        {
            // Sweep the circular hit area between physics positions to catch enemies along the dash.
            Vector2 travel = activeOrigin - previousDashOrigin;
            float radius = Mathf.Max(0f, activeWeapon.attackRadius);
            colliders = Physics2D.OverlapCapsuleAll((activeOrigin + previousDashOrigin) * 0.5f,
                new Vector2(travel.magnitude + radius * 2f, radius * 2f), CapsuleDirection2D.Horizontal,
                Mathf.Atan2(travel.y, travel.x) * Mathf.Rad2Deg);
        }
        else
            colliders = Physics2D.OverlapCircleAll(activeOrigin, activeWeapon.attackRange);
        foreach (Collider2D collider in colliders)
        {
            EnemyChase enemy = collider.GetComponentInParent<EnemyChase>();
            if (enemy == null || hitEnemies.Contains(enemy))
                continue;

            Vector2 directionToEnemy = (Vector2)enemy.transform.position - activeOrigin;
            if (Vector2.Angle(activeDirection, directionToEnemy) > halfAngle)
                continue;

            Vector2 knockbackDirection = directionToEnemy.sqrMagnitude > Mathf.Epsilon
                ? directionToEnemy.normalized
                : activeDirection;
            if (enemy.TakeDamage(
                activeDamage,
                knockbackDirection,
                activeWeapon.knockbackForce,
                activeDamageSourceId,
                activeDuration + Time.fixedDeltaTime,
                activeWeapon.attackAttributes,
                EnemyDamageKind.Melee))
            {
                hitEnemies.Add(enemy);
                enemy.ApplyEffects(activeWeapon.effects, restoreAttacker: activeAttacker != null ? activeAttacker.RestoreEffectResource : null);
            }
        }
    }

    private void DrawAttackRange(Vector2 origin, Vector2 direction, float range, float attackAngle, float opacity)
    {
        if (rangeRenderer == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null)
                return;

            GameObject rangeVisual = new GameObject("Melee Attack Range");
            rangeRenderer = rangeVisual.AddComponent<LineRenderer>();
            rangeMaterial = new Material(shader);
            rangeRenderer.sharedMaterial = rangeMaterial;
            rangeRenderer.useWorldSpace = true;
            rangeRenderer.startWidth = 0.035f;
            rangeRenderer.endWidth = 0.035f;
            rangeRenderer.sortingOrder = short.MaxValue;
            rangeRenderer.numCapVertices = 3;
            rangeRenderer.numCornerVertices = 3;
        }

        rangeRenderer.enabled = true;
        Color color = new Color(1f, 0.2f, 0.1f, opacity);
        rangeRenderer.startColor = color;
        rangeRenderer.endColor = color;
        float centerAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float halfAngle = attackAngle * 0.5f;
        int segments = Mathf.Max(1, Mathf.CeilToInt(attackAngle / 10f));
        bool circle = attackAngle >= 360f;
        int pointCount = circle ? segments + 1 : segments + 3;
        if (rangePoints == null || rangePoints.Length != pointCount)
            rangePoints = new Vector3[pointCount];

        rangePoints[0] = new Vector3(origin.x, origin.y, 0f);
        for (int i = 0; i <= segments; i++)
        {
            float angle = centerAngle - halfAngle + attackAngle * i / segments;
            Vector2 arcPoint = GetArcPoint(origin, angle, range);
            rangePoints[i + (circle ? 0 : 1)] = new Vector3(arcPoint.x, arcPoint.y, 0f);
        }
        if (!circle) rangePoints[rangePoints.Length - 1] = rangePoints[0];
        rangeRenderer.positionCount = rangePoints.Length;
        rangeRenderer.SetPositions(rangePoints);
    }

    private static Vector2 GetArcPoint(Vector2 origin, float angle, float range)
    {
        float radians = angle * Mathf.Deg2Rad;
        return origin + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * range;
    }
}
