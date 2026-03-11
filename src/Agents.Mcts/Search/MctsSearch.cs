namespace Agents.Mcts.Search;

using Agents.Mcts.Config;
using Agents.Mcts.Evaluation;
using Agents.Mcts.Simulation;
using Core.Engine.Actions.Choice;
using Core.Domain.Types;

public sealed class MctsSearch : IMctsSearch
{
    private readonly IStateEvaluator _stateEvaluator;

    public MctsSearch(IStateEvaluator stateEvaluator)
    {
        _stateEvaluator = stateEvaluator ?? throw new ArgumentNullException(nameof(stateEvaluator));
    }

    public ActionChoice FindBestAction(ISimulationFacade simulation, MctsSearchConfig config)
    {
        if (simulation is null)
            throw new ArgumentNullException(nameof(simulation));

        if (config is null)
            throw new ArgumentNullException(nameof(config));

        var legalActions = simulation.GetLegalActions();
        if (legalActions.Count == 0)
            throw new InvalidOperationException("Cannot select an action when no legal actions are available.");

        TeamId perspective = simulation.GetState().Turn.TeamToAct;

        ActionChoice? bestAction = null;
        double bestScore = double.NegativeInfinity;

        foreach (var action in legalActions)
        {
            var marker = simulation.MarkUndo();

            try
            {
                simulation.ApplyAction(action);
                var score = _stateEvaluator.Evaluate(simulation.GetState(), perspective);

                if (score > bestScore ||
                    (score == bestScore && bestAction is not null && CompareActionPreference(action, bestAction) < 0))
                {
                    bestScore = score;
                    bestAction = action;
                }
            }
            finally
            {
                simulation.UndoTo(marker);
            }
        }

        return bestAction ?? legalActions[0];
    }

    private static int CompareActionPreference(ActionChoice left, ActionChoice right)
    {
        return GetActionPreference(left).CompareTo(GetActionPreference(right));
    }

    private static int GetActionPreference(ActionChoice action)
    {
        // Tie-breaker for equal evaluations so temporary one-ply search avoids
        // pathological "change-active" oscillation and produces meaningful play.
        return action switch
        {
            UseAbilityAction => 0,
            MoveAction => 1,
            SkipActiveUnitAction => 2,
            ChangeActiveUnitAction => 3,
            _ => 4
        };
    }
}
