using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // Required for New Input System

[RequireComponent(typeof(Rigidbody2D))] // Automatically adds Rigidbody2D if missing
public class Player : MonoBehaviour, IEffectSource
{
    [Header("Data References")]
    [SerializeField] private PlayerData playerData;
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private HealthBar manaBar;
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [SerializeField] private Sprite front;
    [SerializeField] private Sprite back;
    [SerializeField] private Sprite waterFront;
    [SerializeField] private Sprite waterBack;
    [SerializeField] private WeaponHolder weaponHolder;
    private bool showingFront = true;
    private int lastAttackFacingFrame = -1;

    [Header("Damage & Respawn")]
    [SerializeField] private CanvasGroup damageFlash;
    [SerializeField, Min(0.01f)] private float damageFlashSpeed = 3f;
    private float damageFlashRemaining;
    private Coroutine deathRoutine;
    private PlayerShooter2D deathShooter;
    private bool shooterEnabledBeforeDeath;
    private Color colorBeforeDeath;
    private Vector3 initialPosition;
    public bool IsDying { get; private set; }
    public bool IsGodMode { get; private set; }
    public event System.Action Died;
    public event System.Action TeleportedHome;
    private readonly HashSet<UnityEngine.Object> movementLocks = new HashSet<UnityEngine.Object>();
    private RigidbodyConstraints2D constraintsBeforeMovementLock;
    private bool hasMovementLockConstraints;
    public bool IsMovementLocked
    {
        get
        {
            movementLocks.RemoveWhere(owner => owner == null);
            if (movementLocks.Count == 0) RestoreMovementConstraints();
            return movementLocks.Count > 0;
        }
    }

    public void SetMovementLocked(UnityEngine.Object owner, bool locked)
    {
        if (locked)
        {
            if (owner == null) return;
            if (!hasMovementLockConstraints && _rb != null)
            {
                constraintsBeforeMovementLock = _rb.constraints;
                hasMovementLockConstraints = true;
                _rb.constraints |= RigidbodyConstraints2D.FreezePosition;
            }
            movementLocks.Add(owner);
            EndMeleeDash();
            ClearKnockback();
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
        }
        else
        {
            movementLocks.Remove(owner);
            if (movementLocks.Count == 0) RestoreMovementConstraints();
        }
    }

    private void RestoreMovementConstraints()
    {
        if (!hasMovementLockConstraints) return;
        if (_rb != null) _rb.constraints = constraintsBeforeMovementLock;
        hasMovementLockConstraints = false;
    }

    [Header("Movement Configuration")]
    [SerializeField] private float moveSpeed = 5.0f; // Movement speed in units/sec
    [SerializeField, Min(1f)] private float runSpeedMultiplier = 1.5f;

    [Header("Knockback")]
    [SerializeField, Min(0f)] private float knockbackDuration = 0.12f;

    private Rigidbody2D _rb;
    private Vector2 _inputVector;
    private bool isKnockedBack;
    private Vector2 meleeDashDirection;
    private float meleeDashRemaining;
    private float meleeDashSpeed;
    private float meleeDashTimeRemaining;
    private const float MeleeDashSkin = 0.02f;
    private readonly List<RaycastHit2D> meleeDashHits = new List<RaycastHit2D>();
    public bool IsMeleeDashing { get; private set; }
    public bool CanMeleeDash => isActiveAndEnabled && !isKnockedBack && !IsMovementLocked;

    public void BeginMeleeDash(Vector2 direction, float distance, float duration)
    {
        if (IsMovementLocked) return;
        meleeDashDirection = direction.normalized;
        meleeDashRemaining = distance;
        meleeDashTimeRemaining = Mathf.Max(Time.fixedDeltaTime, duration);
        meleeDashSpeed = distance / meleeDashTimeRemaining;
        IsMeleeDashing = true;
    }

    public void EndMeleeDash()
    {
        if (!IsMeleeDashing) return;
        IsMeleeDashing = false;
        meleeDashRemaining = 0f;
        meleeDashTimeRemaining = 0f;
        if (_rb != null && !isKnockedBack) _rb.linearVelocity = Vector2.zero;
    }

    private Coroutine knockbackRoutine;
    private readonly List<ActiveEffect> activeEffects = new List<ActiveEffect>();
    private float effectTickElapsed;
    public IReadOnlyList<ActiveEffect> ActiveEffects => activeEffects;
    public event System.Action EffectsChanged;
    private Camera mainCamera;
    private Coroutine startupTutorialRoutine;
    private RegionTracker regionTracker;

    public PlayerData Data => playerData;

    private void Awake()
    {
        initialPosition = transform.position;
        if (damageFlash != null)
        {
            damageFlash.alpha = 0f;
            damageFlash.blocksRaycasts = false;
            damageFlash.interactable = false;
        }
        // Cache the Rigidbody2D reference
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Dynamic;
        _rb.gravityScale = 0f;
        _rb.constraints |= RigidbodyConstraints2D.FreezeRotation;
        mainCamera = Camera.main;
        if (playerSpriteRenderer == null)
            playerSpriteRenderer = GetComponent<SpriteRenderer>();

        if (playerSpriteRenderer != null)
            playerSpriteRenderer.flipX = false;

        if (weaponHolder == null)
            weaponHolder = GetComponentInChildren<WeaponHolder>(true);
        showingFront = back == null || playerSpriteRenderer == null || playerSpriteRenderer.sprite != back;
        weaponHolder?.SetPlayerFacing(playerSpriteRenderer, showingFront);

        if (GetComponent<RegionTracker>() == null)
            gameObject.AddComponent<RegionTracker>();
        regionTracker = GetComponent<RegionTracker>();

        if (GetComponent<PlayerConsumables>() == null)
            gameObject.AddComponent<PlayerConsumables>();
    }

    private void Start()
    {
        if (manaBar != null)
        {
            manaBar.SetManaBar(true);
        }

        if (healthBar != null)
        {
            healthBar.SetManaBar(false);
        }

        if (playerData != null)
        {
            playerData.Reset();
            UpdatePlayerBarsFromData();
        }
    }

    private void OnEnable()
    {
        startupTutorialRoutine = StartCoroutine(RequestStartupTutorials());
    }

    private IEnumerator RequestStartupTutorials()
    {
        // Allow scene initialization to finish, then also handle a controller enabled later.
        yield return null;
        while (SceneLoader.IsLoading || Tutorial.Instance == null || !Tutorial.Instance.isActiveAndEnabled)
            yield return null;

        Tutorial tutorial = Tutorial.Instance;
        tutorial.ShowTutorial("startup-controls", "Movement: WASD/arrow keys\nInteract: F\nInventory: E\nWeapon: Scroll/1/2/3\nPotion: Z/X/C");
        tutorial.ShowTutorial("startup-shortcuts", "Map: M\nTeleport to Home: P\nAdd 10 coins: =");
        startupTutorialRoutine = null;
    }

    private void Update()
    {
        UpdateDamageFlash();
        if (IsDying) return;
        if (playerData != null && playerData.currentHealth <= 0f)
        {
            BeginDeath();
            return;
        }
        if (!StateManager.Instance.IsGameplay)
        {
            _inputVector = Vector2.zero;
            return;
        }
        if (GameInput.Pressed(GameBinding.GodMode))
        {
            IsGodMode = !IsGodMode;
            Debug.Log($"God mode: {(IsGodMode ? "ON" : "OFF")}");
        }
        if (GameInput.Pressed(GameBinding.AddCoin))
            addMoney(100);
        if (GameInput.Pressed(GameBinding.Teleport))
        {
            // Release encounter-owned movement locks and camera control before moving home.
            TeleportedHome?.Invoke();
            EndMeleeDash();
            ClearKnockback();
            transform.position = initialPosition;
            _rb.position = initialPosition;
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            Interactable.ClearSelection();
            Physics2D.SyncTransforms();
        }
        Interactable.UpdateInteraction(transform);
        regionTracker?.RefreshRegions();
        if (EffectRuntime.Tick(activeEffects, Time.deltaTime, amount => TakeDamage(amount), ref effectTickElapsed))
            EffectsChanged?.Invoke();
        if (IsDying) return;
        ReadMovementInput();
        FaceTowardCursor();
    }

    private void OnDisable()
    {
        movementLocks.Clear();
        RestoreMovementConstraints();
        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
            RestoreDeathPresentation();
        }
        if (damageFlash != null) damageFlash.alpha = 0f;
        EndMeleeDash();
        ClearKnockback();
        _inputVector = Vector2.zero;
        if (startupTutorialRoutine != null)
        {
            StopCoroutine(startupTutorialRoutine);
            startupTutorialRoutine = null;
        }
        Interactable.ClearSelection();
    }

    private void FixedUpdate()
    {
        if (IsDying || IsMovementLocked)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }
        if (IsMeleeDashing)
        {
            if (isKnockedBack || meleeDashRemaining <= 0.0001f || meleeDashTimeRemaining <= 0f)
                EndMeleeDash();
            else
            {
                float distance = Mathf.Min(meleeDashRemaining, meleeDashSpeed * Time.fixedDeltaTime);
                meleeDashTimeRemaining -= Time.fixedDeltaTime;
                ContactFilter2D filter = new ContactFilter2D();
                filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
                filter.useTriggers = false;
                meleeDashHits.Clear();
                int count = _rb.Cast(meleeDashDirection, filter, meleeDashHits, distance + MeleeDashSkin);
                bool blocked = false;
                for (int i = 0; i < count; i++)
                {
                    Collider2D hit = meleeDashHits[i].collider;
                    if (hit == null || hit.GetComponentInParent<EnemyChase>() != null || hit.attachedRigidbody == _rb) continue;
                    // Touching a wall behind or beside us must not block moving away.
                    if (Vector2.Dot(meleeDashDirection, meleeDashHits[i].normal) >= -0.001f) continue;
                    blocked = true;
                    distance = Mathf.Min(distance, Mathf.Max(0f, meleeDashHits[i].distance - MeleeDashSkin));
                }
                // Finish after the safe partial step; never keep pushing toward the wall.
                meleeDashRemaining = blocked ? 0f : Mathf.Max(0f, meleeDashRemaining - distance);
                if (distance <= 0.0001f)
                {
                    EndMeleeDash();
                    return;
                }
                _rb.linearVelocity = meleeDashDirection * (distance / Time.fixedDeltaTime);
                return;
            }
        }
        // Let physics own movement until the knockback impulse has finished.
        if (isKnockedBack)
            return;

        bool isRunning = GameInput.Held(GameBinding.Run);
        float effectiveMoveSpeed = moveSpeed * (isRunning ? runSpeedMultiplier : 1f) * (IsGodMode ? 5f : 1f) * EffectRuntime.GetMovementMultiplier(activeEffects) *
            (regionTracker != null ? regionTracker.MovementMultiplier : 1f);
        _rb.linearVelocity = _inputVector * effectiveMoveSpeed;
    }

    private void ReadMovementInput()
    {
        if (Keyboard.current == null) return;

        _inputVector = Vector2.zero;

        // Horizontal Movement
        if (GameInput.Held(GameBinding.Left) || Keyboard.current.leftArrowKey.isPressed)
        {
            _inputVector.x -= 1f;
        }
        if (GameInput.Held(GameBinding.Right) || Keyboard.current.rightArrowKey.isPressed)
        {
            _inputVector.x += 1f;
        }

        // Vertical Movement
        if (GameInput.Held(GameBinding.Up) || Keyboard.current.upArrowKey.isPressed)
        {
            _inputVector.y += 1f;
        }
        if (GameInput.Held(GameBinding.Down) || Keyboard.current.downArrowKey.isPressed)
        {
            _inputVector.y -= 1f;
        }

        // Normalize vector to prevent faster diagonal movement
        if (_inputVector.sqrMagnitude > 1f)
        {
            _inputVector.Normalize();
        }
    }

    public bool TakeDamage(float amount)
    {
        if (IsGodMode || IsDying || playerData == null || amount <= 0f || playerData.currentHealth <= 0f)
            return false;

        playerData.ApplyDamage(amount);
        UpdatePlayerBarsFromData();
        if (playerData.currentHealth <= 0f) BeginDeath();
        else damageFlashRemaining = 0.15f;
        return true;
    }

    private void UpdateDamageFlash()
    {
        if (damageFlash == null) return;
        if (IsDying) { damageFlash.alpha = 0.5f; return; }
        float target = damageFlashRemaining > 0f ? 0.3f : 0f;
        damageFlashRemaining = Mathf.Max(0f, damageFlashRemaining - Time.unscaledDeltaTime);
        damageFlash.alpha = Mathf.MoveTowards(damageFlash.alpha, target, damageFlashSpeed * Time.unscaledDeltaTime);
    }

    private void BeginDeath()
    {
        if (IsDying) return;
        IsDying = true;
        movementLocks.Clear();
        RestoreMovementConstraints();
        Died?.Invoke();
        EndMeleeDash();
        ClearKnockback();
        ClearEffects();
        _inputVector = Vector2.zero;
        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;
        Interactable.ClearSelection();
        deathShooter = GetComponentInChildren<PlayerShooter2D>(true);
        if (deathShooter != null)
        {
            shooterEnabledBeforeDeath = deathShooter.enabled;
            deathShooter.enabled = false;
        }
        if (playerSpriteRenderer != null)
        {
            colorBeforeDeath = playerSpriteRenderer.color;
            playerSpriteRenderer.color = Color.red;
        }
        if (damageFlash != null) damageFlash.alpha = 0.5f;
        deathRoutine = StartCoroutine(RespawnAfterDeath());
    }

    private IEnumerator RespawnAfterDeath()
    {
        yield return new WaitForSecondsRealtime(1f);
        Vector3 position = initialPosition;
        Debug.Log($"{position}");
        transform.position = position;
        _rb.position = position;
        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;
        playerData.Heal(playerData.maxHealth);
        playerData.RestoreMP(playerData.maxMP);
        if (GameOptions.DeathPenalty)
            playerData.SetMoney((int)((long)Mathf.Max(0, playerData.money) * 80 / 100));
        UpdatePlayerBarsFromData();
        RestoreDeathPresentation();
        Physics2D.SyncTransforms();
        regionTracker?.RefreshRegions();
        deathRoutine = null;
        Tutorial.RequestTutorial("first-respawn", "After death, you lose 20% of your money.");
    }

    private void RestoreDeathPresentation()
    {
        IsDying = false;
        damageFlashRemaining = 0f;
        if (playerSpriteRenderer != null) playerSpriteRenderer.color = colorBeforeDeath;
        if (deathShooter != null) deathShooter.enabled = shooterEnabledBeforeDeath;
        deathShooter = null;
    }

    public bool TryUseMP(int amount)
    {
        if (IsGodMode) return true;
        bool success = amount <= 0 || (playerData != null && playerData.TryUseMP(amount));
        if (success)
            UpdatePlayerBarsFromData();
        return success;
    }

    public void ClearEffects()
    {
        effectTickElapsed = 0f;
        if (EffectRuntime.Clear(activeEffects))
            EffectsChanged?.Invoke();
    }

    public void ClearPositiveEffects()
    {
        if (activeEffects.RemoveAll(effect => effect != null && effect.effectID > 0) == 0) return;
        if (activeEffects.Count == 0) effectTickElapsed = 0f;
        EffectsChanged?.Invoke();
    }

    public bool RemoveEffect(int effectID)
    {
        bool removed = EffectRuntime.Remove(activeEffects, effectID);
        if (activeEffects.Count == 0)
            effectTickElapsed = 0f;
        if (removed)
            EffectsChanged?.Invoke();
        return removed;
    }

    public void ApplyEffects(EffectDefinition[] effects, System.Action<int, int> restoreAttacker = null, bool stackMatchingLevels = true)
    {
        if (IsDying) return;
        if (EffectRuntime.Apply(activeEffects, effects, restoreAttacker: restoreAttacker, stackMatchingLevels: stackMatchingLevels))
            EffectsChanged?.Invoke();
    }

    public void RestoreEffectResource(int effectID, int amount)
    {
        if (playerData == null || amount <= 0 || playerData.currentHealth <= 0f)
            return;
        if (effectID == EffectRuntime.HPStealing)
            playerData.Heal(amount);
        else if (effectID == EffectRuntime.MPStealing)
            playerData.RestoreMP(Mathf.Min(amount, Mathf.Max(0, playerData.maxMP - playerData.currentMP)));
        UpdatePlayerBarsFromData();
    }

    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (IsDying || IsMovementLocked || !isActiveAndEnabled || force <= 0f || direction.sqrMagnitude <= Mathf.Epsilon)
            return;

        if (!isKnockedBack)
        {
            _rb.linearVelocity = Vector2.zero;
            isKnockedBack = true;
        }
        _rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);
        knockbackRoutine = StartCoroutine(ResetKnockback());
    }

    private IEnumerator ResetKnockback()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, knockbackDuration));
        knockbackRoutine = null;
        ClearKnockback();
    }

    private void ClearKnockback()
    {
        if (knockbackRoutine != null)
        {
            StopCoroutine(knockbackRoutine);
            knockbackRoutine = null;
        }
        isKnockedBack = false;
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }
    }

    public void UpdatePlayerBarsFromData()
    {
        if (healthBar != null && playerData != null)
        {
            healthBar.RefreshFromData(playerData);
        }

        if (manaBar != null && playerData != null)
        {
            manaBar.RefreshFromData(playerData);
        }
    }

    private void FaceTowardCursor()
    {
        // Refresh the region-specific sprite even while standing still.
        SetFacing(showingFront);
        bool attemptingAttack = Mouse.current != null && Mouse.current.leftButton.isPressed;
        if (!attemptingAttack && lastAttackFacingFrame != Time.frameCount && _inputVector.y != 0f)
            SetFacing(_inputVector.y <= 0f);
        if (Mouse.current == null || mainCamera == null || playerSpriteRenderer == null)
            return;

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(new Vector3(
            mouseScreenPosition.x,
            mouseScreenPosition.y,
            Mathf.Abs(mainCamera.transform.position.z - transform.position.z)
        ));

        playerSpriteRenderer.flipX = mouseWorldPosition.x < transform.position.x;
        if (attemptingAttack)
            FaceAttack(mouseWorldPosition);
    }

    public void FaceAttack(Vector2 targetPosition)
    {
        lastAttackFacingFrame = Time.frameCount;
        SetFacing(targetPosition.y <= transform.position.y);
    }

    private void SetFacing(bool faceFront)
    {
        showingFront = faceFront;
        Sprite facingSprite = showingFront ? front : back;
        if (regionTracker != null && regionTracker.IsInWater)
        {
            Sprite waterSprite = showingFront ? waterFront : waterBack;
            if (waterSprite != null) facingSprite = waterSprite;
        }
        if (facingSprite != null && playerSpriteRenderer != null)
            playerSpriteRenderer.sprite = facingSprite;
        weaponHolder?.SetPlayerFacing(playerSpriteRenderer, showingFront);
    }

    public void addMoney(int amount)
    {
        if (playerData != null && amount > 0)
        {
            playerData.AddMoney(amount);
        }
    }
}
