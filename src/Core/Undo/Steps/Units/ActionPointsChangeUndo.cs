namespace Core.Undo.Steps.Units;

using Core.Domain.Types;
using Core.Game.State;
using Core.Undo.Steps;

public sealed class ActionPointsChangeUndo : IUndoStep
{
    public UnitInstanceId UnitId { get; }
    public int OldActionPoints { get; }

    public ActionPointsChangeUndo(UnitInstanceId unitId, int oldActionPoints)
    {
        UnitId = unitId;
        OldActionPoints = oldActionPoints;
    }

    public void Undo(GameState state)
    {
        state.UnitInstances[UnitId].Resources.ActionPoints = OldActionPoints;
    }
}