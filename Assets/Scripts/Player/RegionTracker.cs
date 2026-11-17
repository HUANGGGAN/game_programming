using System;
using UnityEngine;

/// <summary>Tracks all region volumes touching the player's solid colliders, including overlapping regions.</summary>
public class RegionTracker : MonoBehaviour
{
    private Collider2D[] playerColliders;
    private Player player;
    [SerializeField] private RegionEnemySpawner regionEnemySpawner;
    private float nextEffectRefresh;
    private const float EffectRefreshInterval = 1f;
    public RegionType CurrentRegions { get; private set; }
    public bool IsInWater => IsInRegion(RegionType.Water);
    public float MovementMultiplier => IsInRegion(RegionType.Lava) ? 0.3f : IsInWater ? 0.6f : 1f;
    public event Action<RegionType> RegionsChanged;

    public bool IsInRegion(RegionType region) => region != RegionType.None && (CurrentRegions & region) == region;

    private void OnEnable()
    {
        playerColliders = GetComponentsInChildren<Collider2D>(true);
        player = GetComponent<Player>();
        if (regionEnemySpawner == null)
            regionEnemySpawner = FindFirstObjectByType<RegionEnemySpawner>(FindObjectsInactive.Include);
        nextEffectRefresh = 0f;
        RefreshRegions();
    }

    private void FixedUpdate() => RefreshRegions();

    public void RefreshRegions()
    {
        if (!isActiveAndEnabled) return;
        RegionType regions = RegionType.None;
        if (playerColliders == null) playerColliders = GetComponentsInChildren<Collider2D>(true);
        foreach (Region region in Region.ActiveRegions)
            if (region != null && region.isActiveAndEnabled && region.ContainsAny(playerColliders)) regions |= region.Type;
        bool changed = CurrentRegions != regions;
        SetRegions(regions);
        UpdateRegionEffects(changed);
    }

    private void UpdateRegionEffects(bool regionsChanged)
    {
        if (player == null || player.Data == null || player.Data.currentHealth <= 0f) return;
        if (IsInWater) player.RemoveEffect(EffectRuntime.Fire);
        if (!regionsChanged && Time.time < nextEffectRefresh) return;
        nextEffectRefresh = Time.time + EffectRefreshInterval;

        // Water extinguishes every fire level, including fire from overlapping regions.
        if (!IsInWater)
        {
            if (IsInRegion(RegionType.Lava)) ApplyHazard(EffectRuntime.Fire, 4, 5f);
            else if (IsInRegion(RegionType.Lavarock)) ApplyHazard(EffectRuntime.Fire, 2, 2f);
            else if (IsInRegion(RegionType.Fire)) ApplyHazard(EffectRuntime.Fire, 1, 5f);
        }
        if (IsInRegion(RegionType.Frozen)) ApplyHazard(EffectRuntime.Freeze, 1, 5f);
    }

    private void ApplyHazard(int effectID, int level, float duration)
    {
        player.ApplyEffects(new[] { new EffectDefinition { effectID = effectID, level = level, duration = duration } },
            stackMatchingLevels: false);
    }

    private void SetRegions(RegionType regions)
    {
        if (CurrentRegions == regions) return;
        bool wasHome = IsInRegion(RegionType.Home);
        CurrentRegions = regions;
        bool isHome = IsInRegion(RegionType.Home);
        if (wasHome != isHome)
        {
            if (regionEnemySpawner != null) regionEnemySpawner.SetHomeRegionActive(isHome);
            if (isHome) EnemyPoolSpawner.Instance?.DespawnAll();
            if (isHome && player != null && player.Data != null && !player.IsDying)
            {
                player.Data.Heal(player.Data.maxHealth);
                player.Data.RestoreMP(player.Data.maxMP);
                player.UpdatePlayerBarsFromData();
            }
        }
        if (wasHome && !isHome && isActiveAndEnabled && player != null && !player.IsDying)
            Tutorial.RequestTutorial("leave-home", "Find the tree boss and kill it.\nFeel free to explore the world and arm yourself.\nReturn home to restore HP/MP!");
        RegionsChanged?.Invoke(regions);
    }

    private void OnDisable() => SetRegions(RegionType.None);
}
