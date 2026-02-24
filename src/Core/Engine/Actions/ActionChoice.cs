namespace Core.Engine.Actions;

using Core.Domain.Types;

public abstract class ActionChoice
{
    public UnitInstanceId UnitId { get; }

    protected ActionChoice(UnitInstanceId unitId)
    {
        UnitId = unitId;
    }
}