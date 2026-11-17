using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemySpawnRegion
{
    [Tooltip("Assign the PolygonCollider2D or final CompositeCollider2D defining this spawn region.")]
    public Collider2D regionCollider;
    // Keep old scene assignments so they can resolve a collider on the same object.
    [HideInInspector] public SpriteRenderer region;

    public Collider2D ResolveCollider()
    {
        Collider2D area = regionCollider;
        if (area == null && region != null)
        {
            area = region.GetComponent<CompositeCollider2D>();
            if (area == null) area = region.GetComponent<PolygonCollider2D>();
        }
        // Use the final boolean geometry, including subtracted holes, rather than a source polygon.
        if (area != null && area.compositeOperation != Collider2D.CompositeOperation.None)
            area = area.composite;
        return area;
    }
    public List<GameObject> enemyPrefabs = new List<GameObject>();
}

/// <summary>Distance-based waves restricted to polygon/composite collider regions.</summary>
public class RegionEnemySpawner : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform homePosition;
    [SerializeField] private EnemyPoolSpawner enemyPool;
    [SerializeField] private List<EnemySpawnRegion> regions = new List<EnemySpawnRegion>();
    [SerializeField, Min(0f)] private float difficulty_coefficient = 0.01f;
    [SerializeField, Min(0f)] private float point_coefficient = 0.01f;
    [SerializeField, Min(0.01f)] private float spawnPeriod = 3f;
    [SerializeField, Min(0f)] private float totalPoint = 1f;
    [SerializeField, Min(0f)] private float minRadius = 10f;
    [SerializeField, Min(0f)] private float maxRadius = 15f;
    private float timer;
    public bool IsSuppressedByHome { get; private set; }
    public void SetHomeRegionActive(bool active)
    {
        IsSuppressedByHome = active;
        timer = 0f;
    }
    private readonly List<GameObject> candidates = new List<GameObject>();
    private readonly Dictionary<GameObject, List<Collider2D>> collidersByPrefab = new Dictionary<GameObject, List<Collider2D>>();

    public float DistanceFromHome => playerTransform != null && homePosition != null
        ? Vector2.Distance(playerTransform.position, homePosition.position) : 0f;
    public float CurrentStatMultiplier => 1f + DistanceFromHome * Mathf.Max(0f, difficulty_coefficient);
    public float CurrentTotalPoint => Mathf.Max(0f, totalPoint) * (1f + DistanceFromHome * Mathf.Max(0f, point_coefficient));

    private void Start() => ResolveReferences();
    private void ResolveReferences()
    {
        if (enemyPool == null) enemyPool = EnemyPoolSpawner.Instance;
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
    }

    private void OnEnable() => timer = 0f;
    private void Update()
    {
        if (IsSuppressedByHome || StateManager.Instance.IsBossEncounter || Time.deltaTime <= 0f) return;
        ResolveReferences();
        if (playerTransform == null || homePosition == null || enemyPool == null) return;
        timer += Time.deltaTime;
        float period = Mathf.Max(0.01f, spawnPeriod);
        if (timer < period) return;
        timer %= period;
        SpawnNow();
    }

    public List<GameObject> SpawnNow()
    {
        ResolveReferences();
        return SpawnNow(CurrentTotalPoint);
    }

    public List<GameObject> SpawnNow(float enemyPoints)
    {
        ResolveReferences();
        if (!isActiveAndEnabled || IsSuppressedByHome || StateManager.Instance.IsBossEncounter || playerTransform == null || homePosition == null || enemyPool == null ||
            float.IsNaN(enemyPoints) || float.IsInfinity(enemyPoints) || enemyPoints <= 0f)
            return new List<GameObject>();
        Physics2D.SyncTransforms();
        CacheRegions();
        Debug.Log($"Spawning enemies in regions. Point:{enemyPoints}, Multiplier:{CurrentStatMultiplier}");
        return enemyPool.SpawnBatch(new SpawnRequest
        {
            centerPosition = playerTransform.position,
            minRadius = minRadius,
            maxRadius = maxRadius,
            totalPoint = enemyPoints,
            ignoreDifficulty = true,
            statMultiplier = CurrentStatMultiplier,
            allowedPrefabs = candidates,
            isPositionAllowed = IsAllowed
        });
    }

    private void CacheRegions()
    {
        candidates.Clear();
        collidersByPrefab.Clear();
        if (regions == null) return;
        foreach (EnemySpawnRegion entry in regions)
        {
            if (entry == null || entry.enemyPrefabs == null) continue;
            Collider2D area = entry.ResolveCollider();
            if (area == null || !area.enabled || !area.gameObject.activeInHierarchy) continue;
            foreach (GameObject prefab in entry.enemyPrefabs)
            {
                if (prefab == null) continue;
                if (!collidersByPrefab.TryGetValue(prefab, out List<Collider2D> areas))
                {
                    areas = new List<Collider2D>();
                    collidersByPrefab.Add(prefab, areas);
                    candidates.Add(prefab);
                }
                if (!areas.Contains(area)) areas.Add(area);
            }
        }
    }

    private bool IsAllowed(GameObject prefab, Vector3 point)
    {
        if (!collidersByPrefab.TryGetValue(prefab, out List<Collider2D> areas)) return false;
        foreach (Collider2D area in areas)
        {
            if (area == null || !area.enabled || !area.gameObject.activeInHierarchy) continue;
            Bounds bounds = area.bounds;
            if (point.x < bounds.min.x || point.x > bounds.max.x || point.y < bounds.min.y || point.y > bounds.max.y) continue;
            if (area.OverlapPoint((Vector2)point)) return true;
        }
        return false;
    }
}
