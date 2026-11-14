using System.Collections.Generic;
using System.Threading;
using UnityEngine;

internal static class DamageSources
{
    private static int nextId;
    public static int Next() => Interlocked.Increment(ref nextId);
}

public static class Explosion
{
    public static void Perform(Vector2 origin, float radius, float damage, float knockbackForce,
        EffectDefinition[] effects = null, int[] attackAttributes = null, Player attacker = null,
        float effectProbabilityMultiplier = 1f, Vector2 fallbackDirection = default)
    {
        radius = Mathf.Max(0f, radius);
        DrawExplosionRange(origin, radius);
        int sourceId = DamageSources.Next();
        HashSet<EnemyChase> damaged = new HashSet<EnemyChase>();
        foreach (Collider2D collider in Physics2D.OverlapCircleAll(origin, radius))
        {
            EnemyChase enemy = collider.GetComponentInParent<EnemyChase>();
            if (enemy == null || !enemy.isActiveAndEnabled || !damaged.Add(enemy)) continue;
            Vector2 direction = (Vector2)enemy.transform.position - origin;
            if (direction.sqrMagnitude <= Mathf.Epsilon) direction = fallbackDirection;
            if (enemy.TakeDamage(damage, direction, knockbackForce, sourceId, Time.fixedDeltaTime,
                attackAttributes, EnemyDamageKind.Projectile))
                enemy.ApplyEffects(effects, effectProbabilityMultiplier,
                    attacker != null ? attacker.RestoreEffectResource : null);
        }
    }

    private static void DrawExplosionRange(Vector2 origin, float radius)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
            return;

        const int segments = 64;
        const float drawDuration = 0.2f;
        // Keep the visual independent of the pooled bullet that deactivates on impact.
        GameObject rangeVisual = new GameObject("Explosion Range");
        LineRenderer lineRenderer = rangeVisual.AddComponent<LineRenderer>();
        Material material = new Material(shader);
        lineRenderer.sharedMaterial = material;
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true;
        lineRenderer.startWidth = 0.035f;
        lineRenderer.endWidth = 0.035f;
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        lineRenderer.sortingOrder = short.MaxValue;
        lineRenderer.numCornerVertices = 3;

        Vector3[] points = new Vector3[segments];
        for (int i = 0; i < segments; i++)
        {
            float angle = 2f * Mathf.PI * i / segments;
            points[i] = new Vector3(
                origin.x + Mathf.Cos(angle) * radius,
                origin.y + Mathf.Sin(angle) * radius,
                0f);
        }

        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);
        UnityEngine.Object.Destroy(rangeVisual, drawDuration);
        UnityEngine.Object.Destroy(material, drawDuration);
    }

}
