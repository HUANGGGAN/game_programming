using System.Collections.Generic;
using UnityEngine;

public class Tornado : MonoBehaviour
{
    [SerializeField, Min(0f)] private float radius = 5f;
    [SerializeField, Min(0.01f)] private float duration = 4f;
    [SerializeField, Min(0f)] private float force = 2.5f;
    [SerializeField, Min(0f)] private float damage = 5f;
    [SerializeField, Min(0.01f)] private float damage_interval = 0.5f;
    [SerializeField] private SpriteRenderer image;
    [SerializeField] private Sprite[] images = new Sprite[2];
    private readonly List<Collider2D> overlaps = new List<Collider2D>();
    private readonly HashSet<EnemyChase> enemies = new HashSet<EnemyChase>();
    private float elapsed, damageElapsed;
    private int imageIndex;
    private bool launched;

    public void Launch(Vector2 position)
    {
        transform.position = new Vector3(position.x, position.y, 0f);
        elapsed = damageElapsed = 0f;
        imageIndex = 0;
        if (image == null) image = GetComponentInChildren<SpriteRenderer>(true);
        RefreshImage();
        launched = true;
        gameObject.SetActive(true);
    }

    private void FixedUpdate()
    {
        if (!launched) return;
        float dt = Mathf.Min(Time.fixedDeltaTime, Mathf.Max(0f, duration - elapsed));
        elapsed += dt;
        damageElapsed += dt;
        float interval = Mathf.Max(0.01f, damage_interval);
        int ticks = Mathf.FloorToInt((damageElapsed + 0.00001f) / interval);
        damageElapsed = Mathf.Max(0f, damageElapsed - ticks * interval);
        overlaps.Clear();
        enemies.Clear();
        Physics2D.OverlapCircle(transform.position, Mathf.Max(0f, radius), ContactFilter2D.noFilter, overlaps);
        foreach (Collider2D collider in overlaps)
        {
            EnemyChase enemy = EnemyChase.FindDamageTarget(collider);
            if (enemy == null || !enemy.isActiveAndEnabled || enemy.CurrentHealth <= 0f || !enemies.Add(enemy)) continue;
            enemy.PullToward(transform.position, force, dt);
            for (int i = 0; i < ticks && enemy.CurrentHealth > 0f; i++)
                enemy.TakeDamage(damage);
        }
        if (ticks > 0 && images != null && images.Length > 0)
        {
            imageIndex = (imageIndex + ticks) % images.Length;
            RefreshImage();
        }
        if (elapsed >= duration) gameObject.SetActive(false);
    }

    private void RefreshImage()
    {
        if (image != null && images != null && images.Length > 0 && images[imageIndex] != null)
            image.sprite = images[imageIndex];
    }

    private void OnDisable()
    {
        launched = false;
        overlaps.Clear();
        enemies.Clear();
    }
}

public sealed class TornadoAttack : IWeaponAttackHandler
{
    public bool CanHandle(WeaponSO weapon) => weapon is WandSO wand && wand.attackMode == 2;

    public bool TryPerform(WeaponSO weapon, Vector2 origin, Vector2 direction, System.Func<bool> tryConsumeAmmo,
        Vector2? targetPosition = null, Player attacker = null)
    {
        if (!CanHandle(weapon) || !targetPosition.HasValue || BulletPool.Instance == null ||
            !BulletPool.Instance.CanSummonTornado) return false;
        if (!tryConsumeAmmo()) return false;
        return BulletPool.Instance.SummonTornado(targetPosition.Value) != null;
    }
}
