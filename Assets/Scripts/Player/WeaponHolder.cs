using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponHolder : MonoBehaviour
{
    [SerializeField] private SpriteRenderer weaponRenderer;

    private Camera mainCamera;
    private Vector3 defaultLocalPosition;
    private Vector3 defaultLocalScale;

    private void Awake()
    {
        mainCamera = Camera.main;
        defaultLocalPosition = transform.localPosition;
        defaultLocalScale = transform.localScale;

        if (weaponRenderer == null)
            weaponRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        AimAtCursor();
    }

    public void SetEquippedWeapon(WeaponSO weapon)
    {
        if (weaponRenderer == null)
            return;

        weaponRenderer.sprite = weapon != null ? weapon.icon : null;
    }

    private void AimAtCursor()
    {
        if (Mouse.current == null || mainCamera == null)
            return;

        // 1. Get Mouse World Position
        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(new Vector3(
            mouseScreenPosition.x,
            mouseScreenPosition.y,
            Mathf.Abs(mainCamera.transform.position.z - transform.position.z)
        ));

        // 2. Determine Left/Right relative to parent (player)
        float parentX = transform.parent != null ? transform.parent.position.x : transform.position.x;
        bool cursorIsLeft = mouseWorldPosition.x < parentX;

        // 3. Offset local position (mirror the orbit distance if needed)
        Vector3 localPosition = defaultLocalPosition;
        if (cursorIsLeft)
            localPosition.x = -Mathf.Abs(defaultLocalPosition.x);
        else
            localPosition.x = Mathf.Abs(defaultLocalPosition.x);
        transform.localPosition = localPosition;

        // 4. Calculate rotation direction
        Vector2 direction = (Vector2)mouseWorldPosition - (Vector2)transform.position;
        if (direction.sqrMagnitude > Mathf.Epsilon)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        // 5. FIX: Use localScale.y flipping instead of weaponRenderer.flipY 
        // This keeps the weapon right-side up when crossing over to the left side!
        Vector3 localScale = defaultLocalScale;
        if (cursorIsLeft)
        {
            localScale.y = -defaultLocalScale.y; // Flips vertically so it doesn't look upside-down
        }
        else
        {
            localScale.y = defaultLocalScale.y;
        }
        transform.localScale = localScale;
    }
}