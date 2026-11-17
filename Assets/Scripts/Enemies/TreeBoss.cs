using System;
using System.Collections;
using UnityEngine;

public partial class TreeBoss : EnemyChase
{
    [Header("Health")]
    [SerializeField, Min(1f)] private float health = 500f;
    protected override EnemySO EnemyData => null;
    protected override float HealthCapacity => Mathf.Max(1f, health);
    protected override int MaxEffectLevel => 3;
    [Header("Death Presentation")]
    [SerializeField, Min(0f)] private float deathFadeDuration = 2f;
    [SerializeField, Min(0f)] private float deathShakeAmount = 0.15f;
    private SpriteRenderer[] visuals;
    private Color[] originalColors;
    private bool combatEnabled;
    public bool IsDead { get; private set; }
    public override bool ShowWorldHealthBar => false;
    public event Action Died;
    public event Action CheckpointReached;

    protected override bool DespawnWhenFarAway => false;
    protected override bool CanReceiveKnockback => false;
    protected override bool CancelAttackOnHit => false;
    protected override bool OwnsDamageCollider(Collider2D hit)
        => skillRoot == null || !hit.transform.IsChildOf(skillRoot);
    protected override IEnemyAttackModule CreateAttackModule() => null;
    protected override void UpdateMovement() { }
    protected override void UpdateSpriteFacing() { }

    protected override void OnEnable()
    {
        IsDead = false;
        combatEnabled = false;
        base.OnEnable();
        Agent.enabled = false;
        visuals = Array.FindAll(GetComponentsInChildren<SpriteRenderer>(true),
            visual => skillRoot == null || !visual.transform.IsChildOf(skillRoot));
        originalColors = new Color[visuals.Length];
        for (int i = 0; i < visuals.Length; i++) originalColors[i] = visuals[i].color;
        InitializeSkills();
    }

    public void BeginFight() => combatEnabled = true;

    protected override void Update()
    {
        base.Update();
        if (combatEnabled && !IsDead && Time.deltaTime > 0f) TickSkills(Time.deltaTime);
    }

    protected override void OnDisable()
    {
        StopSkills();
        SetPresentationAlpha(1f);
        base.OnDisable();
    }

    public void SetPresentationAlpha(float alpha)
    {
        if (visuals == null) return;
        for (int i = 0; i < visuals.Length; i++)
        {
            if (visuals[i] == null) continue;
            Color color = originalColors[i];
            color.a *= Mathf.Clamp01(alpha);
            visuals[i].color = color;
        }
    }

    public override bool TakeDamage(float amount, Vector2 direction, float force, int sourceId,
        float sourceDuration, int[] attributes = null, EnemyDamageKind damageKind = EnemyDamageKind.Other)
    {
        if (!combatEnabled || IsDead) return false;
        return base.TakeDamage(amount, direction, force, sourceId, sourceDuration, attributes, damageKind);
    }

    protected override void Die()
    {
        if (IsDead) return;
        IsDead = true;
        StopSkills();
        base.Die();
        Died?.Invoke();
    }

    protected override IEnumerator DeactivateAfterDeath()
    {
        Vector3 position = transform.position;
        float elapsed = 0f;
        while (elapsed < deathFadeDuration)
        {
            float alpha = 1f - elapsed / deathFadeDuration;
            for (int i = 0; i < visuals.Length; i++)
                if (visuals[i] != null)
                    visuals[i].color = new Color(1f, 0f, 0f, originalColors[i].a * alpha);
            if (Time.deltaTime > 0f)
                transform.position = position + (Vector3)(UnityEngine.Random.insideUnitCircle * deathShakeAmount);
            yield return null;
            elapsed += Time.deltaTime;
        }
        transform.position = position;
        SetPresentationAlpha(1f);
        gameObject.SetActive(false);
    }
}
