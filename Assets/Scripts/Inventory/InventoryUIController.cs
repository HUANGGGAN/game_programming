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

        return inventoryData.weaponSlots[slotIndex] is WeaponSO;
    }

    private bool EquipShooterForSelectedWeapon()
    {
        if (playerShooter == null)
            return true;

        if (inventoryData.weaponSlots[equippedWeaponIndex] is WeaponSO weapon)
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
            ItemSO item = i < inventoryData.slots.Length ? inventoryData.slots[i] : null;
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
            ItemSO item = i < inventoryData.weaponSlots.Length ? inventoryData.weaponSlots[i] : null;
            weaponSlots[i].SetupSlot(item, i);
            weaponSlots[i].SetSlotType(InventorySlotType.Weapon);
        }

        if (weaponGuiSlots != null)
        {
            for (int i = 0; i < weaponGuiSlots.Length; i++)
            {
                ItemSO item = i < inventoryData.weaponSlots.Length ? inventoryData.weaponSlots[i] : null;
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
            ItemSO item = i < inventoryData.armorSlots.Length ? inventoryData.armorSlots[i] : null;
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
            ItemSO item = i < inventoryData.consumableSlots.Length ? inventoryData.consumableSlots[i] : null;
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
            return inventoryData.weaponSlots[slotIndex];

        return null;
    }

    public int GetWeaponItemCountAtSlot(int slotIndex)
    {
        if (inventoryData == null)
            return 0;

        inventoryData.EnsureDefaults();
        if (slotIndex < 0 || slotIndex >= inventoryData.weaponSlotCounts.Length)
            return 0;

        return inventoryData.weaponSlotCounts[slotIndex];
    }

    public int GetWeaponAmmoCountAtSlot(int slotIndex)
    {
        if (inventoryData == null)
            return 0;

        inventoryData.EnsureDefaults();
        if (slotIndex < 0 || slotIndex >= inventoryData.weaponSlotAmmoCounts.Length)
            return 0;

        return inventoryData.weaponSlotAmmoCounts[slotIndex];
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
            inventoryData.weaponSlots[slotIndex] is GunSO &&
            slotIndex < inventoryData.weaponSlotAmmoCounts.Length &&
            inventoryData.weaponSlotAmmoCounts[slotIndex] > 0;
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
        if (equippedWeaponIndex < 0 || equippedWeaponIndex >= inventoryData.weaponSlotAmmoCounts.Length)
            return 0;

        return inventoryData.weaponSlotAmmoCounts[equippedWeaponIndex];
    }

    public bool TryReloadEquippedWeapon(GunSO weapon, int ammoCapacity)
    {
        if (inventoryData == null || weapon == null || ammoCapacity <= 0)
            return false;

        inventoryData.EnsureDefaults();
        if (equippedWeaponIndex < 0 || equippedWeaponIndex >= inventoryData.weaponSlots.Length ||
            inventoryData.weaponSlots[equippedWeaponIndex] != weapon ||
            inventoryData.weaponSlotCounts[equippedWeaponIndex] <= 0)
            return false;

        inventoryData.weaponSlotAmmoCounts[equippedWeaponIndex] = ammoCapacity;
        return true;
    }
}