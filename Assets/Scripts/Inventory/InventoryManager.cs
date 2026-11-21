using UnityEngine;
using UnityEngine.InputSystem; // Required for the New Input System

public class InventoryManager : MonoBehaviour
{
    private const int WeaponSlotCount = 3;

    [Header("UI Panels")]
    [SerializeField] private GameObject inventoryMenuPanel;
    [SerializeField] private InventoryUIController inventoryUIController;

    private InputAction switchWeaponAction;
    private StateManager stateManager;
    public bool IsInventoryOpen => stateManager != null && stateManager.CurrentState == GameState.Inventory;

    private void Awake()
    {
        if (inventoryUIController == null && inventoryMenuPanel != null)
        {
            inventoryUIController = inventoryMenuPanel.GetComponentInChildren<InventoryUIController>(true);
        }

        switchWeaponAction = new InputAction(
            name: "SwitchWeapon",
            type: InputActionType.Value,
            binding: "<Mouse>/scroll"
        );
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

        // Mouse wheel remains an InputAction; keyboard actions use the shared bindings.
        switchWeaponAction.performed += OnWeaponScrollPerformed;
        switchWeaponAction.Enable();
    }

    private void OnDisable()
    {
        if (stateManager != null)
        {
            stateManager.StateChanged -= ApplyState;
            stateManager.StateChanging -= PrepareState;
        }
        // 2. Unsubscribe and disable to avoid memory leaks
        switchWeaponAction.performed -= OnWeaponScrollPerformed;
        switchWeaponAction.Disable();
    }

    private void OnDestroy()
    {
        switchWeaponAction.Dispose();
    }

    private void Update()
    {
        if (stateManager == null) return;
        if (GameInput.Pressed(GameBinding.Inventory) && (stateManager.IsGameplay || IsInventoryOpen))
        {
            GameInput.Consume();
            ToggleInventory();
            return;
        }
        if (!stateManager.IsGameplay) return;
        for (int i = 0; i < WeaponSlotCount; i++)
            if (GameInput.Pressed((GameBinding)((int)GameBinding.Weapon1 + i)))
            {
                GameInput.Consume();
                SelectWeaponSlot(i);
                break;
            }
    }

    private void SelectWeaponSlot(int slotIndex)
    {
        if (!stateManager.IsGameplay)
            return;
        inventoryUIController?.SetEquippedWeapon(slotIndex);
    }

    private void OnWeaponScrollPerformed(InputAction.CallbackContext context)
    {
        if (inventoryUIController == null || !stateManager.IsGameplay)
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
        if (inventoryMenuPanel != null && StateManager.Instance.IsGameplay &&
            StateManager.Instance.RequestState(GameState.Inventory))
            Tutorial.RequestTutorial("inventory-basics", "This is your inventory.\nDouble Click: Equip/Unequip\nRight Click: Split");
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
        if ((state == GameState.Playing || state == GameState.Boss) && stateManager.CurrentState == GameState.Inventory)
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
