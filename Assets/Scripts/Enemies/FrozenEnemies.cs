using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class FrozenEnemies : EnemyChase
{
    [Tooltip("Images for 81–120 HP, 41–80 HP, and 1–40 HP, in that order.")]
    [SerializeField] private Sprite[] stageImages = new Sprite[3];
    [SerializeField] private FrozenEnemyGroup enemyGroup;
    [SerializeField] private GameObject iceGround;
    public event Action<FrozenEnemies> Broken;
    public bool IsBroken { get; private set; }
    protected override EnemySO EnemyData => null;
    protected override float HealthCapacity => 120f;
    protected override bool GrantDeathRewards => false;
    protected override bool CanReceiveKnockback => false;
    protected override bool DespawnWhenFarAway => false;
    public override bool ShowWorldHealthBar => false;
    protected override IEnemyAttackModule CreateAttackModule() => null;

    protected override void OnEnable()
    {
        base.OnEnable();
        Agent.enabled = false;
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        body.linearVelocity = Vector2.zero;
        body.constraints |= RigidbodyConstraints2D.FreezePosition;
        IsBroken = false;
        RefreshImage();
    }

    protected override void Update() => base.Update();

    public override void ApplyEffects(EffectDefinition[] effects, float probabilityMultiplier = 1f,
        Action<int, int> restoreAttacker = null) { }

    protected override float GetAttackDamageMultiplier(int[] attackAttributes)
    {
        if (attackAttributes != null && Array.IndexOf(attackAttributes, 1) >= 0) return 2.5f;
        return 1f;
    }

    public override bool TakeDamage(float damageAmount, Vector2 direction, float force, int sourceId,
        float sourceDuration, int[] attackAttributes = null, EnemyDamageKind damageKind = EnemyDamageKind.Other, bool isCritical = false)
    {
        bool damaged = base.TakeDamage(damageAmount, direction, force, sourceId, sourceDuration, attackAttributes, damageKind, isCritical);
        if (damaged && !IsBroken) RefreshImage();
        return damaged;
    }

    private void RefreshImage()
    {
        int stage = CurrentHealth > 80f ? 0 : CurrentHealth > 40f ? 1 : 2;
        if (EnemySprite != null && stageImages != null && stage < stageImages.Length && stageImages[stage] != null)
            EnemySprite.sprite = stageImages[stage];
    }

    protected override void Die()
    {
        if (IsBroken) return;
        IsBroken = true;
        if (iceGround != null) iceGround.SetActive(true);
        base.Die();
        if (enemyGroup != null) enemyGroup.Release();
        Broken?.Invoke(this);
    }
}
