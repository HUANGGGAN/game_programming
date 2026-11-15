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

    [Header("Death Effect")]
    [Tooltip("0: None. 1: Fire enemy bullets in eight directions.")]
    [Range(0, 1)] public int DieEffectID;
    public EnemyBulletSO deathEffectBullet;
    [Min(0f)] public float deathEffectDelay = 0.2f;

    [Header("Attack Module")]
    public EnemyAttackType attackType = EnemyAttackType.Melee;
    public EnemyBulletSO rangedBullet;
    [Tooltip("Solid layers that block ranged line of sight. BulletBlocker-tagged objects always block.")]
    public LayerMask obstacleLayers;

    [Header("Attack Stats")]
    [Min(0f)] public float attackRange = 1.5f;
    [Min(0f)] public float damage = 10f;
    [Min(0f)] public float knockbackForce = 4f;
    [Min(0f)] public float attackInterval = 1.2f;
    [Min(0f)] public float attackDuration = 0.4f;
    public int[] attackAttributes;

    [Header("Movement & Health")]
    [Min(0f)] public float speed = 2.5f;
    [Min(0.01f)] public float HP = 35f;
    [Range(0f, 1f)] public float knockbackResistance = 0f;
}
