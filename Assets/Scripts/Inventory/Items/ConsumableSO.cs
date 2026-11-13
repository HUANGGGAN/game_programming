using UnityEngine;

[CreateAssetMenu(fileName = "NewConsumable", menuName = "Item/Consumable")]
public class ConsumableSO : ItemSO
{
    [Header("Consumable Stats")]

    public int id; // Unique identifier for the consumable item
    [Min(0f)] public float cooldown = 1f;
}
