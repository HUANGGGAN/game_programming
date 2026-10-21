using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // New Input System
using TMPro;

public class PlayerShooter2D : MonoBehaviour
{
    [Header("Bullet Setup")]
    [SerializeField] private Transform firePoint; // Optional offset point (e.g., gun tip)
    [SerializeField, Min(0.01f)] private float shootInterval;
    [SerializeField] private InventoryUIController inventoryUIController;
    [SerializeField] private GameObject reloadStatusText;
    [SerializeField] private ChargingBar chargingBar;
    [SerializeField] private TMP_Text[] weaponAmmoTexts = new TMP_Text[3];

    private Camera mainCamera;
    private WeaponHolder weaponHolder;
    private Player player;
    private TMP_Text reloadCountdownText;
    private bool shootingEnabled = true;
    private float nextShotTime;
    private WeaponSO equippedWeapon;
    private readonly MeleeAttack meleeAttack = new MeleeAttack();
    private readonly List<IWeaponAttackHandler> attackHandlers = new List<IWeaponAttackHandler>
    {
        new ChargedAttack(),
        new ProjectileAttack()
    };
    private bool isReloading;
    private float reloadFinishTime;
    private bool isChargingAttack;
    private float chargeStartTime;

    private void Awake()
    {
        mainCamera = Camera.main;
        attackHandlers.Insert(0, meleeAttack);
        player = GetComponent<Player>();
        if (player == null)
            player = GetComponentInParent<Player>();
        attackHandlers.Insert(0, new WandProjectileAttack(
            amount => amount <= 0 || (player != null && player.TryUseMP(amount))));
        weaponHolder = GetComponent<WeaponHolder>();
        if (weaponHolder == null)
            weaponHolder = GetComponentInParent<WeaponHolder>();
        if (inventoryUIController == null)
            inventoryUIController = FindFirstObjectByType<InventoryUIController>();
        if (chargingBar == null)
            chargingBar = GetComponentInChildren<ChargingBar>(true);
        chargingBar?.SetVisible(false);

        if (reloadStatusText != null)
            reloadCountdownText = reloadStatusText.GetComponent<TMP_Text>();

        SetReloadTextVisible(false);
        RefreshWeaponAmmoTexts();
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;

        for (int i = 0; i < attackHandlers.Count; i++)
        {
            if (attackHandlers[i] is ITimedWeaponAttackHandler timedHandler)
                timedHandler.Tick(Time.deltaTime);
        }

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

        if (equippedWeapon is IChargedProjectileWeapon chargedWeapon)
        {
            UpdateChargedAttackInput(chargedWeapon);
            return;
        }

        bool leftClickHeld = Mouse.current != null && Mouse.current.leftButton.isPressed;
        if (shootingEnabled && leftClickHeld && Time.time >= nextShotTime)
        {
            Shoot();
            nextShotTime = Time.time + shootInterval;
        }
    }

    private void LateUpdate()
    {
        if (equippedWeapon is not MeleeSO)
            return;

        Transform weaponTransform = weaponHolder != null ? weaponHolder.transform : transform;
        Vector2 direction = weaponTransform.right;
        if (Mouse.current != null && mainCamera != null)
        {
            Vector2 cursor = Mouse.current.position.ReadValue();
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(
                cursor.x, cursor.y, Mathf.Abs(mainCamera.transform.position.z)));
            Vector2 aim = (Vector2)worldPosition - (Vector2)weaponTransform.position;
            if (aim.sqrMagnitude > Mathf.Epsilon)
                direction = aim.normalized;
        }

        Vector2 origin = weaponTransform.TransformPoint(equippedWeapon.firePointOffset);
        meleeAttack.UpdateRange(origin, direction);
    }

    private void OnDisable()
    {
        meleeAttack.HideRange();
    }

    private void OnDestroy()
    {
        meleeAttack.DisposeRange();
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

            if (item is WandSO || (item is WeaponSO weapon && !weapon.consumeAmmo))
            {
                ammoText.text = string.Empty;
            }
            else if (item is GunSO gun && !gun.hasLimitedAmmo)
            {
                int ammo = inventoryUIController.GetWeaponAmmoCountAtSlot(i);
                ammoText.text = $"{ammo}/{gun.capacity}";
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

        if (FindAttackHandler(equippedWeapon) == null)
        {
            Debug.LogWarning($"No registered attack handler supports {equippedWeapon.name}.", this);
            return false;
        }

        this.inventoryUIController = inventoryUIController;
        this.equippedWeapon = equippedWeapon;
        meleeAttack.SetEquippedWeapon(equippedWeapon as MeleeSO);
        chargingBar?.SetVisible(equippedWeapon is IChargedProjectileWeapon);
        weaponHolder?.SetEquippedWeapon(equippedWeapon);
        SetShootInterval(equippedWeapon.shootInterval);
        nextShotTime = Time.time + shootInterval;
        RefreshWeaponAmmoTexts();
        return true;
    }

    public void UnequipWeapon()
    {
        CancelReload();
        equippedWeapon = null;
        meleeAttack.SetEquippedWeapon(null);
        chargingBar?.SetVisible(false);
        weaponHolder?.SetEquippedWeapon(null);
        RefreshWeaponAmmoTexts();
    }

    private void StartReload()
    {
        if (isReloading || equippedWeapon is not IReloadableWeapon reloadable || inventoryUIController == null ||
            !equippedWeapon.consumeAmmo || equippedWeapon.hasLimitedAmmo || reloadable.AmmoCapacity <= 0 ||
            inventoryUIController.GetEquippedWeaponAmmoCount() >= reloadable.AmmoCapacity)
            return;

        isChargingAttack = false;
        chargeStartTime = 0f;
        chargingBar?.ResetCharge();
        isReloading = true;
        reloadFinishTime = Time.time + Mathf.Max(0f, reloadable.ReloadDuration);
        SetReloadTextVisible(true);
    }

    private void CompleteReload()
    {
        if (inventoryUIController != null && equippedWeapon is IReloadableWeapon reloadable)
            inventoryUIController.TryReloadEquippedWeapon(equippedWeapon, reloadable.AmmoCapacity);

        isReloading = false;
        SetReloadTextVisible(false);
        RefreshWeaponAmmoTexts();
    }

    private void CancelReload()
    {
        isReloading = false;
        reloadFinishTime = 0f;
        isChargingAttack = false;
        chargeStartTime = 0f;
        chargingBar?.ResetCharge();
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

    private void UpdateChargedAttackInput(IChargedProjectileWeapon chargedWeapon)
    {
        if (Mouse.current == null || !shootingEnabled)
        {
            isChargingAttack = false;
            chargeStartTime = 0f;
            chargingBar?.ResetCharge();
            return;
        }

        var leftButton = Mouse.current.leftButton;
        if (!isChargingAttack && leftButton.wasPressedThisFrame && Time.time >= nextShotTime)
        {
            if (equippedWeapon.consumeAmmo && !inventoryUIController.HasEquippedWeaponAmmo())
            {
                StartReload();
                return;
            }

            isChargingAttack = true;
            chargeStartTime = Time.time;
            chargingBar?.SetCharge(0f);
        }

        if (isChargingAttack)
        {
            float currentCharge = chargedWeapon.MaxChargeTime <= 0f
                ? 1f
                : Mathf.Clamp01((Time.time - chargeStartTime) / chargedWeapon.MaxChargeTime);
            chargingBar?.SetCharge(currentCharge);
        }

        if (isChargingAttack && leftButton.wasReleasedThisFrame)
        {
            isChargingAttack = false;
            float chargeRatio = chargedWeapon.MaxChargeTime <= 0f
                ? 1f
                : Mathf.Clamp01((Time.time - chargeStartTime) / chargedWeapon.MaxChargeTime);
            if (Shoot(chargeRatio))
                nextShotTime = Time.time + shootInterval;
            chargingBar?.ResetCharge();
        }
    }

    private bool Shoot(float? chargeRatio = null)
    {
        if (Mouse.current == null || inventoryUIController == null || equippedWeapon == null)
            return false;

        bool shouldConsumeAmmo = equippedWeapon is not WandSO && equippedWeapon.consumeAmmo;
        if (shouldConsumeAmmo && !inventoryUIController.HasEquippedWeaponAmmo())
        {
            StartReload();
            if (!equippedWeapon.hasLimitedAmmo)
                Tutorial.Instance?.ShowTutorial("reload", "You are out of ammo! Press R to reload your weapon. Don't worry, ammo is unlimited");
            return false;
        }

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

        if (!TryPerformAttack(spawnPosition, shootDirection, chargeRatio, mouseWorldPos))
            return false;

        if (shouldConsumeAmmo)
            RefreshWeaponAmmoTexts();
        return true;
    }

    public void RegisterAttackHandler(IWeaponAttackHandler handler)
    {
        if (handler != null && !attackHandlers.Contains(handler))
            attackHandlers.Insert(0, handler);
    }

    private IWeaponAttackHandler FindAttackHandler(WeaponSO weapon)
    {
        for (int i = 0; i < attackHandlers.Count; i++)
        {
            if (attackHandlers[i].CanHandle(weapon))
                return attackHandlers[i];
        }

        return null;
    }

    private bool TryPerformAttack(Vector2 origin, Vector2 direction, float? chargeRatio = null, Vector2? targetPosition = null)
    {
        IWeaponAttackHandler handler = FindAttackHandler(equippedWeapon);
        if (handler == null)
            return false;

        Func<bool> tryConsumeAmmo = () => inventoryUIController.TryConsumeEquippedWeaponAmmo();
        if (chargeRatio.HasValue)
        {
            return handler is IChargedAttackHandler chargedHandler && chargedHandler.TryPerformCharged(
                equippedWeapon,
                origin,
                direction,
                chargeRatio.Value,
                tryConsumeAmmo, targetPosition);
        }

        return handler.TryPerform(equippedWeapon, origin, direction, tryConsumeAmmo, targetPosition);
    }
}
