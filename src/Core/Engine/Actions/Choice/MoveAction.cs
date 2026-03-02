namespace Core.Engine.Actions.Choice;

using Core.Domain.Types;

public sealed class MoveAction : ActionChoice
{
    public HexCoord TargetHex { get; }

    public MoveAction(UnitInstanceId unitId, HexCoord target)
        : base(unitId)
    {
        TargetHex = target;
    }
}