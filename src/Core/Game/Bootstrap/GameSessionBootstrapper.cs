namespace Core.Game.Bootstrap;

using Core.Domain.Repositories;
using Core.Game.Bootstrap.Builders.GameSession;
using Core.Game.Bootstrap.Builders.GameSessionServices;
using Core.Game.Bootstrap.Builders.Gamestate;
using Core.Game.Bootstrap.Builders.Map;
using Core.Game.Bootstrap.Contracts;
using Core.Game.Bootstrap.Builders.Map.Rng;
using Core.Random;
using Core.Game.Session;

public static class GameSessionBootstrapper
{
    private const int MapSeedSalt = 1;
    private const int SimulationSeedSalt = 2;

    public static GameSession Create(
        TemplateRegistry templateRegistry,
        IGameStateSpec gameStateSpec,
        int seed)
    {
        var sessionServices = new GameSessionServicesBuilder().Build(templateRegistry);
        var instanceAllocationState = new InstanceAllocationState();

        var mapSeed = SeedDeriver.Derive(seed, MapSeedSalt);
        var simulationSeed = SeedDeriver.Derive(seed, SimulationSeedSalt);

        var mapBuilder = new MapBuilder(new DeterministicMapGenerationRng());
        var requiredCoords = gameStateSpec.UnitSpawns.Select(unit => unit.Position).ToArray();
        var map = mapBuilder.Build(
            gameStateSpec.MapSpec,
            new MapBuildOptions
            {
                Seed = mapSeed,
                RequiredWalkableCoords = requiredCoords,
                RequireAllRequiredCoordsConnected = requiredCoords.Length > 1,
            }).Map;

        var state = new GameStateBuilder(sessionServices).Build(gameStateSpec, map, instanceAllocationState, simulationSeed);

        return new GameSessionBuilder().Build(
            state,
            templateRegistry,
            sessionServices,
            instanceAllocationState,
            gameStateSpec.AttackerTeamId,
            gameStateSpec.DefenderTeamId);
    }
}
