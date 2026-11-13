using UnityEngine;

public enum MeleeAttackAnimation
{
    NoAttack = -1,
    SwingAttack = 0,
    PokeAttack = 1
}

[CreateAssetMenu(fileName = "NewMelee", menuName = "Weapon/Melee")]
public class MeleeSO : WeaponSO
{
    [Header("Melee Stats")]
    public MeleeAttackAnimation attackAnimation = MeleeAttackAnimation.SwingAttack;
    public float attackRange = 2.0f; // Range of the melee attack
    public float attackAngle = 90.0f; // Angle of the melee attack in degrees
    public float attackDamage = 10.0f; // Damage value for the melee attack
    public float attackDuration = 0.1f; // Duration of the melee attack
    public float knockbackForce = 1.0f; // Force applied to enemies on hit
    [Range(0f, 1f)] public float critRate = 0.05f;
    public float critMul = 3f;
    public bool drawAttackRange;
    [Tooltip("ParticlePool effect ID played once per attack. 0 disables particles.")]
    [Min(0)] public int ParticleID = 0;
    public EffectDefinition[] effects;
    public int[] attackAttributes; // Attribute IDs for the bullet's attack type (e.g., fire, ice, poison)
}
