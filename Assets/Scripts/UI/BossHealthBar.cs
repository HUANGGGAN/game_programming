using UnityEngine;

[RequireComponent(typeof(HealthBar), typeof(CanvasGroup))]
public class BossHealthBar : MonoBehaviour
{
    [SerializeField] private Boss boss;
    [SerializeField] private InventoryManager inventoryManager;

    private HealthBar healthBar;
    private CanvasGroup canvasGroup;
    private float visibleAlpha;

    private void Awake()
    {
        healthBar = GetComponent<HealthBar>();
        canvasGroup = GetComponent<CanvasGroup>();
        visibleAlpha = canvasGroup.alpha;
        if (boss == null)
            boss = FindFirstObjectByType<Boss>(FindObjectsInactive.Include);
        if (inventoryManager == null)
            inventoryManager = FindFirstObjectByType<InventoryManager>(FindObjectsInactive.Include);

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        Refresh();
    }

    private void LateUpdate()
    {
        Refresh();
    }

    private void Refresh()
    {
        bool visible = boss != null && boss.isActiveAndEnabled && !boss.IsDead && boss.CurrentHealth > 0f &&
            (inventoryManager == null || !inventoryManager.IsInventoryOpen);
        // Hide the visuals while keeping this controller active to detect inventory closing.
        canvasGroup.alpha = visible ? visibleAlpha : 0f;
        healthBar.enabled = visible;

        if (boss != null)
        {
            healthBar.UpdateHealth(boss.CurrentHealth, boss.MaxHealth);
            healthBar.SetShielded(boss.ShieldCount > 0);
        }
    }

    private void OnDisable()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        if (healthBar != null)
            healthBar.enabled = false;
    }
}
