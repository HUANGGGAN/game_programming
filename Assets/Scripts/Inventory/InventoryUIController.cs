using UnityEngine;

public class InventoryUIController : MonoBehaviour
{
    [SerializeField] private InventorySO inventoryData;
    [SerializeField] private Transform slotGridTransform;

    private InventorySlot[] uiSlots;

    private void Awake()
    {
        uiSlots = slotGridTransform.GetComponentsInChildren<InventorySlot>();
    }

    private void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (inventoryData == null || uiSlots == null)
            return;

        for (int i = 0; i < uiSlots.Length; i++)
        {
            uiSlots[i].slotIndex = i;

            if (i < inventoryData.slots.Length)
            {
                uiSlots[i].SetupSlot(
                    inventoryData.slots[i],
                    i
                );
            }
            else
            {
                uiSlots[i].SetupSlot(null, i);
            }
        }
    }

    public void SwapItems(int fromIndex, int toIndex)
    {
        inventoryData.SwapSlots(fromIndex, toIndex);
        RefreshUI();
    }
}