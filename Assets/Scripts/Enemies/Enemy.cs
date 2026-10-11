using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyChase : MonoBehaviour
{
    [Header("Target Setup")]
    [SerializeField] private Transform player;

    [Header("Enemy Data")]
    [SerializeField] private EnemySO enemyData;

    [Header("Combat Stats")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float knockbackForce = 4f;
    [SerializeField] private float knockbackResistance = 0f;
    [SerializeField] private float speed = 2.5f;
    [SerializeField] private float attackInterval = 1.2f;
    [SerializeField] private float maxHealth = 35f;
    [SerializeField] private float hitCooldownDuration = 0.2f;

    private NavMeshAgent agent;
    private SpriteRenderer spriteRenderer;
    private bool spriteFacesRightByDefault = true;
    private Color originalSpriteColor;
    private float currentHealth;
    private Vector2 knockbackVelocity;
    private float knockbackRemaining;
    private float lastAttackTime;
    private Coroutine hitFlashRoutine;
    private readonly HashSet<int> recentDamageSourceIds = new HashSet<int>();
    private readonly Dictionary<int, float> recentDamageSourceTimes = new Dictionary<int, float>();
    private readonly List<ActiveEffect> activeEffects = new List<ActiveEffect>();

    private const float KnockbackDuration = 0.12f;
    private const float HitFlashDuration = 0.08f;
    private static readonly Color HitColor = new Color(1f, 0.35f, 0.35f, 1f);

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        ApplyEnemyData();

        // 2D settings
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = speed;
        agent.stoppingDistance = attackRange;
    }

    private void OnEnable()
    {
        // Reset health every time the enemy is spawned/reactivated
        currentHealth = maxHealth;
        recentDamageSourceIds.Clear();
        recentDamageSourceTimes.Clear();
        activeEffects.Clear();

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
        maxHealth = enemyData.HP;
    }

    private void Update()
    {
        EffectRuntime.Tick(activeEffects, Time.deltaTime, amount => TakeDamage(amount));
        if (currentHealth <= 0f || !isActiveAndEnabled)
            return;

        if (!agent.isOnNavMesh)
            return;

        agent.speed = EffectRuntime.HasEffect(activeEffects, EffectRuntime.Faint) ? 0f : speed;
        

        if (player != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            if (distanceToPlayer <= attackRange)
            {
                if (Time.time >= lastAttackTime + attackInterval)
                {
                    Attack();
                }
            }
            else
            {
                agent.SetDestination(player.position);
            }
        }

        if (knockbackRemaining > 0f)
        {
            agent.velocity = knockbackVelocity;
            knockbackRemaining -= Time.deltaTime;
        }

        CleanupExpiredDamageSources();
        UpdateSpriteFacing();
    }

    private void UpdateSpriteFacing()
    {
        if (spriteRenderer == null || Mathf.Abs(agent.velocity.x) <= 0.01f)
            return;

        bool movingLeft = agent.velocity.x < 0f;
        spriteRenderer.flipX = movingLeft == spriteFacesRightByDefault;
    }

    private void Attack()
    {
        if (player == null)
            return;

        Player playerComponent = player.GetComponent<Player>();
        if (playerComponent != null)
        {
            if (playerComponent.TakeDamage(damage) && enemyData != null)
                playerComponent.ApplyEffects(enemyData.effects);
            Vector2 knockbackDirection = (Vector2)player.position - (Vector2)transform.position;
            playerComponent.ApplyKnockback(knockbackDirection, knockbackForce);
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
            bullet.AttackAttributes))
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

    public bool TakeDamage(float damageAmount, Vector2 knockbackDirection, float knockbackForce, int damageSourceId, float damageSourceDuration, int[] attackAttributes = null)
    {
        if (damageAmount <= 0f || currentHealth <= 0f)
            return false;

        if (damageSourceId != 0 && !CanTakeDamageFromSource(damageSourceId, damageSourceDuration))
            return false;

        damageAmount *= GetAttackDamageMultiplier(attackAttributes);
        if (damageAmount <= 0f)
            return false;

        currentHealth -= damageAmount;

        if (currentHealth <= 0f)
        {
            Die();
            return true;
        }

        if (spriteRenderer != null)
        {
            if (hitFlashRoutine == null)
            {
                originalSpriteColor = spriteRenderer.color;
            }
            else
                StopCoroutine(hitFlashRoutine);

            hitFlashRoutine = StartCoroutine(FlashRedRoutine());
        }

        if (knockbackForce > 0f && knockbackDirection.sqrMagnitude > 0f)
        {
            knockbackVelocity = knockbackDirection.normalized * (knockbackForce * (1f - Mathf.Clamp01(knockbackResistance)));
            knockbackRemaining = KnockbackDuration;
        }

        return true;
    }

    public void ApplyEffects(EffectDefinition[] effects, float probabilityMultiplier = 1f)
    {
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
            multiplier *= 0.3f;
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

    private bool CanTakeDamageFromSource(int sourceId, float sourceDuration)
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

    private void Die()
    {
        // Stop movement before deactivating
        if (agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        // Deactivate instead of Destroying to support object pooling
        gameObject.SetActive(false);
    }
}
