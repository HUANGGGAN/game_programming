using UnityEngine;

public class DroppedItem : Interactable
{
    [SerializeField] private SpriteRenderer itemRenderer;
    private InventoryUIController inventoryUI;
    private ItemSO item;
    private int count;
    private Vector3 origin;
    private Vector3 scatter;
    private float elapsed;
    private float duration;
    private float height;

    public void Launch(ItemSO data, Vector3 position, float scatterRadius, float jumpHeight, float jumpDuration, int count = 1)
    {
        item = data;
        this.count = Mathf.Max(1, count);
        origin = position;
        scatter = (Vector3)(Random.insideUnitCircle * scatterRadius);
        duration = Mathf.Max(0.01f, jumpDuration);
        height = jumpHeight;
        elapsed = 0f;
        transform.position = origin;
        if (itemRenderer == null) itemRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (itemRenderer == null)
        {
            var visual = new GameObject("Item Sprite");
            visual.transform.SetParent(transform, false);
            itemRenderer = visual.AddComponent<SpriteRenderer>();
        }
        itemRenderer.sprite = item.icon;
        itemRenderer.transform.localPosition = Vector3.zero;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (elapsed >= duration) return;
        elapsed = Mathf.Min(duration, elapsed + Time.deltaTime);
        float t = elapsed / duration;
        transform.position = origin + scatter * t;
        // Keep the pickup on the ground while the visual follows a coin-like arc.
        itemRenderer.transform.localPosition = Vector3.up * (4f * height * t * (1f - t));
    }

    private void ResolveInventory()
    {
        if (inventoryUI == null)
            inventoryUI = FindFirstObjectByType<InventoryUIController>(FindObjectsInactive.Include);
    }

    public override string GetHint()
    {
        ResolveInventory();
        return inventoryUI != null && inventoryUI.CanGiveItem(item, InventorySlotType.Universal, count)
            ? $"Press {GameInput.Label(GameBinding.Interact)} to pick up" : "Inventory full";
    }

    public override bool TryInteract()
    {
        ResolveInventory();
        if (item == null || inventoryUI == null ||
            !inventoryUI.GiveItem(item, InventorySlotType.Universal, out int given, count) || given <= 0)
            return false;
        count -= given;
        if (count <= 0)
            gameObject.SetActive(false);
        return true;
    }
}
