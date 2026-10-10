using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropSlot : MonoBehaviour, IDropHandler
{
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private InventoryUIController pendingController;
    private InventorySlotType pendingSlotType;
    private int pendingSlotIndex;
    private InventorySlotData pendingSlot;
    private ItemSO pendingItem;

    private void Awake()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmDrop);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelDrop);
        CancelDrop();
    }

    private void OnDisable()
    {
        CancelDrop();
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(ConfirmDrop);
        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(CancelDrop);
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlot source = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<InventorySlot>()
            : null;
        if (source == null || !source.IsDragging || source.UIController == null ||
            confirmationPanel == null || confirmButton == null || cancelButton == null || pendingSlot != null)
            return;

        InventorySlotData slot = source.UIController.GetSlotData(source.slotType, source.slotIndex);
        if (slot == null || slot.item == null || slot.count <= 0)
            return;

        pendingController = source.UIController;
        pendingSlotType = source.slotType;
        pendingSlotIndex = source.slotIndex;
        pendingSlot = slot;
        pendingItem = slot.item;
        confirmationPanel.SetActive(true);
    }

    public void ConfirmDrop()
    {
        // A swap or removal while the panel is open must not discard a replacement item.
        if (pendingController != null && pendingSlot != null &&
            pendingController.GetSlotData(pendingSlotType, pendingSlotIndex) == pendingSlot &&
            pendingSlot.item == pendingItem && pendingItem != null)
            pendingController.DropFromSlot(pendingSlotType, pendingSlotIndex);

        CancelDrop();
    }

    public void CancelDrop()
    {
        pendingController = null;
        pendingSlot = null;
        pendingItem = null;
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }
}
