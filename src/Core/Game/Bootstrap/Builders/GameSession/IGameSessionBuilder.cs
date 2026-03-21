namespace Core.Game.Bootstrap.Builders.GameSession;

using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Game.Session;
using Core.Game.State;

internal interface IGameSessionBuilder
{
    GameSession Build(GameState state, TemplateRegistry templateRegistry, GameSessionServices gameSessionServices, TeamId attackerTeamId, TeamId defenderTeamId);
}
