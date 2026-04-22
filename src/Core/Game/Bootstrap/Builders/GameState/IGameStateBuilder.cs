namespace Core.Game.Bootstrap.Builders.Gamestate;

using Core.Game.Bootstrap.Contracts;
using Core.Game.Session;
using Core.Game.State;
using Core.Map.Grid;

public interface IGameStateBuilder
{
    GameState Build(
        IGameStateSpec spec,
        Map map,
        InstanceAllocationState instanceAllocationState,
        int simulationSeed);
}
