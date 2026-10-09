using UnityEngine;
using System.Threading;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet2D : MonoBehaviour
{
    private static int nextDamageSourceId;

    [SerializeField] private string obstacleTag = "BulletBlocker";

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D bulletCollider;
    private Camera mainCamera;
    private BulletSO bulletData;
    private float remainingLifetime;
    private float currentDamage;
    private float effectProbabilityMultiplier = 1f;
    private int damageSourceId;

    public float Damage => currentDamage;
    public float KnockbackForce => bulletData != null ? bulletData.knockbackForce : 0f;
    public EffectDefinition[] Effects => bulletData != null ? bulletData.effects : null;
    public float EffectProbabilityMultiplier => effectProbabilityMultiplier;
    public int DamageSourceId => damageSourceId;
    public Vector2 TravelDirection => rb != null && rb.linearVelocity.sqrMagnitude > 0f
        ? rb.linearVelocity.normalized
        : transform.right;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bulletCollider = GetComponent<Collider2D>();
        mainCamera = Camera.main;
    }

    public void Initialize(BulletSO data)
    {
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

    public void Launch(Vector2 direction, float damage, float speed, float effectProbabilityMultiplier)
    {
        if (bulletData == null) return;

        currentDamage = damage;
        this.effectProbabilityMultiplier = Mathf.Max(0f, effectProbabilityMultiplier);
        damageSourceId = Interlocked.Increment(ref nextDamageSourceId);

        // Re-enable visual, collider, and GameObject state when re-firing
        gameObject.SetActive(true);
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (bulletCollider != null) bulletCollider.enabled = true;
        remainingLifetime = bulletData.lifetime;

        // Rotate bullet sprite to align with direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(obstacleTag) || other.CompareTag("Enemy"))
        {
            Deactivate();
        }
    }

    public void Deactivate()
    {
        // Stop movement, make invisible, and set inactive
        rb.linearVelocity = Vector2.zero;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (bulletCollider != null) bulletCollider.enabled = false;
        
        gameObject.SetActive(false);
    }
}