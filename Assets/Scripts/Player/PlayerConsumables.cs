using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerConsumables : MonoBehaviour
{
    [SerializeField] private InventoryUIController inventoryUI;
    private Player player;
    private float nextUseTime;
    private float cooldownDuration;

    private static readonly Dictionary<int, Action<Player>> handlers = new Dictionary<int, Action<Player>>
    {
        { 1, target => target.Data.Heal(20f) },
        { 2, target => target.Data.Heal(50f) },
        { 3, target => target.Data.Heal(target.Data.maxHealth) },
        { 4, target => target.Data.RestoreMP(30) },
        { 5, target => target.Data.RestoreMP(70) },
        { 6, target => target.Data.RestoreMP(target.Data.maxMP) },
        { 7, target => target.ClearPositiveEffects() },
        { 8, target => { target.ClearPositiveEffects(); target.Data.Heal(20f); target.Data.RestoreMP(20); } },
        { 9, target => { target.ClearPositiveEffects(); target.Data.Heal(target.Data.maxHealth); target.Data.RestoreMP(target.Data.maxMP); } },
        { 10, target => ApplyResistance(target, EffectRuntime.Fire, 1, 60f) },
        { 11, target => ApplyResistance(target, EffectRuntime.Fire, 2, 60f) },
        { 12, target => ApplyResistance(target, EffectRuntime.Fire, 4, 60f) },
        { 13, target => ApplyResistance(target, EffectRuntime.Freeze, 1, 60f) },
        { 14, target => ApplyResistance(target, EffectRuntime.Freeze, 2, 60f) },
        { 15, target => ApplyResistance(target, EffectRuntime.Freeze, 4, 60f) }
    };

    private static void ApplyResistance(Player target, int effectID, int level, float duration)
    {
        target.ApplyEffects(new[] { new EffectDefinition { effectID = -effectID, level = level, duration = duration } });
    }

    private void Awake()
    {
        player = GetComponent<Player>();
        ResolveInventory();
    }

    private void ResolveInventory()
    {
        if (inventoryUI == null)
            inventoryUI = FindFirstObjectByType<InventoryUIController>(FindObjectsInactive.Include);
    }

    private void Update()
    {
        UpdateCooldownUI();
        if (Time.timeScale == 0f || Keyboard.current == null)
            return;
        if (Keyboard.current.zKey.wasPressedThisFrame)
            TryUseSlot(0);
        if (Keyboard.current.xKey.wasPressedThisFrame)
            TryUseSlot(1);
        if (Keyboard.current.cKey.wasPressedThisFrame)
            TryUseSlot(2);
    }

    public bool TryUseSlot(int slotIndex)
    {
        if (!isActiveAndEnabled || Time.timeScale == 0f || player == null || !player.isActiveAndEnabled ||
            player.Data == null || player.Data.currentHealth <= 0f || Time.time < nextUseTime)
            return false;
            

        ResolveInventory();
        if (inventoryUI == null)
            return false;
        InventorySlotData slot = inventoryUI.GetSlotData(InventorySlotType.Consumable, slotIndex);
        if (slot == null || slot.count <= 0 || slot.item is not ConsumableSO consumable ||
            !handlers.TryGetValue(consumable.id, out Action<Player> handler))
            return false;

        if (!inventoryUI.TryConsumeConsumable(slotIndex))
            return false;
        handler(player);
        player.UpdatePlayerBarsFromData();
        cooldownDuration = Mathf.Max(0f, consumable.cooldown);
        nextUseTime = Time.time + cooldownDuration;
        UpdateCooldownUI();
        return true;
    }

    private void UpdateCooldownUI()
    {
        ResolveInventory();
        inventoryUI?.SetConsumableCooldown(Mathf.Max(0f, nextUseTime - Time.time), cooldownDuration);
    }

    private void OnDisable()
    {
        inventoryUI?.SetConsumableCooldown(0f, 0f);
    }
}
