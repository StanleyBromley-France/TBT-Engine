namespace Agents.Mcts.Evaluation;

using Core.Game.State.ReadOnly;

public interface IStateEvaluator
{
    double Evaluate(IReadOnlyGameState state, StateEvaluationContext context);
}
