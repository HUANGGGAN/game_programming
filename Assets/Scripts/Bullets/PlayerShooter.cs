using UnityEngine;
using UnityEngine.InputSystem; // New Input System
using TMPro;

public class PlayerShooter2D : MonoBehaviour
{
    [Header("Bullet Setup")]
    [SerializeField] private BulletSO bulletData;
    [SerializeField] private Transform firePoint; // Optional offset point (e.g., gun tip)
    [SerializeField, Min(0.01f)] private float shootInterval;
    [SerializeField] private InventoryUIController inventoryUIController;
    [SerializeField] private GameObject reloadStatusText;
    [SerializeField] private TMP_Text[] weaponAmmoTexts = new TMP_Text[3];

    private Camera mainCamera;
    private WeaponHolder weaponHolder;
    private TMP_Text reloadCountdownText;
    private bool shootingEnabled = true;
    private float nextShotTime;
    private WeaponSO equippedWeapon;
    private bool isReloading;
    private float reloadFinishTime;

    private void Awake()
    {
        mainCamera = Camera.main;
        weaponHolder = GetComponent<WeaponHolder>();
        if (weaponHolder == null)
            weaponHolder = GetComponentInParent<WeaponHolder>();
        if (inventoryUIController == null)
            inventoryUIController = FindObjectOfType<InventoryUIController>(true);

        if (reloadStatusText != null)
            reloadCountdownText = reloadStatusText.GetComponent<TMP_Text>();

        SetReloadTextVisible(false);
        RefreshWeaponAmmoTexts();
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            StartReload();

        if (isReloading)
        {
            if (Time.time >= reloadFinishTime)
                CompleteReload();

            if (isReloading)
            {
                UpdateReloadCountdown();
                return;
            }
        }

        bool leftClickHeld = Mouse.current != null && Mouse.current.leftButton.isPressed;
        if (shootingEnabled && leftClickHeld && Time.time >= nextShotTime)
        {
            Shoot();
            nextShotTime = Time.time + shootInterval;
        }
    }

    public void RefreshWeaponAmmoTexts()
    {
        if (weaponAmmoTexts == null)
            return;

        const string infinity = "\u221E";
        for (int i = 0; i < weaponAmmoTexts.Length; i++)
        {
            TMP_Text ammoText = weaponAmmoTexts[i];
            if (ammoText == null)
                continue;

            ItemSO item = inventoryUIController != null ? inventoryUIController.GetItemAtSlot(i) : null;
            if (item == null)
            {
                ammoText.text = string.Empty;
                continue;
            }

            if (item is WeaponSO weapon && !weapon.consumeAmmo)
            {
                ammoText.text = $"{infinity}/{infinity}";
            }
            else if (item is WeaponSO consumingWeapon && !consumingWeapon.hasLimitedAmmo)
            {
                int ammo = inventoryUIController.GetWeaponAmmoCountAtSlot(i);
                ammoText.text = $"{ammo}/{infinity}";
            }
            else
            {
                int itemCount = inventoryUIController.GetWeaponItemCountAtSlot(i);
                ammoText.text = itemCount.ToString();
            }
        }
    }

    public void SetShootInterval(float intervalSeconds)
    {
        shootInterval = Mathf.Max(0.01f, intervalSeconds);
    }

    public bool EquipWeapon(WeaponSO equippedWeapon, InventoryUIController inventoryUIController)
    {
        CancelReload();

        if (equippedWeapon == null || inventoryUIController == null)
        {
            Debug.LogWarning("Cannot equip a null weapon or a weapon without an inventory controller.", this);
            return false;
        }

        if (equippedWeapon is not GunSO gun)
        {
            this.inventoryUIController = inventoryUIController;
            this.equippedWeapon = equippedWeapon;
            bulletData = null;
            weaponHolder?.SetEquippedWeapon(equippedWeapon);
            RefreshWeaponAmmoTexts();
            return true;
        }

        if (gun.bulletData == null || gun.shootInterval <= 0f)
        {
            Debug.LogWarning("Cannot equip a gun without bullet data and a positive shoot interval.", this);
            return false;
        }

        this.inventoryUIController = inventoryUIController;
        this.equippedWeapon = equippedWeapon;
        bulletData = gun.bulletData;
        weaponHolder?.SetEquippedWeapon(equippedWeapon);
        SetShootInterval(gun.shootInterval);
        nextShotTime = Time.time + shootInterval;
        RefreshWeaponAmmoTexts();
        return true;
    }

    public void UnequipWeapon()
    {
        CancelReload();
        equippedWeapon = null;
        bulletData = null;
        weaponHolder?.SetEquippedWeapon(null);
        RefreshWeaponAmmoTexts();
    }

    private void StartReload()
    {
        if (isReloading || equippedWeapon is not GunSO gun || inventoryUIController == null ||
            !gun.consumeAmmo || gun.hasLimitedAmmo || gun.capacity <= 0 ||
            inventoryUIController.GetEquippedWeaponAmmoCount() >= gun.capacity)
            return;

        isReloading = true;
        reloadFinishTime = Time.time + Mathf.Max(0f, gun.reloadTime);
        SetReloadTextVisible(true);
    }

    private void CompleteReload()
    {
        if (inventoryUIController != null && equippedWeapon is GunSO gun)
            inventoryUIController.TryReloadEquippedWeapon(gun, gun.capacity);

        isReloading = false;
        SetReloadTextVisible(false);
        RefreshWeaponAmmoTexts();
    }

    private void CancelReload()
    {
        isReloading = false;
        reloadFinishTime = 0f;
        SetReloadTextVisible(false);
    }

    private void SetReloadTextVisible(bool visible)
    {
        if (reloadStatusText != null && reloadStatusText.activeSelf != visible)
            reloadStatusText.SetActive(visible);
    }

    private void UpdateReloadCountdown()
    {
        if (reloadCountdownText == null)
            return;

        float secondsRemaining = Mathf.Max(0f, reloadFinishTime - Time.time);
        reloadCountdownText.text = $"Reloading: {secondsRemaining:F1}s";
    }

    public void SetShootingEnabled(bool enabled)
    {
        shootingEnabled = enabled;
    }

    private void Shoot()
    {
        if (Mouse.current == null || bulletData == null || inventoryUIController == null || equippedWeapon == null)
            return;

        bool shouldConsumeAmmo = equippedWeapon.consumeAmmo;
        if (shouldConsumeAmmo && !inventoryUIController.HasEquippedWeaponAmmo())
            return;

        Transform weaponTransform = weaponHolder != null ? weaponHolder.transform : transform;
        Vector3 aimOrigin = weaponTransform.position;
        Vector3 firePointPosition = weaponTransform.TransformPoint(equippedWeapon.firePointOffset);
        if (firePoint != null)
        {
            firePoint.position = firePointPosition;
        }

        Vector3 spawnPosition = firePoint != null ? firePoint.position : firePointPosition;

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
        Vector2 shootDirection = ((Vector2)mouseWorldPos - (Vector2)aimOrigin).normalized;

        // 4. Fetch pooled bullet and launch
        Bullet2D bullet = BulletPool.Instance.GetBullet(bulletData);
        if (bullet != null && (!shouldConsumeAmmo || inventoryUIController.TryConsumeEquippedWeaponAmmo()))
        {
            bullet.transform.position = spawnPosition;
            bullet.Launch(shootDirection);
            if (shouldConsumeAmmo)
                RefreshWeaponAmmoTexts();
        }
    }
}