namespace Core.Domain.Units;

/// <summary>
/// Defines the static base statistics for a unit.
/// These represent the authoritative template values before
/// any derived or effect-based modifications are applied.
/// 
/// Percentage-based stats default to 100 (i.e., 100% effectiveness).
/// </summary>
public class UnitBaseStats
{
    // Core stats
    public int MaxHP { get; }
    public int MaxManaPoints { get; }

    public int MovePoints { get; }
    public int ActionPoints { get; } = 2;

    // Percentage modifiers (100 = normal effectiveness)

    // Outgoing
    public int DamageDealt { get; } = 100;
    public int HealingDealt { get; } = 100;

    // Incoming
    public int HealingReceived { get; } = 100;
    public int PhysicalDamageReceived { get; }
    public int MagicDamageReceived { get; }

    public UnitBaseStats(
        int maxHp,
        int maxManaPoints,
        int movePoints,
        int physicalDamageModifier,
        int magicDamageModifier)
    {
        MaxHP = maxHp;
        MaxManaPoints = maxManaPoints;

        MovePoints = movePoints;

        PhysicalDamageReceived = physicalDamageModifier;
        MagicDamageReceived = magicDamageModifier;
    }
}