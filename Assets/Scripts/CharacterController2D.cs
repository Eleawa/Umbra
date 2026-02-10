using UnityEngine;
using UnityEngine.Events;

public class CharacterController2D : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float m_MovementSpeed = 5f;
    [SerializeField] private float m_SprintMultiplier = 1.5f;

    [Header("Dodge Settings")]
    [SerializeField] private float m_DodgeForce = 15f;
    [SerializeField] private float m_DodgeDuration = 0.2f;
    [SerializeField] private float m_DodgeCooldown = 1f;

    [Header("Sprite Settings")]
    [SerializeField] private SpriteRenderer m_SpriteRenderer;
    [SerializeField] private bool m_FlipHorizontally = true;
    [SerializeField] private bool m_FlipVertically = true;

    private Rigidbody2D m_Rigidbody2D;
    private Vector2 m_LastMovementDirection = Vector2.down;

    // Dodge variables
    private bool m_IsDodging = false;
    private float m_DodgeTimer = 0f;
    private float m_DodgeCooldownTimer = 0f;
    private Vector2 m_DodgeDirection;

    [Header("Events")]
    public UnityEvent OnStartMoving;
    public UnityEvent OnStopMoving;
    public UnityEvent OnDodge;

    [System.Serializable]
    public class BoolEvent : UnityEvent<bool> { }
    public BoolEvent OnSprintEvent;

    private bool m_WasSprinting = false;
    private bool m_WasMoving = false;

    private void Awake()
    {
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        m_Rigidbody2D.gravityScale = 0f;
        m_Rigidbody2D.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Auto-find SpriteRenderer if not assigned
        if (m_SpriteRenderer == null)
            m_SpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        OnStartMoving ??= new UnityEvent();
        OnStopMoving ??= new UnityEvent();
        OnDodge ??= new UnityEvent();
        OnSprintEvent ??= new BoolEvent();
    }

    private void Update()
    {
        if (m_IsDodging)
        {
            m_DodgeTimer -= Time.deltaTime;
            if (m_DodgeTimer <= 0f)
                m_IsDodging = false;
        }

        if (m_DodgeCooldownTimer > 0f)
            m_DodgeCooldownTimer -= Time.deltaTime;
    }

    public void Move(Vector2 movement, bool sprint)
    {
        if (m_IsDodging)
            return;

        // Normalize diagonal movement
        if (movement.magnitude > 1f)
            movement.Normalize();

        // Store last movement direction and update sprite flip
        if (movement.magnitude > 0.01f)
        {
            m_LastMovementDirection = movement;
            UpdateSpriteFlip(movement);
        }

        // Sprint events
        bool isSprinting = sprint && movement.magnitude > 0.01f;
        if (isSprinting && !m_WasSprinting)
        {
            m_WasSprinting = true;
            OnSprintEvent.Invoke(true);
        }
        else if (!isSprinting && m_WasSprinting)
        {
            m_WasSprinting = false;
            OnSprintEvent.Invoke(false);
        }

        // Calculate and apply velocity directly (instant response)
        float speed = m_MovementSpeed * (sprint ? m_SprintMultiplier : 1f);
        m_Rigidbody2D.linearVelocity = movement * speed;

        // Movement events
        bool isMoving = movement.magnitude > 0.01f;
        if (isMoving && !m_WasMoving)
        {
            m_WasMoving = true;
            OnStartMoving.Invoke();
        }
        else if (!isMoving && m_WasMoving)
        {
            m_WasMoving = false;
            OnStopMoving.Invoke();
        }
    }

    private void UpdateSpriteFlip(Vector2 direction)
    {
        if (m_SpriteRenderer == null)
            return;

        // Flip horizontally based on left/right movement
        if (m_FlipHorizontally && Mathf.Abs(direction.x) > 0.01f)
        {
            m_SpriteRenderer.flipX = direction.x < 0;
        }
    }

    public bool Dodge()
    {
        if (m_IsDodging || m_DodgeCooldownTimer > 0f)
            return false;

        m_IsDodging = true;
        m_DodgeTimer = m_DodgeDuration;
        m_DodgeCooldownTimer = m_DodgeCooldown;
        m_DodgeDirection = m_LastMovementDirection;

        m_Rigidbody2D.linearVelocity = m_DodgeDirection * m_DodgeForce;
        OnDodge.Invoke();

        return true;
    }

    // Public getters
    public bool IsDodging() => m_IsDodging;
    public bool CanDodge() => !m_IsDodging && m_DodgeCooldownTimer <= 0f;
    public float GetDodgeCooldownPercent() => Mathf.Clamp01(m_DodgeCooldownTimer / m_DodgeCooldown);
    public Vector2 GetFacingDirection() => m_LastMovementDirection;
    public float GetCurrentSpeed() => m_Rigidbody2D.linearVelocity.magnitude;
}