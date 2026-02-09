namespace Core.Domain.Units;

/// <summary>
/// Defines the static base statistics for a unit
/// </summary>
public class UnitBaseStats
{
    public int MaxHP { get; private set; }
    public int MaxManaPoints { get; private set; }
    public int MovePoints { get; private set; }
    public int DefaultActionPoints { get; private set; } = 2;
    public int ArmourPoints { get; private set; }
    public int MagicResistance { get; private set; }
    public UnitBaseStats(int maxHp, int maxMana, int movePoints, int armourPoints, int magicResistance)
    {
        MaxHP = maxHp;
        MaxManaPoints = maxMana;
        MovePoints = movePoints;
        ArmourPoints = armourPoints;
        MagicResistance = magicResistance;
    }
}
