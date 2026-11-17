using UnityEngine;

public class TrapChest : Chest
{
    [Header("Trap")]
    [SerializeField] private EffectDefinition[] playerEffects;
    [SerializeField, Min(0f)] private float enemyPoints = 5f;
    [SerializeField] private RegionEnemySpawner regionEnemySpawner;

    protected override void OnOpened(Player player)
    {
        base.OnOpened(player);
        if (player != null)
            player.ApplyEffects(playerEffects);

        if (regionEnemySpawner == null)
            regionEnemySpawner = FindFirstObjectByType<RegionEnemySpawner>(FindObjectsInactive.Include);
        if (regionEnemySpawner != null)
            regionEnemySpawner.SpawnNow(enemyPoints);
    }
}
