namespace GameRunner.Controllers;

using Core.Domain.Types;
using Core.Engine;
using Core.Engine.Actions.Choice;

public sealed class PlayerTurnContext : IPlayerTurnContext
{
    private readonly EngineFacade _engine;

    public TeamId TeamToAct { get; }

    public IReadOnlyList<ActionChoice> LegalActions { get; }

    public PlayerTurnContext(
        EngineFacade engine,
        TeamId teamToAct,
        IReadOnlyList<ActionChoice> legalActions)
    {
        _engine = engine;
        TeamToAct = teamToAct;
        LegalActions = legalActions;
    }

    public EngineFacade CreateSandboxEngine() => _engine.CreateSandbox();
}
