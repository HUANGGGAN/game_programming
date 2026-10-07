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
    public InventorySlotType slotType;

    [SerializeField] private Image itemIconImage;

    private CanvasGroup iconCanvasGroup;
    private InventoryUIController uiController;
    private ItemSO currentItem;

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
        currentItem = item;

        if (item != null && item.icon != null && CanAccept(item))
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

    public void SetSlotType(InventorySlotType type)
    {
        slotType = type;
        if (currentItem != null && !CanAccept(currentItem))
        {
            itemIconImage.sprite = null;
            itemIconImage.color = Color.clear;
            isEmpty = true;
            currentItem = null;
        }
    }

    public bool CanAccept(ItemSO item)
    {
        if (item == null)
            return true;

        if (slotType == InventorySlotType.Universal)
            return true;

        switch (slotType)
        {
            case InventorySlotType.Weapon:
                return item is WeaponSO;
            case InventorySlotType.Armor:
                return item is ArmorSO;
            case InventorySlotType.Consumable:
                return item is ConsumableSO;
            default:
                return false;
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

        iconCanvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        itemIconImage.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        isDragging = false;
        itemIconImage.transform.localPosition = originalLocalPosition;
        iconCanvasGroup.blocksRaycasts = true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlot draggedSlot = eventData.pointerDrag?.GetComponent<InventorySlot>();

        if (draggedSlot == null)
            return;

        if (draggedSlot.isEmpty || draggedSlot == this)
            return;

        if (uiController == null)
            return;

        uiController.TrySwapItems(draggedSlot.slotType, draggedSlot.slotIndex, slotType, slotIndex);
    }
}