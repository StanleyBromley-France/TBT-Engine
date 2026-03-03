namespace Core.Engine.Victory;

using Core.Game;

public interface IGameOverEvaluator
{
    GameOutcome Evaluate(GameSession session);
}