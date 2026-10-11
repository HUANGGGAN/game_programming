using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public interface IWeaponAttackHandler
{
    bool CanHandle(WeaponSO weapon);
    bool TryPerform(WeaponSO weapon, Vector2 origin, Vector2 direction, Func<bool> tryConsumeAmmo);
}

public interface ITimedWeaponAttackHandler
{
    void Tick(float deltaTime);
}

public sealed class MeleeAttack : IWeaponAttackHandler, ITimedWeaponAttackHandler
{
    private static int nextDamageSourceId;
    private MeleeSO activeWeapon;
    private Vector2 activeOrigin;
    private Vector2 activeDirection;
    private float activeDuration;
    private float elapsedDuration;
    private float activeDamage;
    private int activeDamageSourceId;
    private MeleeSO equippedWeapon;
    private LineRenderer rangeRenderer;
    private Material rangeMaterial;
    private Vector3[] rangePoints;

    public void SetEquippedWeapon(MeleeSO weapon)
    {
        if (equippedWeapon != weapon)
            activeWeapon = null;
        equippedWeapon = weapon;
        if (weapon == null)
            HideRange();
    }

    public void HideRange()
    {
        if (rangeRenderer != null)
            rangeRenderer.enabled = false;
    }

    public void DisposeRange()
    {
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
        if (attacking)
        {
            origin = activeOrigin;
            direction = activeDirection;
        }
        DrawAttackRange(origin, direction, Mathf.Max(0f, weapon.attackRange),
            Mathf.Clamp(weapon.attackAngle, 0f, 360f), attacking ? 0.9f : 0.3f);
    }

    public bool CanHandle(WeaponSO weapon)
    {
        return weapon is MeleeSO;
    }

    public bool TryPerform(WeaponSO weapon, Vector2 origin, Vector2 direction, System.Func<bool> tryConsumeAmmo)
    {
        if (weapon is not MeleeSO meleeWeapon || meleeWeapon.attackRange <= 0f || meleeWeapon.attackDamage <= 0f || direction.sqrMagnitude <= Mathf.Epsilon)
            return false;

        if (activeWeapon != null)
            return false;

        if (meleeWeapon.consumeAmmo && !tryConsumeAmmo())
            return false;

        activeWeapon = meleeWeapon;
        activeOrigin = origin;
        activeDirection = direction.normalized;
        activeDuration = Mathf.Max(0f, meleeWeapon.attackDuration);
        elapsedDuration = 0f;
        activeDamage = CriticalAttack.RollDamage(meleeWeapon.attackDamage, meleeWeapon.critRate, meleeWeapon.critMul);
        activeDamageSourceId = Interlocked.Increment(ref nextDamageSourceId);

        ApplyAttack();
        if (activeDuration <= 0f)
            activeWeapon = null;

        return true;
    }

    public void Tick(float deltaTime)
    {
        if (activeWeapon == null)
            return;

        if (elapsedDuration >= activeDuration)
        {
            activeWeapon = null;
            return;
        }

        ApplyAttack();
        elapsedDuration += deltaTime;
        if (elapsedDuration >= activeDuration)
            activeWeapon = null;
    }

    private void ApplyAttack()
    {
        float halfAngle = Mathf.Clamp(activeWeapon.attackAngle, 0f, 360f) * 0.5f;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(activeOrigin, activeWeapon.attackRange);
        foreach (Collider2D collider in colliders)
        {
            EnemyChase enemy = collider.GetComponentInParent<EnemyChase>();
            if (enemy == null)
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
                enemy.ApplyEffects(activeWeapon.effects);
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
        if (rangePoints == null || rangePoints.Length != segments + 3)
            rangePoints = new Vector3[segments + 3];

        rangePoints[0] = new Vector3(origin.x, origin.y, 0f);
        for (int i = 0; i <= segments; i++)
        {
            float angle = centerAngle - halfAngle + attackAngle * i / segments;
            Vector2 arcPoint = GetArcPoint(origin, angle, range);
            rangePoints[i + 1] = new Vector3(arcPoint.x, arcPoint.y, 0f);
        }
        rangePoints[rangePoints.Length - 1] = rangePoints[0];
        rangeRenderer.positionCount = rangePoints.Length;
        rangeRenderer.SetPositions(rangePoints);
    }

    private static Vector2 GetArcPoint(Vector2 origin, float angle, float range)
    {
        float radians = angle * Mathf.Deg2Rad;
        return origin + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * range;
    }
}