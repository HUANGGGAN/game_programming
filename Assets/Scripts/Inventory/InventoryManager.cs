using UnityEngine;
using UnityEngine.InputSystem; // Required for the New Input System

public class InventoryManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject inventoryMenuPanel;

    private InputAction toggleInventoryAction;

    private void Awake()
    {
        // Define an action bound to the 'E' key
        toggleInventoryAction = new InputAction(
            name: "ToggleInventory",
            binding: "<Keyboard>/e"
        );
    }

    private void OnEnable()
    {
        // 1. Subscribe to the event (Fires ONLY when E is pressed)
        toggleInventoryAction.performed += OnToggleInventoryPerformed;
        toggleInventoryAction.Enable();
    }

    private void OnDisable()
    {
        // 2. Unsubscribe and disable to avoid memory leaks
        toggleInventoryAction.performed -= OnToggleInventoryPerformed;
        toggleInventoryAction.Disable();
    }

    private void Start()
    {
        // Ensure menu starts closed when game launches
        CloseInventory();
    }

    private void OnToggleInventoryPerformed(InputAction.CallbackContext context)
    {
        ToggleInventory();
    }

    public void OpenInventory()
    {
        inventoryMenuPanel.SetActive(true);
        Time.timeScale = 0f; // Pauses gameplay physics while menu is open
    }

    public void CloseInventory()
    {
        inventoryMenuPanel.SetActive(false);
        Time.timeScale = 1f; // Resumes gameplay physics
    }

    public void ToggleInventory()
    {
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