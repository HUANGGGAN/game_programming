/// <summary>Optional ranged enemy subclass. Configure its bullet and obstacle layers on EnemySO.</summary>
public class RangedEnemy : EnemyChase
{
    protected override EnemyAttackType AttackType => EnemyAttackType.Ranged;
}
