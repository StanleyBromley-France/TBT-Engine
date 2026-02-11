namespace Core.Domain.Effects;

/// <summary>
/// Specifies the different unit attributes that effects can modify
/// </summary>
public enum UnitAttributeType
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

