using UnityEngine;

[CreateAssetMenu(fileName = "NewArmor", menuName = "Weapon/Armor")]
public class ArmorSO : ItemSO
{
    [Header("Armor Stats")]
    public float defense = 10.0f; // Amount of damage reduction
    public float healthBonus = 0.0f; // Bonus health provided by the armor
    public float speedPenalty = 0.0f; // Penalty to movement speed when wearing the armor
    public float knockbackResistance = 0.0f; // Resistance to knockback effects
    public int[] resistances; // Array of resistance IDs that this armor provides (e.g., fire, ice, poison)
    public int[] weaknesses;
}