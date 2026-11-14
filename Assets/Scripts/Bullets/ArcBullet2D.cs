using UnityEngine;

// Attach to the bullet root; its colliders and shadow remain on the ground plane.
public class ArcBullet2D : Bullet2D
{
    [SerializeField, Min(0f)] private float arcRatio = 1f;
    [Header("Boomerang (Mode 1)")]
    [SerializeField] private Transform returnTarget;
    [SerializeField, Min(0f)] private float curveRatio = 0.3f;
    [SerializeField] private float spinSpeed = 720f;
    [SerializeField, Min(0f)] private float catchDistance = 0.15f;
    [SerializeField, Range(4, 128)] private int curveSegments = 32;
    [Header("Surrounding (Mode 2)")]
    [SerializeField, Min(0.01f)] private float orbitRadius = 2f;
    private bool isOrbiting;
    private Transform orbitTarget;
    private float orbitAngle;
    private float orbitSpeed;
    private float activeOrbitRadius;
    private Vector2 orbitDirection;
    private Vector2 previousOrbitCenter;
    private bool isBoomerang;
    private bool isReturning;
    private float boomerangSpeed;
    private Vector2 legStart;
    private Vector2 outboundTarget;
    private int nextWaypoint;
    private int segmentCount;
    private Transform visualSprite;
    private Vector2 startPosition;
    private float flightDistance;
    private bool isArcing;
    private Vector2 boomerangDirection;
    protected override bool ImpactsColliders => bulletData != null && (bulletData.bulletMode == 1 || bulletData.bulletMode == 2);
    protected override bool UsesConfiguredSpin => !isBoomerang;
    public override Vector2 TravelDirection => isOrbiting ? orbitDirection : isBoomerang && boomerangDirection.sqrMagnitude > 0f
        ? boomerangDirection : base.TravelDirection;
    protected override bool DespawnOffScreen => !isBoomerang && !isOrbiting;

    protected override bool UsesTargetExplosion =>
        bulletData != null && bulletData.bulletMode == 0 && bulletData.bulletId == "1" && bulletData.isExplosive;

    protected override void EnsureComponents()
    {
        base.EnsureComponents();
        if (visualSprite != null)
            return;
        // Use cast-based hits rather than physical collision responses.
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
        visualSprite.localRotation = Quaternion.identity;
        isBoomerang = bulletData != null && bulletData.bulletMode == 1 && bulletData.bulletId == "1" && targetPosition.HasValue;
        isReturning = false;
        isOrbiting = bulletData != null && bulletData.bulletId == "1" && bulletData.bulletMode == 2;
        if (isOrbiting)
        {
            orbitTarget = Attacker != null ? Attacker.transform : returnTarget;
            if (orbitTarget == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) orbitTarget = player.transform;
            }
            if (orbitTarget == null || !orbitTarget.gameObject.activeInHierarchy)
            {
                Deactivate();
                return;
            }
            activeOrbitRadius = Mathf.Max(0.01f, orbitRadius);
            orbitSpeed = Mathf.Max(0f, speed);
            orbitAngle = Mathf.Atan2(direction.y, direction.x);
            previousOrbitCenter = orbitTarget.position;
            rb.position = previousOrbitCenter + new Vector2(Mathf.Cos(orbitAngle), Mathf.Sin(orbitAngle)) * activeOrbitRadius;
            transform.position = new Vector3(rb.position.x, rb.position.y, transform.position.z);
            orbitDirection = new Vector2(-Mathf.Sin(orbitAngle), Mathf.Cos(orbitAngle));
            rb.linearVelocity = Vector2.zero;
        }
        if (isBoomerang)
        {
            if (returnTarget == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    returnTarget = player.transform;
            }
            boomerangSpeed = Mathf.Max(0f, speed);
            boomerangDirection = direction.normalized;
            legStart = rb.position;
            outboundTarget = targetPosition.Value;
            nextWaypoint = 1;
            segmentCount = Mathf.Clamp(curveSegments, 4, 128);
            rb.linearVelocity = Vector2.zero;
        }
    }

    protected override void FixedUpdate()
    {
        if (isOrbiting)
        {
            UpdateOrbit();
            return;
        }
        if (!isBoomerang)
        {
            base.FixedUpdate();
            return;
        }
        if (returnTarget == null || !returnTarget.gameObject.activeInHierarchy)
        {
            Deactivate();
            return;
        }

        Vector2 position = rb.position;
        Vector2 target = isReturning ? (Vector2)returnTarget.position : outboundTarget;
        float remainingStep = boomerangSpeed * Time.fixedDeltaTime;
        if (isReturning && Vector2.Distance(position, target) <= catchDistance)
        {
            Deactivate();
            return;
        }

        // Spend a fixed distance along sampled quadratic curves. Updating the
        // return endpoint changes the route without teleporting the bullet.
        while (remainingStep > 0f && nextWaypoint <= segmentCount)
        {
            float t = (float)nextWaypoint / segmentCount;
            Vector2 delta = target - legStart;
            Vector2 control = (legStart + target) * 0.5f +
                new Vector2(-delta.y, delta.x) * curveRatio;
            Vector2 waypoint = (1f - t) * (1f - t) * legStart +
                2f * (1f - t) * t * control + t * t * target;
            float distance = Vector2.Distance(position, waypoint);
            Vector2 nextPosition;
            if (distance > remainingStep)
            {
                nextPosition = Vector2.MoveTowards(position, waypoint, remainingStep);
                remainingStep = 0f;
            }
            else
            {
                nextPosition = waypoint;
                remainingStep -= distance;
                nextWaypoint++;
            }
            Vector2 movement = nextPosition - position;
            if (movement.sqrMagnitude > 0f)
                boomerangDirection = movement.normalized;
            if (!SweepForImpacts(movement))
                return;
            position = nextPosition;
            // Advance the cast origin before processing the next curved segment.
            rb.position = position;
        }
        rb.linearVelocity = Vector2.zero;
        if (nextWaypoint > segmentCount)
        {
            if (isReturning)
            {
                Deactivate();
                return;
            }
            isReturning = true;
            BeginDamageSource();
            legStart = position;
            nextWaypoint = 1;
        }
    }

    private void UpdateOrbit()
    {
        if (orbitTarget == null || !orbitTarget.gameObject.activeInHierarchy)
        {
            Deactivate();
            return;
        }
        Vector2 center = orbitTarget.position;
        float angleStep = orbitSpeed / activeOrbitRadius * Time.fixedDeltaTime;
        // Sweep short chords around the circle so fast orbits still hit along the curve.
        int steps = Mathf.Max(1, Mathf.CeilToInt(angleStep / (10f * Mathf.Deg2Rad)));
        float startAngle = orbitAngle;
        for (int i = 1; i <= steps; i++)
        {
            float progress = (float)i / steps;
            float angle = startAngle + angleStep * progress;
            Vector2 next = Vector2.Lerp(previousOrbitCenter, center, progress) +
                new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * activeOrbitRadius;
            Vector2 movement = next - rb.position;
            orbitDirection = movement.sqrMagnitude > Mathf.Epsilon ? movement.normalized :
                new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle));
            if (!SweepForImpacts(movement)) return;
            rb.position = next;
        }
        orbitAngle = Mathf.Repeat(startAngle + angleStep, Mathf.PI * 2f);
        previousOrbitCenter = center;
        rb.linearVelocity = Vector2.zero;
    }

    private void LateUpdate()
    {
        if (isBoomerang)
        {
            visualSprite.Rotate(0f, 0f, spinSpeed * Time.deltaTime, Space.Self);
            return;
        }
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
        isBoomerang = false;
        isReturning = false;
        isOrbiting = false;
        orbitTarget = null;
        if (visualSprite != null)
        {
            visualSprite.localPosition = Vector3.zero;
            visualSprite.localRotation = Quaternion.identity;
        }
        base.Deactivate();
    }
}
