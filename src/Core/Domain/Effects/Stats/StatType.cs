namespace Core.Domain.Effects.Stats;

/// <summary>
/// Specifies the different unit attributes that effects can modify.
/// </summary>
public enum StatType
{
    // Core resources
    MaxHP,
    MaxManaPoints,

    MovePoints,
    ActionPoints,

    // Percentage modifiers (100 = normal effectiveness)

    // Outgoing
    DamageDealt,
    HealingDealt,

    // Incoming
    HealingReceived,

    // Damage-type specific
    PhysicalDamageReceived,
    MagicDamageReceived
}