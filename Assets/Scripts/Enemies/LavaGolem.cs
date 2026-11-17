using UnityEngine;

public class LavaGolem : EnemyChase
{
    [Header("Fire Path")]
    [SerializeField] private GameObject firePrefab;
    [SerializeField] private Sprite[] fireSprites = new Sprite[4];
    [SerializeField, Min(0f)] private float pathLength = 5f;
    [SerializeField, Min(0.01f)] private float fireSpacing = 0.75f;
    [SerializeField, Min(0f)] private float fireSpawnInterval = 0.1f;
    [SerializeField, Min(0.01f)] private float fireLifetime = 5f;

    protected override EnemyAttackType AttackType => EnemyAttackType.Melee;

    protected override void OnMeleeAttackCompleted(AttackIndicator indicator)
    {
        if (firePrefab == null || indicator == null) return;
        indicator.GetPath(out Vector2 origin, out Vector2 direction);
        Sprite[] sprites = fireSprites != null ? System.Array.FindAll(fireSprites, sprite => sprite != null) : new Sprite[0];
        float radius = Mathf.Max(0.01f, fireSpacing) * 0.55f;
        PathSpawner.GetOrCreate().SummonPath(firePrefab, origin, direction, pathLength, fireSpacing,
            fireSpawnInterval, fireLifetime, (piece, index) =>
            {
                SpriteRenderer image = piece.GetComponentInChildren<SpriteRenderer>(true);
                if (image != null && sprites.Length > 0) image.sprite = sprites[Random.Range(0, sprites.Length)];
                Collider2D area = piece.GetComponent<Collider2D>();
                if (area == null)
                {
                    CircleCollider2D circle = piece.AddComponent<CircleCollider2D>();
                    circle.radius = radius;
                    area = circle;
                }
                area.isTrigger = true;
                Region region = piece.GetComponent<Region>();
                if (region == null) region = piece.AddComponent<Region>();
                region.SetType(RegionType.Fire);
            });
    }
}
