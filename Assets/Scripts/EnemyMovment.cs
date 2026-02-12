using UnityEngine;

[RequireComponent(typeof(CharacterController2D))]
public class EnemyMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController2D controller;
    [SerializeField] private Transform player;

    [Header("Detection Settings")]
    [SerializeField] private float detectionRange = 10f;              // How far the enemy can see the player
    [SerializeField] private float attackRange = 2f;                  // How close before stopping to attack
    [SerializeField] private LayerMask obstacleMask;                  // Obstacles that block line of sight
    [SerializeField] private bool requireLineOfSight = true;          // Whether enemy needs clear view of player

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;                    // Enemy movement speed
    [SerializeField] private float chaseSpeedMultiplier = 1.3f;       // Speed boost when chasing
    [SerializeField] private float rotationSpeed = 360f;              // How fast enemy rotates to face player

    [Header("Patrol Settings")]
    [SerializeField] private bool usePatrol = false;                  // Enable patrol behavior
    [SerializeField] private Transform[] patrolPoints;                // Points to patrol between
    [SerializeField] private float patrolWaitTime = 2f;               // Time to wait at each patrol point
    [SerializeField] private float waypointReachedDistance = 0.5f;    // How close to waypoint before moving to next

    [Header("Behavior Settings")]
    [SerializeField] private bool canSprint = false;                  // Whether enemy can sprint
    [SerializeField] private float losePlayerTime = 3f;               // Time before losing track of player
    [SerializeField] private float updatePathInterval = 0.2f;         // How often to recalculate path to player

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    // State variables
    private EnemyState currentState = EnemyState.Idle;
    private Vector2 moveDirection;
    private bool playerDetected = false;
    private float losePlayerTimer = 0f;
    private float pathUpdateTimer = 0f;

    // Patrol variables
    private int currentPatrolIndex = 0;
    private float patrolWaitTimer = 0f;
    private bool waitingAtPatrolPoint = false;

    // Last known player position
    private Vector2 lastKnownPlayerPosition;

    public enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Searching
    }

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<CharacterController2D>();

        // Find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        // Set initial state
        if (usePatrol && patrolPoints != null && patrolPoints.Length > 0)
            currentState = EnemyState.Patrol;
        else
            currentState = EnemyState.Idle;
    }

    private void Update()
    {
        if (player == null)
            return;

        // Update timers
        pathUpdateTimer += Time.deltaTime;

        // Check for player detection
        CheckPlayerDetection();

        // Update state machine
        UpdateStateMachine();

        // Calculate movement direction based on state
        CalculateMovement();
    }

    private void FixedUpdate()
    {
        // Send movement to controller
        bool shouldSprint = canSprint && currentState == EnemyState.Chase;
        controller.Move(moveDirection, shouldSprint);
    }

    private void CheckPlayerDetection()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Check if player is in detection range
        if (distanceToPlayer <= detectionRange)
        {
            // Check line of sight if required
            if (requireLineOfSight)
            {
                Vector2 directionToPlayer = (player.position - transform.position).normalized;
                RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, detectionRange, obstacleMask);

                if (hit.collider == null || hit.collider.transform == player)
                {
                    playerDetected = true;
                    lastKnownPlayerPosition = player.position;
                    losePlayerTimer = 0f;
                }
                else
                {
                    HandlePlayerLost();
                }
            }
            else
            {
                playerDetected = true;
                lastKnownPlayerPosition = player.position;
                losePlayerTimer = 0f;
            }
        }
        else
        {
            HandlePlayerLost();
        }
    }

    private void HandlePlayerLost()
    {
        if (playerDetected)
        {
            losePlayerTimer += Time.deltaTime;
            if (losePlayerTimer >= losePlayerTime)
            {
                playerDetected = false;
            }
        }
    }

    private void UpdateStateMachine()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        switch (currentState)
        {
            case EnemyState.Idle:
                if (playerDetected)
                {
                    currentState = EnemyState.Chase;
                }
                else if (usePatrol && patrolPoints != null && patrolPoints.Length > 0)
                {
                    currentState = EnemyState.Patrol;
                }
                break;

            case EnemyState.Patrol:
                if (playerDetected)
                {
                    currentState = EnemyState.Chase;
                }
                break;

            case EnemyState.Chase:
                if (!playerDetected)
                {
                    currentState = EnemyState.Searching;
                }
                else if (distanceToPlayer <= attackRange)
                {
                    currentState = EnemyState.Attack;
                }
                break;

            case EnemyState.Attack:
                if (distanceToPlayer > attackRange)
                {
                    currentState = EnemyState.Chase;
                }
                else if (!playerDetected)
                {
                    currentState = EnemyState.Searching;
                }
                break;

            case EnemyState.Searching:
                if (playerDetected)
                {
                    currentState = EnemyState.Chase;
                }
                else if (losePlayerTimer >= losePlayerTime)
                {
                    if (usePatrol)
                        currentState = EnemyState.Patrol;
                    else
                        currentState = EnemyState.Idle;
                }
                break;
        }
    }

    private void CalculateMovement()
    {
        moveDirection = Vector2.zero;

        switch (currentState)
        {
            case EnemyState.Idle:
                // No movement
                break;

            case EnemyState.Patrol:
                HandlePatrol();
                break;

            case EnemyState.Chase:
                HandleChase();
                break;

            case EnemyState.Attack:
                HandleAttack();
                break;

            case EnemyState.Searching:
                HandleSearching();
                break;
        }
    }

    private void HandlePatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        // Wait at patrol point
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

        // Move to patrol point
        Transform targetPoint = patrolPoints[currentPatrolIndex];
        Vector2 direction = ((Vector2)targetPoint.position - (Vector2)transform.position).normalized;
        float distance = Vector2.Distance(transform.position, targetPoint.position);

        if (distance <= waypointReachedDistance)
        {
            waitingAtPatrolPoint = true;
            moveDirection = Vector2.zero;
        }
        else
        {
            moveDirection = direction;
        }
    }

    private void HandleChase()
    {
        // Update path at intervals
        if (pathUpdateTimer >= updatePathInterval)
        {
            pathUpdateTimer = 0f;
            lastKnownPlayerPosition = player.position;
        }

        Vector2 direction = (lastKnownPlayerPosition - (Vector2)transform.position).normalized;
        moveDirection = direction;
    }

    private void HandleAttack()
    {
        // Stop moving but face the player
        moveDirection = Vector2.zero;
        
        // Optionally rotate to face player
        Vector2 directionToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
        controller.Move(Vector2.zero, false);
    }

    private void HandleSearching()
    {
        // Move to last known position
        float distanceToLastKnown = Vector2.Distance(transform.position, lastKnownPlayerPosition);
        
        if (distanceToLastKnown > 0.5f)
        {
            Vector2 direction = (lastKnownPlayerPosition - (Vector2)transform.position).normalized;
            moveDirection = direction;
        }
        else
        {
            moveDirection = Vector2.zero;
        }
    }

    // Public methods
    public void SetPlayer(Transform newPlayer)
    {
        player = newPlayer;
    }

    public EnemyState GetCurrentState()
    {
        return currentState;
    }

    public bool IsPlayerDetected()
    {
        return playerDetected;
    }

    public float GetDistanceToPlayer()
    {
        if (player == null)
            return float.MaxValue;
        return Vector2.Distance(transform.position, player.position);
    }

    // Debug visualization
    private void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        // Detection range
        Gizmos.color = playerDetected ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Line to player
        if (player != null && playerDetected)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, player.position);
        }

        // Patrol points
        if (usePatrol && patrolPoints != null)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawWireSphere(patrolPoints[i].position, 0.3f);
                    
                    // Draw line to next patrol point
                    int nextIndex = (i + 1) % patrolPoints.Length;
                    if (patrolPoints[nextIndex] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[nextIndex].position);
                    }
                }
            }
        }

        // Current state text
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2, $"State: {currentState}");
        #endif
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;

        // Draw field of view arc (optional enhancement)
        if (requireLineOfSight)
        {
            Gizmos.color = new Color(1, 1, 0, 0.2f);
            // Simple circle for now, could be enhanced to show actual FOV cone
        }
    }
}
