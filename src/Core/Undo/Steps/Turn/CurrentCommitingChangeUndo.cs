namespace Core.Undo.Steps.Turn;

using Core.Domain.Types;
using Core.Game.State;
using Core.Undo.Steps;

public sealed class CurrentCommitingChangeUndo : IUndoStep
{
    public UnitInstanceId? OldCurrentlyCommiting { get; }

    public CurrentCommitingChangeUndo(UnitInstanceId? oldCurrentlyCommiting)
    {
        OldCurrentlyCommiting = oldCurrentlyCommiting;
    }

    public void Undo(GameState state)
    {
        state.Phase.CurrentlyCommiting = OldCurrentlyCommiting;
    }
}
