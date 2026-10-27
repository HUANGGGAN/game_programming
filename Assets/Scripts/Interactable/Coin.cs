using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] private int scoreValue = 10;

    [Header("Data References")]
    [SerializeField] private PlayerData playerData;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verify that the object entering the trigger is the Player
        if (other.CompareTag("Player"))
        {
            // Optional: Access player components or increment global score here
            if (playerData != null)
            {
                playerData.AddScore(scoreValue);
            }

            // Destroy this coin GameObject immediately
            Destroy(gameObject);
        }
    }
}