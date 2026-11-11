using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "NewLazer", menuName = "Weapon/Lazer")]
public class LazerSO : WeaponSO
{
    [Header("Lazer (Type 2)")]
    public int mode = 0;
    public Color color = Color.red;
    public bool useGradient;
    [Tooltip("Beam color from its origin (left) to its endpoint (right).")]
    public Gradient gradient = new Gradient();
    [FormerlySerializedAs("thickness")]
    [Min(0.001f)] public float maxThickness = 0.1f;
    [Min(0f)] public float growDuration = 0.15f;
    [Min(0f)] public float shrinkDuration = 0.15f;
    [Min(0f)] public float maxRange = 50f;
    [Min(0)] public int endpointParticleID;
    [Tooltip("Persistent endpoint glow. Use a looping particle system with Local simulation space.")]
    public ParticleSystem endpointGlowPrefab;
    [Min(0f)] public float damage = 10f;
    [Min(0f)] public float knockbackForce = 4f;
    public EffectDefinition[] effects;
    public int[] attackAttributes;
    public LayerMask collisionLayers = ~0;
    public string obstacleTag = "BulletBlocker";
    public int sortingOrder = 10;
}
