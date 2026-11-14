using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet2D : MonoBehaviour
{
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
    private readonly HashSet<Collider2D> ricochetContacts = new HashSet<Collider2D>();
    private int remainingRicochetHits;
    private const float RicochetSkin = 0.005f;
    private bool IsRicochet => bulletData != null && bulletData.bulletId != "1" &&
        (bulletData.bulletMode == 2 || bulletData.bulletMode == 3);

    protected virtual bool UsesTargetExplosion => bulletData != null && bulletData.bulletMode == 1 && bulletData.isExplosive;
    protected virtual bool ImpactsColliders => true;
    protected virtual bool UsesConfiguredSpin => true;
    protected virtual bool DespawnOffScreen => !IsRicochet || bulletData.bulletMode != 2;

    public float Damage => currentDamage;
    public float KnockbackForce => bulletData != null ? bulletData.knockbackForce : 0f;
    public EffectDefinition[] Effects => bulletData != null ? bulletData.effects : null;
    public int[] AttackAttributes => bulletData != null ? bulletData.attackAttributes : null;
    public float EffectProbabilityMultiplier => effectProbabilityMultiplier;
    public int DamageSourceId => damageSourceId;
    public Player Attacker { get; set; }
    public virtual Vector2 TravelDirection => IsRicochet && ricochetVelocity.sqrMagnitude > 0f
        ? ricochetVelocity.normalized : rb != null && rb.linearVelocity.sqrMagnitude > 0f
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
        Attacker = null;
        EnsureComponents();
        bulletData = data;
        currentDamage = bulletData != null ? bulletData.damage : 0f;
        effectProbabilityMultiplier = 1f;

        if (bulletData == null) return;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = bulletData.bulletSprite;
            spriteRenderer.color = bulletData.tint;
            if (bulletData.spinningSpeed != 0f && spriteRenderer.transform == transform)
            {
                // Rotate the image independently of casts and ricochet direction.
                SpriteRenderer original = spriteRenderer;
                GameObject visual = new GameObject("Bullet Visual");
                visual.transform.SetParent(transform, false);
                spriteRenderer = visual.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = original.sprite;
                spriteRenderer.color = original.color;
                spriteRenderer.sharedMaterial = original.sharedMaterial;
                spriteRenderer.sortingLayerID = original.sortingLayerID;
                spriteRenderer.sortingOrder = original.sortingOrder;
                spriteRenderer.flipX = original.flipX;
                spriteRenderer.flipY = original.flipY;
                spriteRenderer.maskInteraction = original.maskInteraction;
                original.enabled = false;
            }
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
        BeginDamageSource();
        hasImpacted = false;
        hasLaunched = true;
        remainingPierces = Mathf.Max(0, bulletData.isPiercing);
        remainingRicochetHits = Mathf.Max(1, bulletData.isPiercing);
        ricochetContacts.Clear();
        explosionTarget = UsesTargetExplosion ? targetPosition : null;

        // Re-enable visual, collider, and GameObject state when re-firing
        gameObject.SetActive(true);
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (spriteRenderer != null && spriteRenderer.transform != transform)
            spriteRenderer.transform.localRotation = Quaternion.identity;
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
        ricochetVelocity = rb.linearVelocity;
    }

    private void Update()
    {
        remainingLifetime -= Time.deltaTime;
        if (remainingLifetime <= 0f)
        {
            Deactivate();
            return;
        }

        if (hasLaunched && UsesConfiguredSpin && bulletData != null && bulletData.spinningSpeed != 0f && spriteRenderer != null)
            spriteRenderer.transform.Rotate(0f, 0f, bulletData.spinningSpeed * Time.deltaTime, Space.Self);

        // Check if bullet flew off-screen
        if (DespawnOffScreen && mainCamera != null)
        {
            Vector3 screenPoint = mainCamera.WorldToViewportPoint(transform.position);
            bool isOffScreen = screenPoint.x < 0 || screenPoint.x > 1 || screenPoint.y < 0 || screenPoint.y > 1;

            if (isOffScreen)
            {
                Deactivate();
            }
        }
    }

    protected virtual void FixedUpdate()
    {
        if (hasImpacted || !gameObject.activeInHierarchy)
            return;

        if (IsRicochet)
        {
            UpdateRicochet();
            return;
        }

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

        if (!SweepForImpacts(direction * distance))
            return;
        if (reachesTarget)
            DetonateAtTarget();
    }

    private void UpdateRicochet()
    {
        float speed = ricochetVelocity.magnitude;
        if (speed <= Mathf.Epsilon)
            return;
        Vector2 direction = ricochetVelocity / speed;
        Vector2 position = rb.position;
        float remainingDistance = speed * Time.fixedDeltaTime;
        bool screenBounce = bulletData.bulletMode == 2 && mainCamera != null;
        Vector2 screenMin = Vector2.zero;
        Vector2 screenMax = Vector2.zero;
        if (screenBounce)
        {
            float depth = mainCamera.WorldToViewportPoint(transform.position).z;
            Vector2 a = mainCamera.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
            Vector2 b = mainCamera.ViewportToWorldPoint(new Vector3(1f, 1f, depth));
            Vector2 extents = bulletCollider != null ? (Vector2)bulletCollider.bounds.extents : Vector2.zero;
            screenMin = Vector2.Min(a, b) + extents;
            screenMax = Vector2.Max(a, b) - extents;
            if (screenMin.x >= screenMax.x || screenMin.y >= screenMax.y)
                screenBounce = false;
            else
            {
                // Camera movement can leave a previously valid bullet outside its bounds.
                position = new Vector2(Mathf.Clamp(position.x, screenMin.x, screenMax.x),
                    Mathf.Clamp(position.y, screenMin.y, screenMax.y));
                rb.position = position;
            }
        }

        // Consume the step in segments so a fast bullet can bounce more than once.
        // Bound the loop for overlapping colliders and tight corners.
        for (int bounce = 0; bounce < 16 && remainingDistance > Mathf.Epsilon; bounce++)
        {
            ricochetContacts.RemoveWhere(HasLeftRicochetContact);
            float hitDistance = remainingDistance;
            Vector2 normal = Vector2.zero;
            Collider2D hitCollider = null;
            EnemyChase hitEnemy = null;
            bool hitScreen = false;
            if (screenBounce)
            {
                float xDistance = direction.x > 0f ? (screenMax.x - position.x) / direction.x :
                    direction.x < 0f ? (screenMin.x - position.x) / direction.x : float.PositiveInfinity;
                float yDistance = direction.y > 0f ? (screenMax.y - position.y) / direction.y :
                    direction.y < 0f ? (screenMin.y - position.y) / direction.y : float.PositiveInfinity;
                float edgeDistance = Mathf.Max(0f, Mathf.Min(xDistance, yDistance));
                if (edgeDistance <= hitDistance)
                {
                    hitDistance = edgeDistance;
                    normal = Vector2.zero;
                    if (Mathf.Abs(xDistance - edgeDistance) <= RicochetSkin)
                        normal.x = -Mathf.Sign(direction.x);
                    if (Mathf.Abs(yDistance - edgeDistance) <= RicochetSkin)
                        normal.y = -Mathf.Sign(direction.y);
                    normal.Normalize();
                    hitScreen = true;
                }
            }
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
            filter.useTriggers = true;
            castHits.Clear();
            rb.Cast(direction, filter, castHits, hitDistance);
            foreach (RaycastHit2D hit in castHits)
            {
                Collider2D other = hit.collider;
                if (other == null || !other.enabled || !other.gameObject.activeInHierarchy ||
                    other.attachedRigidbody == rb || ricochetContacts.Contains(other))
                    continue;
                bool obstacle = other.CompareTag(obstacleTag);
                EnemyChase enemy = other.GetComponentInParent<EnemyChase>();
                if (!obstacle && (enemy == null || !enemy.isActiveAndEnabled || enemy.CurrentHealth <= 0f))
                    continue;
                if (hit.distance > hitDistance || (hit.distance == hitDistance && hitCollider != null && !obstacle))
                    continue;
                hitDistance = hit.distance;
                hitCollider = other;
                hitEnemy = obstacle ? null : enemy;
                normal = hit.normal.sqrMagnitude > Mathf.Epsilon ? hit.normal.normalized : -direction;
                hitScreen = false;
            }
            position += direction * hitDistance;
            rb.position = position;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            remainingDistance -= hitDistance;
            if (!hitScreen && hitCollider == null)
                break;

            if (hitCollider != null)
            {
                // Each distinct contact can damage the same enemy again after separation.
                BeginDamageSource();
                ricochetVelocity = direction * speed;
                rb.linearVelocity = direction * speed;
                if (hitEnemy != null)
                {
                    ApplyDirectHit(hitEnemy);
                    foreach (Collider2D collider in hitEnemy.GetComponentsInChildren<Collider2D>())
                        ricochetContacts.Add(collider);
                }
                ricochetContacts.Add(hitCollider);
                if (bulletData.isExplosive)
                    Explode(position);
                if (bulletData.bulletMode == 3 && --remainingRicochetHits <= 0)
                {
                    Deactivate();
                    return;
                }
            }
            if (bulletData.bulletMode == 2)
                direction = hitScreen && normal.x != 0f && normal.y != 0f
                    ? -direction : Vector2.Reflect(direction, normal).normalized;
            else
                direction = (Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-85f, 85f)) * normal).normalized;
            // Start the next sweep just outside the contact surface.
            float separation = Mathf.Min(RicochetSkin, remainingDistance);
            position += normal * separation;
            remainingDistance -= separation;
            rb.position = position;
        }
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rb.rotation = angle;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        // Position was advanced manually using casts; resume its velocity next step.
        ricochetVelocity = direction * speed;
        rb.linearVelocity = Vector2.zero;
    }

    private Vector2 ricochetVelocity;

    private bool HasLeftRicochetContact(Collider2D other)
    {
        if (other == null || !other.enabled || !other.gameObject.activeInHierarchy)
            return true;
        if (bulletCollider == null)
            return false;
        ColliderDistance2D separation = Physics2D.Distance(bulletCollider, other);
        return !separation.isOverlapped && separation.distance > RicochetSkin * 2f;
    }

    // Returns false when an impact deactivates the bullet. Curved projectiles
    // call this for each segment rather than sweeping across the curve's chord.
    protected bool SweepForImpacts(Vector2 displacement)
    {
        if (hasImpacted || !gameObject.activeInHierarchy)
            return false;
        float distance = displacement.magnitude;
        if (!ImpactsColliders || distance <= Mathf.Epsilon)
            return true;
        Vector2 direction = displacement / distance;
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
            return false;
        }

        return true;
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

    protected void BeginDamageSource()
    {
        damageSourceId = DamageSources.Next();
        hitEnemies.Clear();
    }

    public void ApplyDirectHit(EnemyChase enemy)
    {
        if (!hasImpacted && gameObject.activeInHierarchy && enemy != null && enemy.isActiveAndEnabled && hitEnemies.Add(enemy))
            enemy.TakeBulletHit(this);
    }

    private void Explode(Vector2 origin)
    {
        Explosion.Perform(origin, bulletData.explosionRadius, bulletData.explosiveDamage,
            KnockbackForce, Effects, AttackAttributes, Attacker, EffectProbabilityMultiplier, TravelDirection);
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
