namespace Core.Engine.Undo.Steps.Turn;

using Core.Domain.Types;
using Core.Engine.Undo;
using Core.Game;

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