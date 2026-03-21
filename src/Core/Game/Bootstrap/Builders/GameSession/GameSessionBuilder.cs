namespace Core.Game.Bootstrap.Builders.GameSession;

using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Game.Match;
using Core.Game.Session;
using Core.Game.State;
using Core.Undo;

internal sealed class GameSessionBuilder : IGameSessionBuilder
{
    public GameSession Build(
        GameState state,
        TemplateRegistry templateRegistry,
        GameSessionServices gameSessionServices,
        InstanceAllocationState instanceAllocationState,
        TeamId attackerTeamId,
        TeamId defenderTeamId)
    {
        var teams = new TeamPair(attackerTeamId, defenderTeamId);

        var context = new GameContext(
            content: templateRegistry,
            teams: teams,
            sessionServices: gameSessionServices);

        var runtime = new GameRuntime(
            state: state,
            undo: new UndoHistory(),
            outcome: GameOutcome.Ongoing(),
            instanceAllocation: instanceAllocationState);

        return new GameSession(context, runtime);
    }
}
