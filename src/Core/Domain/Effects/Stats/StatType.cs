namespace Core.Domain.Effects.Stats;

/// <summary>
/// Specifies the different unit attributes that effects can modify
/// </summary>
public enum StatType
{
    MovePoints,
    ArmourPoints,
    MagicResistance,
    MaxHP,
    MaxManaPoints,
    ActionPoints,
    HealingReceived,
    HealingDealt,
    DamageTaken,
    DamageDealt
}

