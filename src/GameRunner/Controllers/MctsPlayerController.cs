namespace GameRunner.Controllers;

using Agents.Mcts.Config;
using Agents.Mcts.Search;
using Agents.Mcts.Simulation;
using Core.Engine.Actions.Choice;

public sealed class MctsPlayerController : IPlayerController
{
    private readonly IMctsSearch _search;
    private readonly MctsSearchConfig _config;

    public MctsPlayerController(IMctsSearch search, MctsSearchConfig config)
    {
        _search = search;
        _config = config;
    }

    public ValueTask<ActionChoice> ChooseActionAsync(
        IPlayerTurnContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sandboxEngine = context.CreateSandboxEngine();
        var simulation = new SimulationFacade(sandboxEngine);
        var action = _search.FindBestAction(simulation, _config);
        return ValueTask.FromResult(action);
    }
}
