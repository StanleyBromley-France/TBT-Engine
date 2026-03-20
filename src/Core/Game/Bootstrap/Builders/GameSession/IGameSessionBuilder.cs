namespace Core.Game.Bootstrap.Builders.GameSession;

using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Game.Session;
using Core.Game.State;

public interface IGameSessionBuilder
{
    GameSession Build(GameState state, TemplateRegistry templateRegistry, TeamId attackerTeamId, TeamId defenderTeamId);
}
