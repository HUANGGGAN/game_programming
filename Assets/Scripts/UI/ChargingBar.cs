using UnityEngine;
using UnityEngine.UI;

public class ChargingBar : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image fillImage;

    [Header("Settings")]
    [SerializeField] private float smoothSpeed = 10f;

    private float targetFillAmount;

    private void Awake()
    {
        if (fillImage == null)
            fillImage = GetComponent<Image>();

        targetFillAmount = 0f;
        if (fillImage != null)
            fillImage.fillAmount = 0f;
    }

    private void Update()
    {
        if (fillImage != null && Mathf.Abs(fillImage.fillAmount - targetFillAmount) > 0.001f)
            fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFillAmount, Time.deltaTime * smoothSpeed);
    }

    public void SetCharge(float charge)
    {
        targetFillAmount = Mathf.Clamp01(charge);
    }

    public void ResetCharge()
    {
        targetFillAmount = 0f;
    }

    public void SetVisible(bool visible)
    {
        if (!visible)
            ResetCharge();

        if (gameObject.activeSelf != visible)
            gameObject.SetActive(visible);
    }
}