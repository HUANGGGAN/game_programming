using UnityEngine;
using UnityEngine.Serialization;

public class WaveManager : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField, Range(1f, 10f)] private float baseDifficulty = 1f;
    [FormerlySerializedAs("spawnInterval")]
    [SerializeField, Min(0.01f)] private float spawnPeriod = 3f;
    [SerializeField, Min(0f)] private float totalPoint = 1f;
    [SerializeField, Min(0f)] private float minRadius = 10f;
    [FormerlySerializedAs("spawnRadius")]
    [SerializeField, Min(0f)] private float maxRadius = 15f;

    private IEnemySpawner spawner;
    private float timer;
    private float elapsedTime;

    public float CurrentDifficulty => Mathf.Lerp(Mathf.Clamp(baseDifficulty, 1f, 10f), 10f,
        Mathf.Clamp01(elapsedTime / 200f));

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
        elapsedTime += Time.deltaTime;
        if (spawner == null)
            spawner = EnemyPoolSpawner.Instance;
        if (playerTransform == null || spawner == null)
            return;

        timer += Time.deltaTime;
        float period = Mathf.Max(0.01f, spawnPeriod);
        if (timer >= period)
        {
            timer %= period;
            float currentDifficulty = CurrentDifficulty;
            spawner.SpawnBatch(new SpawnRequest
            {
                centerPosition = playerTransform.position,
                minRadius = minRadius,
                maxRadius = maxRadius,
                totalPoint = totalPoint * currentDifficulty,
                currentDifficulty = currentDifficulty
            });
        }
    }
}
