namespace Core.Undo.Steps.Turn;

using Core.Domain.Types;
using Core.Game;
using Core.Undo.Steps;

public sealed class ActiveUnitChangeUndo : IUndoStep
{
    public UnitInstanceId OldActiveUnitId { get; }

    public ActiveUnitChangeUndo(UnitInstanceId oldActiveUnitId)
    {
        OldActiveUnitId = oldActiveUnitId;
    }

    public void Undo(GameState state)
    {
        state.Phase.ActiveUnitId = OldActiveUnitId;
    }
}