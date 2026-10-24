using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyDamageKind { Other, Projectile, Melee }

[RequireComponent(typeof(NavMeshAgent), typeof(Rigidbody2D))]
public class EnemyChase : MonoBehaviour
{
    [Header("Target Setup")]
    [SerializeField] private Transform player;

    [Header("Enemy Data")]
    [SerializeField] private EnemySO enemyData;

    [Header("Spawn Settings")]
    [SerializeField, Min(0.01f)] private float enemy_point = 1f;
    [SerializeField, Min(0f)] private float enemy_difficulty = 1f;
    [SerializeField, Min(0f)] private float maxDistance = 50f;

    protected virtual bool DespawnWhenFarAway => true;
    protected NavMeshAgent Agent => agent;
    protected Transform PlayerTarget => player;
    protected SpriteRenderer EnemySprite => spriteRenderer;
    protected virtual float MovementSpeed => speed;
    protected virtual float AttackDamage => damage;
    protected virtual float AttackKnockback => knockbackForce;
    protected virtual bool CanReceiveKnockback => true;
    protected virtual bool CanFlashOnHit => true;

    public float EnemyPoint => enemyData != null ? enemyData.enemy_point : enemy_point;
    public float EnemyDifficulty => enemyData != null ? enemyData.enemy_difficulty : enemy_difficulty;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    [Header("Combat Stats")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float knockbackForce = 4f;
    [SerializeField] private float knockbackResistance = 0f;
    [SerializeField] private float speed = 2.5f;
    [SerializeField] private float attackInterval = 1.2f;
    [SerializeField, Min(0f)] private float attackDuration = 0.4f;
    [SerializeField] private AttackIndicator attackIndicator;
    [SerializeField] private float maxHealth = 35f;
    [SerializeField] private float hitCooldownDuration = 0.2f;
    [SerializeField, Min(0f)] private float playerContactPushForce = 1.5f;

    private NavMeshAgent agent;
    private SpriteRenderer spriteRenderer;
    private bool spriteFacesRightByDefault = true;
    private Color originalSpriteColor;
    private float currentHealth;
    [Header("Knockback")]
    [SerializeField, Min(0f)] private float knockbackDuration = 0.12f;
    private Rigidbody2D body;
    private Coroutine knockbackRoutine;
    private bool isKnockedBack;
    private bool agentUpdatePositionBeforeKnockback;
    private bool agentStoppedBeforeKnockback;
    private Vector2? lethalKnockbackVelocity;
    private float lastAttackTime;
    private bool isAttacking;
    private float attackElapsed;
    private bool agentStoppedBeforeAttack;
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

        ApplyEnemyData();

        // 2D settings
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = speed;
        agent.stoppingDistance = attackRange;
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
        lastAttackTime = Time.time - Mathf.Max(0f, attackInterval);
        deathVelocity = Vector2.zero;

        // Reset health every time the enemy is spawned/reactivated
        currentHealth = maxHealth;
        recentDamageSourceIds.Clear();
        recentDamageSourceTimes.Clear();
        activeEffects.Clear();
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

    private void ApplyEnemyData()
    {
        if (enemyData == null)
            return;

        if (spriteRenderer != null && enemyData.sprite != null)
            spriteRenderer.sprite = enemyData.sprite;
        spriteFacesRightByDefault = enemyData.spriteFacesRightByDefault;
        attackRange = enemyData.attackRange;
        damage = enemyData.damage;
        knockbackForce = enemyData.knockbackForce;
        knockbackResistance = enemyData.knockbackResistance;
        speed = enemyData.speed;
        attackInterval = enemyData.attackInterval;
        attackDuration = enemyData.attackDuration;
        maxHealth = enemyData.HP;
        enemy_point = enemyData.enemy_point;
        enemy_difficulty = enemyData.enemy_difficulty;
    }

    protected virtual void Update()
    {
        if (isDying)
            return;

        EffectRuntime.Tick(activeEffects, Time.deltaTime, amount => TakeDamage(amount));
        if (currentHealth <= 0f || !isActiveAndEnabled)
            return;

        if (DespawnWhenFarAway && player != null && Vector2.Distance(transform.position, player.position) > Mathf.Max(0f, maxDistance))
        {
            Die();
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

        agent.speed = EffectRuntime.HasEffect(activeEffects, EffectRuntime.Faint) ? 0f : MovementSpeed;
        if (isAttacking)
        {
            UpdateAttack();
            CleanupExpiredDamageSources();
            return;
        }
        UpdateMovement();
        CleanupExpiredDamageSources();
        UpdateSpriteFacing();
    }

    protected virtual void UpdateMovement()
    {
        if (player != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer <= attackRange)
                TryAttackPlayer();
            else
                agent.SetDestination(player.position);
        }
    }

    protected void TryAttackPlayer()
    {
        if (!isAttacking && !isKnockedBack && attackIndicator != null && player != null &&
            Vector2.Distance(transform.position, player.position) <= attackRange &&
            Time.time >= lastAttackTime + attackInterval)
            BeginAttack();
    }

    private void BeginAttack()
    {
        isAttacking = true;
        attackElapsed = 0f;
        agentStoppedBeforeAttack = agent.isStopped;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        Vector2 direction = (Vector2)player.position - (Vector2)transform.position;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
            direction = Vector2.right;
        FaceDirection(direction);
        attackIndicator.Begin(direction);
    }

    private void UpdateAttack()
    {
        agent.velocity = Vector3.zero;
        attackElapsed += Time.deltaTime;
        attackIndicator.SetProgress(attackDuration > 0f ? attackElapsed / attackDuration : 1f);
        if (attackElapsed < Mathf.Max(0f, attackDuration))
            return;

        if (player != null && attackIndicator.Contains(player.position))
            Attack();
        CancelAttack();
    }

    private void CancelAttack()
    {
        if (attackIndicator != null)
            attackIndicator.Hide();
        if (!isAttacking)
            return;
        isAttacking = false;
        lastAttackTime = Time.time;
        if (agent.enabled && agent.isOnNavMesh)
            agent.isStopped = agentStoppedBeforeAttack;
    }

    private void ApplyKnockback(Vector2 direction, float force)
    {
        if (!CanReceiveKnockback || isDying || force <= 0f || direction.sqrMagnitude <= 0f)
            return;
        float effectiveForce = force * (1f - Mathf.Clamp01(knockbackResistance));
        if (effectiveForce <= 0f)
            return;

        CancelAttack();
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
                playerComponent.ApplyEffects(enemyData.effects);
            Vector2 knockbackDirection = (Vector2)player.position - (Vector2)transform.position;
            playerComponent.ApplyKnockback(knockbackDirection, AttackKnockback);
        }

        lastAttackTime = Time.time;
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
            ApplyEffects(bullet.Effects, bullet.EffectProbabilityMultiplier);
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

        currentHealth -= damageAmount;

        if (currentHealth <= 0f)
        {
            Die();
            return true;
        }

        if (CanFlashOnHit && spriteRenderer != null)
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

    public void ApplyEffects(EffectDefinition[] effects, float probabilityMultiplier = 1f)
    {
        if (isDying)
            return;
        EffectRuntime.Apply(activeEffects, effects, probabilityMultiplier, enemyData != null ? enemyData.resistances : null);
    }

    private float GetAttackDamageMultiplier(int[] attackAttributes)
    {
        if (enemyData == null || attackAttributes == null || attackAttributes.Length == 0)
            return 1f;

        bool resisted = HasMatchingAttribute(attackAttributes, enemyData.resistances);
        bool weakened = HasMatchingAttribute(attackAttributes, enemyData.weaknesses);
        float multiplier = 1f;
        if (resisted)
            multiplier *= 0.15f;
        if (weakened)
            multiplier *= 2f;
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

    protected virtual void Die()
    {
        if (isDying)
            return;
        isDying = true;
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
