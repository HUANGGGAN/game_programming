using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    [Header("Data Reference")]
    [SerializeField] private PlayerData playerData;

    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Format Settings")]
    [SerializeField] private string prefix = "Score: ";

    private void OnEnable()
    {
        if (playerData != null)
        {
            // 1. Subscribe to the change event
            playerData.OnScoreChanged += UpdateScoreDisplay;
        }

        // 2. Refresh display once on startup to show initial score
        UpdateScoreDisplay();
    }

    private void OnDisable()
    {
        if (playerData != null)
        {
            // 3. ALWAYS unsubscribe to prevent memory leaks / null references!
            playerData.OnScoreChanged -= UpdateScoreDisplay;
        }
    }

    // This method ONLY executes when OnScoreChanged is fired
    private void UpdateScoreDisplay()
    {
        if (playerData != null && scoreText != null)
        {
            scoreText.text = $"{prefix}{playerData.currentScore}";
        }
    }
}