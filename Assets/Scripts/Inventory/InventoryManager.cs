using UnityEngine;
using UnityEngine.InputSystem; // Required for the New Input System

public class InventoryManager : MonoBehaviour
{
    private const int WeaponSlotCount = 3;

    [Header("UI Panels")]
    [SerializeField] private GameObject inventoryMenuPanel;
    [SerializeField] private InventoryUIController inventoryUIController;

    private InputAction toggleInventoryAction;
    private InputAction[] equipWeaponActions;
    private InputAction switchWeaponAction;
    public bool IsInventoryOpen => inventoryMenuPanel != null && inventoryMenuPanel.activeSelf;

    private void Awake()
    {
        if (inventoryUIController == null && inventoryMenuPanel != null)
        {
            inventoryUIController = inventoryMenuPanel.GetComponentInChildren<InventoryUIController>(true);
        }

        // Define an action bound to the 'E' key
        toggleInventoryAction = new InputAction(
            name: "ToggleInventory",
            binding: "<Keyboard>/e"
        );
        switchWeaponAction = new InputAction(
            name: "SwitchWeapon",
            type: InputActionType.Value,
            binding: "<Mouse>/scroll"
        );

        equipWeaponActions = new[]
        {
            new InputAction(name: "EquipWeaponSlot1", binding: "<Keyboard>/1"),
            new InputAction(name: "EquipWeaponSlot2", binding: "<Keyboard>/2"),
            new InputAction(name: "EquipWeaponSlot3", binding: "<Keyboard>/3")
        };

        for (int i = 0; i < equipWeaponActions.Length; i++)
        {
            int slotIndex = i;
            equipWeaponActions[i].performed += context => SelectWeaponSlot(slotIndex);
        }
    }

    private void OnEnable()
    {
        if (inventoryUIController != null)
        {
            inventoryUIController.RefreshUI();
        }

        // 1. Subscribe to the event (Fires ONLY when E is pressed)
        toggleInventoryAction.performed += OnToggleInventoryPerformed;
        toggleInventoryAction.Enable();
        switchWeaponAction.performed += OnWeaponScrollPerformed;
        switchWeaponAction.Enable();

        foreach (InputAction action in equipWeaponActions)
        {
            action.Enable();
        }
    }

    private void OnDisable()
    {
        // 2. Unsubscribe and disable to avoid memory leaks
        toggleInventoryAction.performed -= OnToggleInventoryPerformed;
        toggleInventoryAction.Disable();
        switchWeaponAction.performed -= OnWeaponScrollPerformed;
        switchWeaponAction.Disable();

        foreach (InputAction action in equipWeaponActions)
        {
            action.Disable();
        }
    }

    private void OnDestroy()
    {
        toggleInventoryAction.Dispose();
        switchWeaponAction.Dispose();

        foreach (InputAction action in equipWeaponActions)
        {
            action.Dispose();
        }
    }

    private void Start()
    {
        // Ensure menu starts closed when game launches
        CloseInventory();
    }

    private void OnToggleInventoryPerformed(InputAction.CallbackContext context)
    {
        ToggleInventory();
        Tutorial.Instance?.ShowTutorial("inventory", "In the inventory, you can view your weapons and other items.\nThe slots at the left are for your weapons. You can move them around or discard them by dragging and dropping.\n");
    }

    private void SelectWeaponSlot(int slotIndex)
    {
        inventoryUIController?.SetEquippedWeapon(slotIndex);
    }

    private void OnWeaponScrollPerformed(InputAction.CallbackContext context)
    {
        if (inventoryUIController == null)
            return;

        float scrollY = context.ReadValue<Vector2>().y;
        if (Mathf.Approximately(scrollY, 0f))
            return;

        int direction = scrollY > 0f ? -1 : 1;
        int equippedSlotIndex = inventoryUIController.GetEquippedWeaponIndex();

        if (equippedSlotIndex < 0)
        {
            for (int slotIndex = 0; slotIndex < WeaponSlotCount; slotIndex++)
            {
                if (inventoryUIController.GetItemAtSlot(slotIndex) is WeaponSO)
                {
                    SelectWeaponSlot(slotIndex);
                    return;
                }
            }

            return;
        }

        for (int step = 1; step < WeaponSlotCount; step++)
        {
            int slotIndex = (equippedSlotIndex + direction * step + WeaponSlotCount) % WeaponSlotCount;
            if (inventoryUIController.GetItemAtSlot(slotIndex) is WeaponSO)
            {
                SelectWeaponSlot(slotIndex);
                return;
            }
        }
    }

    public void OpenInventory()
    {
        inventoryMenuPanel.SetActive(true);
        if (inventoryUIController != null)
        {
            inventoryUIController.SetWeaponSlotPanelVisible(false);
        }

        Time.timeScale = 0f; // Pauses gameplay physics while menu is open
    }

    public void CloseInventory()
    {
        inventoryMenuPanel.SetActive(false);
        if (inventoryUIController != null)
        {
            inventoryUIController.SetWeaponSlotPanelVisible(true);
        }

        Time.timeScale = Tutorial.Instance != null && Tutorial.Instance.IsShowing ? 0f : 1f;
    }

    public void ToggleInventory()
    {
        if (Tutorial.Instance != null && Tutorial.Instance.IsShowing)
            return;

        if (inventoryMenuPanel.activeSelf)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }
}
