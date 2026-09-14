using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet2D : MonoBehaviour
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private string obstacleTag = "BulletBlocker";

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D bulletCollider;
    private Camera mainCamera;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bulletCollider = GetComponent<Collider2D>();
        mainCamera = Camera.main;
    }

    public void Launch(Vector2 direction)
    {
        // Re-enable visual, collider, and GameObject state when re-firing
        gameObject.SetActive(true);
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (bulletCollider != null) bulletCollider.enabled = true;

        // Rotate bullet sprite to align with direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Apply velocity
        rb.linearVelocity = direction.normalized * speed;
    }

    private void Update()
    {
        // Check if bullet flew off-screen
        Vector3 screenPoint = mainCamera.WorldToViewportPoint(transform.position);
        bool isOffScreen = screenPoint.x < 0 || screenPoint.x > 1 || screenPoint.y < 0 || screenPoint.y > 1;

        if (isOffScreen)
        {
            Deactivate();
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