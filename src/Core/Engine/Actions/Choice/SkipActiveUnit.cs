namespace Core.Engine.Actions.Choice;

using Core.Domain.Types;

public sealed class SkipActiveUnit : ActionChoice
{
    public SkipActiveUnit(UnitInstanceId unitId)
        : base(unitId)
    {
    }
}