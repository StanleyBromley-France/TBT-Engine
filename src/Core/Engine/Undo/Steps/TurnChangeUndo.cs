namespace Core.Engine.Undo.Steps;

using Core.Domain.Types;
using Core.Engine.Undo;
using Core.Game;

public sealed class TurnChangeUndo : IUndoStep
{
    public Turn OldTurn { get; }

    public TurnChangeUndo(Turn oldTurn)
    {
        OldTurn = oldTurn;
    }

    public void Undo(GameState state)
    {
        state.Turn = OldTurn;
    }
}