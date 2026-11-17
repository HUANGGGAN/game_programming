using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Reusable pooled spawning from the beginning to the end of a straight path.</summary>
public class PathSpawner : MonoBehaviour
{
    public static PathSpawner Instance { get; private set; }
    private readonly Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
    private readonly List<ActivePiece> active = new List<ActivePiece>();
    private Transform inactiveRoot;
    private struct ActivePiece { public GameObject prefab, instance; public float expires; }

    public static PathSpawner GetOrCreate()
    {
        if (Instance == null)
            new GameObject("Path Spawner").AddComponent<PathSpawner>();
        return Instance;
    }

    private void Awake()
    {
        Instance = this;
        GameObject inactive = new GameObject("Inactive Pieces");
        inactive.transform.SetParent(transform, false);
        inactive.SetActive(false);
        inactiveRoot = inactive.transform;
    }

    public void SummonPath(GameObject prefab, Vector2 origin, Vector2 direction, float length,
        float spacing, float interval, float lifetime, Action<GameObject, int> configure = null)
    {
        if (prefab == null || !isActiveAndEnabled || direction.sqrMagnitude <= Mathf.Epsilon) return;
        StartCoroutine(SpawnPath(prefab, origin, direction.normalized, Mathf.Max(0f, length),
            Mathf.Max(0.01f, spacing), Mathf.Max(0f, interval), Mathf.Max(0.01f, lifetime), configure));
    }

    private IEnumerator SpawnPath(GameObject prefab, Vector2 origin, Vector2 direction, float length,
        float spacing, float interval, float lifetime, Action<GameObject, int> configure)
    {
        int segments = Mathf.CeilToInt(length / spacing);
        WaitForSeconds delay = interval > 0f ? new WaitForSeconds(interval) : null;
        for (int i = 0; i <= segments; i++)
        {
            if (i > 0 && delay != null) yield return delay;
            if (prefab == null) yield break;
            GameObject piece = Acquire(prefab);
            piece.transform.position = origin + direction * Mathf.Min(length, i * spacing);
            piece.transform.rotation = prefab.transform.rotation;
            piece.transform.localScale = prefab.transform.localScale;
            configure?.Invoke(piece, i);
            piece.SetActive(true);
            active.Add(new ActivePiece { prefab = prefab, instance = piece, expires = Time.time + lifetime });
        }
    }

    private GameObject Acquire(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            pools.Add(prefab, pool);
        }
        GameObject piece = null;
        while (pool.Count > 0 && piece == null) piece = pool.Dequeue();
        // Instantiate inactive so configuration happens before Region.OnEnable registers it.
        if (piece == null) piece = Instantiate(prefab, inactiveRoot);
        piece.SetActive(false);
        piece.transform.SetParent(transform, false);
        return piece;
    }

    private void Update()
    {
        for (int i = active.Count - 1; i >= 0; i--)
            if (active[i].instance == null || Time.time >= active[i].expires) Recycle(i);
    }

    private void Recycle(int index, bool reparent = true)
    {
        ActivePiece piece = active[index];
        active.RemoveAt(index);
        if (piece.instance == null) return;
        piece.instance.SetActive(false);
        // Hierarchy changes are forbidden while Unity activates/deactivates our parent.
        if (reparent) piece.instance.transform.SetParent(inactiveRoot, false);
        if (piece.prefab != null && pools.TryGetValue(piece.prefab, out Queue<GameObject> pool)) pool.Enqueue(piece.instance);
        else Destroy(piece.instance);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        for (int i = active.Count - 1; i >= 0; i--) Recycle(i, false);
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }
}
