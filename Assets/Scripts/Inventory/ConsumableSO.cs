using UnityEngine;

[CreateAssetMenu(fileName = "NewConsumable", menuName = "Item/Consumable")]
public class ConsumableSO : ItemSO
{
    [Header("Consumable Stats")]
    public int[] effects; // Array of effect IDs that this consumable applies when used (e.g., heal, buff, debuff)
    public int[] effectDurations; // Array of durations for each effect in seconds
}