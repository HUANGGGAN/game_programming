using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StorageManager : MonoBehaviour
{
    private GameDataManager.InventoryAPI inventoryData => GameDataManager.Instance.Inventory;
    [SerializeField] private StorageSO storageData;
    [SerializeField] private InventoryUIController inventoryUI;
    [SerializeField] private GameObject storagePanel;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform inventorySlotGrid;
    [SerializeField] private Transform storageSlotGrid;
    [SerializeField] private Button closeButton;
    [SerializeField, Min(0)] private int initialSlotCount = 21;

    private readonly List<StorageSlot> slots = new List<StorageSlot>();
    private readonly List<GameObject> slotFrames = new List<GameObject>();
    private StateManager stateManager;
    private Canvas dragCanvas;
    private bool initialized;
    private int createdStorageSlots;

    private void Awake() => Initialize();

    private void Initialize()
    {
        if (initialized)
            return;
        initialized = true;
        if (inventoryUI == null)
            inventoryUI = FindFirstObjectByType<InventoryUIController>(FindObjectsInactive.Include);
        storageData?.EnsureDefaults(initialSlotCount);
        if (storagePanel != null)
        {
            CanvasGroup panelGroup = storagePanel.GetComponent<CanvasGroup>() ??
                storagePanel.AddComponent<CanvasGroup>();
            panelGroup.ignoreParentGroups = true;
            panelGroup.alpha = 1f;
            panelGroup.interactable = true;
            panelGroup.blocksRaycasts = true;
            Transform root = storagePanel.transform;
            if (inventorySlotGrid == null) inventorySlotGrid = root.Find("InventorySlotGrid");
            if (storageSlotGrid == null)
                storageSlotGrid = root.Find("Scroll View/Viewport/StorageSlotGrid") ?? root.Find("StorageSlotGrid");
            if (closeButton == null) closeButton = root.Find("CloseButton")?.GetComponent<Button>();
        }
        if (slotPrefab != null && slotPrefab.GetComponentsInChildren<StorageSlot>(true).Length == 1 &&
            slotPrefab.GetComponentInChildren<InventorySlot>(true) == null &&
            slotPrefab.GetComponentInChildren<DecompositionSlot>(true) == null)
        {
            CreateSlots(inventorySlotGrid, InventorySO.universalSlotCount, false);
            CreateMissingStorageSlots();
        }
        else
            Debug.LogError("Storage slot prefab must contain one StorageSlot on its Slot child and no InventorySlot or DecompositionSlot components.", this);
        closeButton?.onClick.AddListener(Close);
        stateManager = StateManager.Instance;
        stateManager.StateChanged += ApplyState;
        ApplyState(stateManager.CurrentState);
    }

    private void CreateSlots(Transform grid, int count, bool storage, int startIndex = 0)
    {
        if (grid == null)
            return;
        for (int i = startIndex; i < startIndex + count; i++)
        {
            GameObject frame = Instantiate(slotPrefab, grid, false);
            frame.name = $"{(storage ? "Storage" : "Inventory")} Slot {i + 1}";
            StorageSlot slot = frame.GetComponentInChildren<StorageSlot>(true);
            slot.Initialize(this, storage, i);
            slot.gameObject.SetActive(true);
            frame.SetActive(true);
            slots.Add(slot);
            slotFrames.Add(frame);
            if (storage) createdStorageSlots++;
        }
    }

    public bool Open()
    {
        Initialize();
        CreateMissingStorageSlots();
        if (stateManager.CurrentState != GameState.Playing || storagePanel == null ||
            inventoryData == null || storageData == null ||
            slots.Count != InventorySO.universalSlotCount + storageData.SlotCount)
            return false;
        if (!stateManager.RequestState(GameState.Storage)) return false;
        Tutorial.RequestTutorial("storage-basics", "This is your storage box.");
        return true;
    }

    public void Close() => StateManager.Instance.CloseState(GameState.Storage);

    private void ApplyState(GameState state)
    {
        bool visible = state == GameState.Storage;
        if (!visible && StorageSlot.DraggedSlot != null && StorageSlot.DraggedSlot.Manager == this)
            StorageSlot.DraggedSlot.CancelDrag();
        if (visible) Refresh();
        if (storagePanel != null) storagePanel.SetActive(visible);
    }

    public InventorySlotData GetSlot(bool storage, int index)
    {
        if (inventoryData == null || storageData == null)
            return null;
        return inventoryData.GetStorageSlot(storageData, storage, index);
    }

    public bool CanMove(StorageSlot source, StorageSlot destination) =>
        source != null && destination != null && source.Manager == this && destination.Manager == this &&
        stateManager != null && stateManager.CurrentState == GameState.Storage && storageData != null &&
        inventoryData.CanStorageMove(storageData, source.IsStorage, source.Index, destination.IsStorage, destination.Index);

    public bool QuickMove(StorageSlot source)
    {
        if (source == null || source.Manager != this || StorageSlot.DraggedSlot != null ||
            stateManager == null || stateManager.CurrentState != GameState.Storage) return false;
        bool moved = inventoryData.QuickStorageMove(storageData, source.IsStorage, source.Index);
        if (moved)
        {
            Refresh();
            inventoryUI?.RefreshUI();
        }
        return moved;
    }

    public bool SplitStack(StorageSlot source)
    {
        if (source == null || source.Manager != this || StorageSlot.DraggedSlot != null ||
            stateManager == null || stateManager.CurrentState != GameState.Storage) return false;
        if (!inventoryData.SplitStorage(storageData, source.IsStorage, source.Index)) return false;
        Refresh();
        inventoryUI?.RefreshUI();
        return true;
    }

    public void Move(StorageSlot source, StorageSlot destination)
    {
        if (CanMove(source, destination) && inventoryData.MoveStorage(storageData,
            source.IsStorage, source.Index, destination.IsStorage, destination.Index))
        {
            Refresh();
            inventoryUI?.RefreshUI();
        }
    }

    private void CreateMissingStorageSlots()
    {
        if (storageData == null || storageSlotGrid == null || slotPrefab == null ||
            slotPrefab.GetComponentsInChildren<StorageSlot>(true).Length != 1 ||
            slotPrefab.GetComponentInChildren<InventorySlot>(true) != null ||
            slotPrefab.GetComponentInChildren<DecompositionSlot>(true) != null)
            return;
        CreateSlots(storageSlotGrid, Mathf.Max(0, storageData.SlotCount - createdStorageSlots), true, createdStorageSlots);
    }

    // Call this from a merchant unlock after its payment succeeds.
    public bool AddSlots(int count)
    {
        Initialize();
        if (storageData == null || !storageData.AddSlots(count))
            return false;
        CreateMissingStorageSlots();
        Refresh();
        return true;
    }

    public void Refresh()
    {
        CreateMissingStorageSlots();
        foreach (StorageSlot slot in slots)
            if (slot != null) slot.Refresh();
    }

    public Transform GetDragLayer()
    {
        if (dragCanvas == null)
        {
            Canvas root = storagePanel.GetComponentInParent<Canvas>();
            if (root == null) return storagePanel.transform;
            var layer = new GameObject("Storage Drag Layer", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
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
        if (StorageSlot.DraggedSlot != null && StorageSlot.DraggedSlot.Manager == this)
            StorageSlot.DraggedSlot.CancelDrag();
        if (stateManager != null) stateManager.CloseState(GameState.Storage);
    }

    private void OnDestroy()
    {
        if (stateManager != null) stateManager.StateChanged -= ApplyState;
        closeButton?.onClick.RemoveListener(Close);
        foreach (GameObject frame in slotFrames)
            if (frame != null) Destroy(frame);
        if (dragCanvas != null) Destroy(dragCanvas.gameObject);
    }
}
