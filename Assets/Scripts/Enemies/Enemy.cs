using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyDamageKind { Other, Projectile, Melee, Effect }

[RequireComponent(typeof(NavMeshAgent), typeof(Rigidbody2D))]
public class EnemyChase : MonoBehaviour, IEffectSource
{
    [Header("Target Setup")]
    [SerializeField] private Transform player;

    [Header("Enemy Data")]
    [SerializeField] private EnemySO enemyData;

    [Header("Despawn Settings")]
    [SerializeField, Min(0f)] private float maxDistance = 50f;

    protected virtual bool DespawnWhenFarAway => true;
    protected NavMeshAgent Agent => agent;
    protected Transform PlayerTarget => player;
    protected SpriteRenderer EnemySprite { get => spriteRenderer; set => spriteRenderer = value; }
    protected bool IsKnockedBack => isKnockedBack;
    protected virtual void OnKnockbackStarting() { }
    protected virtual float MovementSpeed => speed;
    protected virtual EnemyAttackType AttackType => enemyData != null ? enemyData.attackType : EnemyAttackType.Melee;
    private IEnemyAttackModule attackModule;

    protected virtual float AttackDamage => damage;
    protected virtual float AttackKnockback => knockbackForce;
    protected virtual bool CanReceiveKnockback => true;
    protected virtual bool CanFlashOnHit => true;

    public float EnemyPoint => enemyData != null ? enemyData.enemy_point : 1f;
    public float EnemyDifficulty => enemyData != null ? enemyData.enemy_difficulty : 1f;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    [Header("Attack Visual")]
    [SerializeField] private AttackIndicator attackIndicator;

    [Header("Hit Response")]
    [SerializeField, Min(0f)] private float hitCooldownDuration = 0.2f;
    [SerializeField, Min(0f)] private float playerContactPushForce = 1.5f;
    [SerializeField, Min(0f)] private float knockbackDuration = 0.12f;

    // EnemySO owns shared enemy stats. Defaults keep enemies without data usable.
    private float attackRange => enemyData != null ? enemyData.attackRange : 1.5f;
    private float damage => enemyData != null ? enemyData.damage : 10f;
    private float knockbackForce => enemyData != null ? enemyData.knockbackForce : 4f;
    private float knockbackResistance => enemyData != null ? enemyData.knockbackResistance : 0f;
    private float speed => enemyData != null ? enemyData.speed : 2.5f;
    private float attackInterval => enemyData != null ? enemyData.attackInterval : 1.2f;
    private float attackDuration => enemyData != null ? enemyData.attackDuration : 0.4f;
    private float maxHealth => enemyData != null ? enemyData.HP : 35f;

    private NavMeshAgent agent;
    private SpriteRenderer spriteRenderer;
    private bool spriteFacesRightByDefault = true;
    private Color originalSpriteColor;
    private float currentHealth;
    private Rigidbody2D body;
    private Coroutine knockbackRoutine;
    private bool isKnockedBack;
    private bool agentUpdatePositionBeforeKnockback;
    private bool agentStoppedBeforeKnockback;
    private Vector2? lethalKnockbackVelocity;
    private Coroutine hitFlashRoutine;
    private Coroutine deathRoutine;
    private bool isDying;
    private Collider2D[] deathColliders;
    private bool[] deathColliderStates;
    private bool agentEnabledBeforeDeath;
    private Rigidbody2D deathBody;
    private bool bodySimulatedBeforeDeath;
    private Vector2 deathVelocity;
    private readonly HashSet<int> recentDamageSourceIds = new HashSet<int>();
    private readonly Dictionary<int, float> recentDamageSourceTimes = new Dictionary<int, float>();
    private readonly List<ActiveEffect> activeEffects = new List<ActiveEffect>();
    private float effectTickElapsed;
    public IReadOnlyList<ActiveEffect> ActiveEffects => activeEffects;
    public event System.Action EffectsChanged;

    private const float HitFlashDuration = 0.08f;
    private const float DeathBodyDuration = 0.5f;
    private static readonly Color HitColor = new Color(1f, 0.35f, 0.35f, 1f);

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        body = GetComponent<Rigidbody2D>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.constraints |= RigidbodyConstraints2D.FreezeRotation;
        body.bodyType = RigidbodyType2D.Kinematic;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (attackIndicator == null)
            attackIndicator = GetComponentInChildren<AttackIndicator>(true);
        if (attackIndicator != null)
            attackIndicator.Hide();

        ApplyEnemyVisuals();

        // 2D settings
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = speed;
        agent.stoppingDistance = attackRange;
    }

    protected virtual IEnemyAttackModule CreateAttackModule()
    {
        return AttackType == EnemyAttackType.Ranged
            ? new EnemyRangedAttackModule(this, agent, attackIndicator, attackRange, attackInterval, attackDuration,
                FaceDirection, enemyData != null ? enemyData.rangedBullet : null, enemyData != null ? enemyData.obstacleLayers : (LayerMask)0)
            : new EnemyMeleeAttackModule(this, agent, attackIndicator, attackRange, attackInterval, attackDuration, FaceDirection, Attack);
    }

    protected virtual void OnEnable()
    {
        if (isDying)
        {
            for (int i = 0; i < deathColliders.Length; i++)
            {
                if (deathColliders[i] != null)
                    deathColliders[i].enabled = deathColliderStates[i];
            }
            agent.enabled = agentEnabledBeforeDeath;
            if (deathBody != null)
            {
                deathBody.linearVelocity = Vector2.zero;
                deathBody.angularVelocity = 0f;
                deathBody.simulated = bodySimulatedBeforeDeath;
            }
        }
        isDying = false;
        // Re-read data on pool reuse, including changes to attack type or timing.
        attackModule?.Cancel();
        ApplyEnemyVisuals();
        agent.speed = speed;
        agent.stoppingDistance = attackRange;
        attackModule = CreateAttackModule();
        attackModule?.Reset();
        deathVelocity = Vector2.zero;

        // Reset health every time the enemy is spawned/reactivated
        currentHealth = maxHealth;
        EnemyFeedbackPool.Register(this);
        recentDamageSourceIds.Clear();
        recentDamageSourceTimes.Clear();
        ClearEffects();
        ClearKnockback();
        lethalKnockbackVelocity = null;

        // Auto-find Player if not assigned in Inspector
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
    }

    private void ApplyEnemyVisuals()
    {
        if (enemyData == null)
            return;

        if (spriteRenderer != null && enemyData.sprite != null)
            spriteRenderer.sprite = enemyData.sprite;
        spriteFacesRightByDefault = enemyData.spriteFacesRightByDefault;
    }

    protected virtual void Update()
    {
        if (isDying)
            return;

        if (EffectRuntime.Tick(activeEffects, Time.deltaTime, amount => TakeDamage(
            amount, Vector2.zero, 0f, 0, hitCooldownDuration, null, EnemyDamageKind.Effect), ref effectTickElapsed))
            EffectsChanged?.Invoke();
        if (currentHealth <= 0f || !isActiveAndEnabled)
            return;

        if (DespawnWhenFarAway && player != null && Vector2.Distance(transform.position, player.position) > Mathf.Max(0f, maxDistance))
        {
            Despawn();
            return;
        }

        if (isKnockedBack)
        {
            if (agent.enabled && agent.isOnNavMesh)
                agent.nextPosition = transform.position;
            CleanupExpiredDamageSources();
            return;
        }

        if (!agent.isOnNavMesh)
            return;

        agent.speed = MovementSpeed * EffectRuntime.GetMovementMultiplier(activeEffects);
        if (attackModule != null && attackModule.IsAttacking)
        {
            attackModule.Tick(player);
            CleanupExpiredDamageSources();
            return;
        }
        UpdateMovement();
        CleanupExpiredDamageSources();
        UpdateSpriteFacing();
    }

    protected virtual void UpdateMovement()
    {
        if (player == null) return;
        float distance = Vector2.Distance(transform.position, player.position);
        bool clearShot = attackModule == null || attackModule.HasLineOfSight(player);
        // A blocked ranged enemy must keep approaching instead of stopping at its firing distance.
        agent.stoppingDistance = AttackType == EnemyAttackType.Ranged && !clearShot ? 0f : attackRange;
        if (distance <= attackRange && clearShot)
        {
            if (AttackType == EnemyAttackType.Ranged)
            {
                agent.SetDestination(transform.position);
                agent.velocity = Vector3.zero;
            }
            TryAttackPlayer();
        }
        else agent.SetDestination(player.position);
    }

    protected void TryAttackPlayer()
    {
        if (!isKnockedBack) attackModule?.TryBegin(player);
    }

    private void CancelAttack() => attackModule?.Cancel();

    private void ApplyKnockback(Vector2 direction, float force)
    {
        if (!CanReceiveKnockback || isDying || force <= 0f || direction.sqrMagnitude <= 0f)
            return;
        float effectiveForce = force * (1f - Mathf.Clamp01(knockbackResistance));
        if (effectiveForce <= 0f)
            return;

        CancelAttack();
        OnKnockbackStarting();
        if (!isKnockedBack)
        {
            agentUpdatePositionBeforeKnockback = agent.updatePosition;
            agentStoppedBeforeKnockback = agent.enabled && agent.isOnNavMesh && agent.isStopped;
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            agent.updatePosition = false;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.linearVelocity = Vector2.zero;
            isKnockedBack = true;
        }
        body.AddForce(direction.normalized * effectiveForce, ForceMode2D.Impulse);
        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);
        knockbackRoutine = StartCoroutine(ResetKnockback());
    }

    private IEnumerator ResetKnockback()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, knockbackDuration));
        knockbackRoutine = null;
        ClearKnockback();
    }

    protected void ClearKnockback()
    {
        if (knockbackRoutine != null)
        {
            StopCoroutine(knockbackRoutine);
            knockbackRoutine = null;
        }
        if (!isKnockedBack)
            return;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
        if (agent.enabled && agent.isOnNavMesh)
        {
            // nextPosition remains constrained to the NavMesh while physics owns movement.
            agent.nextPosition = transform.position;
            if (agentUpdatePositionBeforeKnockback)
            {
                transform.position = agent.nextPosition;
                body.position = transform.position;
            }
            agent.velocity = Vector3.zero;
            agent.isStopped = agentStoppedBeforeKnockback;
        }
        agent.updatePosition = agentUpdatePositionBeforeKnockback;
        isKnockedBack = false;
    }

    protected void StopHitFlash()
    {
        if (hitFlashRoutine == null)
            return;
        StopCoroutine(hitFlashRoutine);
        hitFlashRoutine = null;
        if (spriteRenderer != null)
            spriteRenderer.color = originalSpriteColor;
    }

    protected virtual void UpdateSpriteFacing()
    {
        FaceDirection(agent.velocity);
    }

    protected void FaceDirection(Vector2 direction)
    {
        if (spriteRenderer == null || Mathf.Abs(direction.x) <= 0.01f)
            return;

        bool movingLeft = direction.x < 0f;
        spriteRenderer.flipX = movingLeft == spriteFacesRightByDefault;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PushAwayFromPlayer(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        PushAwayFromPlayer(collision);
    }

    private void PushAwayFromPlayer(Collision2D collision)
    {
        if (!CanReceiveKnockback || currentHealth <= 0f || playerContactPushForce <= 0f || isKnockedBack)
            return;

        Player contactedPlayer = collision.collider.GetComponentInParent<Player>();
        if (contactedPlayer == null)
            return;

        Vector2 direction = (Vector2)transform.position - (Vector2)contactedPlayer.transform.position;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
            direction = collision.contactCount > 0 ? collision.GetContact(0).normal : Vector2.right;

        ApplyKnockback(direction, playerContactPushForce);
    }

    private void Attack()
    {
        if (player == null)
            return;

        Player playerComponent = player.GetComponent<Player>();
        if (playerComponent != null)
        {
            if (playerComponent.TakeDamage(AttackDamage) && enemyData != null)
                playerComponent.ApplyEffects(enemyData.effects, RestoreEffectResource);
            Vector2 knockbackDirection = (Vector2)player.position - (Vector2)transform.position;
            playerComponent.ApplyKnockback(knockbackDirection, AttackKnockback);
        }

    }

    public void TakeBulletHit(Bullet2D bullet)
    {
        if (bullet != null && TakeDamage(
            bullet.Damage,
            bullet.TravelDirection,
            bullet.KnockbackForce,
            bullet.DamageSourceId,
            hitCooldownDuration,
            bullet.AttackAttributes,
            EnemyDamageKind.Projectile))
            ApplyEffects(bullet.Effects, bullet.EffectProbabilityMultiplier,
                bullet.Attacker != null ? bullet.Attacker.RestoreEffectResource : null);
    }

    public bool TakeDamage(float damageAmount)
    {
        return TakeDamage(damageAmount, Vector2.zero, 0f, 0);
    }

    public bool TakeDamage(float damageAmount, Vector2 knockbackDirection, float knockbackForce)
    {
        return TakeDamage(damageAmount, knockbackDirection, knockbackForce, 0);
    }

    private bool TakeDamage(float damageAmount, Vector2 knockbackDirection, float knockbackForce, int damageSourceId)
    {
        return TakeDamage(damageAmount, knockbackDirection, knockbackForce, damageSourceId, hitCooldownDuration, null);
    }

    public virtual bool TakeDamage(float damageAmount, Vector2 knockbackDirection, float knockbackForce, int damageSourceId, float damageSourceDuration, int[] attackAttributes = null, EnemyDamageKind damageKind = EnemyDamageKind.Other)
    {
        if (damageAmount <= 0f || currentHealth <= 0f)
            return false;

        if (damageSourceId != 0 && !CanTakeDamageFromSource(damageSourceId, damageSourceDuration))
            return false;

        damageAmount *= GetAttackDamageMultiplier(attackAttributes);
        if (damageAmount <= 0f)
            return false;

        CancelAttack();
        // Record the lethal hit's knockback before Die captures the corpse's momentum.
        if (CanReceiveKnockback && knockbackForce > 0f && knockbackDirection.sqrMagnitude > 0f)
        {
            if (damageAmount >= currentHealth)
            {
                Vector2 impulse = knockbackDirection.normalized * knockbackForce * (1f - Mathf.Clamp01(knockbackResistance));
                if (impulse.sqrMagnitude > 0f)
                    lethalKnockbackVelocity = (isKnockedBack ? body.linearVelocity : Vector2.zero) + impulse / body.mass;
            }
            ApplyKnockback(knockbackDirection, knockbackForce);
        }

        float healthLost = Mathf.Min(currentHealth, damageAmount);
        currentHealth -= damageAmount;
        EnemyFeedbackPool.Damaged(this, healthLost);

        if (currentHealth <= 0f)
        {
            Die();
            return true;
        }

        if (damageKind != EnemyDamageKind.Effect && CanFlashOnHit && spriteRenderer != null)
        {
            if (hitFlashRoutine == null)
            {
                originalSpriteColor = spriteRenderer.color;
            }
            else
                StopCoroutine(hitFlashRoutine);

            hitFlashRoutine = StartCoroutine(FlashRedRoutine());
        }

        return true;
    }

    public void ApplyEffects(EffectDefinition[] effects, float probabilityMultiplier = 1f, System.Action<int, int> restoreAttacker = null)
    {
        if (EffectRuntime.Apply(activeEffects, effects, probabilityMultiplier, enemyData != null ? enemyData.resistances : null,
            restoreAttacker, !isDying))
            EffectsChanged?.Invoke();
    }

    public void RestoreEffectResource(int effectID, int amount)
    {
        if (!isDying && effectID == EffectRuntime.HPStealing && amount > 0)
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        // Enemies currently have no MP resource to restore.
    }

    public bool RemoveEffect(int effectID)
    {
        bool removed = EffectRuntime.Remove(activeEffects, effectID);
        if (activeEffects.Count == 0)
            effectTickElapsed = 0f;
        if (removed)
            EffectsChanged?.Invoke();
        return removed;
    }

    public void ClearEffects()
    {
        effectTickElapsed = 0f;
        if (EffectRuntime.Clear(activeEffects))
            EffectsChanged?.Invoke();
    }

    private float GetAttackDamageMultiplier(int[] attackAttributes)
    {
        if (enemyData == null || attackAttributes == null || attackAttributes.Length == 0)
            return 1f;

        bool resisted = HasMatchingAttribute(attackAttributes, enemyData.resistances);
        bool weakened = HasMatchingAttribute(attackAttributes, enemyData.weaknesses);
        float multiplier = 1f;
        if (resisted)
            multiplier *= 0.25f;
        if (weakened)
            multiplier *= 2.5f;
        return multiplier;
    }

    private static bool HasMatchingAttribute(int[] attackAttributes, int[] targetAttributes)
    {
        if (targetAttributes == null)
            return false;

        for (int i = 0; i < attackAttributes.Length; i++)
        {
            for (int j = 0; j < targetAttributes.Length; j++)
            {
                if (attackAttributes[i] == targetAttributes[j])
                    return true;
            }
        }

        return false;
    }

    protected bool CanTakeDamageFromSource(int sourceId, float sourceDuration)
    {
        if (sourceId == 0)
            return true;

        if (recentDamageSourceIds.Contains(sourceId))
            return false;

        recentDamageSourceIds.Add(sourceId);
        recentDamageSourceTimes[sourceId] = Time.time + Mathf.Max(hitCooldownDuration, sourceDuration);
        return true;
    }

    private void CleanupExpiredDamageSources()
    {
        if (recentDamageSourceTimes.Count == 0)
            return;

        float currentTime = Time.time;
        List<int> expiredSourceIds = new List<int>();

        foreach (KeyValuePair<int, float> source in recentDamageSourceTimes)
        {
            if (currentTime >= source.Value)
            {
                expiredSourceIds.Add(source.Key);
            }
        }

        for (int i = 0; i < expiredSourceIds.Count; i++)
        {
            recentDamageSourceIds.Remove(expiredSourceIds[i]);
            recentDamageSourceTimes.Remove(expiredSourceIds[i]);
        }
    }

    private IEnumerator FlashRedRoutine()
    {
        if (spriteRenderer == null)
            yield break;

        spriteRenderer.color = HitColor;
        yield return new WaitForSeconds(HitFlashDuration);
        spriteRenderer.color = originalSpriteColor;
        hitFlashRoutine = null;
    }

    public virtual void Despawn()
    {
        CancelAttack();
        ClearKnockback();
        StopHitFlash();
        ClearEffects();
        recentDamageSourceIds.Clear();
        recentDamageSourceTimes.Clear();
        lethalKnockbackVelocity = null;
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }
        // OnDisable also cancels a pending corpse coroutine and restores its tint.
        gameObject.SetActive(false);
    }

    protected virtual void Die()
    {
        if (isDying)
            return;
        isDying = true;
        PerformDeathEffect();
        ClearEffects();
        Player playerComponent = player != null ? player.GetComponentInParent<Player>() : null;
        int money = enemyData != null ? enemyData.money : 1;
        playerComponent?.addMoney(money);
        if (playerComponent != null && playerComponent.Data != null) playerComponent.Data.AddKill();
        EnemyFeedbackPool.Died(this, money);
        CancelAttack();
        currentHealth = 0f;
        StopHitFlash();
        if (spriteRenderer != null)
        {
            originalSpriteColor = spriteRenderer.color;
            spriteRenderer.color = HitColor;
        }
        deathBody = GetComponent<Rigidbody2D>();
        deathVelocity = lethalKnockbackVelocity ?? (isKnockedBack ? body.linearVelocity
            : agent.enabled && agent.isOnNavMesh ? (Vector2)agent.velocity : body.linearVelocity);
        ClearKnockback();

        // Stop AI and collisions; the death coroutine preserves the captured movement.
        agentEnabledBeforeDeath = agent.enabled;
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
        agent.enabled = false;

        deathColliders = GetComponentsInChildren<Collider2D>(true);
        deathColliderStates = new bool[deathColliders.Length];
        for (int i = 0; i < deathColliders.Length; i++)
        {
            deathColliderStates[i] = deathColliders[i].enabled;
            deathColliders[i].enabled = false;
        }
        if (deathBody != null)
        {
            bodySimulatedBeforeDeath = deathBody.simulated;
            deathBody.simulated = false;
        }
        deathRoutine = StartCoroutine(DeactivateAfterDeath());
    }

    private void PerformDeathEffect()
    {
        if (enemyData == null || enemyData.DieEffectID != 1 || enemyData.deathEffectBullet == null ||
            BulletPool.Instance == null || !BulletPool.Instance.CanFireEnemyBullet) return;

        BulletPool.Instance.ScheduleDeathBurst(enemyData.deathEffectBullet, transform.position, this,
            Mathf.Max(0f, enemyData.deathEffectDelay));
    }

    private IEnumerator DeactivateAfterDeath()
    {
        float elapsed = 0f;
        while (elapsed < DeathBodyDuration)
        {
            yield return null;
            float deltaTime = Mathf.Min(Time.deltaTime, DeathBodyDuration - elapsed);
            transform.position += (Vector3)(deathVelocity * deltaTime);
            elapsed += deltaTime;
        }
        deathRoutine = null;
        if (spriteRenderer != null)
            spriteRenderer.color = originalSpriteColor;
        gameObject.SetActive(false);
    }

    protected virtual void OnDisable()
    {
        EnemyFeedbackPool.Unregister(this);
        CancelAttack();
        ClearKnockback();
        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }
        StopHitFlash();
        // Also restore the tint if despawning interrupts the dying period.
        if (isDying && spriteRenderer != null)
            spriteRenderer.color = originalSpriteColor;
    }
}
