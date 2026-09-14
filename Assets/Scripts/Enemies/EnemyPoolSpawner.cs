using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyPoolSpawner : MonoBehaviour, IEnemySpawner
{
    public static EnemyPoolSpawner Instance { get; private set; }

    [Header("Pool Setup")]
    [SerializeField] private List<GameObject> enemyPrefabs;
    [SerializeField] private int initialPoolSizePerType = 10;

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
            if (prefab == null) continue;

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

        if (!pools.ContainsKey(request.enemyPrefab))
        {
            Debug.LogWarning($"[EnemyPoolSpawner] Prefab '{request.enemyPrefab.name}' is not in pool registry.");
            return spawnedEnemies;
        }

        List<GameObject> pool = pools[request.enemyPrefab];

        for (int i = 0; i < request.count; i++)
        {
            GameObject enemy = GetInactiveObject(pool, request.enemyPrefab);
            
            // Calculate a random spawn point around the center within radius
            Vector3 randomPoint = request.centerPosition + (Vector3)(Random.insideUnitCircle * request.radius);

            // Snap the spawn point to the nearest valid NavMesh location
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.Warp(hit.position); // Snap NavMeshAgent cleanly
                }
                else
                {
                    enemy.transform.position = hit.position;
                }

                enemy.SetActive(true);
                spawnedEnemies.Add(enemy);
            }
        }

        return spawnedEnemies;
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