namespace Core.Undo.Steps;

using Core.Game;

public interface IUndoStep
{
    void Undo(GameState state);
}