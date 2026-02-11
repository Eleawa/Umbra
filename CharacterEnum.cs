// ═══════════════════════════════════════════════════════
//  CharacterEnums.cs
//  Standalone shared enums — no dependencies.
//  Must be in Assets/Scripts alongside the other files.
// ═══════════════════════════════════════════════════════

public enum CharacterType
{
    Player,
    Enemy,
    Boss
}

public enum CharacterState
{
    Idle,
    Walking,
    Sprinting,
    Dodging,
    Attacking,
    DashAttacking,
    Hurt,
    Dead,
    // AI only
    Patrolling,
    Chasing,
    Searching,
    ReturningToStart
}