namespace Core.Engine.Actions;

using Core.Domain.Types;

public sealed class MoveAction : ActionChoice
{
    public HexCoord Target { get; }

    public MoveAction(UnitInstanceId unitId, HexCoord target)
        : base(unitId)
    {
        Target = target;
    }
}