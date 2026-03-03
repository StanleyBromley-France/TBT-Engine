namespace Core.Engine.Undo.Steps.Rng;

using Core.Engine.Undo;
using Core.Game;

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