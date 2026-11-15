using UnityEngine;
using UnityEngine.AI;

/// <summary>Melee enemy with a short, arcing leap toward the player's recorded position.</summary>
public class Wolf : EnemyChase
{
    [Header("Jump")]
    [SerializeField, Min(0f)] private float jumpCooldown = 3f;
    [SerializeField, Min(0.01f)] private float jumpSpeed = 8f;
    [SerializeField, Min(0f)] private float jumpHeight = 1.2f;
    [SerializeField, Min(0.01f)] private float maxJumpDistance = 5f;
    [Tooltip("Visual child lifted during jumps. The enemy root and its colliders remain on the ground.")]
    [SerializeField] private Transform jumpVisual;

    private Vector3 visualRestPosition;
    private Vector3 jumpStart;
    private Vector3 jumpTarget;
    private float jumpElapsed;
    private float jumpDuration;
    private float nextJumpTime;
    private bool jumping;
    private bool wasStopped;

    protected override EnemyAttackType AttackType => EnemyAttackType.Melee;

    protected override void Awake()
    {
        base.Awake();
        if (jumpVisual == null && EnemySprite != null)
        {
            if (EnemySprite.transform != transform) jumpVisual = EnemySprite.transform;
            else
            {
                // Keep the sprite reference used by hit flashes and data initialization intact.
                SpriteRenderer original = EnemySprite;
                GameObject visual = new GameObject("Wolf Visual");
                visual.transform.SetParent(transform, false);
                SpriteRenderer sprite = visual.AddComponent<SpriteRenderer>();
                sprite.sprite = original.sprite;
                sprite.color = original.color;
                sprite.sharedMaterial = original.sharedMaterial;
                sprite.sortingLayerID = original.sortingLayerID;
                sprite.sortingOrder = original.sortingOrder;
                sprite.flipX = original.flipX;
                sprite.flipY = original.flipY;
                sprite.maskInteraction = original.maskInteraction;
                original.enabled = false;
                EnemySprite = sprite;
                jumpVisual = visual.transform;
            }
        }
        // Never lift the root: it owns navigation and collision.
        if (jumpVisual == transform) jumpVisual = null;
        if (jumpVisual != null) visualRestPosition = jumpVisual.localPosition;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        jumping = false;
        if (jumpVisual != null) jumpVisual.localPosition = visualRestPosition;
        nextJumpTime = Time.time + Mathf.Max(0f, jumpCooldown);
    }

    protected override void Update()
    {
        if (jumping && (IsKnockedBack || CurrentHealth <= 0f || PlayerTarget == null ||
            !Agent.enabled || !Agent.isOnNavMesh || EffectRuntime.GetMovementMultiplier(ActiveEffects) <= 0f))
            EndJump();
        base.Update();
    }

    protected override void UpdateMovement()
    {
        if (jumping)
        {
            AdvanceJump();
            return;
        }
        if (PlayerTarget != null && jumpVisual != null && Time.time >= nextJumpTime &&
            EffectRuntime.GetMovementMultiplier(ActiveEffects) > 0f &&
            Vector2.Distance(transform.position, PlayerTarget.position) > Agent.stoppingDistance + 0.1f && TryBeginJump())
            return;
        base.UpdateMovement();
    }

    private bool TryBeginJump()
    {
        Vector3 direction = PlayerTarget.position - transform.position;
        direction.z = 0f;
        float distance = Mathf.Min(direction.magnitude, Mathf.Max(0.01f, maxJumpDistance));
        Vector3 desired = transform.position + direction.normalized * distance;
        NavMeshQueryFilter filter = new NavMeshQueryFilter { agentTypeID = Agent.agentTypeID, areaMask = Agent.areaMask };
        if (!NavMesh.SamplePosition(desired, out NavMeshHit landing, 0.5f, filter)) return false;
        if (NavMesh.Raycast(transform.position, landing.position, out NavMeshHit obstruction, filter)) return false;
        if (Vector2.Distance(transform.position, landing.position) <= Agent.stoppingDistance + 0.1f) return false;

        jumpStart = transform.position;
        jumpTarget = landing.position;
        jumpDuration = Mathf.Max(Time.deltaTime, Vector2.Distance(jumpStart, jumpTarget) / Mathf.Max(0.01f, jumpSpeed));
        jumpElapsed = 0f;
        wasStopped = Agent.isStopped;
        Agent.ResetPath();
        Agent.isStopped = true;
        Agent.velocity = Vector3.zero;
        jumping = true;
        FaceDirection(direction);
        return true;
    }

    private void AdvanceJump()
    {
        jumpElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(jumpElapsed / jumpDuration);
        Vector3 desired = Vector3.Lerp(jumpStart, jumpTarget, Mathf.SmoothStep(0f, 1f, t));
        // Recheck the path in case navigation changes during the jump.
        NavMeshQueryFilter filter = new NavMeshQueryFilter { agentTypeID = Agent.agentTypeID, areaMask = Agent.areaMask };
        if (NavMesh.Raycast(transform.position, desired, out NavMeshHit obstruction, filter))
        {
            EndJump();
            return;
        }
        Agent.Move(desired - transform.position);
        jumpVisual.localPosition = visualRestPosition + Vector3.up * (4f * Mathf.Max(0f, jumpHeight) * t * (1f - t));
        if (t >= 1f)
        {
            EndJump();
            TryAttackPlayer();
        }
    }

    private void EndJump()
    {
        if (!jumping) return;
        jumping = false;
        nextJumpTime = Time.time + Mathf.Max(0f, jumpCooldown);
        if (jumpVisual != null) jumpVisual.localPosition = visualRestPosition;
        if (Agent != null && Agent.enabled && Agent.isOnNavMesh)
        {
            Agent.velocity = Vector3.zero;
            Agent.isStopped = wasStopped;
        }
    }

    protected override void OnKnockbackStarting() => EndJump();

    public override bool TakeDamage(float amount, Vector2 direction, float force, int sourceId, float sourceDuration,
        int[] attributes = null, EnemyDamageKind damageKind = EnemyDamageKind.Other)
    {
        bool accepted = base.TakeDamage(amount, direction, force, sourceId, sourceDuration, attributes, damageKind);
        if (accepted) EndJump();
        return accepted;
    }

    protected override void Die() { EndJump(); base.Die(); }
    public override void Despawn() { EndJump(); base.Despawn(); }
    protected override void OnDisable() { EndJump(); base.OnDisable(); }
}
