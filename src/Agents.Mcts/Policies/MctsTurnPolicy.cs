namespace Agents.Mcts.Policies;

using Agents.Mcts.Config;
using Core.Engine.Actions.Choice;
using Core.Engine.Turn;
using Core.Game.State.ReadOnly;
using Agents.Mcts.Search;
using Agents.Mcts.Simulation;

public sealed class MctsTurnPolicy : ITurnPolicy
{
    private readonly IMctsSearch _search;
    private readonly ISandboxFactory _sandboxFactory;
    private readonly MctsSearchConfig _config;

    public MctsTurnPolicy(
        IMctsSearch search,
        ISandboxFactory sandboxFactory,
        MctsSearchConfig config)
    {
        _search = search ?? throw new ArgumentNullException(nameof(search));
        _sandboxFactory = sandboxFactory ?? throw new ArgumentNullException(nameof(sandboxFactory));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public ActionChoice ChooseAction(TurnPolicyContext context, IReadOnlyGameState state, IReadOnlyList<ActionChoice> legalActions)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        if (state is null)
            throw new ArgumentNullException(nameof(state));

        if (legalActions is null)
            throw new ArgumentNullException(nameof(legalActions));

        if (legalActions.Count == 0)
            throw new InvalidOperationException("Cannot choose an action when no legal actions are available.");

        var simulation = _sandboxFactory.CreateFrom(context.Engine);
        return _search.FindBestAction(simulation, _config);
    }
}
