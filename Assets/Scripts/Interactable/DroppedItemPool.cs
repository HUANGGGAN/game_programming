using System.Collections.Generic;
using UnityEngine;

public class DroppedItemPool : MonoBehaviour
{
    public static DroppedItemPool Instance { get; private set; }
    [SerializeField] private ItemDatabaseSO itemDatabase;
    [SerializeField] private DroppedItem prefab;
    [SerializeField] private Transform player;
    [SerializeField, Min(1)] private int maxPoolSize = 100;
    [SerializeField, Min(0f)] private float despawnDistance = 50f;
    [SerializeField, Min(0f)] private float scatterRadius = 0.6f;
    [SerializeField, Min(0f)] private float jumpHeight = 0.6f;
    [SerializeField, Min(0.01f)] private float jumpDuration = 0.6f;
    private readonly List<DroppedItem> items = new List<DroppedItem>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { enabled = false; return; }
        Instance = this;
        ResolvePlayer();
    }

    private void ResolvePlayer()
    {
        if (player != null) return;
        Player found = FindFirstObjectByType<Player>();
        if (found != null) player = found.transform;
    }

    private float Distance(Vector3 position)
    {
        Vector3 offset = position - player.position;
        return Mathf.Abs(offset.x) + Mathf.Abs(offset.y);
    }

    public void Drop(EnemySO enemy, Vector3 position)
    {
        if (!isActiveAndEnabled || enemy == null || itemDatabase == null) return;
        ItemSO item = itemDatabase.GetRandomDrop(enemy.itemDrops, enemy.categoryDrops, out int count);
        if (item == null) return;
        ResolvePlayer();
        if (player == null || Distance(position) > despawnDistance) return;

        DroppedItem available = null;
        DroppedItem furthest = null;
        float furthestDistance = -1f;
        for (int i = items.Count - 1; i >= 0; i--)
        {
            DroppedItem candidate = items[i];
            if (candidate == null) { items.RemoveAt(i); continue; }
            if (!candidate.gameObject.activeSelf) { available = candidate; continue; }
            float distance = Distance(candidate.transform.position);
            if (distance > furthestDistance) { furthestDistance = distance; furthest = candidate; }
        }
        if (available == null && items.Count < Mathf.Max(1, maxPoolSize))
        {
            if (prefab != null) available = Instantiate(prefab, transform);
            else
            {
                var root = new GameObject("Dropped Item");
                root.transform.SetParent(transform, false);
                available = root.AddComponent<DroppedItem>();
            }
            available.gameObject.SetActive(false);
            items.Add(available);
        }
        if (available == null) available = furthest;
        if (available == null) return;
        available.gameObject.SetActive(false);
        available.Launch(item, position, scatterRadius, jumpHeight, jumpDuration, count);
    }

    private void Update()
    {
        ResolvePlayer();
        if (player == null) return;
        foreach (DroppedItem item in items)
            if (item != null && item.gameObject.activeSelf && Distance(item.transform.position) > despawnDistance)
                item.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
