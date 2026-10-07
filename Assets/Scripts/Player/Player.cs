using UnityEngine;
using UnityEngine.InputSystem; // Required for New Input System

[RequireComponent(typeof(Rigidbody2D))] // Automatically adds Rigidbody2D if missing
public class Player : MonoBehaviour
{
    [Header("Data References")]
    [SerializeField] private ScoreData scoreData;
    [SerializeField] private SpriteRenderer playerSpriteRenderer;

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
        if (playerSpriteRenderer == null)
            playerSpriteRenderer = GetComponent<SpriteRenderer>();

        if (playerSpriteRenderer != null)
            playerSpriteRenderer.flipX = false;
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
        FaceTowardCursor();
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