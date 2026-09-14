using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyChase : MonoBehaviour
{
    [Header("Target Setup")]
    [SerializeField] private Transform player;

    [Header("Health Setup")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private string bulletTag = "Bullet"; // Set your Bullet prefab tag

    private NavMeshAgent agent;
    private int currentHealth;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // 2D settings
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    private void OnEnable()
    {
        // Reset health every time the enemy is spawned/reactivated
        currentHealth = maxHealth;

        // Auto-find Player if not assigned in Inspector
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
    }

    private void Update()
    {
        if (player == null || !agent.isOnNavMesh)
            return;

        agent.SetDestination(player.position);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if hit by a Bullet
        if (other.CompareTag(bulletTag) || other.GetComponent<Bullet2D>() != null)
        {
            TakeDamage(1);
        }
    }

    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Stop movement before deactivating
        if (agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        // Deactivate instead of Destroying to support object pooling
        gameObject.SetActive(false);
    }
}