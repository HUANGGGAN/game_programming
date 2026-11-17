using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BossEncounterTrigger : MonoBehaviour
{
    private BossManager manager;
    public void Initialize(BossManager owner) => manager = owner;
    private void OnTriggerEnter2D(Collider2D other) => TryActivate(other);
    private void OnTriggerStay2D(Collider2D other) => TryActivate(other);
    private void TryActivate(Collider2D other)
    {
        if (other.GetComponentInParent<Player>() != null && manager != null)
            manager.ActivateEncounter();
    }
}
