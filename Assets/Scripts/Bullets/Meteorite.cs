using UnityEngine;

/// <summary>Collider-free pooled meteor. Add FinishExplosion as an event at the end of the explosion clip.</summary>
public class Meteorite : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField, Min(0.01f)] private float fallSpeed = 15f;
    [SerializeField, Min(0f)] private float screenEdgeMargin = 1f;
    [SerializeField, Min(0f)] private float explosionRadius = 2f;
    [SerializeField, Min(0f)] private float explosionDamage = 20f;
    [SerializeField, Min(0f)] private float knockbackForce = 5f;
    [SerializeField] private EffectDefinition[] effects;
    [SerializeField] private int[] attackAttributes;
    [Tooltip("Animator state name used for automatic return after its non-looping animation completes.")]
    [SerializeField] private string explosionStateName = "ExplosionStage";
    private Vector2 target;
    private Player attacker;
    private bool exploding;
    private bool launched;
    private bool enteredExplosionState;

    public void Launch(Vector2 position, Player owner)
    {
        target = position;
        attacker = owner;
        exploding = false;
        enteredExplosionState = false;
        launched = true;
        Camera camera = Camera.main;
        float distance = 10f;
        if (camera != null)
        {
            float depth = Vector3.Dot(new Vector3(position.x, position.y, 0f) - camera.transform.position, camera.transform.forward);
            Vector3 topLeft = camera.ViewportToWorldPoint(new Vector3(0f, 1f, depth));
            // Equal X/Y displacement keeps the incoming path at 45 degrees.
            distance = Mathf.Max(0f, Mathf.Min(position.x - topLeft.x, topLeft.y - position.y)) + screenEdgeMargin;
        }
        transform.position = new Vector3(position.x - distance, position.y + distance, 0f);
        gameObject.SetActive(true);
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
            animator.ResetTrigger("Explode");
        }
    }

    private void Update()
    {
        if (!launched) return;
        if (exploding)
        {
            if (animator == null) { FinishExplosion(); return; }
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            bool transitioning = animator.IsInTransition(0);
            bool inExplosion = state.IsName(explosionStateName);
            if (inExplosion || (transitioning && animator.GetNextAnimatorStateInfo(0).IsName(explosionStateName)))
                enteredExplosionState = true;

            // Exit is a state-machine node, not a state we can query by name.
            // Once the explosion has started, leaving it also completes this pooled meteor.
            if (!transitioning && enteredExplosionState &&
                (!inExplosion || (!state.loop && state.normalizedTime >= 1f)))
                FinishExplosion();
            return;
        }
        transform.position = Vector3.MoveTowards(transform.position, new Vector3(target.x, target.y, 0f), Mathf.Max(0.01f, fallSpeed) * Time.deltaTime);
        if (((Vector2)transform.position - target).sqrMagnitude > 0.000001f) return;
        exploding = true;
        if (animator != null) animator.SetTrigger("Explode");
        Explosion.Perform(target, explosionRadius, explosionDamage, knockbackForce, effects, attackAttributes, attacker,
            fallbackDirection: new Vector2(1f, -1f).normalized);
    }

    // Animation event on the final frame of the explosion clip (or automatic ExplosionStage completion).
    public void FinishExplosion()
    {
        if (!exploding) return;
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        launched = false;
        exploding = false;
        enteredExplosionState = false;
        attacker = null;
    }
}

public sealed class MeteoriteAttack : IWeaponAttackHandler
{
    public bool CanHandle(WeaponSO weapon) => weapon is WandSO wand && wand.attackMode == 1;

    public bool TryPerform(WeaponSO weapon, Vector2 origin, Vector2 direction, System.Func<bool> tryConsumeAmmo,
        Vector2? targetPosition = null, Player attacker = null)
    {
        if (!CanHandle(weapon) || !targetPosition.HasValue || BulletPool.Instance == null || !BulletPool.Instance.CanSummonMeteorite)
            return false;
        if (!tryConsumeAmmo()) return false;
        WandSO wand = (WandSO)weapon;
        BulletPool.Instance.SummonMeteoriteBurst(targetPosition.Value, wand.meteoriteScatterRadius, attacker);
        return true;
    }
}
