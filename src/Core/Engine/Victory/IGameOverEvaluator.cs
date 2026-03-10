namespace Core.Engine.Victory;

using Core.Game;
using Core.Game.Match;

public interface IGameOverEvaluator
{
    GameOutcome Evaluate(GameSession session);
}