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

        ValidateConfig(config);

        if (legalActions.Count == 1)
            return legalActions[0];

        TeamId perspective = simulation.GetState().Turn.TeamToAct;
        var root = new MctsNode(perspective, legalActions);
        var random = new Random(config.RandomSeed);

        for (var iteration = 0; iteration < config.IterationBudget; iteration++)
            RunIteration(simulation, root, perspective, config, random);

        return SelectBestRootAction(root);
    }

    private void RunIteration(
        ISimulationFacade simulation,
        MctsNode root,
        TeamId rootPerspective,
        MctsSearchConfig config,
        Random random)
    {
        var marker = simulation.MarkUndo();
        var path = new List<MctsNode> { root };
        var node = root;
        var depth = 0;

        try
        {
            // Selection: follow fully expanded children until it reachs a leaf,
            // depth limit, or terminal state.
            while (depth < config.MaxDepth &&
                   !simulation.IsTerminal() &&
                   node.CanSelectChild)
            {
                node = SelectChild(node, rootPerspective, config.ExplorationConstant);
                simulation.ApplyAction(node.ActionFromParent!);
                path.Add(node);
                depth++;
            }

            // Expansion: grow the tree by one previously unseen action.
            if (depth < config.MaxDepth &&
                !simulation.IsTerminal() &&
                node.CanExpand)
            {
                var action = node.RemoveUnexpandedActionAt(random.Next(node.UnexpandedActionCount));
                simulation.ApplyAction(action);
                depth++;

                var child = node.AddChild(
                    action,
                    simulation.GetState().Turn.TeamToAct,
                    simulation.IsTerminal() || depth >= config.MaxDepth
                        ? Array.Empty<ActionChoice>()
                        : simulation.GetLegalActions());

                node = child;
                path.Add(node);
            }

            var reward = Rollout(simulation, rootPerspective, depth, config, random);

            // Backpropagation: the rollout result is always stored from the
            // root player's perspective, and selection flips sign as turns alternate.
            foreach (var visitedNode in path)
                visitedNode.RecordSimulation(reward);
        }
        finally
        {
            simulation.UndoTo(marker);
        }
    }

    private double Rollout(
        ISimulationFacade simulation,
        TeamId rootPerspective,
        int depth,
        MctsSearchConfig config,
        Random random)
    {
        // Rollout continues from the expanded node until depth cutoff or terminal state.
        while (depth < config.MaxDepth && !simulation.IsTerminal())
        {
            var legalActions = simulation.GetLegalActions();
            if (legalActions.Count == 0)
                break;

            var action = config.RolloutPolicy == MctsRolloutPolicy.Heuristic
                ? SelectHeuristicRolloutAction(simulation, legalActions, rootPerspective, config)
                : legalActions[random.Next(legalActions.Count)];

            simulation.ApplyAction(action);
            depth++;
        }

        return EvaluateState(simulation, rootPerspective, config);
    }

    private ActionChoice SelectHeuristicRolloutAction(
        ISimulationFacade simulation,
        IReadOnlyList<ActionChoice> legalActions,
        TeamId rootPerspective,
        MctsSearchConfig config)
    {
        var actor = simulation.GetState().Turn.TeamToAct;
        var profile = actor == rootPerspective
            ? config.Profile
            : config.OpponentProfile;
        var context = StateEvaluationContext.Create(simulation, config, actor, profile);

        ActionChoice? bestAction = null;
        var bestScore = double.NegativeInfinity;

        foreach (var action in legalActions)
        {
            var marker = simulation.MarkUndo();

            try
            {
                simulation.ApplyAction(action);
                var score = _stateEvaluator.Evaluate(
                    simulation.GetState(),
                    context.WithOutcome(simulation.GetOutcome()));

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

    private double EvaluateState(
        ISimulationFacade simulation,
        TeamId perspective,
        MctsSearchConfig config)
    {
        var context = StateEvaluationContext.Create(simulation, config, perspective, config.Profile);
        return _stateEvaluator.Evaluate(simulation.GetState(), context);
    }

    private static MctsNode SelectChild(MctsNode node, TeamId rootPerspective, double explorationConstant)
    {
        // When it is the root players turn root value is maximized; when it is
        // the opponent's turn it is minimized by negating exploitation.
        var maximizing = node.TeamToAct == rootPerspective;

        MctsNode? bestChild = null;

        var bestScore = double.NegativeInfinity;

        foreach (var child in node.Children)
        {
            // Standard UCT exploration bonus. Unvisited children are forced first.
            var explorationBonus = child.Visits == 0
                ? double.PositiveInfinity
                : explorationConstant * Math.Sqrt(Math.Log(Math.Max(1, node.Visits)) / child.Visits);

            var estimatedValue = child.AverageValue;
            var score = (maximizing ? estimatedValue : -estimatedValue) + explorationBonus;

            if (score > bestScore || (score == bestScore && bestChild is not null && CompareActionPreference(child.ActionFromParent!, bestChild.ActionFromParent!) < 0))
            {
                bestScore = score;
                bestChild = child;
            }
        }

        return bestChild ?? throw new InvalidOperationException("No child node available for selection.");
    }

    private static ActionChoice SelectBestRootAction(MctsNode root)
    {
        // Final move choice prefers the action the search trusted most often,
        // then uses value and stable tie-breaking for deterministic output.
        MctsNode? bestChild = null;

        foreach (var child in root.Children)
        {
            if (bestChild is null)
            {
                bestChild = child;
                continue;
            }

            if (child.Visits > bestChild.Visits)
            {
                bestChild = child;
                continue;
            }

            if (child.Visits == bestChild.Visits && child.AverageValue > bestChild.AverageValue)
            {
                bestChild = child;
                continue;
            }

            if (child.Visits == bestChild.Visits &&
                child.AverageValue == bestChild.AverageValue &&
                GetActionPreference(child.ActionFromParent!) < GetActionPreference(bestChild.ActionFromParent!))
            {
                bestChild = child;
            }
        }

        return bestChild?.ActionFromParent
            ?? throw new InvalidOperationException("Search completed without exploring any root action.");
    }

    private static void ValidateConfig(MctsSearchConfig config)
    {
        if (config.IterationBudget <= 0)
            throw new ArgumentOutOfRangeException(nameof(config.IterationBudget), "Iteration budget must be positive.");

        if (config.MaxDepth <= 0)
            throw new ArgumentOutOfRangeException(nameof(config.MaxDepth), "Max depth must be positive.");

        if (config.MaxAttackerTurns <= 0)
            throw new ArgumentOutOfRangeException(nameof(config.MaxAttackerTurns), "Max attacker turns must be positive.");

        if (config.Profile is null)
            throw new ArgumentNullException(nameof(config.Profile));

        if (config.OpponentProfile is null)
            throw new ArgumentNullException(nameof(config.OpponentProfile));
    }

    private static int CompareActionPreference(ActionChoice left, ActionChoice right)
    {
        var leftPreference = GetActionPreference(left);
        var rightPreference = GetActionPreference(right);

        if (leftPreference < rightPreference) return -1;
        if (leftPreference > rightPreference) return 1;
        return 0;
    }

    private static int GetActionPreference(ActionChoice action)
    {
        // Tie-break equal-valued actions toward game-progressing choices.
        // Ability use is preferred first because it is usually the highest-impact
        // committing action. Movement comes next because it at least improves
        // board position. Skip is preferred over ChangeActive because it commits
        // phase progress, while ChangeActive is the least committal option and
        // is most likely to cause unproductive oscillation when scores are equal.
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
