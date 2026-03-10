namespace Core.Engine.Victory;

using Core.Game.Match;
using Core.Game.Session;

public interface IGameOverEvaluator
{
    GameOutcome Evaluate(GameSession session);
}