using UnityEngine;
using UnityEngine.UI;

public interface IPlayerBar
{
    void RefreshFromData(PlayerData data);
}

public class HealthBar : MonoBehaviour, IPlayerBar
{
    [Header("UI Components")]
    [SerializeField] private Image fillImage;

    [Header("Settings")]
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private bool isManaBar = false;

    private float targetFillAmount = 1f;
    private Color originalFillColor;
    private bool hasOriginalFillColor;

    public void SetShielded(bool shielded)
    {
        if (fillImage == null)
            return;

        if (!hasOriginalFillColor)
        {
            originalFillColor = fillImage.color;
            hasOriginalFillColor = true;
        }
        fillImage.color = shielded
            ? new Color(0.8f, 0.8f, 0.8f, originalFillColor.a)
            : originalFillColor;
    }

    private void Update()
    {
        if (fillImage != null && Mathf.Abs(fillImage.fillAmount - targetFillAmount) > 0.001f)
        {
            fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFillAmount, Time.deltaTime * smoothSpeed);
        }
    }

    public void SetManaBar(bool value)
    {
        isManaBar = value;
    }

    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        if (maxHealth <= 0f)
        {
            targetFillAmount = 0f;
            return;
        }

        targetFillAmount = Mathf.Clamp01(currentHealth / maxHealth);
    }

    public void RefreshFromData(PlayerData data)
    {
        if (data == null)
            return;

        float current = isManaBar ? data.currentMP : data.currentHealth;
        float max = isManaBar ? data.maxMP : data.maxHealth;
        UpdateHealth(current, max);
    }
}
