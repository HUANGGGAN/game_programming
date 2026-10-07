using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Item/Normal")]
public class ItemSO : ScriptableObject
{
    [Header("Basic Stats")]
    public string itemName = "New Item";
    public int itemID; // Unique identifier for the item
    public string rarity; // e.g., Common, Rare, Epic, Legendary
    public Sprite icon;
    [TextArea] public string description;
}

public class WeaponSO : ItemSO
{
    [Header("Weapon Stats")]
    public float shootInterval = 1.0f; // Time between shots
    public bool consumeAmmo = true; // Whether the weapon consumes ammo
    public bool hasLimitedAmmo = false; // Whether the weapon has limited ammo
    public float knockbackForce = 1.0f; // Force applied to enemies on hit
    public Vector2 firePointOffset = new Vector2(0.5f, 0.1f); // Offset for the fire point relative to the weapon's position
    public int[] effect; // Array of effect IDs that this weapon can apply to enemies (e.g., burn, freeze, poison)
}

[CreateAssetMenu(fileName = "NewGun", menuName = "Item/Gun")]
public class GunSO : WeaponSO
{
    [Header("Gun Stats")]
    public int capacity = 30; // Number of bullets per magazine
    public float reloadTime = 2.0f; // Time to reload in seconds
    public float spread = 5.0f; // Bullet spread in degrees
    public int burstCount = 1; // Number of bullets fired in a burst
    public BulletSO bulletData; // Reference to the bullet data used by this gun
}

[CreateAssetMenu(fileName = "NewMelee", menuName = "Item/Melee")]
public class MeleeSO : WeaponSO
{
    [Header("Melee Stats")]
    public BulletSO bulletData; // May have a bullet data for melee attacks
    public float attackRange = 2.0f; // Range of the melee attack
    public float attackAngle = 90.0f; // Angle of the melee attack in degrees
    public float attackDuration = 0.5f; // Duration of the melee attack animation
}

[CreateAssetMenu(fileName = "NewWand", menuName = "Item/Wand")]
public class WandSO : WeaponSO
{
    [Header("Wand Stats")]
    public int MPCost = 0; // Mana cost per shot
    public BulletSO bulletData; // Reference to the bullet data used by this gun
    // Additional wand-specific stats can be added here
}

[CreateAssetMenu(fileName = "NewArmor", menuName = "Item/Armor")]
public class ArmorSO : ItemSO
{
    [Header("Armor Stats")]
    public float defense = 10.0f; // Amount of damage reduction
    public float healthBonus = 0.0f; // Bonus health provided by the armor
    public float speedPenalty = 0.0f; // Penalty to movement speed when wearing the armor
    public float knockbackResistance = 0.0f; // Resistance to knockback effects
    public int[] resistances; // Array of resistance IDs that this armor provides (e.g., fire, ice, poison)
}

[CreateAssetMenu(fileName = "NewConsumable", menuName = "Item/Consumable")]
public class ConsumableSO : ItemSO
{
    [Header("Consumable Stats")]
    public int[] effects; // Array of effect IDs that this consumable applies when used (e.g., heal, buff, debuff)
}