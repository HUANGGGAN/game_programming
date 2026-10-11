using System;
using System.Collections.Generic;
using UnityEngine;

public class Boss : EnemyChase
{
    public enum MovingState { Normal, Charging, Dashing, DashCooldown }

    [Header("Boss Movement")]
    [SerializeField, Min(0f)] private float normalSpeed = 2.5f;
    [SerializeField, Min(0.01f)] private float dashSpeed = 12f;
    [SerializeField, Min(0f)] private float dashSkillCooldown = 10f;
    [SerializeField, Min(0f)] private float chargeDuration = 3f;
    [SerializeField, Min(0f)] private float dashDamage = 20f;
    [SerializeField, Min(0f)] private float dashKnockback = 8f;

    [Header("Boss Shields")]
    [SerializeField, Min(0)] private int maxShieldCount = 3;
    [SerializeField, Min(0.01f)] private float gainShieldTime = 10f;
    [SerializeField] private GameObject shieldPrefab;
    [SerializeField, Min(0f)] private float shieldOrbitRadius = 1.5f;
    [SerializeField] private float shieldOrbitSpeed = 90f; // Degrees per second
    [SerializeField] private Sprite bossShieldSprite;

    public event Action Died;
    public bool IsDead { get; private set; }
    public MovingState MovementState { get; private set; }
    public int ShieldCount { get; private set; }

    private const float DashCooldownDuration = 1f;
    private const int DashesPerSkill = 3;
    private float skillCooldownRemaining;
    private float chargeRemaining;
    private float dashCooldownRemaining;
    private float shieldTimer;
    private int completedDashes;
    private Vector2 dashTarget;
    private Vector2 dashDirection;
    private float dashTimeRemaining;
    private Color normalColor;
    private float normalStoppingDistance;
    private float shieldOrbitAngle;
    private readonly List<GameObject> shieldVisuals = new List<GameObject>();

    protected override bool DespawnWhenFarAway => false;
    protected override float MovementSpeed => MovementState == MovingState.Charging || MovementState == MovingState.DashCooldown ? 0f
        : MovementState == MovingState.Dashing ? dashSpeed : normalSpeed;
    protected override float AttackDamage => MovementState == MovingState.Dashing ? dashDamage : base.AttackDamage;
    protected override float AttackKnockback => MovementState == MovingState.Dashing ? dashKnockback : base.AttackKnockback;
    protected override bool CanReceiveKnockback => false;
    protected override bool CanFlashOnHit => MovementState != MovingState.Charging;

    protected override void Awake()
    {
        base.Awake();
        normalColor = EnemySprite != null ? EnemySprite.color : Color.white;
        normalStoppingDistance = Agent.stoppingDistance;
    }

    protected override void OnEnable()
    {
        IsDead = false;
        base.OnEnable();
        MovementState = MovingState.Normal;
        skillCooldownRemaining = Mathf.Max(0f, dashSkillCooldown);
        shieldTimer = 8f;
        shieldOrbitAngle = 0f;
        completedDashes = 0;
        dashCooldownRemaining = 0f;
        SetShieldCount(0);
        RestoreNormalAppearance();
    }

    protected override void Update()
    {
        if (IsDead || Time.deltaTime <= 0f)
            return;

        shieldTimer += Time.deltaTime;
        float period = Mathf.Max(1f, gainShieldTime);
        if (shieldTimer >= period)
        {
            int gained = Mathf.FloorToInt(shieldTimer / period);
            shieldTimer %= period;
            if (gained > 0)
            {
                Tutorial.Instance?.ShowTutorial("shield", "The boss has gained a shield! You must break the shield by melee attacking.\nThe boss won't take damage from ranged attacks while the shield is active.", bossShieldSprite);
            }
            SetShieldCount(Mathf.Min(Mathf.Max(0, maxShieldCount), ShieldCount + gained * 4));
        }
        base.Update();
    }

    private void LateUpdate()
    {
        if (IsDead || ShieldCount == 0)
            return;

        shieldOrbitAngle = Mathf.Repeat(shieldOrbitAngle + shieldOrbitSpeed * Time.deltaTime, 360f);
        PositionShieldVisuals();
    }

    private void RefreshShieldVisuals()
    {
        if (shieldPrefab != null)
        {
            while (shieldVisuals.Count < ShieldCount)
            {
                GameObject visual = Instantiate(shieldPrefab, transform, false);
                // These represent the existing shield count, rather than adding combat hitboxes.
                foreach (Collider2D collider in visual.GetComponentsInChildren<Collider2D>(true))
                    collider.enabled = false;
                foreach (Rigidbody2D body in visual.GetComponentsInChildren<Rigidbody2D>(true))
                    body.simulated = false;
                shieldVisuals.Add(visual);
            }
        }

        for (int i = 0; i < shieldVisuals.Count; i++)
            shieldVisuals[i].SetActive(!IsDead && shieldPrefab != null && i < ShieldCount);
        PositionShieldVisuals();
    }

    private void PositionShieldVisuals()
    {
        if (ShieldCount <= 0)
            return;

        float angleInterval = 360f / ShieldCount;
        for (int i = 0; i < ShieldCount && i < shieldVisuals.Count; i++)
        {
            float angle = (shieldOrbitAngle + angleInterval * i) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * Mathf.Max(0f, shieldOrbitRadius);
            shieldVisuals[i].transform.position = transform.position + offset;
        }
    }

    private void HideShieldVisuals()
    {
        foreach (GameObject visual in shieldVisuals)
            visual.SetActive(false);
    }

    protected override void OnDisable()
    {
        HideShieldVisuals();
        base.OnDisable();
    }

    protected override void UpdateMovement()
    {
        switch (MovementState)
        {
            case MovingState.Normal:
                skillCooldownRemaining -= Time.deltaTime;
                if (skillCooldownRemaining <= 0f && PlayerTarget != null)
                {
                    completedDashes = 0;
                    BeginCharging();
                }
                else
                    base.UpdateMovement();
                break;
            case MovingState.Charging:
                Agent.speed = 0f;
                Agent.velocity = Vector3.zero;
                chargeRemaining -= Time.deltaTime;
                if (chargeRemaining <= 0f)
                    BeginDash();
                break;
            case MovingState.Dashing:
                UpdateDash();
                break;
            case MovingState.DashCooldown:
                Agent.speed = 0f;
                Agent.velocity = Vector3.zero;
                dashCooldownRemaining -= Time.deltaTime;
                if (dashCooldownRemaining <= 0f)
                    BeginDash();
                break;
        }
    }

    private void BeginCharging()
    {
        MovementState = MovingState.Charging;
        chargeRemaining = Mathf.Max(0f, chargeDuration);
        ClearKnockback();
        StopHitFlash();
        Agent.ResetPath();
        Agent.speed = 0f;
        Agent.velocity = Vector3.zero;
        if (EnemySprite != null)
            EnemySprite.color = Color.yellow;
        if (chargeRemaining <= 0f)
            BeginDash();
    }

    private void BeginDash()
    {
        if (PlayerTarget == null)
        {
            FinishSkill();
            return;
        }

        MovementState = MovingState.Dashing;
        RestoreNormalAppearance();
        dashTarget = PlayerTarget.position;
        dashDirection = (dashTarget - (Vector2)transform.position).normalized;
        FaceDirection(dashDirection);
        float distance = Vector2.Distance(transform.position, dashTarget);
        dashTimeRemaining = distance / Mathf.Max(0.01f, dashSpeed) + 0.5f;
        Agent.stoppingDistance = 0f;
        Agent.speed = Mathf.Max(0.01f, dashSpeed);
        Agent.ResetPath();
        Agent.velocity = Vector3.zero;
    }

    private void UpdateDash()
    {
        // Capture a fresh target before each dash, then keep it fixed during that dash.
        Vector2 offset = dashTarget - (Vector2)transform.position;
        float distance = offset.magnitude;
        // Command a bounded world-space velocity instead of adding Move offsets to the simulation.
        float speed = Mathf.Min(Mathf.Max(0f, Agent.speed), distance / Time.deltaTime);
        Agent.velocity = distance > 0.01f ? (Vector3)(offset.normalized * speed) : Vector3.zero;
        TryAttackPlayer();
        dashTimeRemaining -= Time.deltaTime;
        if (distance <= 0.05f || dashTimeRemaining <= 0f)
        {
            Agent.velocity = Vector3.zero;
            completedDashes++;
            if (completedDashes < DashesPerSkill)
                BeginDashCooldown();
            else
                FinishSkill();
        }
    }

    private void BeginDashCooldown()
    {
        MovementState = MovingState.DashCooldown;
        dashCooldownRemaining = DashCooldownDuration;
        ClearKnockback();
        Agent.speed = 0f;
        Agent.velocity = Vector3.zero;
    }

    protected override void UpdateSpriteFacing()
    {
        if (MovementState == MovingState.Dashing)
            FaceDirection(dashDirection);
        else
            base.UpdateSpriteFacing();
    }

    private void FinishSkill()
    {
        MovementState = MovingState.Normal;
        skillCooldownRemaining = Mathf.Max(0f, dashSkillCooldown);
        ClearKnockback();
        Agent.velocity = Vector3.zero;
        Agent.stoppingDistance = normalStoppingDistance;
        Agent.speed = Mathf.Max(0f, normalSpeed);
        RestoreNormalAppearance();
    }

    private void RestoreNormalAppearance()
    {
        StopHitFlash();
        if (EnemySprite != null)
            EnemySprite.color = normalColor;
    }

    private void SetShieldCount(int count)
    {
        count = Mathf.Clamp(count, 0, Mathf.Max(0, maxShieldCount));
        if (ShieldCount == count)
            return;
        ShieldCount = count;
        RefreshShieldVisuals();
    }

    public override bool TakeDamage(float damageAmount, Vector2 knockbackDirection, float knockbackForce,
        int damageSourceId, float damageSourceDuration, int[] attackAttributes = null,
        EnemyDamageKind damageKind = EnemyDamageKind.Other)
    {
        if (IsDead || damageAmount <= 0f)
            return false;
        if (ShieldCount > 0)
        {
            if (damageKind != EnemyDamageKind.Melee ||
                !CanTakeDamageFromSource(damageSourceId, damageSourceDuration))
                return false;
            SetShieldCount(ShieldCount - 1);
            // Removing a shield consumes the swing without applying health damage or effects.
            return false;
        }
        return base.TakeDamage(damageAmount, knockbackDirection, knockbackForce, damageSourceId,
            damageSourceDuration, attackAttributes, damageKind);
    }

    protected override void Die()
    {
        if (IsDead)
            return;
        IsDead = true;
        HideShieldVisuals();
        RestoreNormalAppearance();
        base.Die();
        Died?.Invoke();
    }
}
