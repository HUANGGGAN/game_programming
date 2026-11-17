using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Vine : EnemyChase
{
    [SerializeField] private SpriteRenderer vineImage;
    [SerializeField, Min(1f)] private float vineHealth = 36f;
    private BoxCollider2D hitbox;
    private Player trappedPlayer;
    private float elapsed;
    private float duration;
    private float width;
    private bool reachedPlayer;
    private readonly List<Collider2D> overlaps = new List<Collider2D>();
    protected override EnemySO EnemyData => null;
    protected override float HealthCapacity => vineHealth;
    protected override bool GrantDeathRewards => false;
    protected override bool CanReceiveKnockback => false;
    protected override bool CancelAttackOnHit => false;
    protected override bool DespawnWhenFarAway => false;
    protected override IEnemyAttackModule CreateAttackModule() => null;

    public void Configure(Sprite sprite, float health, float growthDuration, float targetWidth)
    {
        ReleasePlayer();
        if (vineImage == null) vineImage = GetComponentInChildren<SpriteRenderer>(true);
        hitbox = GetComponent<BoxCollider2D>();
        hitbox.isTrigger = true;
        vineHealth = Mathf.Max(1f, health);
        duration = Mathf.Max(0.01f, growthDuration);
        width = Mathf.Max(0f, targetWidth);
        elapsed = 0f;
        reachedPlayer = false;
        if (vineImage != null)
        {
            float imageHeight = Mathf.Max(0.01f, vineImage.size.y);
            if (sprite != null) vineImage.sprite = sprite;
            vineImage.drawMode = SpriteDrawMode.Tiled;
            vineImage.size = new Vector2(0f, imageHeight);
            if (vineImage.sprite == null)
                Debug.LogWarning("Vine needs a prefab sprite or a sprite assigned in TreeBoss Vine Images.", this);
        }
        SetWidth(0f);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Agent.enabled = false;
        SetWidth(0f);
    }

    // TreeBoss ticks vines so checkpoints also pause their growth.
    protected override void Update() { }
    public void Tick(float deltaTime)
    {
        if (!isActiveAndEnabled || CurrentHealth <= 0f) return;
        base.Update();
        if (CurrentHealth <= 0f) return;
        elapsed += Mathf.Max(0f, deltaTime);
        SetWidth(width * Mathf.Clamp01(elapsed / duration));
        if (!reachedPlayer && elapsed >= duration + 3f)
        {
            ReleasePlayer();
            gameObject.SetActive(false);
            return;
        }
        if (reachedPlayer || hitbox == null || !hitbox.enabled) return;
        overlaps.Clear();
        hitbox.Overlap(ContactFilter2D.noFilter, overlaps);
        foreach (Collider2D other in overlaps)
        {
            Player player = other.GetComponentInParent<Player>();
            if (player == null || player.IsDying) continue;
            reachedPlayer = true;
            trappedPlayer = player;
            player.SetMovementLocked(this, true);
            break;
        }
    }

    private void SetWidth(float value)
    {
        if (vineImage != null) vineImage.size = new Vector2(value, vineImage.size.y);
        if (hitbox == null) return;
        float colliderWidth = Mathf.Max(0f, value - 1f);
        hitbox.enabled = true;
        hitbox.size = new Vector2(Mathf.Max(0.001f, colliderWidth), hitbox.size.y);
        hitbox.offset = new Vector2(colliderWidth * 0.5f, hitbox.offset.y);
    }

    private void ReleasePlayer()
    {
        if (trappedPlayer != null) trappedPlayer.SetMovementLocked(this, false);
        trappedPlayer = null;
    }
    protected override void Die() { ReleasePlayer(); base.Die(); }
    protected override void OnDisable() { ReleasePlayer(); base.OnDisable(); }
}
