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
    private StateManager stateManager;
    public bool IsInventoryOpen => stateManager != null && stateManager.CurrentState == GameState.Inventory;

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
        stateManager = StateManager.Instance;
        if (inventoryUIController != null)
        {
            inventoryUIController.RefreshUI();
        }
        stateManager.StateChanged += ApplyState;
        stateManager.StateChanging += PrepareState;
        ApplyState(stateManager.CurrentState);

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
        if (stateManager != null)
        {
            stateManager.StateChanged -= ApplyState;
            stateManager.StateChanging -= PrepareState;
        }
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

    private void OnToggleInventoryPerformed(InputAction.CallbackContext context)
    {
        ToggleInventory();
        if (IsInventoryOpen)
            Tutorial.Instance?.ShowTutorial("inventory", "In the inventory, you can view your weapons and other items.\nThe slots at the left are for your weapons. You can move them around or discard them by dragging and dropping.\n");
    }

    private void SelectWeaponSlot(int slotIndex)
    {
        if (stateManager.CurrentState != GameState.Playing)
            return;
        inventoryUIController?.SetEquippedWeapon(slotIndex);
    }

    private void OnWeaponScrollPerformed(InputAction.CallbackContext context)
    {
        if (inventoryUIController == null || stateManager.CurrentState != GameState.Playing)
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
        if (inventoryMenuPanel != null && StateManager.Instance.CurrentState == GameState.Playing)
            StateManager.Instance.RequestState(GameState.Inventory);
    }

    public void CloseInventory()
    {
        StateManager.Instance.CloseState(GameState.Inventory);
    }

    public void ToggleInventory()
    {
        if (IsInventoryOpen)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }

    private void PrepareState(GameState state)
    {
        if (state == GameState.Playing && stateManager.CurrentState == GameState.Inventory)
            inventoryUIController?.RefreshUI();
    }

    private void ApplyState(GameState state)
    {
        bool inventoryOpen = state == GameState.Inventory;
        if (inventoryMenuPanel != null)
            inventoryMenuPanel.SetActive(inventoryOpen);
        inventoryUIController?.SetWeaponSlotPanelVisible(!inventoryOpen);
    }
}
