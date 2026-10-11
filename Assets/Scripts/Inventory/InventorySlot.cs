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
    private InventoryUIController uiController;
    private ItemSO currentItem;

    private Vector3 originalLocalPosition;
    private bool isDragging;
    private bool isEmpty;

    public InventoryUIController UIController => uiController;
    public bool IsDragging => isDragging;

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
