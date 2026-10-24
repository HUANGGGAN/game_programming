using UnityEngine;

public class AttackIndicator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer attackRectangle;
    [SerializeField] private Transform fillPart;
    [Tooltip("Position offset in world units: X along the attack direction, Y perpendicular to it.")]
    [SerializeField] private Vector2 offset;

    private Vector3 fullFillScale;
    private bool hasFullFillScale;
    private Vector3 initialLocalPosition;
    private bool hasInitialPosition;

    private void EnsureComponents()
    {
        if (!hasInitialPosition)
        {
            initialLocalPosition = transform.localPosition;
            hasInitialPosition = true;
        }
        if (attackRectangle == null)
            attackRectangle = GetComponent<SpriteRenderer>();
        if (fillPart == null)
            fillPart = transform.Find("FillPart");
        if (fillPart != null && !hasFullFillScale)
        {
            fullFillScale = fillPart.localScale;
            hasFullFillScale = true;
        }
    }

    // Author the rectangle along local +X, with its left edge at the enemy.
    public void Begin(Vector2 direction)
    {
        EnsureComponents();
        transform.rotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        Vector3 origin = transform.parent != null
            ? transform.parent.TransformPoint(initialLocalPosition)
            : initialLocalPosition;
        transform.position = origin + transform.rotation * (Vector3)offset;
        SetProgress(0f);
        gameObject.SetActive(true);
    }

    public void SetProgress(float progress)
    {
        EnsureComponents();
        if (fillPart != null)
            fillPart.localScale = new Vector3(
                fullFillScale.x * Mathf.Clamp01(progress), fullFillScale.y, fullFillScale.z);
    }

    public bool Contains(Vector2 worldPosition)
    {
        EnsureComponents();
        if (attackRectangle == null || attackRectangle.sprite == null)
            return false;

        Vector3 localPosition = attackRectangle.transform.InverseTransformPoint(worldPosition);
        Bounds bounds = attackRectangle.localBounds;
        return localPosition.x >= bounds.min.x && localPosition.x <= bounds.max.x &&
            localPosition.y >= bounds.min.y && localPosition.y <= bounds.max.y;
    }

    public void Hide()
    {
        SetProgress(0f);
        gameObject.SetActive(false);
    }
}
