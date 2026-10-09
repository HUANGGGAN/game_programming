using UnityEngine;

public class InventoryUIController : MonoBehaviour
{
    [SerializeField] private InventorySO inventoryData;
    [SerializeField] private PlayerShooter2D playerShooter;
    [SerializeField] private Transform universalSlotGridTransform;
    [SerializeField] private Transform weaponSlotGridTransform;
    [SerializeField] private Transform weaponSlotPanelTransform;
    [SerializeField] private Transform armorSlotGridTransform;
    [SerializeField] private Transform consumableSlotGridTransform;

    private InventorySlot[] universalSlots;
    private InventorySlot[] weaponSlots;
    private WeaponSlot[] weaponGuiSlots;
    private InventorySlot[] armorSlots;
    private InventorySlot[] consumableSlots;
    private int equippedWeaponIndex = -1;
    private CanvasGroup weaponPanelCanvasGroup;
    private float weaponPanelVisibleAlpha = 1f;
    private bool weaponPanelWasInteractable;
    private bool weaponPanelBlockedRaycasts;
    private bool weaponPanelCanvasGroupStateCached;

    private void Awake()
    {
        if (inventoryData != null)
            inventoryData.EnsureDefaults();

        if (playerShooter == null)
            playerShooter = FindObjectOfType<PlayerShooter2D>();

        if (universalSlotGridTransform != null)
        {
            universalSlots = universalSlotGridTransform.GetComponentsInChildren<InventorySlot>(true);
        }

        if (weaponSlotGridTransform != null)
        {
            weaponSlots = weaponSlotGridTransform.GetComponentsInChildren<InventorySlot>(true);
        }

        if (weaponSlotPanelTransform != null)
        {
            weaponGuiSlots = weaponSlotPanelTransform.GetComponentsInChildren<WeaponSlot>(true);
            CacheWeaponSlotCanvasGroup();
        }

        if (armorSlotGridTransform != null)
        {
            armorSlots = armorSlotGridTransform.GetComponentsInChildren<InventorySlot>(true);
        }

        if (consumableSlotGridTransform != null)
        {
            consumableSlots = consumableSlotGridTransform.GetComponentsInChildren<InventorySlot>(true);
        }
    }

    private void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (inventoryData == null)
            return;

        inventoryData.EnsureDefaults();
        inventoryData.RemoveItemsWithNoCount();

        RefreshUniversalSlots();
        RefreshArmorSlots();
        RefreshConsumableSlots();
        ReapplyEquippedWeapon(); // Weapons could change while the inventory is closed, so reapply the equipped weapon when refreshing the UI
        RefreshWeaponSlots();
        playerShooter?.RefreshWeaponAmmoTexts();
    }

    public void SetEquippedWeapon(int slotIndex)
    {
        if (!IsWeaponAtSlot(slotIndex))
        {
            if (playerShooter != null)
                playerShooter.UnequipWeapon();

            equippedWeaponIndex = -1;
            RefreshWeaponSlots();
            playerShooter?.RefreshWeaponAmmoTexts();
            return;
        }

        equippedWeaponIndex = slotIndex;
        if (!EquipShooterForSelectedWeapon())
        {
            equippedWeaponIndex = -1;
            if (playerShooter != null)
                playerShooter.UnequipWeapon();
        }

        RefreshWeaponSlots();
        playerShooter?.RefreshWeaponAmmoTexts();
    }

    public int GetEquippedWeaponIndex()
    {
        return equippedWeaponIndex;
    }

    private void ReapplyEquippedWeapon()
    {
        if (equippedWeaponIndex < 0)
            return;

        if (!IsWeaponAtSlot(equippedWeaponIndex))
        {
            equippedWeaponIndex = -1;
            if (playerShooter != null)
                playerShooter.UnequipWeapon();
            return;
        }

        if (!EquipShooterForSelectedWeapon())
        {
            equippedWeaponIndex = -1;
            if (playerShooter != null)
                playerShooter.UnequipWeapon();
        }
    }

    private bool IsWeaponAtSlot(int slotIndex)
    {
        if (inventoryData == null || slotIndex < 0 || slotIndex >= inventoryData.weaponSlots.Length)
            return false;

        return inventoryData.weaponSlots[slotIndex].item is WeaponSO;
    }

    private bool EquipShooterForSelectedWeapon()
    {
        if (playerShooter == null)
            return true;

        if (inventoryData.weaponSlots[equippedWeaponIndex].item is WeaponSO weapon)
            return playerShooter.EquipWeapon(weapon, this);

        playerShooter.UnequipWeapon();
        return true;
    }

    public void SetWeaponSlotPanelVisible(bool visible) // Toggle
    {
        if (weaponSlotPanelTransform == null)
            return;

        CacheWeaponSlotCanvasGroup();
        weaponPanelCanvasGroup.alpha = visible ? weaponPanelVisibleAlpha : 0f;
        weaponPanelCanvasGroup.interactable = visible && weaponPanelWasInteractable;
        weaponPanelCanvasGroup.blocksRaycasts = visible && weaponPanelBlockedRaycasts;
    }

    private void CacheWeaponSlotCanvasGroup()
    {
        if (weaponSlotPanelTransform == null)
            return;

        if (weaponPanelCanvasGroup == null)
        {
            weaponPanelCanvasGroup = weaponSlotPanelTransform.GetComponent<CanvasGroup>();
            if (weaponPanelCanvasGroup == null)
            {
                weaponPanelCanvasGroup = weaponSlotPanelTransform.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (weaponPanelCanvasGroupStateCached)
            return;

        weaponPanelVisibleAlpha = weaponPanelCanvasGroup.alpha;
        weaponPanelWasInteractable = weaponPanelCanvasGroup.interactable;
        weaponPanelBlockedRaycasts = weaponPanelCanvasGroup.blocksRaycasts;
        weaponPanelCanvasGroupStateCached = true;
    }

    private void RefreshUniversalSlots()
    {
        if (universalSlots == null)
            return;

        for (int i = 0; i < universalSlots.Length; i++)
        {
            ItemSO item = i < inventoryData.slots.Length ? inventoryData.slots[i].item : null;
            universalSlots[i].SetupSlot(item, i);
            universalSlots[i].SetSlotType(InventorySlotType.Universal);
        }
    }

    private void RefreshWeaponSlots()
    {
        if (weaponSlots == null)
            return;

        for (int i = 0; i < weaponSlots.Length; i++)
        {
            ItemSO item = i < inventoryData.weaponSlots.Length ? inventoryData.weaponSlots[i].item : null;
            weaponSlots[i].SetupSlot(item, i);
            weaponSlots[i].SetSlotType(InventorySlotType.Weapon);
        }

        if (weaponGuiSlots != null)
        {
            for (int i = 0; i < weaponGuiSlots.Length; i++)
            {
                ItemSO item = i < inventoryData.weaponSlots.Length ? inventoryData.weaponSlots[i].item : null;
                weaponGuiSlots[i].SetWeaponIcon(item != null ? item.icon : null);
                bool isEquipped = i == equippedWeaponIndex;
                weaponGuiSlots[i].SetEquipped(isEquipped);
            }
        }
    }

    private void RefreshArmorSlots()
    {
        if (armorSlots == null)
            return;

        for (int i = 0; i < armorSlots.Length; i++)
        {
            ItemSO item = i < inventoryData.armorSlots.Length ? inventoryData.armorSlots[i].item : null;
            armorSlots[i].SetupSlot(item, i);
            armorSlots[i].SetSlotType(InventorySlotType.Armor);
        }
    }

    private void RefreshConsumableSlots()
    {
        if (consumableSlots == null)
            return;

        for (int i = 0; i < consumableSlots.Length; i++)
        {
            ItemSO item = i < inventoryData.consumableSlots.Length ? inventoryData.consumableSlots[i].item : null;
            consumableSlots[i].SetupSlot(item, i);
            consumableSlots[i].SetSlotType(InventorySlotType.Consumable);
        }
    }

    // For InventoryManager scrolling and equipping weapons
    public ItemSO GetItemAtSlot(int slotIndex)
    {
        if (inventoryData == null)
            return null;

        if (slotIndex >= 0 && slotIndex < inventoryData.weaponSlots.Length)
            return inventoryData.weaponSlots[slotIndex].item;

        return null;
    }
    public bool GiveItem(ItemSO item, InventorySlotType type, int count = 1, int ammoCount = 0)
    {
        if (inventoryData == null)
            return false;

        if (item == null || count <= 0)
            return false;

        bool success = inventoryData.TryGiveItem(item, type, count, ammoCount);
        if (success)
            RefreshUI();

        return success;
    }

    public bool DropFromSlot(InventorySlotType slotType, int slotIndex)
    {
        if (inventoryData == null)
            return false;

        bool success = inventoryData.TryDropFromSlot(slotType, slotIndex);
        if (success)
            RefreshUI();

        return success;
    }

    public bool RemoveItemAmount(ItemSO item, int amount)
    {
        if (inventoryData == null || item == null || amount <= 0)
            return false;

        bool success = inventoryData.TryRemoveItemAmount(item, amount);
        if (success)
            RefreshUI();

        return success;
    }
    public int GetWeaponItemCountAtSlot(int slotIndex)
    {
        if (inventoryData == null)
            return 0;

        inventoryData.EnsureDefaults();
        if (slotIndex < 0 || slotIndex >= inventoryData.weaponSlots.Length)
            return 0;

        return inventoryData.weaponSlots[slotIndex].count;
    }

    public int GetWeaponAmmoCountAtSlot(int slotIndex)
    {
        if (inventoryData == null)
            return 0;

        inventoryData.EnsureDefaults();
        if (slotIndex < 0 || slotIndex >= inventoryData.weaponSlots.Length)
            return 0;

        return inventoryData.weaponSlots[slotIndex].ammoCount;
    }

    // For InventorySlot drag-and-drop swapping
    public bool TrySwapItems(InventorySlotType fromType, int fromIndex, InventorySlotType toType, int toIndex)
    {
        if (inventoryData == null)
            return false;

        if (!inventoryData.TrySwapSlots(fromType, fromIndex, toType, toIndex))
            return false;

        RefreshUI();
        return true;
    }

    // For PlayerShooter2D to check if the equipped weapon has ammo before shooting
    public bool HasEquippedWeaponAmmo()
    {
        if (inventoryData == null)
            return false;

        inventoryData.EnsureDefaults();
        int slotIndex = equippedWeaponIndex;
        return slotIndex >= 0 && slotIndex < inventoryData.weaponSlots.Length &&
            inventoryData.weaponSlots[slotIndex].item is WeaponSO &&
            inventoryData.weaponSlots[slotIndex].ammoCount > 0;
    }

    // For PlayerShooter2D to consume ammo when shooting
    public bool TryConsumeEquippedWeaponAmmo()
    {
        return inventoryData != null && inventoryData.TryConsumeWeaponAmmo(equippedWeaponIndex);
    }

    public int GetEquippedWeaponAmmoCount()
    {
        if (inventoryData == null)
            return 0;

        inventoryData.EnsureDefaults();
        if (equippedWeaponIndex < 0 || equippedWeaponIndex >= inventoryData.weaponSlots.Length)
            return 0;

        return inventoryData.weaponSlots[equippedWeaponIndex].ammoCount;
    }

    public bool TryReloadEquippedWeapon(WeaponSO weapon, int ammoCapacity)
    {
        if (inventoryData == null || weapon == null || ammoCapacity <= 0)
            return false;

        inventoryData.EnsureDefaults();
        if (equippedWeaponIndex < 0 || equippedWeaponIndex >= inventoryData.weaponSlots.Length ||
            inventoryData.weaponSlots[equippedWeaponIndex].item != weapon ||
            inventoryData.weaponSlots[equippedWeaponIndex].count <= 0)
            return false;

        inventoryData.weaponSlots[equippedWeaponIndex].ammoCount = ammoCapacity;
        return true;
    }
}