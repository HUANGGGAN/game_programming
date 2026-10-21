using UnityEngine;

// Attach to the bullet root; its colliders and shadow remain on the ground plane.
public class ArcBullet2D : Bullet2D
{
    [SerializeField, Min(0f)] private float arcRatio = 1f;
    private Transform visualSprite;
    private Vector2 startPosition;
    private float flightDistance;
    private bool isArcing;
    protected override bool ImpactsColliders => false;

    protected override bool UsesTargetExplosion => base.UsesTargetExplosion ||
        (bulletData != null && bulletData.bulletMode == 0 && bulletData.bulletId == "1" && bulletData.isExplosive);

    protected override void EnsureComponents()
    {
        base.EnsureComponents();
        if (visualSprite != null)
            return;
        // Fly over enemies and obstacles without physical collision responses.
        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>(true))
            collider.isTrigger = true;
        // Lift only the image, so height never changes casts, explosion positions, or the shadow.
        SpriteRenderer rootRenderer = spriteRenderer;
        GameObject visual = new GameObject("Arc Sprite");
        visual.transform.SetParent(transform, false);
        visualSprite = visual.transform;
        spriteRenderer = visual.AddComponent<SpriteRenderer>();
        if (rootRenderer != null)
        {
            spriteRenderer.sharedMaterial = rootRenderer.sharedMaterial;
            spriteRenderer.sortingLayerID = rootRenderer.sortingLayerID;
            spriteRenderer.sortingOrder = rootRenderer.sortingOrder;
            spriteRenderer.flipX = rootRenderer.flipX;
            spriteRenderer.flipY = rootRenderer.flipY;
            rootRenderer.enabled = false;
        }
    }

    public override void Launch(Vector2 direction, float damage, float speed, float effectProbabilityMultiplier,
        Vector2? targetPosition = null)
    {
        base.Launch(direction, damage, speed, effectProbabilityMultiplier, targetPosition);
        startPosition = rb.position;
        flightDistance = targetPosition.HasValue ? Vector2.Distance(startPosition, targetPosition.Value) : 0f;
        isArcing = bulletData != null && bulletData.bulletMode == 0 && bulletData.bulletId == "1" &&
            targetPosition.HasValue && flightDistance > Mathf.Epsilon;
        visualSprite.localPosition = Vector3.zero;
    }

    private void LateUpdate()
    {
        if (!isArcing)
            return;
        float t = Mathf.Clamp01(Vector2.Distance(startPosition, rb.position) / flightDistance);
        float height = Mathf.Max(0f, arcRatio * flightDistance) * t * (1f - t);
        visualSprite.position = transform.position + Vector3.up * height;
    }

    public override void Deactivate()
    {
        EnsureComponents();
        isArcing = false;
        if (visualSprite != null)
            visualSprite.localPosition = Vector3.zero;
        base.Deactivate();
    }
}
