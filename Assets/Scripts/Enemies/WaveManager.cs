using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [SerializeField] private GameObject basicEnemyPrefab;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int enemiesPerWave = 10;
    [SerializeField] private float spawnRadius = 15f; // Spawns outside screen view

    private IEnemySpawner spawner;
    private float timer;

    private void Start()
    {
        spawner = EnemyPoolSpawner.Instance;
        
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }
    }

    private void Update()
    {
        if (playerTransform == null || spawner == null) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            TriggerWave();
        }
    }

    private void TriggerWave()
    {
        SpawnRequest request = new SpawnRequest
        {
            enemyPrefab = basicEnemyPrefab,
            count = enemiesPerWave,
            centerPosition = playerTransform.position,
            radius = spawnRadius
        };

        spawner.SpawnBatch(request);
    }
}