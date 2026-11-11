using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public sealed class LazerAttack : IWeaponAttackHandler
{
    private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();
    private LineRenderer beam;
    private Material material;
    private Gradient solidGradient;
    private Color solidColor;
    private LazerSO activeWeapon;
    private LazerSO visualWeapon;
    private float visibility;
    private ParticleSystem endpointGlow;
    private ParticleSystem endpointGlowPrefab;

    public bool CanHandle(WeaponSO weapon) => weapon is LazerSO lazer && lazer.mode == 0;

    public bool TryPerform(WeaponSO weapon, Vector2 origin, Vector2 direction, Func<bool> tryConsumeAmmo,
        Vector2? targetPosition = null, Player attacker = null)
    {
        if (!CanHandle(weapon) || direction.sqrMagnitude <= Mathf.Epsilon || !EnsureBeam())
            return false;
        if (tryConsumeAmmo == null || !tryConsumeAmmo())
        {
            Hide();
            return false;
        }
        LazerSO lazer = (LazerSO)weapon;
        if (visualWeapon != lazer)
            visibility = 0f;
        visualWeapon = lazer;
        activeWeapon = lazer;
        EnemyChase enemy = Draw(lazer, origin, direction, targetPosition, out Vector2 endpoint);
        Vector2 hitDirection = targetPosition.HasValue ? targetPosition.Value - origin : direction;
        if (hitDirection.sqrMagnitude <= Mathf.Epsilon)
            hitDirection = direction;
        // Each pulse is already timed by PlayerShooter; source 0 avoids sharing
        // another projectile's per-source immunity window.
        if (enemy != null && enemy.TakeDamage(lazer.damage, hitDirection.normalized,
            lazer.knockbackForce, 0, 0f, lazer.attackAttributes, EnemyDamageKind.Projectile))
            enemy.ApplyEffects(lazer.effects, restoreAttacker: attacker != null ? attacker.RestoreEffectResource : null);
        if (lazer.endpointParticleID > 0)
            ParticlePool.Instance?.Play(lazer.endpointParticleID.ToString(CultureInfo.InvariantCulture), endpoint);
        return true;
    }

    public void UpdateBeam(LazerSO weapon, Vector2 origin, Vector2 direction, Vector2? targetPosition)
    {
        if (weapon != activeWeapon || activeWeapon == null)
            return;
        Draw(weapon, origin, direction, targetPosition, out _);
    }

    private EnemyChase Draw(LazerSO weapon, Vector2 origin, Vector2 direction, Vector2? targetPosition, out Vector2 endpoint)
    {
        Vector2 aim = targetPosition.HasValue ? targetPosition.Value - origin : direction;
        float distance = Mathf.Max(0f, weapon.maxRange);
        direction = aim.sqrMagnitude > Mathf.Epsilon ? aim.normalized : direction.normalized;
        endpoint = origin + direction * distance;
        EnemyChase target = null;
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(weapon.collisionLayers);
        filter.useTriggers = true;
        hits.Clear();
        Physics2D.Raycast(origin, direction, filter, hits, distance);
        hits.Sort((a, b) =>
        {
            int order = a.distance.CompareTo(b.distance);
            if (order != 0) return order;
            return IsObstacle(b.collider, weapon).CompareTo(IsObstacle(a.collider, weapon));
        });
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || !hit.collider.enabled || !hit.collider.gameObject.activeInHierarchy)
                continue;
            bool obstacle = IsObstacle(hit.collider, weapon);
            EnemyChase enemy = hit.collider.GetComponentInParent<EnemyChase>();
            if (!obstacle && (enemy == null || !enemy.isActiveAndEnabled || enemy.CurrentHealth <= 0f))
                continue;
            endpoint = hit.point;
            target = obstacle ? null : enemy;
            break;
        }
        if (weapon.useGradient && weapon.gradient != null)
            beam.colorGradient = weapon.gradient;
        else
        {
            // Replace every key when switching back from a multicolor weapon.
            if (solidGradient == null || solidColor != weapon.color)
            {
                solidColor = weapon.color;
                solidGradient = new Gradient();
                solidGradient.SetKeys(
                    new[] { new GradientColorKey(solidColor, 0f), new GradientColorKey(solidColor, 1f) },
                    new[] { new GradientAlphaKey(solidColor.a, 0f), new GradientAlphaKey(solidColor.a, 1f) });
            }
            beam.colorGradient = solidGradient;
        }
        ApplyThickness();
        beam.sortingOrder = weapon.sortingOrder;
        beam.SetPosition(0, origin);
        beam.SetPosition(1, endpoint);
        beam.enabled = true;
        UpdateEndpointGlow(weapon, endpoint);
        return target;
    }

    private void UpdateEndpointGlow(LazerSO weapon, Vector2 endpoint)
    {
        if (endpointGlowPrefab != weapon.endpointGlowPrefab)
        {
            if (endpointGlow != null)
            {
                endpointGlow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                UnityEngine.Object.Destroy(endpointGlow.gameObject);
            }
            endpointGlow = null;
            endpointGlowPrefab = weapon.endpointGlowPrefab;
        }
        if (endpointGlowPrefab == null)
            return;
        if (endpointGlow == null)
            endpointGlow = UnityEngine.Object.Instantiate(endpointGlowPrefab, (Vector3)endpoint, Quaternion.identity);
        endpointGlow.transform.position = endpoint;
        endpointGlow.gameObject.SetActive(true);
        if (!endpointGlow.isPlaying)
            endpointGlow.Play(true);
    }

    private void StopEndpointGlow()
    {
        if (endpointGlow != null)
            endpointGlow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private static bool IsObstacle(Collider2D collider, LazerSO weapon) =>
        collider != null && !string.IsNullOrEmpty(weapon.obstacleTag) && collider.CompareTag(weapon.obstacleTag);

    private bool EnsureBeam()
    {
        if (beam != null)
            return true;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
            return false;
        material = new Material(shader);
        beam = new GameObject("Lazer Beam").AddComponent<LineRenderer>();
        beam.sharedMaterial = material;
        beam.useWorldSpace = true;
        beam.positionCount = 2;
        beam.numCapVertices = 4;
        beam.enabled = false;
        return true;
    }

    public void Hide()
    {
        activeWeapon = null;
    }

    public void TickVisual(float deltaTime)
    {
        if (beam == null || visualWeapon == null)
            return;
        bool growing = activeWeapon != null;
        float duration = growing ? visualWeapon.growDuration : visualWeapon.shrinkDuration;
        visibility = duration <= 0f ? (growing ? 1f : 0f) :
            Mathf.MoveTowards(visibility, growing ? 1f : 0f, Mathf.Max(0f, deltaTime) / duration);
        ApplyThickness();
        beam.enabled = visibility > 0f;
        if (visibility <= 0f)
            StopEndpointGlow();
    }

    private void ApplyThickness()
    {
        beam.startWidth = beam.endWidth = visualWeapon != null
            ? Mathf.Max(0f, visualWeapon.maxThickness) * Mathf.SmoothStep(0f, 1f, visibility) : 0f;
    }

    public void HideImmediate()
    {
        StopEndpointGlow();
        activeWeapon = null;
        visualWeapon = null;
        visibility = 0f;
        if (beam != null)
        {
            beam.startWidth = beam.endWidth = 0f;
            beam.enabled = false;
        }
    }

    public void Dispose()
    {
        HideImmediate();
        if (beam != null)
            UnityEngine.Object.Destroy(beam.gameObject);
        if (material != null)
            UnityEngine.Object.Destroy(material);
        if (endpointGlow != null)
            UnityEngine.Object.Destroy(endpointGlow.gameObject);
    }
}
