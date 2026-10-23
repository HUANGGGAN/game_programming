using UnityEngine;
using TMPro;

public class InventoryUIController : MonoBehaviour
{
    [SerializeField] private InventorySO inventoryData;
    [SerializeField] private PlayerShooter2D playerShooter;
    [SerializeField] private Transform universalSlotGridTransform;
    [SerializeField] private Transform weaponSlotGridTransform;
    [SerializeField] private Transform weaponSlotPanelTransform;
    [SerializeField] private Transform armorSlotGridTransform;
    [SerializeField] private Transform consumableSlotGridTransform;
    [Header("Slot Prefabs (Frame/Slot)")]
    [SerializeField] private GameObject SlotPrefab;
    //[SerializeField] private GameObject weaponSlotPrefab;
    //[SerializeField] private GameObject armorSlotPrefab;
    //[SerializeField] private GameObject consumableSlotPrefab;

    [SerializeField, Range(-32768, 32767)] private int draggedItemSortingOrder = 32767;

    [Header("Item Description")]
    [SerializeField] private RectTransform descriptionPanel;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Vector2 descriptionCursorOffset = new Vector2(16f, 0f);
    private InventorySlot descriptionOwner;

    private Canvas dragCanvas;
    private Transform draggedIcon;
    private Transform draggedIconParent;
    private int draggedIconSiblingIndex;
    private Vector3 draggedIconLocalPosition;
    private Vector3 draggedIconLocalScale;
    private Quaternion draggedIconLocalRotation;

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
        if (descriptionPanel != null)
        {
            if (descriptionText == null)
                descriptionText = descriptionPanel.GetComponentInChildren<TMP_Text>(true);
            CanvasGroup group = descriptionPanel.GetComponent<CanvasGroup>();
            if (group == null)
                group = descriptionPanel.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            descriptionPanel.gameObject.SetActive(false);
        }
        if (inventoryData != null)
            inventoryData.EnsureDefaults();

        if (playerShooter == null)
            playerShooter = FindFirstObjectByType<PlayerShooter2D>();

        universalSlots = CreateSlots(universalSlotGridTransform, SlotPrefab,
            InventorySlotType.Universal, InventorySO.universalSlotCount);
        weaponSlots = CreateSlots(weaponSlotGridTransform, SlotPrefab,
            InventorySlotType.Weapon, InventorySO.weaponSlotCount);
        armorSlots = CreateSlots(armorSlotGridTransform, SlotPrefab,
            InventorySlotType.Armor, InventorySO.armorSlotCount);
        consumableSlots = CreateSlots(consumableSlotGridTransform, SlotPrefab,
            InventorySlotType.Consumable, InventorySO.consumableSlotCount);

        if (weaponSlotPanelTransform != null)
        {
            weaponGuiSlots = weaponSlotPanelTransform.GetComponentsInChildren<WeaponSlot>(true);
            CacheWeaponSlotCanvasGroup();
        }
    }

    private InventorySlot[] CreateSlots(Transform grid, GameObject prefab, InventorySlotType type, int count)
    {
        if (grid == null || prefab == null)
        {
            Debug.LogError($"Assign the {type} slot grid and frame prefab on InventoryUIController.", this);
            return System.Array.Empty<InventorySlot>();
        }
        if (prefab.GetComponentsInChildren<InventorySlot>(true).Length != 1)
        {
            Debug.LogError($"The {type} frame prefab must contain exactly one InventorySlot.", prefab);
            return System.Array.Empty<InventorySlot>();
        }

        // Replace old slot frames without removing grid decorations or layout components.
        foreach (Transform child in grid)
        {
            if (child.GetComponentInChildren<InventorySlot>(true) == null)
                continue;
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        InventorySlot[] createdSlots = new InventorySlot[count];
        for (int i = 0; i < count; i++)
        {
            GameObject frame = Instantiate(prefab, grid, false);
            frame.name = $"{type} Frame {i + 1}";
            InventorySlot slot = frame.GetComponentInChildren<InventorySlot>(true);
            slot.Initialize(this, type, i);
            slot.gameObject.SetActive(true);
            frame.SetActive(true);
            createdSlots[i] = slot;
        }
        return createdSlots;
    }

    private void OnEnable()
    {
        RefreshUI();
    }

    public void ShowItemDescription(InventorySlot owner, Vector2 cursor, Camera eventCamera)
    {
        if (descriptionPanel == null || descriptionText == null || owner == null || owner.CurrentItem == null ||
            InventorySlot.DraggedSlot != null || !isActiveAndEnabled)
            return;

        descriptionOwner = owner;
        descriptionText.text = owner.CurrentItem.description ?? string.Empty;
        bool onRight = RectTransformUtility.WorldToScreenPoint(eventCamera, owner.transform.position).x < Screen.width * 0.5f;
        bool onTop = RectTransformUtility.WorldToScreenPoint(eventCamera, owner.transform.position).y < Screen.height * 0.5f;
        descriptionPanel.pivot = new Vector2(onRight ? 0f : 1f, onTop ? 0f : 1f);
        Vector2 screenPosition = cursor + new Vector2((onRight ? 1f : -1f) * Mathf.Abs(descriptionCursorOffset.x), (onTop ? 1f : -1f) * Mathf.Abs(descriptionCursorOffset.y));
        Canvas canvas = descriptionPanel.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        if (descriptionPanel.parent is RectTransform parent &&
            RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, screenPosition, camera, out Vector3 position))
            descriptionPanel.position = position;
        descriptionPanel.gameObject.SetActive(true);
        descriptionPanel.SetAsLastSibling();
    }

    public void HideItemDescription(InventorySlot owner = null)
    {
        if (owner != null && descriptionOwner != owner)
            return;
        descriptionOwner = null;
        if (descriptionPanel != null)
            descriptionPanel.gameObject.SetActive(false);
    }

    public void BeginItemDrag(Transform icon)
    {
        HideItemDescription();
        if (icon == null)
            return;
        Canvas parentCanvas = icon.GetComponentInParent<Canvas>();
        if (parentCanvas == null)
            return;

        RestoreDraggedIcon();
        Canvas rootCanvas = parentCanvas.rootCanvas;
        if (dragCanvas == null)
        {
            GameObject layer = new GameObject("Inventory Drag Layer", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            RectTransform rect = layer.GetComponent<RectTransform>();
            rect.SetParent(rootCanvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            dragCanvas = layer.GetComponent<Canvas>();
            CanvasGroup group = layer.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
        }
        dragCanvas.overrideSorting = true;
        dragCanvas.sortingLayerID = rootCanvas.sortingLayerID;
        dragCanvas.sortingOrder = draggedItemSortingOrder;

        draggedIcon = icon;
        draggedIconParent = icon.parent;
        draggedIconSiblingIndex = icon.GetSiblingIndex();
        draggedIconLocalPosition = icon.localPosition;
        draggedIconLocalScale = icon.localScale;
        draggedIconLocalRotation = icon.localRotation;
        icon.SetParent(dragCanvas.transform, true);
        icon.SetAsLastSibling();
    }

    public void EndItemDrag(Transform icon)
    {
        if (draggedIcon == icon)
            RestoreDraggedIcon();
    }

    private void RestoreDraggedIcon()
    {
        Transform icon = draggedIcon;
        Transform parent = draggedIconParent;
        // Reparenting into a closed inventory can invoke the slot's OnDisable immediately.
        draggedIcon = null;
        draggedIconParent = null;
        if (icon != null && parent != null)
        {
            icon.SetParent(parent, false);
            icon.SetSiblingIndex(draggedIconSiblingIndex);
            icon.localPosition = draggedIconLocalPosition;
            icon.localScale = draggedIconLocalScale;
            icon.localRotation = draggedIconLocalRotation;
        }
    }

    private void OnDisable()
    {
        HideItemDescription();
        Frame.ClearSelection();
        RestoreDraggedIcon();
    }

    private void OnDestroy()
    {
        RestoreDraggedIcon();
        if (dragCanvas != null)
            Destroy(dragCanvas.gameObject);
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
            universalSlots[i].SetSlotType(InventorySlotType.Universal);
            int count = i < inventoryData.slots.Length ? inventoryData.slots[i].count : 0;
            universalSlots[i].SetupSlot(item, i, count);
        }
    }

    private void RefreshWeaponSlots()
    {
        if (weaponSlots == null)
            return;

        for (int i = 0; i < weaponSlots.Length; i++)
        {
            ItemSO item = i < inventoryData.weaponSlots.Length ? inventoryData.weaponSlots[i].item : null;
            weaponSlots[i].SetSlotType(InventorySlotType.Weapon);
            int count = i < inventoryData.weaponSlots.Length ? inventoryData.weaponSlots[i].count : 0;
            weaponSlots[i].SetupSlot(item, i, count);
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
            armorSlots[i].SetSlotType(InventorySlotType.Armor);
            int count = i < inventoryData.armorSlots.Length ? inventoryData.armorSlots[i].count : 0;
            armorSlots[i].SetupSlot(item, i, count);
        }
    }

    private void RefreshConsumableSlots()
    {
        if (consumableSlots == null)
            return;

        for (int i = 0; i < consumableSlots.Length; i++)
        {
            ItemSO item = i < inventoryData.consumableSlots.Length ? inventoryData.consumableSlots[i].item : null;
            consumableSlots[i].SetSlotType(InventorySlotType.Consumable);
            int count = i < inventoryData.consumableSlots.Length ? inventoryData.consumableSlots[i].count : 0;
            consumableSlots[i].SetupSlot(item, i, count);
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
    public bool CanGiveItem(ItemSO item, InventorySlotType type, int count = 1)
    {
        return inventoryData != null && inventoryData.CanGiveItem(item, type, count);
    }

    public bool GiveItem(ItemSO item, InventorySlotType type, int count = 1, int ammoCount = 0)
    {
        return GiveItem(item, type, out _, count, ammoCount);
    }

    public bool GiveItem(ItemSO item, InventorySlotType type, out int givenCount, int count = 1, int ammoCount = 0)
    {
        givenCount = 0;
        if (inventoryData == null)
            return false;

        if (item == null || count <= 0)
            return false;

        bool success = inventoryData.TryGiveItem(item, type, out givenCount, count, ammoCount);
        if (success)
            RefreshUI();

        return success;
    }

    public InventorySlotData GetSlotData(InventorySlotType slotType, int slotIndex)
    {
        if (inventoryData == null)
            return null;

        inventoryData.EnsureDefaults();
        InventorySlotData[] slots = inventoryData.GetSlotsForType(slotType);
        return slots != null && slotIndex >= 0 && slotIndex < slots.Length ? slots[slotIndex] : null;
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

    public int GetItemCount(ItemSO item)
    {
        return inventoryData != null ? inventoryData.GetItemCount(item) : 0;
    }

    public bool TryConsumeConsumable(int slotIndex)
    {
        if (inventoryData == null || !inventoryData.TryConsumeConsumable(slotIndex))
            return false;
        RefreshConsumableSlots();
        return true;
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
        if (slotIndex < 0 || slotIndex >= inventoryData.weaponSlots.Length)
            return false;
        InventorySlotData slot = inventoryData.weaponSlots[slotIndex];
        return slot.item is WeaponSO weapon && slot.count > 0 &&
            (!weapon.consumeAmmo || (weapon.hasLimitedAmmo ? slot.count > 0 : slot.ammoCount > 0));
    }

    // For PlayerShooter2D to consume magazine ammo or one limited-ammo item.
    public bool TryConsumeEquippedWeaponAmmo()
    {
        if (inventoryData == null || !inventoryData.TryConsumeWeaponAmmo(equippedWeaponIndex))
            return false;
        if (!IsWeaponAtSlot(equippedWeaponIndex))
        {
            equippedWeaponIndex = -1;
            playerShooter?.UnequipWeapon();
        }
        RefreshWeaponSlots();
        playerShooter?.RefreshWeaponAmmoTexts();
        return true;
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
