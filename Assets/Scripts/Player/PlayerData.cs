using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Game/Player Data")]
public class PlayerData : ScriptableObject
{
    public int currentScore;
    public int money;
    public float currentHealth;
    public float maxHealth = 100f;
    public int currentMP;
    public int maxMP = 100;

    public event Action OnScoreChanged;
    public event Action OnPlayerStatsChanged;

    public void AddScore(int amount)
    {
        currentScore += amount;
        OnScoreChanged?.Invoke();
    }

    public void ResetScore()
    {
        currentScore = 0;
        money = 0;
        currentHealth = maxHealth;
        currentMP = maxMP;
        OnScoreChanged?.Invoke();
        OnPlayerStatsChanged?.Invoke();
    }

    public void SetScore(int amount)
    {
        currentScore = amount;
        OnScoreChanged?.Invoke();
    }

    public void AddMoney(int amount)
    {
        money += amount;
        OnPlayerStatsChanged?.Invoke();
    }

    public void SetMoney(int amount)
    {
        money = amount;
        OnPlayerStatsChanged?.Invoke();
    }

    public void SetMaxHealth(float amount)
    {
        maxHealth = Mathf.Max(0f, amount);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        OnPlayerStatsChanged?.Invoke();
    }

    public void ApplyDamage(float amount)
    {
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnPlayerStatsChanged?.Invoke();
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnPlayerStatsChanged?.Invoke();
    }

    public void SetMaxMP(int amount)
    {
        maxMP = Mathf.Max(0, amount);
        currentMP = Mathf.Clamp(currentMP, 0, maxMP);
        OnPlayerStatsChanged?.Invoke();
    }

    public void UseMP(int amount)
    {
        currentMP = Mathf.Max(0, currentMP - amount);
        OnPlayerStatsChanged?.Invoke();
    }

    public void RestoreMP(int amount)
    {
        currentMP = Mathf.Min(maxMP, currentMP + amount);
        OnPlayerStatsChanged?.Invoke();
    }
}
