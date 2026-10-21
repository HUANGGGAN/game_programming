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
    }

    public void SetScore(int amount)
    {
        currentScore = amount;
        OnScoreChanged?.Invoke();
    }

    public void AddMoney(int amount)
    {
        money += amount;
    }

    public void SetMoney(int amount)
    {
        money = amount;
    }

    public void SetMaxHealth(float amount)
    {
        maxHealth = Mathf.Max(0f, amount);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    public void ApplyDamage(float amount)
    {
        currentHealth = Mathf.Max(0f, currentHealth - amount);
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    public void SetMaxMP(int amount)
    {
        maxMP = Mathf.Max(0, amount);
        currentMP = Mathf.Clamp(currentMP, 0, maxMP);
    }

    public void UseMP(int amount)
    {
        currentMP = Mathf.Max(0, currentMP - Mathf.Max(0, amount));
    }

    public bool TryUseMP(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (currentMP < amount)
            return false;

        if (amount > 0)
            UseMP(amount);
        return true;
    }

    public void RestoreMP(int amount)
    {
        currentMP = Mathf.Min(maxMP, currentMP + amount);
    }
}
