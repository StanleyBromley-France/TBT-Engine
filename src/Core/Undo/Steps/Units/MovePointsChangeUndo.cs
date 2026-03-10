namespace Core.Undo.Steps.Units;

using Core.Domain.Types;
using Core.Game;
using Core.Undo.Steps;

public sealed class MovePointsChangeUndo : IUndoStep
{
    public UnitInstanceId UnitId { get; }
    public int OldMovePoints { get; }

    public MovePointsChangeUndo(UnitInstanceId unitId, int oldMovePoints)
    {
        UnitId = unitId;
        OldMovePoints = oldMovePoints;
    }

    public void Undo(GameState state)
    {
        state.UnitInstances[UnitId].Resources.MovePoints = OldMovePoints;
    }
}