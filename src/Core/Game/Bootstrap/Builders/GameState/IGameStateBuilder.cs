namespace Core.Game.Bootstrap.Builders.Gamestate;

using Core.Domain.Repositories;
using Core.Game.Bootstrap.Contracts;
using Core.Game.Session;
using Core.Game.State;
using Core.Map.Grid;

public interface IGameStateBuilder
{
    GameState Build(IGameStateSpec spec, Map map);
}
