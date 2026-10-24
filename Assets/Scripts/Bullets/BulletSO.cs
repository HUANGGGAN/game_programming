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

[CreateAssetMenu(fileName = "NewBulletData", menuName = "Weapons/Bullet Data")]
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
    [Min(0)] public int isPiercing = 0; // Enemies passed through before stopping on the next hit
    public int bulletMode = 0; // 0: normal; 1: explosive bullet detonates at the captured aim position
    public bool isExplosive = false; // If true, bullet can explode on impact
    public float explosionRadius = 2f; // Only relevant if isExplosive is true
    public float knockbackForce = 5f; // Force applied to enemies on hit
    public float explosiveDamage = 20f; // Damage dealt by the explosion
    [Range(0f, 1f)] public float critRate = 0.05f;
    public float critMul = 3f;
    public EffectDefinition[] effects;
    public int[] attackAttributes; // Attribute ID for the bullet's attack type (e.g., fire, ice, poison)
}
