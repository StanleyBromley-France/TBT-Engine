namespace Agents.Mcts.Search;

using Agents.Mcts.Config;
using Agents.Mcts.Evaluation;
using Agents.Mcts.Hashing;
using Agents.Mcts.Simulation;
using Core.Domain.Types;
using Core.Engine.Actions.Choice;

public sealed class MctsSearch : IMctsSearch
{
    private readonly IStateEvaluator _stateEvaluator;
    private readonly IGameStateHasher _stateHasher;

    public MctsSearch(IStateEvaluator stateEvaluator, IGameStateHasher stateHasher)
    {
        _stateEvaluator = stateEvaluator ?? throw new ArgumentNullException(nameof(stateEvaluator));
        _stateHasher = stateHasher ?? throw new ArgumentNullException(nameof(stateHasher));
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

        var rootState = simulation.GetState();
        TeamId perspective = rootState.Turn.TeamToAct;
        var root = new MctsNode(_stateHasher.Compute(rootState), perspective, legalActions);
        var stateNodesByKey = new Dictionary<GameStateKey, MctsNode>
        {
            [root.StateKey] = root
        };

        var random = new Random(config.RandomSeed);

        for (var iteration = 0; iteration < config.IterationBudget; iteration++)
            RunIteration(simulation, root, stateNodesByKey, perspective, config, random);

        return SelectBestRootAction(root);
    }

    private void RunIteration(
        ISimulationFacade simulation,
        MctsNode root,
        IDictionary<GameStateKey, MctsNode> stateNodesByKey,
        TeamId rootPerspective,
        MctsSearchConfig config,
        Random random)
    {
        var marker = simulation.MarkUndo();
        var nodePath = new List<MctsNode> { root };
        var edgePath = new List<MctsEdge>();
        var node = root;
        var depth = 0;

        try
        {
            // Selection: follow fully expanded children until it reachs a leaf,
            // depth limit, or terminal state.
            while (depth < config.MaxDepth &&
                   !simulation.IsTerminal() &&
                   node.CanSelectOutgoingEdge)
            {
                var edge = SelectEdge(node, rootPerspective, config.ExplorationConstant);
                simulation.ApplyAction(edge.Action);
                node = edge.NextStateNode;
                edgePath.Add(edge);
                nodePath.Add(node);
                depth++;
            }

            // Expansion: grow the tree by one previously unseen action.
            if (depth < config.MaxDepth &&
                !simulation.IsTerminal() &&
                node.CanExpand)
            {
                var action = node.RemoveUnexpandedActionAt(random.Next(node.UnexpandedActionCount));

                // Advance simulation
                simulation.ApplyAction(action);
                depth++;

                var advancedState = simulation.GetState();
                var advancedStateKey = _stateHasher.Compute(advancedState);

                if (!stateNodesByKey.TryGetValue(advancedStateKey, out var advancedNode))
                {
                    advancedNode = new MctsNode(
                        advancedStateKey,
                        advancedState.Turn.TeamToAct,
                        simulation.IsTerminal()
                            ? Array.Empty<ActionChoice>()
                            : simulation.GetLegalActions());

                    stateNodesByKey.Add(advancedStateKey, advancedNode);
                }

                var edge = node.AddOutgoingEdge(action, advancedNode);

                edgePath.Add(edge);
                nodePath.Add(advancedNode);
            }

            var reward = Rollout(simulation, rootPerspective, depth, config, random);

            // Backpropagation: the rollout result is always stored from the
            // root player's perspective, and selection flips sign as turns alternate.
            foreach (var visitedNode in nodePath)
                visitedNode.RecordSimulation(reward);

            foreach (var visitedEdge in edgePath)
                visitedEdge.RecordSimulation(reward);
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

    private static MctsEdge SelectEdge(MctsNode node, TeamId rootPerspective, double explorationConstant)
    {
        // When it is the root players turn root value is maximized; when it is
        // the opponent's turn it is minimized by negating exploitation.
        var maximizing = node.TeamToAct == rootPerspective;

        MctsEdge? bestEdge = null;
        var bestScore = double.NegativeInfinity;

        foreach (var edge in node.OutgoingEdges)
        {
            // Standard UCT exploration bonus. Unvisited OutgoingEdges are forced first.
            var explorationBonus = edge.Visits == 0
                ? double.PositiveInfinity
                : explorationConstant * Math.Sqrt(Math.Log(Math.Max(1, node.Visits)) / edge.Visits);

            var exploitation = edge.AverageValue;
            var score = (maximizing ? exploitation : -exploitation) + explorationBonus;

            if (score > bestScore ||
                (score == bestScore &&
                 bestEdge is not null &&
                 CompareActionPreference(edge.Action, bestEdge.Action) < 0))
            {
                bestScore = score;
                bestEdge = edge;
            }
        }

        return bestEdge ?? throw new InvalidOperationException("No child node available for selection.");
    }

    private static ActionChoice SelectBestRootAction(MctsNode root)
    {
        // Final move choice prefers the action the search trusted most often,
        // then uses value and stable tie-breaking for deterministic output.
        MctsEdge? bestEdge = null;

        foreach (var edge in root.OutgoingEdges)
        {
            if (bestEdge is null)
            {
                bestEdge = edge;
                continue;
            }

            if (edge.Visits > bestEdge.Visits)
            {
                bestEdge = edge;
                continue;
            }

            if (edge.Visits == bestEdge.Visits && edge.AverageValue > bestEdge.AverageValue)
            {
                bestEdge = edge;
                continue;
            }

            if (edge.Visits == bestEdge.Visits &&
                edge.AverageValue == bestEdge.AverageValue &&
                GetActionPreference(edge.Action) < GetActionPreference(bestEdge.Action))
            {
                bestEdge = edge;
            }
        }

        return bestEdge?.Action
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
        // committing action.
        return action switch
        {
            UseAbilityAction => 0,
            MoveAction => 1,
            SkipActiveUnitAction => 2,
            _ => 3
        };
    }
}
