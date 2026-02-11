// ═══════════════════════════════════════════════════════════════════════════
//  CharacterStats.cs
//  Add to every character GameObject alongside CharacterStateController.
//  Handles Health and Stamina. Automatically pushes Hurt/Dead state.
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterStateController))]
public class CharacterStats : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Leave empty — auto-generated from CharacterStateController type.")]
    [SerializeField] private CharacterStateConfig config;

    [Header("Runtime (read-only)")]
    [SerializeField] private float currentHealth;
    [SerializeField] private float currentStamina;

    // ── Health Events ─────────────────────────────────────────────────────
    [Header("Health Events")]
    public UnityEvent<float> OnHealthChanged;   // current health value
    public UnityEvent<float> OnDamageTaken;     // damage amount
    public UnityEvent<float> OnHealed;          // heal amount
    public UnityEvent OnDied;

    // ── Stamina Events ────────────────────────────────────────────────────
    [Header("Stamina Events")]
    public UnityEvent<float> OnStaminaChanged;  // current stamina value
    public UnityEvent OnStaminaDepleted;
    public UnityEvent OnStaminaFull;

    // ── Private ───────────────────────────────────────────────────────────
    private CharacterStateController stateCtrl;
    private float healthRegenTimer = 0f;
    private float staminaRegenTimer = 0f;
    private bool isDead = false;
    private bool staminaWasFull = true;

    // ── Properties ────────────────────────────────────────────────────────
    public float CurrentHealth => currentHealth;
    public float MaxHealth => config != null ? config.maxHealth : 100f;
    public float CurrentStamina => currentStamina;
    public float MaxStamina => config != null ? config.maxStamina : 100f;
    public float HealthPercent => MaxHealth > 0 ? currentHealth / MaxHealth : 0f;
    public float StaminaPercent => MaxStamina > 0 ? currentStamina / MaxStamina : 0f;
    public bool IsDead => isDead;

    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        stateCtrl = GetComponent<CharacterStateController>();

        if (config == null)
        {
            if (stateCtrl.Type == CharacterType.Player) config = CharacterStateConfig.MakePlayer();
            else if (stateCtrl.Type == CharacterType.Boss) config = CharacterStateConfig.MakeBoss();
            else config = CharacterStateConfig.MakeEnemy();
        }

        OnHealthChanged = OnHealthChanged ?? new UnityEvent<float>();
        OnDamageTaken = OnDamageTaken ?? new UnityEvent<float>();
        OnHealed = OnHealed ?? new UnityEvent<float>();
        OnDied = OnDied ?? new UnityEvent();
        OnStaminaChanged = OnStaminaChanged ?? new UnityEvent<float>();
        OnStaminaDepleted = OnStaminaDepleted ?? new UnityEvent();
        OnStaminaFull = OnStaminaFull ?? new UnityEvent();

        currentHealth = config.maxHealth;
        currentStamina = config.hasStamina ? config.maxStamina : 0f;
    }

    private void Update()
    {
        if (isDead) return;
        TickHealthRegen();
        TickStaminaRegen();
    }

    // ── Health API ────────────────────────────────────────────────────────

    /// <summary>Deal damage. Returns actual damage applied.</summary>
    public float TakeDamage(float amount)
    {
        if (isDead || amount <= 0f) return 0f;

        float applied = Mathf.Min(amount, currentHealth);
        currentHealth -= applied;
        healthRegenTimer = 0f;

        OnHealthChanged.Invoke(currentHealth);
        OnDamageTaken.Invoke(applied);

        if (currentHealth <= 0f)
            Die();
        else
            stateCtrl.TrySetState(CharacterState.Hurt);

        return applied;
    }

    /// <summary>Restore health. Returns actual amount healed.</summary>
    public float Heal(float amount)
    {
        if (isDead || amount <= 0f) return 0f;

        float before = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, config.maxHealth);
        float applied = currentHealth - before;

        if (applied > 0f)
        {
            OnHealthChanged.Invoke(currentHealth);
            OnHealed.Invoke(applied);
        }
        return applied;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        currentHealth = 0f;
        stateCtrl.ForceSetState(CharacterState.Dead);
        OnDied.Invoke();
    }

    private void TickHealthRegen()
    {
        if (!config.canRegen || currentHealth >= config.maxHealth) return;
        healthRegenTimer += Time.deltaTime;
        if (healthRegenTimer < config.healthRegenDelay) return;
        Heal(config.healthRegenRate * Time.deltaTime);
    }

    // ── Stamina API ───────────────────────────────────────────────────────

    /// <summary>Spend a flat stamina cost. Returns false if not enough stamina.</summary>
    public bool TryUseStamina(float amount)
    {
        if (!config.hasStamina || amount <= 0f) return true;
        if (currentStamina < amount) return false;

        currentStamina -= amount;
        staminaRegenTimer = 0f;
        OnStaminaChanged.Invoke(currentStamina);

        if (currentStamina <= 0f)
        {
            currentStamina = 0f;
            staminaWasFull = false;
            OnStaminaDepleted.Invoke();
        }
        return true;
    }

    /// <summary>Drain stamina over time (e.g. sprinting). Call every frame.</summary>
    public void DrainStamina(float amountPerSecond)
    {
        if (!config.hasStamina || amountPerSecond <= 0f) return;
        currentStamina = Mathf.Max(0f, currentStamina - amountPerSecond * Time.deltaTime);
        staminaRegenTimer = 0f;
        OnStaminaChanged.Invoke(currentStamina);

        if (currentStamina <= 0f && staminaWasFull)
        {
            staminaWasFull = false;
            OnStaminaDepleted.Invoke();
        }
    }

    public bool HasEnoughStamina(float amount) =>
        !config.hasStamina || currentStamina >= amount;

    private void TickStaminaRegen()
    {
        if (!config.hasStamina || currentStamina >= config.maxStamina) return;
        staminaRegenTimer += Time.deltaTime;
        if (staminaRegenTimer < config.staminaRegenDelay) return;

        float before = currentStamina;
        currentStamina = Mathf.Min(currentStamina + config.staminaRegenRate * Time.deltaTime,
                                   config.maxStamina);
        if (currentStamina != before)
            OnStaminaChanged.Invoke(currentStamina);

        if (currentStamina >= config.maxStamina && !staminaWasFull)
        {
            staminaWasFull = true;
            OnStaminaFull.Invoke();
        }
    }

    // ── Shorthand for common costs ────────────────────────────────────────
    public bool TrySpendDodgeStamina() => TryUseStamina(config.dodgeStaminaCost);
    public bool TrySpendDashStamina() => TryUseStamina(config.dashStaminaCost);
    public void DrainSprintStamina() => DrainStamina(config.sprintStaminaCost);

    // ── Debug overlay ─────────────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || config == null) return;
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2.5f,
            $"HP {currentHealth:F0}/{config.maxHealth:F0}  " +
            $"ST {currentStamina:F0}/{config.maxStamina:F0}");
    }
#endif
}