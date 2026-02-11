// ═══════════════════════════════════════════════════════════════════════════
//  CharacterStateConfig.cs
//
//  ScriptableObject data asset — NOT added to GameObjects.
//  Create via: Assets → Create → Character → State Config
//  OR leave the slot empty and the scripts auto-generate a preset at runtime.
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;

[CreateAssetMenu(fileName = "CharacterStateConfig",
                 menuName = "Character/State Config")]
public class CharacterStateConfig : ScriptableObject
{
    // ── Identity ──────────────────────────────────────────────────────────
    [Header("Identity")]
    public CharacterType characterType = CharacterType.Enemy;

    // ── Health ────────────────────────────────────────────────────────────
    [Header("Health")]
    public float maxHealth = 100f;
    public bool canRegen = false;
    public float healthRegenRate = 0f;    // HP per second
    public float healthRegenDelay = 5f;    // seconds after last hit

    // ── Stamina ───────────────────────────────────────────────────────────
    [Header("Stamina")]
    public bool hasStamina = true;
    public float maxStamina = 100f;
    public float staminaRegenRate = 20f;  // per second
    public float staminaRegenDelay = 1f;   // seconds after last use
    public float sprintStaminaCost = 15f;  // per second while sprinting
    public float dodgeStaminaCost = 25f;  // flat cost per dodge
    public float dashStaminaCost = 0f;   // flat cost per dash

    // ── Allowed States ────────────────────────────────────────────────────
    [Header("Allowed States")]
    public bool allowIdle = true;
    public bool allowWalking = true;
    public bool allowSprinting = false;
    public bool allowDodging = false;
    public bool allowAttacking = true;
    public bool allowDashAttacking = false;
    public bool allowHurt = true;
    public bool allowDead = true;
    public bool allowPatrolling = true;
    public bool allowChasing = true;
    public bool allowSearching = true;
    public bool allowReturningToStart = true;

    // ── Runtime query ─────────────────────────────────────────────────────
    public bool IsStateAllowed(CharacterState state)
    {
        switch (state)
        {
            case CharacterState.Idle: return allowIdle;
            case CharacterState.Walking: return allowWalking;
            case CharacterState.Sprinting: return allowSprinting;
            case CharacterState.Dodging: return allowDodging;
            case CharacterState.Attacking: return allowAttacking;
            case CharacterState.DashAttacking: return allowDashAttacking;
            case CharacterState.Hurt: return allowHurt;
            case CharacterState.Dead: return allowDead;
            case CharacterState.Patrolling: return allowPatrolling;
            case CharacterState.Chasing: return allowChasing;
            case CharacterState.Searching: return allowSearching;
            case CharacterState.ReturningToStart: return allowReturningToStart;
            default: return false;
        }
    }

    // ── Presets (auto-generated when no asset is assigned) ────────────────
    public static CharacterStateConfig MakePlayer()
    {
        var c = CreateInstance<CharacterStateConfig>();
        c.characterType = CharacterType.Player;
        c.maxHealth = 100f;
        c.canRegen = false;
        c.hasStamina = true;
        c.maxStamina = 100f;
        c.staminaRegenRate = 20f;
        c.staminaRegenDelay = 1f;
        c.sprintStaminaCost = 15f;
        c.dodgeStaminaCost = 25f;
        c.allowSprinting = true;
        c.allowDodging = true;
        c.allowDashAttacking = false;
        c.allowPatrolling = false;
        c.allowChasing = false;
        c.allowSearching = false;
        c.allowReturningToStart = false;
        return c;
    }

    public static CharacterStateConfig MakeEnemy()
    {
        var c = CreateInstance<CharacterStateConfig>();
        c.characterType = CharacterType.Enemy;
        c.maxHealth = 60f;
        c.canRegen = false;
        c.hasStamina = false;
        c.maxStamina = 0f;
        c.allowSprinting = false;
        c.allowDodging = false;
        c.allowDashAttacking = true;
        return c;
    }

    public static CharacterStateConfig MakeBoss()
    {
        var c = CreateInstance<CharacterStateConfig>();
        c.characterType = CharacterType.Boss;
        c.maxHealth = 500f;
        c.canRegen = true;
        c.healthRegenRate = 5f;
        c.healthRegenDelay = 8f;
        c.hasStamina = true;
        c.maxStamina = 200f;
        c.staminaRegenRate = 30f;
        c.staminaRegenDelay = 2f;
        c.dashStaminaCost = 20f;
        c.allowSprinting = true;
        c.allowDodging = true;
        c.allowDashAttacking = true;
        return c;
    }
}