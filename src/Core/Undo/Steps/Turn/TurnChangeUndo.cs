namespace Core.Undo.Steps.Turn;

using Core.Domain.Types;
using Core.Game.State;
using Core.Undo.Steps;

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