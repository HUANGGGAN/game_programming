using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/Enemy Data")]
public class EnemySO : ScriptableObject
{
    [Header("Visuals")]
    public Sprite sprite;
    public bool spriteFacesRightByDefault = true;

    [Header("Status Effects")]
    public EffectDefinition[] effects;
    public int[] resistances;
    public int[] weaknesses;

    [Header("Spawn Settings")]
    [Min(0.01f)] public float enemy_point = 1f;
    [Min(0f)] public float enemy_difficulty = 1f;
    [Min(0)] public int money = 1;

    [Header("Combat Stats")]
    public float attackRange = 1.5f;
    public float damage = 10f;
    public float knockbackForce = 4f;
    public float knockbackResistance = 0f;
    public float speed = 2.5f;
    public float attackInterval = 1.2f;
    public float attackDuration = 0.4f;
    public float HP = 35f;
    public int[] attackAttributes; // Attribute IDs for the bullet's attack type (e.g., fire, ice, poison)
}
