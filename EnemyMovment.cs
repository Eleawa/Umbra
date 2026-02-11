// ═══════════════════════════════════════════════════════════════════════════
//  EnemyMovment.cs  (filename kept as-is to match your project)
//
//  Required on the same GameObject:
//    CharacterController2D, Rigidbody2D,
//    CharacterStateController, CharacterStats
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CharacterStateController))]
[RequireComponent(typeof(CharacterStats))]
public class EnemyMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController2D controller;
    [SerializeField] private CharacterStateController stateCtrl;
    [SerializeField] private CharacterStats stats;
    [SerializeField] private Transform player;

    [Header("Detection Settings")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private bool requireLineOfSight = true;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float chaseSpeedMultiplier = 1.3f;

    [Header("Patrol Settings")]
    [SerializeField] private bool usePatrol = false;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolWaitTime = 2f;
    [SerializeField] private float waypointReachedDistance = 0.5f;

    [Header("Behavior Settings")]
    [SerializeField] private bool canSprint = false;
    [SerializeField] private float losePlayerTime = 3f;
    [SerializeField] private float updatePathInterval = 0.2f;

    [Header("Return to Start Settings")]
    [SerializeField] private float returnToStartDistance = 0.5f;

    [Header("Dash Attack Settings")]
    [SerializeField] private float dashWindupTime = 0.4f;
    [SerializeField] private float dashForce = 18f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 1.5f;
    [SerializeField] private float dashHitRadius = 0.4f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Dash Attack Events")]
    public UnityEvent OnDashWindup;
    public UnityEvent OnDashLaunch;
    public UnityEvent OnDashHit;
    public UnityEvent OnDashEnd;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    // ── Enemy state ───────────────────────────────────────────────────────
    public enum EnemyState
    { Idle, Patrol, Chase, Attack, Searching, ReturnToStart }

    private EnemyState currentState = EnemyState.Idle;

    // ── Cached components ─────────────────────────────────────────────────
    private Rigidbody2D rb;
    private Collider2D[] cols;
    private Vector2 moveDirection;

    // ── Detection ─────────────────────────────────────────────────────────
    private bool playerDetected = false;
    private float losePlayerTimer = 0f;
    private float pathUpdateTimer = 0f;
    private Vector2 lastKnownPlayerPosition;
    private Vector2 startPosition;

    // ── Patrol ────────────────────────────────────────────────────────────
    private int currentPatrolIndex = 0;
    private float patrolWaitTimer = 0f;
    private bool waitingAtPatrolPoint = false;

    // ── Dash ──────────────────────────────────────────────────────────────
    private enum DashPhase { Ready, Windup, Dashing }
    private DashPhase dashPhase = DashPhase.Ready;
    private float dashPhaseTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector2 dashDirection;
    private bool dashHitRegistered = false;

    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController2D>();
        if (stateCtrl == null) stateCtrl = GetComponent<CharacterStateController>();
        if (stats == null) stats = GetComponent<CharacterStats>();

        rb = GetComponent<Rigidbody2D>();
        cols = GetComponents<Collider2D>();
        startPosition = transform.position;

        if (player == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }

        OnDashWindup = OnDashWindup ?? new UnityEvent();
        OnDashLaunch = OnDashLaunch ?? new UnityEvent();
        OnDashHit = OnDashHit ?? new UnityEvent();
        OnDashEnd = OnDashEnd ?? new UnityEvent();

        stats.OnDied.AddListener(OnDeath);

        ChangeState(usePatrol && patrolPoints != null && patrolPoints.Length > 0
            ? EnemyState.Patrol : EnemyState.Idle);
    }

    private void Update()
    {
        if (player == null || stats.IsDead) return;
        pathUpdateTimer += Time.deltaTime;
        CheckPlayerDetection();
        UpdateStateMachine();
        CalculateMovement();
    }

    private void FixedUpdate()
    {
        if (stats.IsDead) return;
        if (dashPhase == DashPhase.Dashing) return;
        bool shouldSprint = canSprint && currentState == EnemyState.Chase;
        controller.Move(moveDirection, shouldSprint);
    }

    // ── Detection ─────────────────────────────────────────────────────────
    private void CheckPlayerDetection()
    {
        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= detectionRange)
        {
            if (requireLineOfSight)
            {
                Vector2 dir = (player.position - transform.position).normalized;
                RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, detectionRange, obstacleMask);
                if (hit.collider == null || hit.collider.transform == player)
                    DetectPlayer();
                else
                    HandlePlayerLost();
            }
            else DetectPlayer();
        }
        else HandlePlayerLost();
    }

    private void DetectPlayer()
    {
        playerDetected = true;
        lastKnownPlayerPosition = player.position;
        losePlayerTimer = 0f;
    }

    private void HandlePlayerLost()
    {
        if (!playerDetected) return;
        losePlayerTimer += Time.deltaTime;
        if (losePlayerTimer >= losePlayerTime)
            playerDetected = false;
    }

    // ── State machine ─────────────────────────────────────────────────────
    private void UpdateStateMachine()
    {
        float dist = Vector2.Distance(transform.position, player.position);

        switch (currentState)
        {
            case EnemyState.Idle:
                if (playerDetected)
                    ChangeState(EnemyState.Chase);
                else if (usePatrol && patrolPoints != null && patrolPoints.Length > 0)
                    ChangeState(EnemyState.Patrol);
                break;

            case EnemyState.Patrol:
                if (playerDetected)
                    ChangeState(EnemyState.Chase);
                break;

            case EnemyState.Chase:
                if (!playerDetected)
                    ChangeState(EnemyState.Searching);
                else if (dist <= attackRange)
                    ChangeState(EnemyState.Attack);
                break;

            case EnemyState.Attack:
                if (dashPhase != DashPhase.Ready) break;   // never cut off a dash

                if (!playerDetected)
                {
                    ResetDashCooldown();
                    ChangeState(EnemyState.Searching);
                }
                else if (dist > attackRange)
                {
                    ResetDashCooldown();        // player left → reset so next entry dashes immediately
                    ChangeState(EnemyState.Chase);
                }
                break;

            case EnemyState.Searching:
                if (playerDetected)
                    ChangeState(EnemyState.Chase);
                else if (losePlayerTimer >= losePlayerTime)
                    ChangeState(EnemyState.ReturnToStart);
                break;

            case EnemyState.ReturnToStart:
                if (playerDetected) { ChangeState(EnemyState.Chase); break; }
                if (Vector2.Distance(transform.position, startPosition) <= returnToStartDistance)
                {
                    moveDirection = Vector2.zero;
                    ChangeState(usePatrol && patrolPoints != null && patrolPoints.Length > 0
                        ? EnemyState.Patrol : EnemyState.Idle);
                }
                break;
        }
    }

    private void ChangeState(EnemyState next)
    {
        currentState = next;

        CharacterState mapped;
        switch (next)
        {
            case EnemyState.Patrol: mapped = CharacterState.Patrolling; break;
            case EnemyState.Chase: mapped = CharacterState.Chasing; break;
            case EnemyState.Attack: mapped = CharacterState.DashAttacking; break;
            case EnemyState.Searching: mapped = CharacterState.Searching; break;
            case EnemyState.ReturnToStart: mapped = CharacterState.ReturningToStart; break;
            default: mapped = CharacterState.Idle; break;
        }
        stateCtrl.TrySetState(mapped);
    }

    // ── Movement dispatch ─────────────────────────────────────────────────
    private void CalculateMovement()
    {
        moveDirection = Vector2.zero;
        switch (currentState)
        {
            case EnemyState.Patrol: HandlePatrol(); break;
            case EnemyState.Chase: HandleChase(); break;
            case EnemyState.Attack: HandleAttack(); break;
            case EnemyState.Searching: HandleSearching(); break;
            case EnemyState.ReturnToStart: HandleReturnToStart(); break;
        }
    }

    private void HandlePatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (waitingAtPatrolPoint)
        {
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= patrolWaitTime)
            {
                waitingAtPatrolPoint = false;
                patrolWaitTimer = 0f;
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            }
            return;
        }

        Transform target = patrolPoints[currentPatrolIndex];
        float dist = Vector2.Distance(transform.position, target.position);
        if (dist <= waypointReachedDistance) { waitingAtPatrolPoint = true; return; }
        moveDirection = ((Vector2)target.position - (Vector2)transform.position).normalized;
    }

    private void HandleChase()
    {
        if (pathUpdateTimer >= updatePathInterval)
        {
            pathUpdateTimer = 0f;
            lastKnownPlayerPosition = player.position;
        }
        moveDirection = (lastKnownPlayerPosition - (Vector2)transform.position).normalized;
    }

    private void HandleAttack()
    {
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
            moveDirection = Vector2.zero;
            return;
        }

        switch (dashPhase)
        {
            case DashPhase.Ready:
                StartDashWindup();
                break;
            case DashPhase.Windup:
                moveDirection = Vector2.zero;
                dashPhaseTimer -= Time.deltaTime;
                if (dashPhaseTimer <= 0f) LaunchDash();
                break;
            case DashPhase.Dashing:
                dashPhaseTimer -= Time.deltaTime;
                if (!dashHitRegistered) CheckDashHit();
                if (dashPhaseTimer <= 0f) EndDash();
                break;
        }
    }

    private void HandleSearching()
    {
        float dist = Vector2.Distance(transform.position, lastKnownPlayerPosition);
        moveDirection = dist > 0.5f
            ? (lastKnownPlayerPosition - (Vector2)transform.position).normalized
            : Vector2.zero;
    }

    private void HandleReturnToStart()
    {
        float dist = Vector2.Distance(transform.position, startPosition);
        moveDirection = dist > returnToStartDistance
            ? (startPosition - (Vector2)transform.position).normalized
            : Vector2.zero;
    }

    // ── Dash ──────────────────────────────────────────────────────────────
    private void StartDashWindup()
    {
        dashPhase = DashPhase.Windup;
        dashPhaseTimer = dashWindupTime;
        dashHitRegistered = false;
        dashDirection = ((Vector2)player.position - (Vector2)transform.position).normalized;
        OnDashWindup.Invoke();
    }

    private void LaunchDash()
    {
        dashPhase = DashPhase.Dashing;
        dashPhaseTimer = dashDuration;
        foreach (Collider2D c in cols) c.enabled = false;
        rb.linearVelocity = dashDirection * dashForce;
        OnDashLaunch.Invoke();
    }

    private void CheckDashHit()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, dashHitRadius, playerLayer);
        if (hit == null) return;
        dashHitRegistered = true;
        OnDashHit.Invoke();
    }

    private void EndDash()
    {
        rb.linearVelocity = Vector2.zero;
        foreach (Collider2D c in cols) c.enabled = true;
        dashPhase = DashPhase.Ready;
        dashCooldownTimer = dashCooldown;
        OnDashEnd.Invoke();
    }

    private void ResetDashCooldown()
    {
        dashCooldownTimer = 0f;
        if (dashPhase == DashPhase.Windup)
        {
            dashPhase = DashPhase.Ready;
            foreach (Collider2D c in cols) c.enabled = true;
        }
    }

    // ── Death ─────────────────────────────────────────────────────────────
    private void OnDeath()
    {
        moveDirection = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        foreach (Collider2D c in cols) c.enabled = true;
        enabled = false;
    }

    // ── Public API ────────────────────────────────────────────────────────
    public void SetPlayer(Transform t) => player = t;
    public void SetStartPosition(Vector2 pos) => startPosition = pos;
    public EnemyState GetCurrentState() => currentState;
    public bool IsPlayerDetected() => playerDetected;
    public bool IsDashing() => dashPhase == DashPhase.Dashing;
    public float GetDistanceToPlayer() =>
        player == null ? float.MaxValue : Vector2.Distance(transform.position, player.position);

    // ── Gizmos ────────────────────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = playerDetected ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, dashHitRadius);

        if (player != null && playerDetected)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, player.position);
        }

        Vector3 sv = Application.isPlaying ? (Vector3)(Vector2)startPosition : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(sv, returnToStartDistance);
        if (Application.isPlaying && currentState == EnemyState.ReturnToStart)
            Gizmos.DrawLine(transform.position, sv);

        if (usePatrol && patrolPoints != null)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null) continue;
                Gizmos.DrawWireSphere(patrolPoints[i].position, 0.3f);
                int n = (i + 1) % patrolPoints.Length;
                if (patrolPoints[n] != null)
                    Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[n].position);
            }
        }

#if UNITY_EDITOR
        string lbl = "State: " + currentState;
        if (currentState == EnemyState.Attack)
            lbl += "\nDash: " + dashPhase + "  CD:" + dashCooldownTimer.ToString("F1") + "s";
        if (Application.isPlaying && stats != null)
            lbl += "\nHP:" + stats.CurrentHealth.ToString("F0");
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2, lbl);
#endif
    }
}