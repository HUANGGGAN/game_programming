using UnityEngine;
using UnityEngine.InputSystem; // New Input System

public class PlayerShooter2D : MonoBehaviour
{
    [Header("Bullet Setup")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint; // Optional offset point (e.g., gun tip)

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        // Detect Left Click using New Input System
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        if (Mouse.current == null) return;

        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;

        // 1. Get mouse position in screen space
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

        // 2. Convert to World Space with explicit Z depth relative to the camera
        // We set Z to distance between camera and the Z=0 plane
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(
            mouseScreenPos.x,
            mouseScreenPos.y,
            Mathf.Abs(mainCamera.transform.position.z) // Accounts for Cinemachine camera distance
        ));

        // Ensure 2D projection stays strictly at Z = 0
        mouseWorldPos.z = 0f;

        // 3. Calculate direction vector snapshot
        Vector2 shootDirection = ((Vector2)mouseWorldPos - (Vector2)spawnPosition).normalized;

        // 4. Fetch pooled bullet and launch
        Bullet2D bullet = BulletPool.Instance.GetBullet();
        if (bullet != null)
        {
            bullet.transform.position = spawnPosition;
            bullet.Launch(shootDirection);
        }
    }
}