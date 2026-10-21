using System.Collections.Generic;
using System.Collections;
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
    private Coroutine startupTutorialRoutine;

    public PlayerData Data => playerData;

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
            playerData.ResetScore();
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
        while (Tutorial.Instance == null || !Tutorial.Instance.isActiveAndEnabled)
            yield return null;

        Tutorial tutorial = Tutorial.Instance;
        tutorial.ShowTutorial("start", "Welcome to the tutorial level, don't worry about dying. Pick up various weapons and try to defeat the boss. The game will get harder over time until the boss is defeated, so keep an eye on the time.");
        tutorial.ShowTutorial("movement", "WASD to move, left click to shoot\nF to interact with objects\n1-3 or mouse wheel to switch weapons\nE to open inventory.");
        startupTutorialRoutine = null;
    }

    private void Update()
    {
        Interactable.UpdateInteraction(transform);
        EffectRuntime.Tick(activeEffects, Time.deltaTime, amount => TakeDamage(amount));
        ReadMovementInput();
        HandleScoreInput();
        FaceTowardCursor();
    }

    private void OnDisable()
    {
        if (startupTutorialRoutine != null)
        {
            StopCoroutine(startupTutorialRoutine);
            startupTutorialRoutine = null;
        }
        Interactable.ClearSelection();
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

    public bool TryUseMP(int amount)
    {
        bool success = amount <= 0 || (playerData != null && playerData.TryUseMP(amount));
        if (success)
            UpdatePlayerBarsFromData();
        return success;
    }

    public void ClearEffects()
    {
        activeEffects.Clear();
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
