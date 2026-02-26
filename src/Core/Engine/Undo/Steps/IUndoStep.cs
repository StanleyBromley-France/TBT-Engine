namespace Core.Engine.Undo;

using Core.Game;

public interface IUndoStep
{
    void Undo(GameState state);
}