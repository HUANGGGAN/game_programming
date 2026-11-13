using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DecompositionManager : MonoBehaviour
{
    [SerializeField] private InventorySO inventoryData;
    [SerializeField] private DecompositionInventorySO decompositionInventory;
    [SerializeField] private InventoryUIController inventoryUI;
    [SerializeField] private GameObject decompositionPanel;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform inventorySlotGrid;
    [SerializeField] private Transform decompositionSlotGrid;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button confirmButton;
    [Tooltip("Common, Uncommon, Rare, Epic, Legendary, Mythic, in that order.")]
    [SerializeField] private ItemSO[] weaponPieces = new ItemSO[6];
    [SerializeField] private TMP_Text statusText;

    private readonly List<DecompositionSlot> slots = new List<DecompositionSlot>();
    private readonly List<GameObject> slotFrames = new List<GameObject>();
    private StateManager stateManager;
    private Canvas dragCanvas;
    private bool initialized;

    private void Awake() => Initialize();

    private void Initialize()
    {
        if (initialized)
            return;
        initialized = true;
        if (inventoryUI == null)
            inventoryUI = FindFirstObjectByType<InventoryUIController>(FindObjectsInactive.Include);
        if (inventoryData == null && inventoryUI != null)
            inventoryData = inventoryUI.InventoryData;
        if (decompositionPanel != null)
        {
            CanvasGroup panelGroup = decompositionPanel.GetComponent<CanvasGroup>() ??
                decompositionPanel.AddComponent<CanvasGroup>();
            panelGroup.ignoreParentGroups = true;
            panelGroup.alpha = 1f;
            panelGroup.interactable = true;
            panelGroup.blocksRaycasts = true;
            Transform root = decompositionPanel.transform;
            if (inventorySlotGrid == null) inventorySlotGrid = root.Find("InventorySlotGrid");
            if (decompositionSlotGrid == null) decompositionSlotGrid = root.Find("DecompositionSlotGrid");
            if (closeButton == null) closeButton = root.Find("CloseButton")?.GetComponent<Button>();
            if (confirmButton == null) confirmButton = root.Find("ConfirmButton")?.GetComponent<Button>();
        }
        if (slotPrefab != null && slotPrefab.GetComponentsInChildren<DecompositionSlot>(true).Length == 1 &&
            slotPrefab.GetComponentInChildren<InventorySlot>(true) == null)
        {
            CreateSlots(inventorySlotGrid, InventorySO.universalSlotCount, false);
            CreateSlots(decompositionSlotGrid, DecompositionInventorySO.SlotCount, true);
        }
        else
            Debug.LogError("Decomposition slot prefab must contain one DecompositionSlot on its item icon/Slot child, with Frame on the parent and no InventorySlot components.", this);
        closeButton?.onClick.AddListener(Close);
        confirmButton?.onClick.AddListener(Confirm);
        stateManager = StateManager.Instance;
        stateManager.StateChanged += ApplyState;
        ApplyState(stateManager.CurrentState);
    }

    private void CreateSlots(Transform grid, int count, bool decomposition)
    {
        if (grid == null)
            return;
        for (int i = 0; i < count; i++)
        {
            GameObject frame = Instantiate(slotPrefab, grid, false);
            frame.name = $"{(decomposition ? "Decomposition" : "Inventory")} Slot {i + 1}";
            DecompositionSlot slot = frame.GetComponentInChildren<DecompositionSlot>(true);
            slot.Initialize(this, decomposition, i);
            slot.gameObject.SetActive(true);
            frame.SetActive(true);
            slots.Add(slot);
            slotFrames.Add(frame);
        }
    }

    public bool Open()
    {
        Initialize();
        if (stateManager.CurrentState != GameState.Merchant || decompositionPanel == null ||
            inventoryData == null || decompositionInventory == null ||
            slots.Count != InventorySO.universalSlotCount + DecompositionInventorySO.SlotCount)
            return false;
        if (statusText != null) statusText.text = string.Empty;
        return stateManager.RequestState(GameState.Decomposition);
    }

    public void Close() => StateManager.Instance.CloseState(GameState.Decomposition);

    private void ApplyState(GameState state)
    {
        bool visible = state == GameState.Decomposition;
        if (!visible && DecompositionSlot.DraggedSlot != null && DecompositionSlot.DraggedSlot.Manager == this)
            DecompositionSlot.DraggedSlot.CancelDrag();
        if (visible) Refresh();
        if (decompositionPanel != null) decompositionPanel.SetActive(visible);
    }

    public InventorySlotData GetSlot(bool decomposition, int index)
    {
        if (inventoryData == null || decompositionInventory == null)
            return null;
        inventoryData.EnsureDefaults();
        decompositionInventory.EnsureDefaults();
        InventorySlotData[] data = decomposition ? decompositionInventory.slots : inventoryData.slots;
        return index >= 0 && index < data.Length ? data[index] : null;
    }

    public bool CanMove(DecompositionSlot source, DecompositionSlot destination) =>
        source != null && destination != null && source.Manager == this && destination.Manager == this &&
        stateManager != null && stateManager.CurrentState == GameState.Decomposition && decompositionInventory != null &&
        decompositionInventory.CanMove(inventoryData, source.IsDecomposition, source.Index, destination.IsDecomposition, destination.Index);

    public void Move(DecompositionSlot source, DecompositionSlot destination)
    {
        if (CanMove(source, destination) && decompositionInventory.TryMove(inventoryData,
            source.IsDecomposition, source.Index, destination.IsDecomposition, destination.Index))
        {
            Refresh();
            inventoryUI?.RefreshUI();
        }
    }

    public void Confirm()
    {
        if (stateManager.CurrentState != GameState.Decomposition || decompositionInventory == null || DecompositionSlot.DraggedSlot != null)
            return;
        bool success = decompositionInventory.TryDecompose(weaponPieces);
        if (statusText != null)
            statusText.text = success ? "Items decomposed." : "Nothing decomposed. Check piece registrations and stack capacity.";
        Refresh();
    }

    public void Refresh()
    {
        foreach (DecompositionSlot slot in slots)
            if (slot != null) slot.Refresh();
    }

    public Transform GetDragLayer()
    {
        if (dragCanvas == null)
        {
            Canvas root = decompositionPanel.GetComponentInParent<Canvas>();
            if (root == null) return decompositionPanel.transform;
            var layer = new GameObject("Decomposition Drag Layer", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            layer.transform.SetParent(root.rootCanvas.transform, false);
            dragCanvas = layer.GetComponent<Canvas>();
            dragCanvas.overrideSorting = true;
            dragCanvas.sortingLayerID = root.rootCanvas.sortingLayerID;
            dragCanvas.sortingOrder = 32767;
            layer.GetComponent<CanvasGroup>().blocksRaycasts = false;
        }
        return dragCanvas.transform;
    }

    private void OnDisable()
    {
        if (DecompositionSlot.DraggedSlot != null && DecompositionSlot.DraggedSlot.Manager == this)
            DecompositionSlot.DraggedSlot.CancelDrag();
        if (stateManager != null) stateManager.CloseState(GameState.Decomposition);
    }

    private void OnDestroy()
    {
        if (stateManager != null) stateManager.StateChanged -= ApplyState;
        closeButton?.onClick.RemoveListener(Close);
        confirmButton?.onClick.RemoveListener(Confirm);
        foreach (GameObject frame in slotFrames)
            if (frame != null) Destroy(frame);
        if (dragCanvas != null) Destroy(dragCanvas.gameObject);
    }
}
