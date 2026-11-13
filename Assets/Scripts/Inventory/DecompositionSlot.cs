using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DecompositionSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
    IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Image itemIconImage;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Color maxStackCountColor = Color.green;
    [SerializeField, Min(0f)] private float descriptionHoverDelay = 0.5f;
    public DecompositionManager Manager { get; private set; }
    public bool IsDecomposition { get; private set; }
    public int Index { get; private set; }
    public ItemSO CurrentItem { get; private set; }
    public bool IsDragging { get; private set; }
    public static DecompositionSlot DraggedSlot { get; private set; }
    public static event System.Action DragEnded;
    private CanvasGroup iconGroup;
    private Transform originalParent;
    private int originalSibling;
    private Vector3 originalPosition;
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private PointerEventData hoverPointer;
    private float hoverStarted;
    private bool pointerInside;

    public void Initialize(DecompositionManager manager, bool decomposition, int index)
    {
        Manager = manager;
        IsDecomposition = decomposition;
        Index = index;
        if (itemIconImage == null) itemIconImage = GetComponent<Image>();
        if (countText == null) countText = GetComponentInChildren<TMP_Text>(true);
        if (itemIconImage != null)
            iconGroup = itemIconImage.GetComponent<CanvasGroup>() ?? itemIconImage.gameObject.AddComponent<CanvasGroup>();
        Refresh();
    }

    public void Refresh()
    {
        InventorySlotData data = Manager != null ? Manager.GetSlot(IsDecomposition, Index) : null;
        CurrentItem = data != null && data.count > 0 ? data.item : null;
        if (itemIconImage != null)
        {
            itemIconImage.sprite = CurrentItem != null ? CurrentItem.icon : null;
            itemIconImage.color = CurrentItem != null && CurrentItem.icon != null ? Color.white : Color.clear;
        }
        if (countText != null)
        {
            countText.raycastTarget = false;
            countText.text = CurrentItem != null && CurrentItem.maxStackSize > 1 ? data.count.ToString() : string.Empty;
            countText.color = CurrentItem != null && data.count == CurrentItem.maxStackSize ? maxStackCountColor : Color.black;
        }
    }

    public bool CanReceiveDropFrom(DecompositionSlot source) => source != null && source != this &&
        source.IsDragging && Manager != null && Manager.CanMove(source, this);

    public void OnBeginDrag(PointerEventData data)
    {
        if (CurrentItem == null || itemIconImage == null || itemIconImage.sprite == null || Manager == null ||
            StateManager.Instance.CurrentState != GameState.Decomposition)
            return;
        IsDragging = true;
        DraggedSlot = this;
        Transform icon = itemIconImage.transform;
        originalParent = icon.parent;
        originalSibling = icon.GetSiblingIndex();
        originalPosition = icon.localPosition;
        originalScale = icon.localScale;
        originalRotation = icon.localRotation;
        iconGroup.blocksRaycasts = false;
        icon.SetParent(Manager.GetDragLayer(), true);
    }

    public void OnDrag(PointerEventData data)
    {
        if (IsDragging) itemIconImage.transform.position = data.position;
    }

    public void OnEndDrag(PointerEventData data) => EndDrag();
    public void CancelDrag() => EndDrag();

    private void EndDrag()
    {
        if (!IsDragging) return;
        IsDragging = false;
        hoverPointer = null;
        Manager?.HideDescription(this);
        if (DraggedSlot == this) DraggedSlot = null;
        if (itemIconImage != null && originalParent != null)
        {
            Transform icon = itemIconImage.transform;
            icon.SetParent(originalParent, false);
            icon.SetSiblingIndex(originalSibling);
            icon.localPosition = originalPosition;
            icon.localScale = originalScale;
            icon.localRotation = originalRotation;
        }
        if (iconGroup != null) iconGroup.blocksRaycasts = true;
        DragEnded?.Invoke();
    }

    public void OnDrop(PointerEventData data)
    {
        DecompositionSlot source = DraggedSlot;
        if (CanReceiveDropFrom(source)) Manager.Move(source, this);
    }

    private void Update()
    {
        if (!pointerInside)
            return;
        if (DraggedSlot != null)
        {
            hoverStarted = Time.unscaledTime;
            Manager?.HideDescription(this);
            return;
        }
        if (hoverPointer != null && Time.unscaledTime - hoverStarted >= descriptionHoverDelay)
            Manager?.ShowDescription(this, hoverPointer);
    }

    public void OnPointerEnter(PointerEventData data)
    {
        hoverPointer = data;
        if (!pointerInside)
            hoverStarted = Time.unscaledTime;
        pointerInside = true;
    }

    public void OnPointerExit(PointerEventData data)
    {
        hoverPointer = null;
        pointerInside = false;
        Manager?.HideDescription(this);
    }

    public void OnPointerClick(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left || data.dragging || DraggedSlot != null) return;
        GetComponentInParent<Frame>()?.Select();
        hoverPointer = data;
        hoverStarted = Time.unscaledTime - descriptionHoverDelay;
        Manager?.ShowDescription(this, data);
    }

    private void OnDisable()
    {
        hoverPointer = null;
        pointerInside = false;
        Manager?.HideDescription(this);
        EndDrag();
    }
}
