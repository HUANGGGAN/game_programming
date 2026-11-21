using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameplayLevel { MainGame, Tutorial }

/// <summary>Owns all access to level-specific player, inventory, and decomposition assets.</summary>
public sealed class GameDataManager : MonoBehaviour
{
    [Serializable]
    private class LevelData
    {
        public PlayerData player;
        public InventorySO inventory;
        public DecompositionInventorySO decomposition;
    }
    [SerializeField] private LevelData mainGame = new LevelData();
    [SerializeField] private LevelData tutorial = new LevelData();
    private static GameDataManager instance;
    public static GameDataManager Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<GameDataManager>(FindObjectsInactive.Include);
            if (instance == null) instance = new GameObject(nameof(GameDataManager)).AddComponent<GameDataManager>();
            return instance;
        }
    }
    public GameplayLevel ActiveLevel { get; private set; }
    private bool initialized;
    private PlayerData boundPlayer;
    private event Action moneyChanged;
    private event Action scoreChanged;
    private PlayerAPI playerAPI;
    private InventoryAPI inventoryAPI;
    private DecompositionAPI decompositionAPI;
    public PlayerAPI Player => playerAPI ??= new PlayerAPI(this);
    public InventoryAPI Inventory => inventoryAPI ??= new InventoryAPI(this);
    public DecompositionAPI Decomposition => decompositionAPI ??= new DecompositionAPI(this);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;
    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(this); return; }
        instance = this;
        DontDestroyOnLoad(transform.root.gameObject);
    }
    private LevelData Active
    {
        get
        {
            if (!initialized) SelectLevel(ActiveLevel, true);
            return ActiveLevel == GameplayLevel.MainGame ? mainGame : tutorial;
        }
    }
    public void SelectLevel(GameplayLevel level, bool newGame)
    {
        if (boundPlayer != null)
        {
            boundPlayer.OnMoneyChanged -= RaiseMoney;
            boundPlayer.OnScoreChanged -= RaiseScore;
        }
        ActiveLevel = level;
        LevelData data = level == GameplayLevel.MainGame ? mainGame : tutorial;
        data.player ??= ScriptableObject.CreateInstance<PlayerData>();
        data.inventory ??= ScriptableObject.CreateInstance<InventorySO>();
        data.decomposition ??= ScriptableObject.CreateInstance<DecompositionInventorySO>();
        // Never let an accidental Inspector assignment make the tutorial share main-game assets.
        if (level == GameplayLevel.Tutorial)
        {
            if (data.player == mainGame.player) data.player = Instantiate(data.player);
            if (data.inventory == mainGame.inventory) data.inventory = Instantiate(data.inventory);
            if (data.decomposition == mainGame.decomposition) data.decomposition = Instantiate(data.decomposition);
        }
        initialized = true;
        boundPlayer = data.player;
        boundPlayer.OnMoneyChanged += RaiseMoney;
        boundPlayer.OnScoreChanged += RaiseScore;
        if (newGame)
        {
            data.player.Reset();
            data.inventory.Clear();
            data.decomposition.Clear();
        }
        data.inventory.BindItemAcquisition(data.player.RegisterItem);
        RaiseMoney();
        RaiseScore();
    }
    private void RaiseMoney() => moneyChanged?.Invoke();
    private void RaiseScore() => scoreChanged?.Invoke();
    private void OnDestroy()
    {
        if (boundPlayer != null)
        {
            boundPlayer.OnMoneyChanged -= RaiseMoney;
            boundPlayer.OnScoreChanged -= RaiseScore;
        }
        if (instance == this) instance = null;
    }
    private static InventorySlotData Copy(InventorySlotData slot) => slot == null ? null : new InventorySlotData
    { item = slot.item, count = slot.count, ammoCount = slot.ammoCount, attackModeIndex = slot.attackModeIndex };
    private static InventorySlotData[] Copy(InventorySlotData[] slots)
    {
        var copy = new InventorySlotData[slots.Length];
        for (int i = 0; i < slots.Length; i++) copy[i] = Copy(slots[i]);
        return copy;
    }
    private static InventorySlotData At(InventorySlotData[] slots, int index) =>
        index >= 0 && index < slots.Length ? slots[index] : null;

    private static bool QuickTransfer(InventorySlotData[] source, int index, InventorySlotData[] destination, Func<int, bool> canMove)
    {
        InventorySlotData item = At(source, index);
        if (item == null || item.item == null || item.count <= 0) return false;
        bool moved = false;
        for (int pass = 0; pass < 2 && item.count > 0; pass++)
            for (int i = 0; i < destination.Length && item.count > 0; i++)
            {
                if (!canMove(i) || (pass == 0 ? destination[i].item != item.item : destination[i].item != null)) continue;
                moved |= InventorySlotData.TryStackInto(item, destination[i]);
            }
        return moved;
    }
    private static bool Split(InventorySlotData[] slots, int index)
    {
        InventorySlotData source = At(slots, index);
        foreach (InventorySlotData destination in slots)
            if (InventorySlotData.TrySplitHalf(source, destination)) return true;
        return false;
    }

    public sealed class PlayerAPI
    {
        private readonly GameDataManager owner;
        internal PlayerAPI(GameDataManager owner) { this.owner = owner; }
        private PlayerData Data => owner.Active.player;
        public int currentScore => Data.currentScore;
        public int money => Data.money;
        public int killCount => Data.killCount;
        public int uniqueWeapon => Data.uniqueWeapon;
        public float currentHealth => Data.currentHealth;
        public float maxHealth => Data.maxHealth;
        public int currentMP => Data.currentMP;
        public int maxMP => Data.maxMP;
        public event Action OnMoneyChanged { add => owner.moneyChanged += value; remove => owner.moneyChanged -= value; }
        public event Action OnScoreChanged { add => owner.scoreChanged += value; remove => owner.scoreChanged -= value; }
        public void AddKill() => Data.AddKill();
        public void RegisterItem(ItemSO item) => Data.RegisterItem(item);
        public void AddScore(int amount) => Data.AddScore(amount);
        public void SetScore(int amount) => Data.SetScore(amount);
        public void AddMoney(int amount) => Data.AddMoney(amount);
        public void SetMoney(int amount) => Data.SetMoney(amount);
        public void SetMaxHealth(float amount) => Data.SetMaxHealth(amount);
        public void ApplyDamage(float amount) => Data.ApplyDamage(amount);
        public void Heal(float amount) => Data.Heal(amount);
        public void SetMaxMP(int amount) => Data.SetMaxMP(amount);
        public void UseMP(int amount) => Data.UseMP(amount);
        public bool TryUseMP(int amount) => Data.TryUseMP(amount);
        public void RestoreMP(int amount) => Data.RestoreMP(amount);
    }

    public sealed class InventoryAPI
    {
        private readonly GameDataManager owner;
        internal InventoryAPI(GameDataManager owner) { this.owner = owner; }
        private InventorySO Data => owner.Active.inventory;
        // Slot reads are snapshots; all mutations go through the operations below.
        public InventorySlotData[] slots => GetSlotsForType(InventorySlotType.Universal);
        public InventorySlotData[] weaponSlots => GetSlotsForType(InventorySlotType.Weapon);
        public InventorySlotData[] armorSlots => GetSlotsForType(InventorySlotType.Armor);
        public InventorySlotData[] consumableSlots => GetSlotsForType(InventorySlotType.Consumable);
        public InventorySlotData[] GetSlotsForType(InventorySlotType type) { Data.EnsureDefaults(); return Copy(Data.GetSlotsForType(type)); }
        public InventorySlotData GetSlot(InventorySlotType type, int index)
        {
            Data.EnsureDefaults();
            return Copy(At(Data.GetSlotsForType(type), index));
        }
        public bool MatchesSlot(InventorySlotType type, int index, InventorySlotData snapshot)
        {
            Data.EnsureDefaults();
            InventorySlotData slot = At(Data.GetSlotsForType(type), index);
            return slot != null && snapshot != null && slot.item == snapshot.item && slot.count == snapshot.count &&
                slot.ammoCount == snapshot.ammoCount && slot.attackModeIndex == snapshot.attackModeIndex;
        }
        public void EnsureDefaults() => Data.EnsureDefaults();
        public void RegisterOwnedWeapons() => Data.RegisterOwnedWeapons();
        public void RemoveItemsWithNoCount() => Data.RemoveItemsWithNoCount();
        public bool CanGiveItem(ItemSO item, InventorySlotType type, int count = 1) => Data.CanGiveItem(item, type, count);
        public bool TryGiveItem(ItemSO item, InventorySlotType type, out int given, int count = 1, int ammoCount = 0) => Data.TryGiveItem(item, type, out given, count, ammoCount);
        public bool TryExchangeItems(ItemSO currency, int price, ItemSO item, int count, int ammo = 0) => Data.TryExchangeItems(currency, price, item, count, ammo);
        public bool TryDropFromSlot(InventorySlotType type, int index) => Data.TryDropFromSlot(type, index);
        public int GetItemCount(ItemSO item) => Data.GetItemCount(item);
        public bool TryConsumeConsumable(int index) => Data.TryConsumeConsumable(index);
        public bool TryRemoveItemAmount(ItemSO item, int count) => Data.TryRemoveItemAmount(item, count);
        public bool TryQuickMove(InventorySlotType type, int index, out InventorySlotType to, out int destination) => Data.TryQuickMove(type, index, out to, out destination);
        public bool TrySplitUniversal(int index) => Data.TrySplitUniversal(index);
        public bool TrySwapSlots(InventorySlotType from, int index, InventorySlotType to, int destination) => Data.TrySwapSlots(from, index, to, destination);
        public bool TryConsumeWeaponAmmo(int index) => Data.TryConsumeWeaponAmmo(index);
        public bool TrySwitchWeaponAttackMode(int index) => Data.TrySwitchWeaponAttackMode(index);
        public bool CanPlaceItem(InventorySlotType type, ItemSO item) => Data.CanPlaceItem(type, item);
        public InventorySlotType GetPreferredSlotType(ItemSO item, InventorySlotType fallback) => Data.GetPreferredSlotType(item, fallback);
        public WeaponSO ResolveWeapon(int index) { Data.EnsureDefaults(); return At(Data.weaponSlots, index)?.ResolveWeapon(); }
        public bool ReloadWeapon(int index, WeaponSO weapon, int capacity)
        {
            Data.EnsureDefaults();
            InventorySlotData slot = At(Data.weaponSlots, index);
            if (slot == null || capacity <= 0 || slot.count <= 0 || slot.ResolveWeapon() != weapon) return false;
            slot.ammoCount = capacity;
            return true;
        }
        public void RestoreWeaponState(int index, int mode, int ammo)
        {
            InventorySlotData slot = At(Data.weaponSlots, index);
            if (slot != null) { slot.attackModeIndex = mode; slot.ammoCount = ammo; }
        }
        public InventorySlotData GetStorageSlot(StorageSO storage, bool side, int index)
        {
            if (storage == null) return null;
            storage.EnsureDefaults(); Data.EnsureDefaults();
            return Copy(At(side ? storage.slots : Data.slots, index));
        }
        public bool CanStorageMove(StorageSO storage, bool fromSide, int from, bool toSide, int to) =>
            storage != null && storage.CanMove(Data, fromSide, from, toSide, to);
        public bool MoveStorage(StorageSO storage, bool fromSide, int from, bool toSide, int to) =>
            storage != null && storage.TryMove(Data, fromSide, from, toSide, to);
        public bool QuickStorageMove(StorageSO storage, bool side, int index)
        {
            if (storage == null) return false;
            storage.EnsureDefaults(); Data.EnsureDefaults();
            return QuickTransfer(side ? storage.slots : Data.slots, index, side ? Data.slots : storage.slots,
                to => storage.CanMove(Data, side, index, !side, to));
        }
        public bool SplitStorage(StorageSO storage, bool side, int index)
        {
            if (storage == null) return false;
            storage.EnsureDefaults(); Data.EnsureDefaults();
            return Split(side ? storage.slots : Data.slots, index);
        }
    }

    public sealed class DecompositionAPI
    {
        private readonly GameDataManager owner;
        internal DecompositionAPI(GameDataManager owner) { this.owner = owner; }
        private DecompositionInventorySO Data => owner.Active.decomposition;
        private InventorySO Inventory => owner.Active.inventory;
        public InventorySlotData[] slots { get { Data.EnsureDefaults(); return Copy(Data.slots); } }
        public void EnsureDefaults() => Data.EnsureDefaults();
        public bool CanMove(bool side, int from, bool toSide, int to) => Data.CanMove(Inventory, side, from, toSide, to);
        public bool TryMove(bool side, int from, bool toSide, int to) => Data.TryMove(Inventory, side, from, toSide, to);
        public bool TryDecompose(ItemSO[] pieces) => Data.TryDecompose(pieces);
        public bool QuickMove(bool side, int index)
        {
            Data.EnsureDefaults(); Inventory.EnsureDefaults();
            return QuickTransfer(side ? Data.slots : Inventory.slots, index, side ? Inventory.slots : Data.slots,
                to => Data.CanMove(Inventory, side, index, !side, to));
        }
        public bool SplitStack(bool side, int index)
        {
            Data.EnsureDefaults(); Inventory.EnsureDefaults();
            return Split(side ? Data.slots : Inventory.slots, index);
        }
    }
}
