using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterController2D : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float m_MovementSpeed = 5f;
    [SerializeField] private float m_SprintMultiplier = 1.5f;
    [Range(0, .3f)][SerializeField] private float m_MovementSmoothing = .05f;

    [Header("Dodge Settings")]
    [SerializeField] private float m_DodgeForce = 15f;
    [SerializeField] private float m_DodgeDuration = 0.2f;
    [SerializeField] private float m_DodgeCooldown = 1f;

    [Header("Flip Settings")]
    [SerializeField] private SpriteRenderer m_Sprite;

    private Rigidbody2D m_Rigidbody2D;
    private Vector2 m_Velocity = Vector2.zero;
    private Vector2 m_LastMovementDirection = Vector2.down;

    // Dodge
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

        if (m_Sprite == null)
            m_Sprite = GetComponentInChildren<SpriteRenderer>();

        if (OnStartMoving == null) OnStartMoving = new UnityEvent();
        if (OnStopMoving == null) OnStopMoving = new UnityEvent();
        if (OnDodge == null) OnDodge = new UnityEvent();
        if (OnSprintEvent == null) OnSprintEvent = new BoolEvent();
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

        if (movement.magnitude > 1f)
            movement.Normalize();

        if (movement.magnitude > 0.01f)
        {
            m_LastMovementDirection = movement;
            HandleFlip(m_LastMovementDirection);
        }

        // Sprint event
        if (sprint && movement.magnitude > 0.01f)
        {
            if (!m_WasSprinting)
            {
                m_WasSprinting = true;
                OnSprintEvent.Invoke(true);
            }
        }
        else if (m_WasSprinting)
        {
            m_WasSprinting = false;
            OnSprintEvent.Invoke(false);
        }

        float currentSpeed = m_MovementSpeed * (sprint ? m_SprintMultiplier : 1f);
        Vector2 targetVelocity = movement * currentSpeed;

        m_Rigidbody2D.linearVelocity = Vector2.SmoothDamp(
            m_Rigidbody2D.linearVelocity,
            targetVelocity,
            ref m_Velocity,
            m_MovementSmoothing
        );

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

    private void HandleFlip(Vector2 direction)
    {
        if (m_Sprite == null)
            return;

        // Flip X (Left / Right)
        if (Mathf.Abs(direction.x) > 0.01f)
            m_Sprite.flipX = direction.x < 0;

        // Flip Y (Up / Down)
        if (Mathf.Abs(direction.y) > 0.01f)
            m_Sprite.flipY = direction.y < 0;
    }

    // Getters
    public bool IsDodging() => m_IsDodging;
    public bool CanDodge() => !m_IsDodging && m_DodgeCooldownTimer <= 0f;
    public Vector2 GetFacingDirection() => m_LastMovementDirection;
    public float GetCurrentSpeed() => m_Rigidbody2D.linearVelocity.magnitude;
}
