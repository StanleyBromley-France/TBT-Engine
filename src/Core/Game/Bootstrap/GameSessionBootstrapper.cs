namespace Core.Game.Bootstrap;

using Core.Domain.Repositories;
using Core.Game.Bootstrap.Builders.GameSession;
using Core.Game.Bootstrap.Builders.GameSessionServices;
using Core.Game.Bootstrap.Builders.Gamestate;
using Core.Game.Bootstrap.Builders.Map;
using Core.Game.Bootstrap.Contracts;
using Core.Game.Bootstrap.Builders.Map.Rng;
using Core.Game.Session;

public static class GameSessionBootstrapper
{
    public static GameSession Create(
        TemplateRegistry templateRegistry,
        IGameStateSpec gameStateSpec,
        int seed)
    {
        var sessionServices = new GameSessionServicesBuilder().Build(templateRegistry);
        var instanceAllocationState = new InstanceAllocationState();

        var mapBuilder = new MapBuilder(new DeterministicMapGenerationRng());
        var requiredCoords = gameStateSpec.UnitSpawns.Select(unit => unit.Position).ToArray();
        var map = mapBuilder.Build(
            gameStateSpec.MapSpec,
            new MapBuildOptions
            {
                Seed = seed,
                RequiredWalkableCoords = requiredCoords,
                RequireAllRequiredCoordsConnected = requiredCoords.Length > 1,
            }).Map;

        var state = new GameStateBuilder(sessionServices).Build(gameStateSpec, map, instanceAllocationState);

        return new GameSessionBuilder().Build(
            state,
            templateRegistry,
            sessionServices,
            instanceAllocationState,
            gameStateSpec.AttackerTeamId,
            gameStateSpec.DefenderTeamId);
    }
}
