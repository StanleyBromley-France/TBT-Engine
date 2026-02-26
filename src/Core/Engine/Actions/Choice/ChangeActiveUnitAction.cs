using Core.Domain.Types;

namespace Core.Engine.Actions.Choice;

public sealed class ChangeActiveUnitAction : ActionChoice
{
    public UnitInstanceId NewActiveUnitId { get; }

    public ChangeActiveUnitAction(UnitInstanceId unitId, UnitInstanceId newActiveUnitId)
        : base(unitId)
    {
        NewActiveUnitId = newActiveUnitId;
    }
}