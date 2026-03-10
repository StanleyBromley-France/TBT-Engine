namespace Core.Undo.Steps;

using Core.Game.State;

public interface IUndoStep
{
    void Undo(GameState state);
}