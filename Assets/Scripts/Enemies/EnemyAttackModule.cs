using System;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyAttackType { Melee, Ranged }

public interface IEnemyAttackModule
{
    bool IsAttacking { get; }
    bool HasLineOfSight(Transform target);
    void Reset();
    void TryBegin(Transform target);
    void Tick(Transform target);
    void Cancel();
}

/// <summary>Owns attack windup, cooldown, and movement locking, independently of enemy health/death.</summary>
public abstract class EnemyAttackModule : IEnemyAttackModule
{
    protected readonly EnemyChase owner;
    protected readonly NavMeshAgent agent;
    protected readonly AttackIndicator indicator;
    private readonly float range, interval, duration;
    private readonly Action<Vector2> face;
    private float lastAttackTime, elapsed;
    private bool wasStopped;
    public bool IsAttacking { get; private set; }

    protected EnemyAttackModule(EnemyChase owner, NavMeshAgent agent, AttackIndicator indicator,
        float range, float interval, float duration, Action<Vector2> face)
    {
        this.owner = owner; this.agent = agent; this.indicator = indicator;
        this.range = range; this.interval = interval; this.duration = duration; this.face = face;
    }
    public virtual bool HasLineOfSight(Transform target) => target != null;
    protected virtual bool CanBegin => indicator != null;
    public void Reset() { Cancel(); lastAttackTime = Time.time - Mathf.Max(0f, interval); }
    public void TryBegin(Transform target)
    {
        if (IsAttacking || target == null || !CanBegin || !HasLineOfSight(target) ||
            Vector2.Distance(owner.transform.position, target.position) > range || Time.time < lastAttackTime + interval) return;
        IsAttacking = true;
        elapsed = 0f;
        wasStopped = agent.isStopped;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        Vector2 direction = (Vector2)target.position - (Vector2)owner.transform.position;
        if (direction.sqrMagnitude <= Mathf.Epsilon) direction = Vector2.right;
        face(direction);
        if (indicator != null) indicator.Begin(direction);
    }
    public void Tick(Transform target)
    {
        if (!IsAttacking) return;
        if (!HasLineOfSight(target)) { Cancel(); return; }
        agent.velocity = Vector3.zero;
        elapsed += Time.deltaTime;
        if (indicator != null) indicator.SetProgress(duration > 0f ? elapsed / duration : 1f);
        if (elapsed < Mathf.Max(0f, duration)) return;
        Perform(target);
        Cancel();
    }
    protected abstract void Perform(Transform target);
    public void Cancel()
    {
        if (indicator != null) indicator.Hide();
        if (!IsAttacking) return;
        IsAttacking = false;
        lastAttackTime = Time.time;
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = wasStopped;
    }
}

public sealed class EnemyMeleeAttackModule : EnemyAttackModule
{
    private readonly Action attack;
    public EnemyMeleeAttackModule(EnemyChase owner, NavMeshAgent agent, AttackIndicator indicator,
        float range, float interval, float duration, Action<Vector2> face, Action attack)
        : base(owner, agent, indicator, range, interval, duration, face) { this.attack = attack; }
    protected override void Perform(Transform target)
    {
        if (target != null && indicator != null && indicator.Contains(target.position)) attack();
    }
}

public sealed class EnemyRangedAttackModule : EnemyAttackModule
{
    private readonly EnemyBulletSO bullet;
    private readonly LayerMask obstacles;
    public EnemyRangedAttackModule(EnemyChase owner, NavMeshAgent agent, AttackIndicator indicator,
        float range, float interval, float duration, Action<Vector2> face, EnemyBulletSO bullet, LayerMask obstacles)
        : base(owner, agent, indicator, range, interval, duration, face) { this.bullet = bullet; this.obstacles = obstacles; }
    protected override bool CanBegin => bullet != null && BulletPool.Instance != null && BulletPool.Instance.CanFireEnemyBullet;
    public override bool HasLineOfSight(Transform target)
    {
        if (target == null) return false;
        Vector2 origin = owner.transform.position;
        Vector2 direction = (Vector2)target.position - origin;
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(origin, direction.normalized, direction.magnitude))
        {
            Collider2D collider = hit.collider;
            if (collider == null || collider.GetComponentInParent<EnemyChase>() != null || collider.GetComponentInParent<Player>() != null) continue;
            if (collider.CompareTag("BulletBlocker") || (!collider.isTrigger && (obstacles.value & (1 << collider.gameObject.layer)) != 0)) return false;
        }
        return true;
    }
    protected override void Perform(Transform target)
    {
        if (target == null || !HasLineOfSight(target) || BulletPool.Instance == null) return;
        EnemyBullet projectile = BulletPool.Instance.GetEnemyBullet(bullet);
        if (projectile != null)
            projectile.Launch(owner.transform.position, (Vector2)target.position - (Vector2)owner.transform.position,
                owner, target.position);
    }
}
