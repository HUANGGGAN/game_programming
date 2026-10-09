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
    private int activeDamageSourceId;

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
        activeDamageSourceId = Interlocked.Increment(ref nextDamageSourceId);

        if (meleeWeapon.drawAttackRange)
        {
            float attackAngle = Mathf.Clamp(meleeWeapon.attackAngle, 0f, 360f);
            DrawAttackRange(activeOrigin, activeDirection, meleeWeapon.attackRange, attackAngle, activeDuration);
        }

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
                activeWeapon.attackDamage,
                knockbackDirection,
                activeWeapon.knockbackForce,
                activeDamageSourceId,
                activeDuration + Time.fixedDeltaTime))
            {
                enemy.ApplyEffects(activeWeapon.effects);
            }
        }
    }

    private static void DrawAttackRange(Vector2 origin, Vector2 direction, float range, float attackAngle, float duration)
    {
        float centerAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float halfAngle = attackAngle * 0.5f;
        float drawDuration = Mathf.Max(0.15f, duration);
        int segments = Mathf.Max(1, Mathf.CeilToInt(attackAngle / 10f));
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
            return;

        Color color = new Color(1f, 0.75f, 0.1f, 1f);
        GameObject rangeVisual = new GameObject("Melee Attack Range");
        LineRenderer lineRenderer = rangeVisual.AddComponent<LineRenderer>();
        Material material = new Material(shader);
        lineRenderer.sharedMaterial = material;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = 0.035f;
        lineRenderer.endWidth = 0.035f;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.sortingOrder = short.MaxValue;
        lineRenderer.numCapVertices = 3;
        lineRenderer.numCornerVertices = 3;

        Vector3[] points = new Vector3[segments + 3];
        const float renderZ = 0f;
        points[0] = new Vector3(origin.x, origin.y, renderZ);
        Vector2 previousPoint = GetArcPoint(origin, centerAngle - halfAngle, range);
        points[1] = new Vector3(previousPoint.x, previousPoint.y, renderZ);

        for (int i = 1; i <= segments; i++)
        {
            float angle = centerAngle - halfAngle + attackAngle * i / segments;
            Vector2 arcPoint = GetArcPoint(origin, angle, range);
            points[i + 1] = new Vector3(arcPoint.x, arcPoint.y, renderZ);
        }

        points[points.Length - 1] = points[0];
        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);
        UnityEngine.Object.Destroy(rangeVisual, drawDuration);
        UnityEngine.Object.Destroy(material, drawDuration);
    }

    private static Vector2 GetArcPoint(Vector2 origin, float angle, float range)
    {
        float radians = angle * Mathf.Deg2Rad;
        return origin + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * range;
    }
}