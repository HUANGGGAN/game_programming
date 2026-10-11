using System.Collections.Generic;
using UnityEngine;

// A point budget shared by randomly selected, difficulty-eligible enemy types.
public struct SpawnRequest
{
    public Vector3 centerPosition;
    public float minRadius;
    public float maxRadius;
    public float totalPoint;
    public float currentDifficulty;
}

public interface IEnemySpawner
{
    /// <summary>
    /// Spawns a batch of enemies based on a spawn request configuration.
    /// </summary>
    List<GameObject> SpawnBatch(SpawnRequest request);

    /// <summary>
    /// Despawns an enemy back into the pool.
    /// </summary>
    void Despawn(GameObject enemy);
}