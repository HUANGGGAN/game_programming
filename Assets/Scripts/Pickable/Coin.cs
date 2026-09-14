using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] private int scoreValue = 10;

    [Header("Data References")]
    [SerializeField] private ScoreData scoreData;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verify that the object entering the trigger is the Player
        if (other.CompareTag("Player"))
        {
            // Optional: Access player components or increment global score here
            if (scoreData != null)
            {
                scoreData.AddScore(scoreValue);
            }

            // Destroy this coin GameObject immediately
            Destroy(gameObject);
        }
    }
}