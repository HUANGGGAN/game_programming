using UnityEngine;

public class PickableItem : Interactable
{
    [SerializeField] private ItemSO item;
    [SerializeField] private int count = 1;
    [SerializeField] private int ammoCount = 0;
    [SerializeField] private InventoryUIController inventoryUI;
    [SerializeField, Tooltip("SpriteRenderer on a child object. Automatically found if unassigned.")]
    private SpriteRenderer itemSpriteRenderer;

    protected override void OnEnable()
    {
        base.OnEnable();
        UpdateItemSprite();
    }

    private void UpdateItemSprite()
    {
        if (itemSpriteRenderer != null &&
            (itemSpriteRenderer.transform == transform || !itemSpriteRenderer.transform.IsChildOf(transform)))
            itemSpriteRenderer = null;

        if (itemSpriteRenderer == null)
        {
            foreach (SpriteRenderer childRenderer in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (childRenderer.transform == transform)
                    continue;

                itemSpriteRenderer = childRenderer;
                break;
            }
        }

        if (itemSpriteRenderer != null && item != null)
            itemSpriteRenderer.sprite = item.icon;
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
            ? "Press F to pick up"
            : "Inventory full";
    }

    public override bool TryInteract()
    {
        ResolveInventory();
        if (inventoryUI == null || !inventoryUI.GiveItem(item, InventorySlotType.Universal, out int givenCount, count, ammoCount))
            return false;

        count -= givenCount;
        if (count > 0)
            return true;

        gameObject.SetActive(false);
        Destroy(gameObject);
        return true;
    }
}
