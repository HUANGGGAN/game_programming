using UnityEngine;

[CreateAssetMenu(fileName = "NewInventory", menuName = "Inventory/Inventory Storage")]
public class InventorySO : ScriptableObject
{
    // Fixed size array for 12 slots
    public ItemSO[] slots = new ItemSO[12];

    public void SwapSlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= slots.Length || indexB < 0 || indexB >= slots.Length)
            return;

        ItemSO temp = slots[indexA];
        slots[indexA] = slots[indexB];
        slots[indexB] = temp;
    }
}