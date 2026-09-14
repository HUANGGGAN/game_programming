using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    [Header("Data Reference")]
    [SerializeField] private ScoreData scoreData;

    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Format Settings")]
    [SerializeField] private string prefix = "Score: ";

    private void OnEnable()
    {
        if (scoreData != null)
        {
            // 1. Subscribe to the change event
            scoreData.OnScoreChanged += UpdateScoreDisplay;
        }

        // 2. Refresh display once on startup to show initial score
        UpdateScoreDisplay();
    }

    private void OnDisable()
    {
        if (scoreData != null)
        {
            // 3. ALWAYS unsubscribe to prevent memory leaks / null references!
            scoreData.OnScoreChanged -= UpdateScoreDisplay;
        }
    }

    // This method ONLY executes when OnScoreChanged is fired
    private void UpdateScoreDisplay()
    {
        if (scoreData != null && scoreText != null)
        {
            scoreText.text = $"{prefix}{scoreData.currentScore}";
        }
    }
}