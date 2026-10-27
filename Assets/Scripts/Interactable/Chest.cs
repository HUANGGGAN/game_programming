using UnityEngine;
using UnityEngine.UI;
//using AnnulusGames.UnityProgressBar;

public class Chest : Interactable
{
    public enum ChestState { Locked, Closed, Opening }

    [SerializeField] private ItemSO requiredKey;
    [SerializeField, Min(0f)] private float openTime = 2f;
    [SerializeField] private PickableItem itemPrefab;
    [SerializeField] private InventoryUIController inventoryUI;
    [SerializeField] private GameObject progressBarPrefab;
    [SerializeField] private Vector3 progressBarOffset = new Vector3(0f, 0.8f, 0f);

    private ChestState state;
    private Transform openingPlayer;
    private float elapsedOpenTime;
    private ProgressBar progressBar;
    private bool completed;

    public ChestState State => state;

    private void Awake()
    {
        state = requiredKey != null ? ChestState.Locked : ChestState.Closed;
    }

    private void ResolveInventory()
    {
        if (inventoryUI == null)
            inventoryUI = FindFirstObjectByType<InventoryUIController>(FindObjectsInactive.Include);
    }

    private int KeyCount()
    {
        ResolveInventory();
        return inventoryUI != null ? inventoryUI.GetItemCount(requiredKey) : 0;
    }

    public override string GetHint()
    {
        if (state == ChestState.Opening)
            return string.Empty;
        if (state == ChestState.Locked)
            return $"You need {requiredKey.itemName} to open ({KeyCount()}/1)";
        return "Press F to open";
    }

    public override Color GetHintColor()
    {
        return state == ChestState.Locked ? (KeyCount() > 0 ? Color.green : Color.red) : Color.white;
    }

    public override bool TryInteract()
    {
        if (completed || state == ChestState.Opening || !IsInRange(InteractionPlayer) || itemPrefab == null)
            return false;

        if (state == ChestState.Locked)
        {
            ResolveInventory();
            if (inventoryUI == null || !inventoryUI.RemoveItemAmount(requiredKey, 1))
                return false;
            state = ChestState.Closed;
        }

        openingPlayer = InteractionPlayer;
        elapsedOpenTime = 0f;
        state = ChestState.Opening;
        EnsureProgressBar();
        if (progressBar != null)
        {
            progressBar.SetProgress(0f);
            progressBar.SetVisible(true);
        }
        if (openTime <= 0f)
            CompleteOpening();
        return true;
    }

    private void Update()
    {
        if (state != ChestState.Opening || completed)
            return;

        if (openingPlayer == null || !openingPlayer.gameObject.activeInHierarchy || !IsInRange(openingPlayer))
        {
            CancelOpening();
            return;
        }

        elapsedOpenTime += Time.deltaTime;
        if (progressBar != null)
            progressBar.SetProgress(openTime <= 0f ? 1f : elapsedOpenTime / openTime);
        if (elapsedOpenTime >= openTime)
            CompleteOpening();
    }

    private void EnsureProgressBar()
    {
        if (progressBar != null || progressBarPrefab == null)
            return;

        GameObject canvasObject = new GameObject("Chest Progress Canvas", typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.transform.localPosition = progressBarOffset;
        canvasObject.transform.localRotation = Quaternion.identity;
        canvasObject.transform.localScale = Vector3.one * 0.01f;
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = short.MaxValue;
        GameObject visual = Instantiate(progressBarPrefab, canvasObject.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        progressBar = visual.GetComponent<ProgressBar>();
        if (progressBar == null)
            progressBar = visual.AddComponent<ProgressBar>();
    }

    private void CancelOpening()
    {
        state = ChestState.Closed;
        openingPlayer = null;
        elapsedOpenTime = 0f;
        if (progressBar != null)
            progressBar.SetVisible(false);
    }

    protected override void OnDisable()
    {
        if (state == ChestState.Opening)
            CancelOpening();
        base.OnDisable();
    }

    private void CompleteOpening()
    {
        if (completed)
            return;

        completed = true;
        PickableItem spawnedItem = Instantiate(itemPrefab, transform.position, Quaternion.identity);
        spawnedItem.gameObject.SetActive(true);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
