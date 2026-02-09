namespace Core.Domain.Units;

/// <summary>
/// Defines the static base statistics for a unit
/// </summary>
public class UnitBaseStats
{
    public int MaxHP { get; }
    public int MaxManaPoints { get; }
    public int MovePoints { get; }
    public int DefaultActionPoints { get; } = 2;
    public int ArmourPoints { get; }
    public int MagicResistance { get; }
    public UnitBaseStats(int maxHp, int maxMana, int movePoints, int armourPoints, int magicResistance)
    {
        MaxHP = maxHp;
        MaxManaPoints = maxMana;
        MovePoints = movePoints;
        ArmourPoints = armourPoints;
        MagicResistance = magicResistance;
    }
}
