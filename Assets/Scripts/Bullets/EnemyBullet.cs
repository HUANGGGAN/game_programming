using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyBullet : MonoBehaviour
{
    [SerializeField] private string obstacleTag = "BulletBlocker";
    private EnemyBulletSO data;
    private float prefabAngle;
    private EnemyChase attacker;
    private Rigidbody2D body;
    private Collider2D hitCollider;
    private SpriteRenderer visual;
    private Vector2 direction;
    private Vector2? target;
    private float remainingLifetime;
    private int remainingPierces;
    private bool launched;
    private float damageMultiplier = 1f;
    private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();
    private readonly HashSet<Player> hitPlayers = new HashSet<Player>();

    private void EnsureComponents()
    {
        if (body != null) return;
        prefabAngle = transform.eulerAngles.z;
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
        hitCollider = GetComponent<Collider2D>();
        hitCollider.isTrigger = true;
        visual = GetComponentInChildren<SpriteRenderer>(true);
        if (visual != null && visual.transform == transform)
        {
            SpriteRenderer original = visual;
            GameObject child = new GameObject("Enemy Bullet Visual");
            child.transform.SetParent(transform, false);
            visual = child.AddComponent<SpriteRenderer>();
            visual.sharedMaterial = original.sharedMaterial;
            visual.sortingLayerID = original.sortingLayerID;
            visual.sortingOrder = original.sortingOrder;
            visual.flipX = original.flipX;
            visual.flipY = original.flipY;
            original.enabled = false;
        }
    }

    public void Initialize(EnemyBulletSO bulletData)
    {
        EnsureComponents();
        data = bulletData;
        if (visual != null)
        {
            visual.sprite = data.bulletSprite;
            visual.color = data.tint;
            visual.transform.localScale = new Vector3(data.imageScale.x, data.imageScale.y, 1f);
        }
        if (hitCollider is BoxCollider2D box) box.size = data.colliderSize;
        else if (hitCollider is CircleCollider2D circle)
            circle.radius = Mathf.Max(Mathf.Abs(data.colliderSize.x), Mathf.Abs(data.colliderSize.y)) * 0.5f;
        else if (hitCollider is CapsuleCollider2D capsule) capsule.size = data.colliderSize;
    }

    public float GetClearanceRadius(EnemyBulletSO bulletData)
    {
        Collider2D shape = GetComponent<Collider2D>();
        Vector3 scale = transform.lossyScale;
        Vector2 size = new Vector2(Mathf.Abs(bulletData.colliderSize.x * scale.x), Mathf.Abs(bulletData.colliderSize.y * scale.y));
        float radius = shape is CircleCollider2D ? Mathf.Max(size.x, size.y) * 0.5f : size.magnitude * 0.5f;
        if (shape != null)
        {
            Vector2 offset = Vector2.Scale(shape.offset, new Vector2(scale.x, scale.y));
            radius += offset.magnitude;
        }
        return Mathf.Max(0.01f, radius) + 0.02f;
    }

    public void Launch(Vector2 origin, Vector2 aim, EnemyChase owner, Vector2? targetPosition = null, float damageMultiplier = 1f)
    {
        if (data == null) return;
        attacker = owner;
        this.damageMultiplier = Mathf.Max(0f, damageMultiplier);
        direction = aim.sqrMagnitude > Mathf.Epsilon ? aim.normalized : Vector2.right;
        target = data.bulletMode == 1 && data.isExplosive ? targetPosition : null;
        remainingLifetime = data.lifetime;
        remainingPierces = Mathf.Max(0, data.isPiercing);
        hitPlayers.Clear();
        transform.position = new Vector3(origin.x, origin.y, 0f);
        transform.rotation = Quaternion.Euler(0f, 0f, data.preserveAngle ? prefabAngle : Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        body.position = origin;
        body.rotation = transform.eulerAngles.z;
        body.linearVelocity = Vector2.zero;
        if (visual != null) visual.transform.localRotation = Quaternion.identity;
        launched = true;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!launched) return;
        remainingLifetime -= Time.deltaTime;
        if (remainingLifetime <= 0f) { Deactivate(); return; }
        if (visual != null && data.spinningSpeed != 0f)
            visual.transform.Rotate(0f, 0f, data.spinningSpeed * Time.deltaTime, Space.Self);
    }

    private void FixedUpdate()
    {
        if (!launched) return;
        Vector2 origin = body.position;
        float distance = Mathf.Max(0f, data.speed) * Time.fixedDeltaTime;
        bool reachesTarget = target.HasValue && Vector2.Distance(origin, target.Value) <= distance;
        if (reachesTarget) distance = Vector2.Distance(origin, target.Value);
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        filter.useTriggers = true;
        hits.Clear();
        body.Cast(direction, filter, hits, distance);
        hits.Sort((a, b) =>
        {
            int comparison = a.distance.CompareTo(b.distance);
            if (comparison != 0) return comparison;
            return (b.collider.CompareTag(obstacleTag) ? 1 : 0).CompareTo(a.collider.CompareTag(obstacleTag) ? 1 : 0);
        });
        foreach (RaycastHit2D hit in hits)
        {
            Collider2D collider = hit.collider;
            if (collider == null || collider.attachedRigidbody == body || !collider.enabled || !collider.gameObject.activeInHierarchy) continue;
            // Death bursts start with overlapping projectiles; they must never block each other.
            if (collider.GetComponentInParent<EnemyBullet>() != null) continue;
            bool obstacle = collider.CompareTag(obstacleTag);
            Player player = obstacle ? null : collider.GetComponentInParent<Player>();
            if (!obstacle && (player == null || !player.isActiveAndEnabled || hitPlayers.Contains(player))) continue;
            Vector2 impact = origin + direction * hit.distance;
            if (player != null)
            {
                hitPlayers.Add(player);
                if (player.TakeDamage(Mathf.Round(data.damage * damageMultiplier)))
                {
                    player.ApplyKnockback(direction, data.knockbackForce);
                    player.ApplyEffects(data.effects, attacker != null && attacker.isActiveAndEnabled ? attacker.RestoreEffectResource : null);
                }
                if (remainingPierces > 0)
                {
                    remainingPierces--;
                    if (data.isExplosive) Detonate(impact);
                    continue;
                }
            }
            body.position = impact;
            transform.position = new Vector3(impact.x, impact.y, transform.position.z);
            if (data.isExplosive) Detonate(impact);
            Deactivate();
            return;
        }
        body.position = origin + direction * distance;
        transform.position = new Vector3(body.position.x, body.position.y, transform.position.z);
        if (reachesTarget)
        {
            Detonate(target.Value);
            Deactivate();
        }
    }

    private void Detonate(Vector2 position)
    {
        Explosion.PerformEnemy(position, data.explosionRadius, Mathf.Round(data.explosiveDamage * damageMultiplier), data.knockbackForce,
            data.effects, attacker, direction, data.explosionParticleId);
    }

    public void Deactivate()
    {
        if (launched && data != null && data.ParticleID > 0)
            ParticlePool.Instance?.Play(data.ParticleID.ToString(System.Globalization.CultureInfo.InvariantCulture), transform.position);
        launched = false;
        if (body != null) body.linearVelocity = Vector2.zero;
        gameObject.SetActive(false);
    }

    private void OnDisable() { launched = false; attacker = null; }
}
