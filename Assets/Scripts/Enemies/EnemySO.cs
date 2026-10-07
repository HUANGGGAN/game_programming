using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/Enemy Data")]
public class EnemySO : ScriptableObject
{
    [Header("Status Effects")]
    public int[] effect;
    public int[] resistances;

    [Header("Combat Stats")]
    public float attackRange = 1.5f;
    public float damage = 10f;
    public float knockbackForce = 4f;
    public float knockbackResistance = 0f;
    public float speed = 2.5f;
    public float attackInterval = 1.2f;
    public float attackDuration = 0.4f;
    public float HP = 35f;
}
