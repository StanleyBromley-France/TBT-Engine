namespace Agents.Mcts.Evaluation;

using Agents.Mcts.Config;
using Agents.Mcts.Simulation;
using Core.Domain.Types;
using Core.Game.Match;

public sealed class StateEvaluationContext
{
    public StateEvaluationContext(
        TeamId perspective,
        int maxAttackerTurns,
        GameOutcome outcome,
        MctsAgentProfile profile)
    {
        Perspective = perspective;
        MaxAttackerTurns = maxAttackerTurns;
        Outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    public TeamId Perspective { get; }
    public int MaxAttackerTurns { get; }
    public GameOutcome Outcome { get; }
    public MctsAgentProfile Profile { get; }

    public StateEvaluationContext WithOutcome(GameOutcome outcome)
    {
        return new StateEvaluationContext(
            perspective: Perspective,
            maxAttackerTurns: MaxAttackerTurns,
            outcome: outcome,
            profile: Profile);
    }

    public static StateEvaluationContext Create(
        ISimulationFacade simulation,
        MctsSearchConfig config,
        TeamId perspective,
        MctsAgentProfile profile)
    {
        if (simulation is null)
            throw new ArgumentNullException(nameof(simulation));

        if (config is null)
            throw new ArgumentNullException(nameof(config));

        if (profile is null)
            throw new ArgumentNullException(nameof(profile));

        return new StateEvaluationContext(
            perspective: perspective,
            maxAttackerTurns: config.MaxAttackerTurns,
            outcome: simulation.GetOutcome(),
            profile: profile);
    }
}
