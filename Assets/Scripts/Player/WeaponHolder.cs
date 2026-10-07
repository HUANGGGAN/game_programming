using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponHolder : MonoBehaviour
{
    [SerializeField] private SpriteRenderer weaponRenderer;

    private Camera mainCamera;
    private Vector3 defaultLocalPosition;

    private void Awake()
    {
        mainCamera = Camera.main;
        defaultLocalPosition = transform.localPosition;
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

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(new Vector3(
            mouseScreenPosition.x,
            mouseScreenPosition.y,
            Mathf.Abs(mainCamera.transform.position.z - transform.position.z)
        ));

        float parentX = transform.parent != null ? transform.parent.position.x : transform.position.x;
        bool cursorIsLeft = mouseWorldPosition.x < parentX;
        Vector3 localPosition = defaultLocalPosition;
        if (cursorIsLeft)
            localPosition.x = -localPosition.x;
        transform.localPosition = localPosition;

        Vector2 direction = (Vector2)mouseWorldPosition - (Vector2)transform.position;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (weaponRenderer != null)
            weaponRenderer.flipY = direction.x < 0f;
    }
}