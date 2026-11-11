using UnityEngine;

public static class CriticalAttack
{
    public static float RollDamage(float baseDamage, float critRate, float critMultiplier)
    {
        return UnityEngine.Random.value < Mathf.Clamp01(critRate)
            ? baseDamage * critMultiplier
            : baseDamage;
    }
}

[CreateAssetMenu(fileName = "NewBulletData", menuName = "Weapon/Bullet Data")]
public class BulletSO : ScriptableObject
{
    [Header("Visuals & Identification")]
    public string bulletId;       // Used by pooler to distinguish bullet pools
    public Sprite bulletSprite;
    [Min(0)] public int ParticleID = 0; // 0: no particle effect on deactivation
    public Color tint = Color.white;
    public Vector2 colliderSize = new Vector2(0.2f, 0.2f);

    [Header("Stats")]
    public float damage = 10f;
    public float speed = 15f;
    public float lifetime = 3f;
    [Tooltip("Enemies pierced in normal modes. In type 0 mode 3, total enemy/obstacle hits before deactivation (minimum 1).")]
    [Min(0)] public int isPiercing = 0;
    [Tooltip("Type 0: 0 normal, 1 target explosion, 2 reflect off enemies/obstacles/screen edges, 3 random direction on hits.")]
    public int bulletMode = 0;
    public bool isExplosive = false; // If true, bullet can explode on impact
    public float explosionRadius = 2f; // Only relevant if isExplosive is true
    public float knockbackForce = 5f; // Force applied to enemies on hit
    public float explosiveDamage = 20f; // Damage dealt by the explosion
    [Range(0f, 1f)] public float critRate = 0.05f;
    public float critMul = 3f;
    public EffectDefinition[] effects;
    public int[] attackAttributes; // Attribute ID for the bullet's attack type (e.g., fire, ice, poison)
}
