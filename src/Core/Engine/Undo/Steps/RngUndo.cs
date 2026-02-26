namespace Core.Engine.Undo.Steps;

using Core.Engine.Undo;
using Core.Game;

public sealed class RngUndo : IUndoStep
{
    public RngState OldStateSnapshot { get; }

    public RngUndo(RngState oldStateSnapshot)
    {
        OldStateSnapshot = oldStateSnapshot;
    }

    public void Undo(GameState state)
    {
        state.Rng = OldStateSnapshot;
    }
}