using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler
{
    public int slotIndex;

    [SerializeField] private Image itemIconImage;

    private CanvasGroup iconCanvasGroup;
    private InventoryUIController uiController;

    private Vector3 originalLocalPosition;
    private bool isDragging;
    private bool isEmpty;

    private void Awake()
    {
        uiController = GetComponentInParent<InventoryUIController>();

        iconCanvasGroup = itemIconImage.GetComponent<CanvasGroup>();

        if (iconCanvasGroup == null)
            iconCanvasGroup = itemIconImage.gameObject.AddComponent<CanvasGroup>();
    }

    public void SetupSlot(ItemSO item, int index)
    {
        slotIndex = index;

        if (item != null && item.icon != null)
        {
            itemIconImage.sprite = item.icon;
            itemIconImage.color = Color.white;
            isEmpty = false;
        }
        else
        {
            itemIconImage.sprite = null;
            itemIconImage.color = Color.clear;
            isEmpty = true;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (itemIconImage.sprite == null)
            return;

        if (isEmpty)
            return;

        isDragging = true;

        originalLocalPosition = itemIconImage.transform.localPosition;

        // Prevent the dragged icon from blocking the slot underneath.
        iconCanvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        // Move icon to mouse position.
        itemIconImage.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        isDragging = false;

        // Put icon back where it originally was.
        itemIconImage.transform.localPosition = originalLocalPosition;

        // Enable raycasts again.
        iconCanvasGroup.blocksRaycasts = true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlot draggedSlot =
            eventData.pointerDrag?.GetComponent<InventorySlot>();

        if (draggedSlot == null)
            return;

        if (draggedSlot.isEmpty)
            return;

        if (draggedSlot == this)
            return;

        if (uiController == null)
            return;

        uiController.SwapItems(
            draggedSlot.slotIndex,
            this.slotIndex
        );
    }
}