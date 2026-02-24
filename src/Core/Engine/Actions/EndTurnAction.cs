namespace Core.Engine.Actions;

using Core.Domain.Types;

public sealed class EndTurnAction : ActionChoice
{
    public EndTurnAction(UnitInstanceId unitId)
        : base(unitId)
    {
    }
}