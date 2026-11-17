using UnityEngine;

public class IceStatuePuzzle : MonoBehaviour
{
    [SerializeField, Tooltip("Statues in deactivation order, 1 through 5. Auto-found by name if empty.")]
    private IceStatue[] statues;
    [SerializeField] private GameObject treasure;
    [SerializeField] private Transform player;
    [SerializeField] private RegionEnemySpawner regionEnemySpawner;
    [SerializeField, Min(0f)] private float activeDistance = 30f;
    [SerializeField, Min(0f)] private float enemyPoints = 5f;
    [SerializeField, Min(0f)] private float punishmentEnemyPoints = 10f;
    private int nextStatue;
    public bool IsSolved => statues != null && statues.Length > 0 && nextStatue == statues.Length;

    private void Awake()
    {
        if (statues == null || statues.Length == 0)
        {
            statues = GetComponentsInChildren<IceStatue>(true);
            System.Array.Sort(statues, (a, b) => string.CompareOrdinal(a.name, b.name));
        }
        foreach (IceStatue statue in statues)
            if (statue != null) statue.Initialize(this);
        if (treasure == null)
        {
            Transform child = transform.Find("Treasure");
            if (child != null) treasure = child.gameObject;
        }
        if (treasure != null) treasure.SetActive(false);
    }

    private void Update()
    {
        if (player == null)
        {
            Player found = FindFirstObjectByType<Player>();
            if (found != null) player = found.transform;
        }
        bool nearby = player != null &&
            ((Vector2)(player.position - transform.position)).sqrMagnitude <= activeDistance * activeDistance;
        foreach (IceStatue statue in statues)
        {
            if (statue == null) continue;
            statue.enabled = nearby && !statue.IsDeactivated;
            if (nearby && statue.gameObject.activeInHierarchy) statue.Tick(Time.deltaTime);
        }
    }

    public bool TryDeactivate(IceStatue statue, Transform interactingPlayer)
    {
        if (IsSolved || interactingPlayer == null || nextStatue >= statues.Length) return false;
        if (statue != statues[nextStatue])
        {
            nextStatue = 0;
            foreach (IceStatue entry in statues)
                if (entry != null)
                {
                    entry.Revive();
                    entry.enabled = true;
                }
            statue.FlashWrongOrder();
            SpawnEnemies(Mathf.Max(enemyPoints, punishmentEnemyPoints));
            return true;
        }
        statue.BeginDeactivation(interactingPlayer);
        SpawnEnemies(enemyPoints);
        return true;
    }

    public void CompleteDeactivation(IceStatue statue)
    {
        if (IsSolved || nextStatue >= statues.Length || statues[nextStatue] != statue) return;
        statue.CompleteDeactivation();
        nextStatue++;
        if (IsSolved && treasure != null) treasure.SetActive(true);
    }

    private void SpawnEnemies(float points)
    {
        if (regionEnemySpawner == null)
            regionEnemySpawner = FindFirstObjectByType<RegionEnemySpawner>(FindObjectsInactive.Include);
        if (regionEnemySpawner != null) regionEnemySpawner.SpawnNow(points);
    }

    private void OnDisable()
    {
        if (statues == null) return;
        foreach (IceStatue statue in statues)
            if (statue != null) statue.enabled = false;
    }
}
