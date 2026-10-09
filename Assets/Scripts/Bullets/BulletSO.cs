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
    public Color tint = Color.white;
    public Vector2 colliderSize = new Vector2(0.2f, 0.2f);

    [Header("Stats")]
    public float damage = 10f;
    public float speed = 15f;
    public float lifetime = 3f;
    public bool isPiercing = false; // If true, bullet can pass through enemies
    public bool isExplosive = false; // If true, bullet can explode on impact
    public float explosionRadius = 2f; // Only relevant if isExplosive is true
    public float knockbackForce = 5f; // Force applied to enemies on hit
    [Range(0f, 1f)] public float critRate = 0.05f;
    public float critMul = 3f;
    public EffectDefinition[] effects;
    public int attackAttribute = 0; // Attribute ID for the bullet's attack type (e.g., fire, ice, poison)
}