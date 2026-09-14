using System.Collections.Generic;
using UnityEngine;

// Structure defining what to spawn, how many, and where
public struct SpawnRequest
{
    public GameObject enemyPrefab; // Type of enemy
    public int count;               // How many to spawn
    public Vector3 centerPosition;  // Center point of spawn area
    public float radius;            // Radial spread range
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