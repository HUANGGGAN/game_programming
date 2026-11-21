using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyBulletData", menuName = "Game/Enemy Bullet Data")]
public class EnemyBulletSO : ScriptableObject
{
    [Header("Visuals & Identification")]
    public string bulletId;       // Used by pooler to distinguish bullet pools
    public Sprite bulletSprite;
    [Tooltip("Keep the prefab angle instead of aligning with the firing direction.")]
    public bool preserveAngle = false;
    [Tooltip("Scale of the bullet image only; does not affect its collider.")]
    public Vector2 imageScale = Vector2.one;
    [Min(0)] public int ParticleID = 0; // 0: no particle effect on deactivation
    public Color tint = Color.white;
    [Tooltip("Visual rotation in degrees per second. Zero disables spinning; negative values reverse it.")]
    public float spinningSpeed = 0f;
    public Vector2 colliderSize = new Vector2(0.2f, 0.2f);

    [Header("Stats")]
    public float damage = 10f;
    public float speed = 15f;
    public float lifetime = 3f;
    [Tooltip("Additional players pierced before deactivation.")]
    [Min(0)] public int isPiercing = 0;
    [Tooltip("0: impact projectile, 1: explode at the player position recorded when fired.")]
    [Range(0, 1)]
    public int bulletMode = 0;
    public bool isExplosive = false; // If true, bullet can explode on impact
    [Min(0)] public int explosionParticleId = 0;
    public float explosionRadius = 2f; // Only relevant if isExplosive is true
    public float knockbackForce = 5f; // Force applied to enemies on hit
    public float explosiveDamage = 20f; // Damage dealt by the explosion
    public EffectDefinition[] effects;
    public int[] attackAttributes; // Attribute ID for the bullet's attack type (e.g., fire, ice, poison)
}
