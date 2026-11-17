using UnityEngine;

public class IceStatue : Interactable
{
    [SerializeField] private SpriteRenderer statueRenderer;
    [SerializeField] private Sprite deactivatedIcon;
    [SerializeField, Min(0f)] private float height = 1f;
    [SerializeField, Min(0.01f)] private float speed = 1f;
    [SerializeField, Min(0f)] private float holdRadius = 2f;
    [SerializeField, Min(0f)] private float holdTime = 3f;

    private IceStatuePuzzle puzzle;
    private Vector3 lowestPosition;
    private Sprite activeIcon;
    private float currentHeight;
    private float elapsed;
    private bool rising = true;
    private bool charging;
    private Transform holdingPlayer;
    private float wrongOrderFlashRemaining;
    private Color colorBeforeWrongOrder;
    public bool IsDeactivated { get; private set; }

    private void Awake()
    {
        if (statueRenderer == null) statueRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (statueRenderer != null && statueRenderer.transform == transform)
        {
            // Animate a visual child so the interaction and hold radius stay fixed.
            SpriteRenderer original = statueRenderer;
            var visual = new GameObject("Statue Visual");
            visual.transform.SetParent(transform, false);
            statueRenderer = visual.AddComponent<SpriteRenderer>();
            statueRenderer.sprite = original.sprite;
            statueRenderer.color = original.color;
            statueRenderer.sharedMaterial = original.sharedMaterial;
            statueRenderer.sortingLayerID = original.sortingLayerID;
            statueRenderer.sortingOrder = original.sortingOrder;
            statueRenderer.flipX = original.flipX;
            statueRenderer.flipY = original.flipY;
            original.enabled = false;
        }
        if (statueRenderer != null)
        {
            lowestPosition = statueRenderer.transform.localPosition;
            activeIcon = statueRenderer.sprite;
        }
    }

    public void Initialize(IceStatuePuzzle owner) => puzzle = owner;

    public override string GetHint() => charging ? "Stay near the statue" : "Press F to deactivate";

    public override bool TryInteract()
    {
        if (puzzle == null || IsDeactivated || charging || !IsInRange(InteractionPlayer)) return false;
        return puzzle.TryDeactivate(this, InteractionPlayer);
    }

    public void BeginDeactivation(Transform player)
    {
        holdingPlayer = player;
        elapsed = 0f;
        charging = true;
    }

    // The puzzle only ticks nearby statues, including their final descent.
    public void Tick(float deltaTime)
    {
        if (wrongOrderFlashRemaining > 0f)
        {
            wrongOrderFlashRemaining = Mathf.Max(0f, wrongOrderFlashRemaining - deltaTime);
            if (wrongOrderFlashRemaining <= 0f)
                RestoreWrongOrderColor();
        }
        float maximum = Mathf.Max(0f, height);
        if (charging && (holdingPlayer == null || !holdingPlayer.gameObject.activeInHierarchy ||
            ((Vector2)(holdingPlayer.position - transform.position)).sqrMagnitude > holdRadius * holdRadius))
            CancelDeactivation();

        float target = IsDeactivated ? 0f : charging || rising ? maximum : 0f;
        currentHeight = Mathf.MoveTowards(currentHeight, target, Mathf.Max(0.01f, speed) * deltaTime);
        if (statueRenderer != null)
            statueRenderer.transform.localPosition = lowestPosition + Vector3.up * currentHeight;

        if (charging)
        {
            // Hold time begins once the statue reaches its highest position.
            if (currentHeight < maximum) return;
            elapsed += deltaTime;
            if (elapsed >= Mathf.Max(0f, holdTime)) puzzle.CompleteDeactivation(this);
        }
        else if (!IsDeactivated && currentHeight == target)
            rising = !rising;
    }

    public void CompleteDeactivation()
    {
        CancelDeactivation();
        IsDeactivated = true;
        if (statueRenderer != null && deactivatedIcon != null) statueRenderer.sprite = deactivatedIcon;
        enabled = false;
    }

    public void Revive()
    {
        CancelDeactivation();
        IsDeactivated = false;
        rising = true;
        if (statueRenderer != null) statueRenderer.sprite = activeIcon;
    }

    private void CancelDeactivation()
    {
        charging = false;
        elapsed = 0f;
        holdingPlayer = null;
    }

    public void FlashWrongOrder()
    {
        if (statueRenderer == null) return;
        if (wrongOrderFlashRemaining <= 0f)
            colorBeforeWrongOrder = statueRenderer.color;
        wrongOrderFlashRemaining = 1.5f;
        statueRenderer.color = Color.red;
    }

    private void RestoreWrongOrderColor()
    {
        if (statueRenderer != null) statueRenderer.color = colorBeforeWrongOrder;
        wrongOrderFlashRemaining = 0f;
    }

    protected override void OnDisable()
    {
        if (wrongOrderFlashRemaining > 0f) RestoreWrongOrderColor();
        CancelDeactivation();
        base.OnDisable();
    }
}
