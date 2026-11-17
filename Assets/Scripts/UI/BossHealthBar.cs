using UnityEngine;

[RequireComponent(typeof(HealthBar), typeof(CanvasGroup))]
public class BossHealthBar : MonoBehaviour
{
    [SerializeField] private Boss boss;
    [SerializeField] private TreeBoss treeBoss;
    [SerializeField] private InventoryManager inventoryManager;

    private HealthBar healthBar;
    private CanvasGroup canvasGroup;
    [SerializeField, Range(0f, 1f)] private float visibleAlpha = 1f;

    private void Awake()
    {
        healthBar = GetComponent<HealthBar>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (boss == null && treeBoss == null)
            treeBoss = FindFirstObjectByType<TreeBoss>(FindObjectsInactive.Include);
        if (boss == null && treeBoss == null)
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

    public void SetBoss(TreeBoss value)
    {
        treeBoss = value;
        boss = null;
        if (healthBar != null && canvasGroup != null) Refresh();
    }

    private void OnEnable()
    {
        if (healthBar != null && canvasGroup != null) Refresh();
    }

    private void Refresh()
    {
        if (visibleAlpha <= 0f) visibleAlpha = 1f;
        EnemyChase activeBoss = treeBoss != null ? (EnemyChase)treeBoss : boss;
        bool dead = treeBoss != null ? treeBoss.IsDead : boss == null || boss.IsDead;
        bool visible = activeBoss != null && activeBoss.isActiveAndEnabled && !dead && activeBoss.CurrentHealth > 0f &&
            (inventoryManager == null || !inventoryManager.IsInventoryOpen);
        // Hide the visuals while keeping this controller active to detect inventory closing.
        canvasGroup.alpha = visible ? visibleAlpha : 0f;
        healthBar.enabled = visible;

        if (activeBoss != null)
        {
            healthBar.UpdateHealth(activeBoss.CurrentHealth, activeBoss.MaxHealth);
            healthBar.SetShielded(treeBoss == null && boss != null && boss.ShieldCount > 0);
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
