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
    private float currentHealth;
    private Vector2 knockbackVelocity;
    private float knockbackRemaining;
    private float lastAttackTime;
    private Coroutine hitFlashRoutine;
    private readonly HashSet<int> recentDamageSourceIds = new HashSet<int>();
    private readonly Dictionary<int, float> recentDamageSourceTimes = new Dictionary<int, float>();

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
        if (!agent.isOnNavMesh)
            return;

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
    }

    private void Attack()
    {
        if (player == null)
            return;

        Player playerComponent = player.GetComponent<Player>();
        if (playerComponent != null)
        {
            playerComponent.TakeDamage(damage);
        }

        lastAttackTime = Time.time;
        Debug.Log($"Enemy attack! Damage: {damage}, Range: {attackRange}, Player: {player.name}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Bullet2D bullet = other.GetComponent<Bullet2D>();
        if (bullet == null)
            bullet = other.GetComponentInParent<Bullet2D>();

        if (bullet != null)
            TakeDamage(bullet.Damage, bullet.TravelDirection, bullet.KnockbackForce, bullet.GetInstanceID());
    }

    public void TakeDamage(float damageAmount)
    {
        TakeDamage(damageAmount, Vector2.zero, 0f, 0);
    }

    private void TakeDamage(float damageAmount, Vector2 knockbackDirection, float knockbackForce, int damageSourceId)
    {
        if (damageAmount <= 0f || currentHealth <= 0f)
            return;

        if (damageSourceId != 0 && !CanTakeDamageFromSource(damageSourceId))
            return;

        currentHealth -= damageAmount;

        if (currentHealth <= 0f)
        {
            Die();
            return;
        }

        if (spriteRenderer != null)
        {
            if (hitFlashRoutine != null)
            {
                StopCoroutine(hitFlashRoutine);
            }

            hitFlashRoutine = StartCoroutine(FlashRedRoutine());
        }

        if (knockbackForce > 0f && knockbackDirection.sqrMagnitude > 0f)
        {
            knockbackVelocity = knockbackDirection.normalized * (knockbackForce * (1f - Mathf.Clamp01(knockbackResistance)));
            knockbackRemaining = KnockbackDuration;
        }
    }

    private bool CanTakeDamageFromSource(int sourceId)
    {
        if (sourceId == 0)
            return true;

        if (recentDamageSourceIds.Contains(sourceId))
            return false;

        recentDamageSourceIds.Add(sourceId);
        recentDamageSourceTimes[sourceId] = Time.time;
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
            if (currentTime - source.Value >= hitCooldownDuration)
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

        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = HitColor;
        yield return new WaitForSeconds(HitFlashDuration);
        spriteRenderer.color = originalColor;
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