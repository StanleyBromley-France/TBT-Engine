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
    public int MaxHP { get; }
    public int MaxManaPoints { get; }

    public int MovePoints { get; }
    public int ActionPoints { get; } = 2;

    public int ArmourPoints { get; }
    public int MagicResistance { get; }

    // Percentage modifiers (100 = normal effectiveness)
    public int HealingReceived { get; } = 100;
    public int HealingDealt { get; } = 100;
    public int DamageTaken { get; } = 100;
    public int DamageDealt { get; } = 100;

    public UnitBaseStats(
        int maxHp,
        int maxManaPoints,
        int movePoints,
        int armourPoints,
        int magicResistance)
    {
        MaxHP = maxHp;
        MaxManaPoints = maxManaPoints;

        MovePoints = movePoints;

        ArmourPoints = armourPoints;
        MagicResistance = magicResistance;
    }
}
