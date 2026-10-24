using UnityEngine;
using System.Threading;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet2D : MonoBehaviour
{
    private static int nextDamageSourceId;

    [SerializeField] private string obstacleTag = "BulletBlocker";

    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    private Collider2D bulletCollider;
    private Camera mainCamera;
    protected BulletSO bulletData;
    private float remainingLifetime;
    private float currentDamage;
    private float effectProbabilityMultiplier = 1f;
    private int damageSourceId;
    private bool hasImpacted;
    private bool hasLaunched;
    private int remainingPierces;
    private Vector2? explosionTarget;
    private bool componentsInitialized;
    private readonly List<RaycastHit2D> castHits = new List<RaycastHit2D>();
    private readonly HashSet<EnemyChase> hitEnemies = new HashSet<EnemyChase>();

    protected virtual bool UsesTargetExplosion => bulletData != null && bulletData.bulletMode == 1 && bulletData.isExplosive;
    protected virtual bool ImpactsColliders => true;

    public float Damage => currentDamage;
    public float KnockbackForce => bulletData != null ? bulletData.knockbackForce : 0f;
    public EffectDefinition[] Effects => bulletData != null ? bulletData.effects : null;
    public int[] AttackAttributes => bulletData != null ? bulletData.attackAttributes : null;
    public float EffectProbabilityMultiplier => effectProbabilityMultiplier;
    public int DamageSourceId => damageSourceId;
    public Vector2 TravelDirection => rb != null && rb.linearVelocity.sqrMagnitude > 0f
        ? rb.linearVelocity.normalized
        : transform.right;

    protected virtual void Awake()
    {
        EnsureComponents();
    }

    protected virtual void EnsureComponents()
    {
        if (componentsInitialized)
            return;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bulletCollider = GetComponent<Collider2D>();
        mainCamera = Camera.main;
        componentsInitialized = true;
    }

    public void Initialize(BulletSO data)
    {
        EnsureComponents();
        bulletData = data;
        currentDamage = bulletData != null ? bulletData.damage : 0f;
        effectProbabilityMultiplier = 1f;

        if (bulletData == null) return;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = bulletData.bulletSprite;
            spriteRenderer.color = bulletData.tint;
        }

        BoxCollider2D boxCollider = bulletCollider as BoxCollider2D;
        if (boxCollider != null)
        {
            boxCollider.size = bulletData.colliderSize;
        }
    }

    public void Launch(Vector2 direction)
    {
        Launch(direction, bulletData != null ? bulletData.damage : 0f);
    }

    public void Launch(Vector2 direction, float damage)
    {
        Launch(direction, damage, bulletData != null ? bulletData.speed : 0f, 1f);
    }

    public virtual void Launch(Vector2 direction, float damage, float speed, float effectProbabilityMultiplier, Vector2? targetPosition = null)
    {
        EnsureComponents();
        if (bulletData == null) return;

        currentDamage = damage;
        this.effectProbabilityMultiplier = Mathf.Max(0f, effectProbabilityMultiplier);
        damageSourceId = Interlocked.Increment(ref nextDamageSourceId);
        hasImpacted = false;
        hasLaunched = true;
        hitEnemies.Clear();
        remainingPierces = Mathf.Max(0, bulletData.isPiercing);
        explosionTarget = UsesTargetExplosion ? targetPosition : null;

        // Re-enable visual, collider, and GameObject state when re-firing
        gameObject.SetActive(true);
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (bulletCollider != null) bulletCollider.enabled = true;
        remainingLifetime = bulletData.lifetime;

        // Rotate bullet sprite to align with direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        // Synchronize pooled spawn transforms before the first physics sweep.
        rb.position = transform.position;
        rb.rotation = angle;

        // Apply velocity
        rb.linearVelocity = direction.normalized * Mathf.Max(0f, speed);
    }

    private void Update()
    {
        remainingLifetime -= Time.deltaTime;
        if (remainingLifetime <= 0f)
        {
            Deactivate();
            return;
        }

        // Check if bullet flew off-screen
        if (mainCamera != null)
        {
            Vector3 screenPoint = mainCamera.WorldToViewportPoint(transform.position);
            bool isOffScreen = screenPoint.x < 0 || screenPoint.x > 1 || screenPoint.y < 0 || screenPoint.y > 1;

            if (isOffScreen)
            {
                Deactivate();
            }
        }
    }

    private void FixedUpdate()
    {
        if (hasImpacted || !gameObject.activeInHierarchy)
            return;

        if (explosionTarget.HasValue && Vector2.Distance(rb.position, explosionTarget.Value) <= 0.001f)
        {
            DetonateAtTarget();
            return;
        }

        Vector2 velocity = rb.linearVelocity;
        float distance = velocity.magnitude * Time.fixedDeltaTime;
        if (distance <= 0f)
            return;

        Vector2 direction = velocity.normalized;
        bool reachesTarget = false;
        if (explosionTarget.HasValue)
        {
            float targetDistance = Mathf.Max(0f, Vector2.Dot(explosionTarget.Value - rb.position, direction));
            reachesTarget = targetDistance <= distance;
            distance = Mathf.Min(distance, targetDistance);
        }
        if (!ImpactsColliders)
        {
            if (reachesTarget)
                DetonateAtTarget();
            return;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        filter.useTriggers = true;
        castHits.Clear();
        rb.Cast(direction, filter, castHits, distance);

        Vector2 origin = rb.position;
        // Process the full sweep in travel order; blockers win ties with enemy hits.
        castHits.Sort((a, b) =>
        {
            int comparison = a.distance.CompareTo(b.distance);
            if (comparison != 0)
                return comparison;
            bool aBlocks = a.collider != null && a.collider.CompareTag(obstacleTag);
            bool bBlocks = b.collider != null && b.collider.CompareTag(obstacleTag);
            return bBlocks.CompareTo(aBlocks);
        });

        foreach (RaycastHit2D hit in castHits)
        {
            Collider2D other = hit.collider;
            if (other == null || !other.enabled || !other.gameObject.activeInHierarchy || other.attachedRigidbody == rb)
                continue;

            bool isObstacle = other.CompareTag(obstacleTag);
            EnemyChase enemy = other.GetComponentInParent<EnemyChase>();
            if (!isObstacle && !other.CompareTag("Enemy") && enemy == null)
                continue;

            Vector2 impactPosition = origin + direction * hit.distance;
            if (!isObstacle && enemy != null && hitEnemies.Contains(enemy))
                continue;
            if (!isObstacle && enemy != null && remainingPierces > 0)
            {
                if (enemy.isActiveAndEnabled)
                {
                    ApplyDirectHit(enemy);
                    remainingPierces--;
                    if (bulletData.isExplosive)
                        Explode(impactPosition);
                }
                continue;
            }

            rb.position = impactPosition;
            transform.position = new Vector3(impactPosition.x, impactPosition.y, transform.position.z);
            HandleImpact(other);
            return;
        }

        if (reachesTarget)
            DetonateAtTarget();
    }

    private void DetonateAtTarget()
    {
        Vector2 position = explosionTarget.Value;
        rb.position = position;
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        hasImpacted = true;
        Explode(position);
        Deactivate();
    }

    private void HandleImpact(Collider2D other)
    {
        EnemyChase enemy = other.GetComponentInParent<EnemyChase>();
        if (enemy != null)
            ApplyDirectHit(enemy);

        hasImpacted = true;
        if (bulletData != null && bulletData.isExplosive)
            Explode(transform.position);

        Deactivate();
    }

    public void ApplyDirectHit(EnemyChase enemy)
    {
        if (!hasImpacted && gameObject.activeInHierarchy && enemy != null && enemy.isActiveAndEnabled && hitEnemies.Add(enemy))
            enemy.TakeBulletHit(this);
    }

    private void Explode(Vector2 origin)
    {
        Vector2 fallbackDirection = TravelDirection;
        float radius = Mathf.Max(0f, bulletData.explosionRadius);
        DrawExplosionRange(origin, radius);
        // Direct impact and blast must both be accepted by the directly hit enemy.
        int explosionSourceId = Interlocked.Increment(ref nextDamageSourceId);
        HashSet<EnemyChase> damagedEnemies = new HashSet<EnemyChase>();
        Collider2D[] colliders = Physics2D.OverlapCircleAll(origin, radius);

        foreach (Collider2D collider in colliders)
        {
            EnemyChase enemy = collider.GetComponentInParent<EnemyChase>();
            if (enemy == null || !enemy.isActiveAndEnabled || !damagedEnemies.Add(enemy))
                continue;

            Vector2 direction = (Vector2)enemy.transform.position - origin;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
                direction = fallbackDirection;

            if (enemy.TakeDamage(
                bulletData.explosiveDamage,
                direction,
                KnockbackForce,
                explosionSourceId,
                Time.fixedDeltaTime,
                AttackAttributes,
                EnemyDamageKind.Projectile))
                enemy.ApplyEffects(Effects, EffectProbabilityMultiplier);
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

    public virtual void Deactivate()
    {
        EnsureComponents();
        hasImpacted = true;
        // Pool prewarming also calls Deactivate; only fired bullets should emit particles.
        bool playParticles = hasLaunched;
        hasLaunched = false;
        if (playParticles && bulletData != null && bulletData.ParticleID > 0)
            ParticlePool.Instance?.Play(bulletData.ParticleID.ToString(System.Globalization.CultureInfo.InvariantCulture), transform.position);
        // Stop movement, make invisible, and set inactive
        rb.linearVelocity = Vector2.zero;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (bulletCollider != null) bulletCollider.enabled = false;
        
        gameObject.SetActive(false);
    }
}
