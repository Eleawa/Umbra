// ═════════════════════════════════════════════════════════════════════════════
//  CharacterStateController.cs
//
//  Lightweight runtime state machine. Attach to every character.
//  Uses CharacterStateConfig to validate transitions.
//  Other scripts (EnemyMovement, PlayerMovement, etc.) call TrySetState().
// ═════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UnityEngine.Events;

public class CharacterStateController : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Assign a CharacterStateConfig asset. Leave null to auto-generate from CharacterType.")]
    [SerializeField] private CharacterStateConfig config;
    [SerializeField] private CharacterType characterType = CharacterType.Enemy;

    [Header("Events")]
    public UnityEvent<CharacterState> OnStateEnter;  // new state
    public UnityEvent<CharacterState> OnStateExit;   // old state

    private CharacterState current = CharacterState.Idle;
    private CharacterState previous = CharacterState.Idle;

    public CharacterState CurrentState => current;
    public CharacterState PreviousState => previous;
    public CharacterType Type => config != null ? config.characterType : characterType;

    private void Awake()
    {
        if (config == null)
        {
            config = characterType switch
            {
                CharacterType.Player => CharacterStateConfig.MakePlayer(),
                CharacterType.Boss => CharacterStateConfig.MakeBoss(),
                _ => CharacterStateConfig.MakeEnemy()
            };
        }

        OnStateEnter ??= new UnityEvent<CharacterState>();
        OnStateExit ??= new UnityEvent<CharacterState>();
    }

    /// <summary>
    /// Attempt to move to <paramref name="newState"/>.
    /// Returns false (and does nothing) if the state is not allowed by the config.
    /// </summary>
    public bool TrySetState(CharacterState newState)
    {
        if (current == newState) return true;
        if (config != null && !config.IsStateAllowed(newState)) return false;

        Transition(newState);
        return true;
    }

    /// <summary>Force a state regardless of config — use for death, cutscenes, etc.</summary>
    public void ForceSetState(CharacterState newState)
    {
        if (current == newState) return;
        Transition(newState);
    }

    private void Transition(CharacterState newState)
    {
        previous = current;
        OnStateExit.Invoke(previous);
        current = newState;
        OnStateEnter.Invoke(current);
    }

    public bool IsInState(CharacterState state) => current == state;
    public bool IsStateAllowed(CharacterState state) => config == null || config.IsStateAllowed(state);
}