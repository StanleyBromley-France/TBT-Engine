namespace Core.Engine.Actions.Choice;

using Core.Domain.Types;

public sealed class SkipActiveUnitAction : ActionChoice
{
    public SkipActiveUnitAction(UnitInstanceId unitId)
        : base(unitId)
    {
    }
}