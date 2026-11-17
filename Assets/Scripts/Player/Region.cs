using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum RegionType
{
    None = 0,
    Water = 1,
    Lava = 2,
    Lavarock = 4,
    Fire = 8,
    Frozen = 16,
    Home = 32
}

/// <summary>Attach to a region object with one or more 2D colliders, normally triggers.</summary>
public class Region : MonoBehaviour
{
    [SerializeField] private RegionType regionType = RegionType.Water;
    public RegionType Type => regionType;
    public void SetType(RegionType type) => regionType = type;
    internal static readonly HashSet<Region> ActiveRegions = new HashSet<Region>();
    private Collider2D[] regionColliders;
    private readonly List<Collider2D> overlaps = new List<Collider2D>();

    private void OnEnable()
    {
        regionColliders = GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D area in regionColliders)
            if (area is UnityEngine.Tilemaps.TilemapCollider2D tilemap &&
                area.GetComponentInParent<Region>() == this && tilemap.hasTilemapChanges)
                tilemap.ProcessTilemapChanges();
        foreach (Collider2D area in regionColliders)
        {
            if (area is CompositeCollider2D composite && area.GetComponentInParent<Region>() == this)
            {
                // A region is a filled area, not just the edges of its tilemap.
                composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
                composite.isTrigger = true;
                if (composite.generationType == CompositeCollider2D.GenerationType.Manual)
                    composite.GenerateGeometry();
            }
        }
        ActiveRegions.Add(this);
    }

    private void OnDisable() => ActiveRegions.Remove(this);

    internal bool ContainsAny(Collider2D[] playerColliders)
    {
        foreach (Collider2D area in regionColliders)
        {
            if (area == null || !area.enabled || !area.gameObject.activeInHierarchy || area.GetComponentInParent<Region>() != this) continue;
            foreach (Collider2D playerCollider in playerColliders)
            {
                if (playerCollider == null || playerCollider.isTrigger || !playerCollider.enabled || !playerCollider.gameObject.activeInHierarchy) continue;
                // Query from the player shape: this also handles tilemaps and composites
                // whose geometry cannot be represented by a single convex distance test.
                ContactFilter2D filter = ContactFilter2D.noFilter;
                filter.useTriggers = true;
                overlaps.Clear();
                playerCollider.Overlap(filter, overlaps);
                if (overlaps.Contains(area)) return true;
                if (area is not CompositeCollider2D && area is not UnityEngine.Tilemaps.TilemapCollider2D)
                {
                    ColliderDistance2D distance = Physics2D.Distance(area, playerCollider);
                    if (distance.isValid && (distance.isOverlapped || distance.distance <= 0.001f)) return true;
                }
            }
        }
        return false;
    }
}
