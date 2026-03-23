namespace GameRunner.Controllers;

using Core.Domain.Types;
using Core.Engine;
using Core.Engine.Actions.Choice;

public interface IPlayerTurnContext
{
    TeamId TeamToAct { get; }

    IReadOnlyList<ActionChoice> LegalActions { get; }

    EngineFacade CreateSandboxEngine();
}
