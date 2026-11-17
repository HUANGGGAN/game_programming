using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Run after all Awake callbacks, before enemy Start/Update, keeping renderers visible.
[DefaultExecutionOrder(-1000)]
public class FrozenEnemyGroup : MonoBehaviour
{
    private readonly List<Behaviour> suspended = new List<Behaviour>();
    private readonly List<Rigidbody2D> bodies = new List<Rigidbody2D>();
    private readonly List<Collider2D> colliders = new List<Collider2D>();
    public bool IsReleased { get; private set; }

    private void Start()
    {
        foreach (Behaviour component in GetComponentsInChildren<Behaviour>(true))
        {
            if (component == this || !component.enabled ||
                (component is not MonoBehaviour && component is not Animator && component is not NavMeshAgent)) continue;
            suspended.Add(component);
            component.enabled = false;
        }
        foreach (Rigidbody2D body in GetComponentsInChildren<Rigidbody2D>(true))
        {
            if (!body.simulated) continue;
            bodies.Add(body);
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;
        }
        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>(true))
        {
            if (!collider.enabled) continue;
            colliders.Add(collider);
            collider.enabled = false;
        }
    }

    public void Release()
    {
        if (IsReleased) return;
        IsReleased = true;
        foreach (Rigidbody2D body in bodies)
            if (body != null) body.simulated = true;
        foreach (Collider2D collider in colliders)
            if (collider != null) collider.enabled = true;
        // Restore navigation before enemy OnEnable initializes its attack module.
        foreach (Behaviour component in suspended)
            if (component is NavMeshAgent) component.enabled = true;
        foreach (Behaviour component in suspended)
            if (component != null && component is not NavMeshAgent) component.enabled = true;
        bodies.Clear();
        colliders.Clear();
        suspended.Clear();
    }
}
