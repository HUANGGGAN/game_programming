using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // Required for New Input System

[RequireComponent(typeof(Rigidbody2D))] // Automatically adds Rigidbody2D if missing
public class Player : MonoBehaviour
{
    [Header("Data References")]
    [SerializeField] private PlayerData playerData;
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private HealthBar manaBar;
    [SerializeField] private SpriteRenderer playerSpriteRenderer;

    [Header("Movement Configuration")]
    [SerializeField] private float moveSpeed = 5.0f; // Movement speed in units/sec

    private Rigidbody2D _rb;
    private Vector2 _inputVector;
    private Vector2 knockbackVelocity;
    private float knockbackRemaining;
    private readonly List<ActiveEffect> activeEffects = new List<ActiveEffect>();
    private Camera mainCamera;

    private const float KnockbackDuration = 0.12f;

    private void Awake()
    {
        // Cache the Rigidbody2D reference
        _rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        if (playerSpriteRenderer == null)
            playerSpriteRenderer = GetComponent<SpriteRenderer>();

        if (playerSpriteRenderer != null)
            playerSpriteRenderer.flipX = false;
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
            playerData.ResetScore();
            playerData.OnPlayerStatsChanged += UpdatePlayerBarsFromData;
            UpdatePlayerBarsFromData();
        }
    }

    private void OnDestroy()
    {
        if (playerData != null)
        {
            playerData.OnPlayerStatsChanged -= UpdatePlayerBarsFromData;
        }
    }

    private void Update()
    {
        EffectRuntime.Tick(activeEffects, Time.deltaTime, amount => TakeDamage(amount));
        ReadMovementInput();
        HandleScoreInput();
        FaceTowardCursor();
    }

    private void FixedUpdate()
    {
        float effectiveMoveSpeed = EffectRuntime.HasEffect(activeEffects, EffectRuntime.Faint) ? 0f : moveSpeed;
        _rb.linearVelocity = _inputVector * effectiveMoveSpeed + knockbackVelocity;

        if (knockbackRemaining > 0f)
        {
            knockbackRemaining -= Time.fixedDeltaTime;
            if (knockbackRemaining <= 0f)
                knockbackVelocity = Vector2.zero;
        }
    }

    private void ReadMovementInput()
    {
        if (Keyboard.current == null) return;

        _inputVector = Vector2.zero;

        // Horizontal Movement
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            _inputVector.x -= 1f;
        }
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            _inputVector.x += 1f;
        }

        // Vertical Movement
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
        {
            _inputVector.y += 1f;
        }
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
        {
            _inputVector.y -= 1f;
        }

        // Normalize vector to prevent faster diagonal movement
        if (_inputVector.sqrMagnitude > 1f)
        {
            _inputVector.Normalize();
        }
    }

    private void HandleScoreInput()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (playerData != null)
            {
                playerData.currentScore += 10;
                Debug.Log($"Score is now: {playerData.currentScore}");
            }
        }
    }

    public bool TakeDamage(float amount)
    {
        if (playerData == null || amount <= 0f || playerData.currentHealth <= 0f)
            return false;

        playerData.ApplyDamage(amount);
        UpdatePlayerBarsFromData();
        return true;
    }

    public void ApplyEffects(EffectDefinition[] effects)
    {
        EffectRuntime.Apply(activeEffects, effects);
    }

    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (force <= 0f || direction.sqrMagnitude <= Mathf.Epsilon)
            return;

        knockbackVelocity = direction.normalized * force;
        knockbackRemaining = KnockbackDuration;
    }

    private void UpdatePlayerBarsFromData()
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
        if (Mouse.current == null || mainCamera == null || playerSpriteRenderer == null)
            return;

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(new Vector3(
            mouseScreenPosition.x,
            mouseScreenPosition.y,
            Mathf.Abs(mainCamera.transform.position.z - transform.position.z)
        ));

        playerSpriteRenderer.flipX = mouseWorldPosition.x < transform.position.x;
    }
}