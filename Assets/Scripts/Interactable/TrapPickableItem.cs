using UnityEngine;

public class TrapPickableItem : PickableItem
{
    [Header("Trap")]
    [SerializeField, Min(0f)] private float enemyPoints = 5f;
    [SerializeField] private RegionEnemySpawner regionEnemySpawner;
    private bool triggered;

    public override bool TryInteract()
    {
        if (!base.TryInteract())
            return false;

        // A partially collected stack can be picked up again without retriggering.
        if (!triggered)
        {
            triggered = true;
            if (regionEnemySpawner == null)
                regionEnemySpawner = FindFirstObjectByType<RegionEnemySpawner>(FindObjectsInactive.Include);
            if (regionEnemySpawner != null)
                regionEnemySpawner.SpawnNow(enemyPoints);
        }
        return true;
    }
}
