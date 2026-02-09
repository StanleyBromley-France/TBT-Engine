namespace Core.Domain.Units;

/// <summary>
/// UnitResources represent mutable, runtime-only values for a unit.
/// These values are modified exclusively by GameMutationContext and
/// are exposed publicly only through IReadOnlyUnitResources.
/// </summary>
public class UnitResources : IReadOnlyUnitResources
{
    public int HP { get; set; }
    public int MovePoints { get; set; }
    public int ActionPoints { get; set; }
    public int Mana { get; set; }

    public UnitResources(int hp, int movePoints, int actionPoints, int mana)
    {
        HP = hp;
        MovePoints = movePoints;
        ActionPoints = actionPoints;
        Mana = mana;
    }
}
