using UnityEngine;
using UnityEngine.InputSystem; // Required for New Input System

[RequireComponent(typeof(Rigidbody2D))] // Automatically adds Rigidbody2D if missing
public class Player : MonoBehaviour
{
    [Header("Data References")]
    [SerializeField] private ScoreData scoreData;

    [Header("Movement Configuration")]
    [SerializeField] private float moveSpeed = 5.0f; // Movement speed in units/sec

    private Rigidbody2D _rb;
    private Vector2 _inputVector;
    private Camera mainCamera;

    private void Awake()
    {
        // Cache the Rigidbody2D reference
        _rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
    }

    private void Start()
    {
        if (scoreData != null)
        {
            scoreData.ResetScore();
        }
    }

    private void Update()
    {
        ReadMovementInput();
        HandleScoreInput();
        RotateTowardsCursor();
    }

    private void FixedUpdate()
    {
        // Apply velocity directly to the physics body during the physics step
        _rb.linearVelocity = _inputVector * moveSpeed;
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
            if (scoreData != null)
            {
                scoreData.currentScore += 10;
                Debug.Log($"Score is now: {scoreData.currentScore}");
            }
        }
    }

    private void RotateTowardsCursor()
    {
        // 1. Get current mouse position in screen coordinates using the New Input System
        if (Mouse.current == null) return;
        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();

        // 2. Convert screen position to 2D world space
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(new Vector3(
            mouseScreenPosition.x, 
            mouseScreenPosition.y, 
            mainCamera.nearClipPlane
        ));

        // 3. Calculate direction vector from player to mouse
        Vector2 direction = (Vector2)mouseWorldPosition - (Vector2)transform.position;

        // 4. Calculate angle in degrees
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 5. Apply rotation (subtract 90f if your sprite faces UP by default)
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}