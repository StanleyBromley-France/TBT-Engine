namespace Agents.Mcts.Evaluation;

using Core.Domain.Types;
using Core.Game.State.ReadOnly;

public interface IStateEvaluator
{
    double Evaluate(IReadOnlyGameState state, TeamId perspective);
}
