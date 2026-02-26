namespace Core.Engine.Undo.Steps;

using Core.Domain.Types;
using Core.Engine.Undo;
using Core.Game;

public sealed class ActionPointsUndo : IUndoStep
{
    public UnitInstanceId UnitId { get; }
    public int OldActionPoints { get; }

    public ActionPointsUndo(UnitInstanceId unitId, int oldActionPoints)
    {
        UnitId = unitId;
        OldActionPoints = oldActionPoints;
    }

    public void Undo(GameState state)
    {
        state.UnitInstances[UnitId].Resources.ActionPoints = OldActionPoints;
    }
}