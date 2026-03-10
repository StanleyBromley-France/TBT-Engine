namespace Core.Undo.Steps.Rng;

using Core.Game;
using Core.Undo.Steps;

public sealed class RngStateChangeUndo : IUndoStep
{
    public RngState OldStateSnapshot { get; }

    public RngStateChangeUndo(RngState oldStateSnapshot)
    {
        OldStateSnapshot = oldStateSnapshot;
    }

    public void Undo(GameState state)
    {
        state.Rng = OldStateSnapshot;
    }
}