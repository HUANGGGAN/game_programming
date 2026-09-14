using System;
using UnityEngine;

[CreateAssetMenu(fileName = "scoreData", menuName = "Game/Score Data")]
public class ScoreData : ScriptableObject
{
    public int currentScore;

    // Event broadcast whenever the score updates
    public event Action OnScoreChanged;

    /// <summary>
    /// Call this method anywhere to safely add points and notify UI listeners.
    /// </summary>
    public void AddScore(int amount)
    {
        currentScore += amount;
        OnScoreChanged?.Invoke(); // Fire event if there are listeners
    }

    /// <summary>
    /// Call this when starting a new game session.
    /// </summary>
    public void ResetScore()
    {
        currentScore = 0;
        OnScoreChanged?.Invoke();
    }

    public void SetScore(int amount)
    {
        currentScore = amount;
        OnScoreChanged?.Invoke();
    }
}