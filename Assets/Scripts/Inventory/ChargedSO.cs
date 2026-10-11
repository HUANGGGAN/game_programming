using UnityEngine;

[CreateAssetMenu(fileName = "NewChargedWeapon", menuName = "Weapon/Charged")]
public class ChargedSO : GunSO, IChargedProjectileWeapon
{
    [Header("Charge Stats")]
    public float baseDamage = 10f;
    public float baseSpeed = 10f;
    [Min(0f)] public float maxChargeTime = 1f;
    public float attackMul = 3f;
    public float speedMul = 2f;
    public float probMul = 2f;

    public float BaseDamage => baseDamage;
    public float BaseSpeed => baseSpeed;
    public float MaxChargeTime => maxChargeTime;
    public float AttackMultiplier => attackMul;
    public float SpeedMultiplier => speedMul;
    public float ProbabilityMultiplier => probMul;
}