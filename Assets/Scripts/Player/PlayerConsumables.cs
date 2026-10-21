using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerConsumables : MonoBehaviour
{
    [SerializeField] private InventoryUIController inventoryUI;
    private Player player;

    private static readonly Dictionary<int, Action<Player>> handlers = new Dictionary<int, Action<Player>>
    {
        { 1, target => target.Data.Heal(20f) },
        { 2, target => target.ClearEffects() },
        { 3, target => target.Data.RestoreMP(20) }
    };

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
            player.Data == null || player.Data.currentHealth <= 0f)
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
        return true;
    }
}
