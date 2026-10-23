using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlot : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler
{
    public int slotIndex;
    public InventorySlotType slotType;

    [SerializeField] private Image itemIconImage;
    [SerializeField] private TMP_Text countText;

    private CanvasGroup iconCanvasGroup;
    [SerializeField] private InventoryUIController uiController;
    private ItemSO currentItem;

    private Vector3 originalLocalPosition;
    private bool isDragging;
    private bool isEmpty;

    public InventoryUIController UIController => uiController;
    public bool IsDragging => isDragging;
    public ItemSO CurrentItem => currentItem;
    public static InventorySlot DraggedSlot { get; private set; }

    private void Awake()
    {
        uiController = GetComponentInParent<InventoryUIController>();
        ResolveCountText();

        iconCanvasGroup = itemIconImage.GetComponent<CanvasGroup>();

        if (iconCanvasGroup == null)
            iconCanvasGroup = itemIconImage.gameObject.AddComponent<CanvasGroup>();
    }

    public void SetupSlot(ItemSO item, int index, int count = 1)
    {
        slotIndex = index;
        currentItem = item;
        ResolveCountText();
        if (countText != null)
        {
            int maxStack = item != null ? Mathf.Max(1, item.maxStackSize) : 1;
            countText.text = item != null && maxStack > 1 && CanAccept(item) ? count.ToString() : string.Empty;
            countText.color = item != null && count == maxStack ? Color.green : Color.black;
        }

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
            if (countText != null)
                countText.text = string.Empty;
        }
    }

    private void ResolveCountText()
    {
        if (countText == null)
            countText = GetComponentInChildren<TMP_Text>(true);
        if (countText != null)
            countText.raycastTarget = false;
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
        DraggedSlot = this;

        originalLocalPosition = itemIconImage.transform.localPosition;
        uiController?.BeginItemDrag(itemIconImage.transform);

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
        if (DraggedSlot == this)
            DraggedSlot = null;
        uiController?.EndItemDrag(itemIconImage.transform);
        itemIconImage.transform.localPosition = originalLocalPosition;
        iconCanvasGroup.blocksRaycasts = true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlot draggedSlot = eventData.pointerDrag?.GetComponent<InventorySlot>();

        if (draggedSlot == null)
            return;

        if (!CanReceiveDropFrom(draggedSlot))
            return;

        uiController.TrySwapItems(draggedSlot.slotType, draggedSlot.slotIndex, slotType, slotIndex);
    }

    public bool CanReceiveDropFrom(InventorySlot source)
    {
        return source != null && source != this && source.IsDragging && !source.isEmpty &&
            (source.slotType != slotType || source.slotIndex != slotIndex) &&
            source.currentItem != null && uiController != null && source.uiController == uiController &&
            CanAccept(source.currentItem) && source.CanAccept(currentItem);
    }

    private void OnDisable()
    {
        if (DraggedSlot == this)
            DraggedSlot = null;
        if (!isDragging)
            return;
        isDragging = false;
        if (itemIconImage != null)
        {
            uiController?.EndItemDrag(itemIconImage.transform);
            itemIconImage.transform.localPosition = originalLocalPosition;
        }
        if (iconCanvasGroup != null)
            iconCanvasGroup.blocksRaycasts = true;
    }
}
