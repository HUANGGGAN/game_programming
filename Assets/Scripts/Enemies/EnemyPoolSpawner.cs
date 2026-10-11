using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyPoolSpawner : MonoBehaviour, IEnemySpawner
{
    public static EnemyPoolSpawner Instance { get; private set; }

    [Header("Pool Setup")]
    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();
    [SerializeField] private int initialPoolSizePerType = 10;
    [SerializeField, Min(0)] private int totalEnemyCount = 100;

    // Dictionary tracking pooled instances per prefab type
    private Dictionary<GameObject, List<GameObject>> pools = new Dictionary<GameObject, List<GameObject>>();

    private void Awake()
    {
        Instance = this;
        InitializePools();
    }

    private void InitializePools()
    {
        foreach (GameObject prefab in enemyPrefabs)
        {
            if (prefab == null || pools.ContainsKey(prefab)) continue;
            if (prefab.GetComponent<EnemyChase>() == null || prefab.GetComponent<NavMeshAgent>() == null)
            {
                Debug.LogWarning($"[EnemyPoolSpawner] {prefab.name} needs EnemyChase and NavMeshAgent.", this);
                continue;
            }

            List<GameObject> pool = new List<GameObject>();
            for (int i = 0; i < initialPoolSizePerType; i++)
            {
                GameObject enemy = CreateNewEnemyInstance(prefab);
                pool.Add(enemy);
            }
            pools.Add(prefab, pool);
        }
    }

    private GameObject CreateNewEnemyInstance(GameObject prefab)
    {
        GameObject enemy = Instantiate(prefab, transform);
        enemy.SetActive(false);
        return enemy;
    }

    public List<GameObject> SpawnBatch(SpawnRequest request)
    {
        List<GameObject> spawnedEnemies = new List<GameObject>();
        if (request.totalPoint <= 0f || float.IsNaN(request.totalPoint) || float.IsInfinity(request.totalPoint))
            return spawnedEnemies;

        int activeEnemyCount = 0;
        foreach (List<GameObject> pool in pools.Values)
        {
            foreach (GameObject enemy in pool)
            {
                if (enemy != null && enemy.activeInHierarchy)
                    activeEnemyCount++;
            }
        }
        if (activeEnemyCount >= totalEnemyCount)
            return spawnedEnemies;

        List<GameObject> candidates = new List<GameObject>();
        foreach (GameObject prefab in pools.Keys)
        {
            EnemyChase enemy = prefab.GetComponent<EnemyChase>();
            if (enemy.EnemyPoint >= 0.01f && enemy.EnemyPoint <= request.totalPoint &&
                enemy.EnemyDifficulty <= request.currentDifficulty)
                candidates.Add(prefab);
        }

        float remainingPoints = request.totalPoint;
        while (candidates.Count > 0 && activeEnemyCount < totalEnemyCount)
        {
            candidates.RemoveAll(prefab => prefab.GetComponent<EnemyChase>().EnemyPoint > remainingPoints);
            if (candidates.Count == 0)
                break;

            GameObject prefab = candidates[Random.Range(0, candidates.Count)];
            if (!TryFindSpawnPoint(request, prefab.GetComponent<NavMeshAgent>(), out Vector3 position))
            {
                candidates.Remove(prefab);
                continue;
            }

            GameObject enemy = GetInactiveObject(pools[prefab], prefab);
            enemy.transform.position = position;
            enemy.SetActive(true);
            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
            if (!agent.Warp(position))
            {
                enemy.SetActive(false);
                candidates.Remove(prefab);
                continue;
            }

            remainingPoints -= prefab.GetComponent<EnemyChase>().EnemyPoint;
            spawnedEnemies.Add(enemy);
            activeEnemyCount++;
        }
        return spawnedEnemies;
    }

    private bool TryFindSpawnPoint(SpawnRequest request, NavMeshAgent agent, out Vector3 position)
    {
        position = default;
        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };
        if (!NavMesh.SamplePosition(request.centerPosition, out NavMeshHit centerHit, 2f, filter))
            return false;

        float minRadius = Mathf.Max(0f, request.minRadius);
        float maxRadius = Mathf.Max(minRadius, request.maxRadius);
        NavMeshPath path = new NavMeshPath();
        for (int attempt = 0; attempt < 20; attempt++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Mathf.Sqrt(Random.Range(minRadius * minRadius, maxRadius * maxRadius));
            Vector3 sample = request.centerPosition + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            if (!NavMesh.SamplePosition(sample, out NavMeshHit hit, 2f, filter))
                continue;

            float actualRadius = Vector2.Distance(hit.position, request.centerPosition);
            if (actualRadius < minRadius || actualRadius > maxRadius)
                continue;
            if (!NavMesh.CalculatePath(hit.position, centerHit.position, filter, path) ||
                path.status != NavMeshPathStatus.PathComplete)
                continue;

            position = hit.position;
            return true;
        }
        return false;
    }

    public void Despawn(GameObject enemy)
    {
        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        enemy.SetActive(false);
    }

    private GameObject GetInactiveObject(List<GameObject> pool, GameObject prefab)
    {
        foreach (GameObject enemy in pool)
        {
            if (!enemy.activeInHierarchy)
            {
                return enemy;
            }
        }

        // Expand pool dynamically if empty
        GameObject newEnemy = CreateNewEnemyInstance(prefab);
        pool.Add(newEnemy);
        return newEnemy;
    }
}
