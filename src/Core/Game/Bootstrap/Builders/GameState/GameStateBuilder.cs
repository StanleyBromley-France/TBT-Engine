namespace Core.Game.Bootstrap.Builders.Gamestate;

using Core.Domain.Effects.Instances.Mutable;
using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Domain.Units.Instances.Mutable;
using Core.Game.Bootstrap.Contracts;
using Core.Game.State;
using Core.Map.Grid;
using Core.Game.Session;
using Core.Game.Requests;

public sealed class GameStateBuilder : IGameStateBuilder
{
    private readonly GameSessionServices _sessionServices;

    internal GameStateBuilder(GameSessionServices sessionServices)
    {
        _sessionServices = sessionServices;
    }

    public GameState Build(
        IGameStateSpec spec,
        Map map,
        InstanceAllocationState instanceAllocationState,
        int simulationSeed)
    {
        var unitInstances = BuildUnitInstances(spec, instanceAllocationState);
        var activeEffects = BuildActiveEffects(unitInstances.Keys);

        return new GameState(
            map: map,
            unitInstances: unitInstances,
            activeEffects: activeEffects,
            turn: spec.InitialTurn,
            phase: new ActivationPhase(),
            rng: new RngState(seed: simulationSeed, position: 0));
    }

    private Dictionary<UnitInstanceId, UnitInstance> BuildUnitInstances(
        IGameStateSpec spec,
        InstanceAllocationState instanceAllocationState)
    {
        var instances = new Dictionary<UnitInstanceId, UnitInstance>();

        foreach (var spawn in spec.UnitSpawns)
        {

            var unitRequest = new SpawnUnitRequest(spawn.UnitTemplateId, spawn.TeamId, spawn.Position);

            var unit = _sessionServices.CreateUnit(unitRequest, instanceAllocationState);

            if (!instances.TryAdd(unit.Id, unit))
            {
                throw new InvalidOperationException($"Duplicate unit instance id '{unit.Id}'.");
            }
        }

        return instances;
    }

    private static Dictionary<UnitInstanceId, Dictionary<EffectInstanceId, EffectInstance>> BuildActiveEffects(
        IEnumerable<UnitInstanceId> unitIds)
    {
        var effects = new Dictionary<UnitInstanceId, Dictionary<EffectInstanceId, EffectInstance>>();
        foreach (var unitId in unitIds)
        {
            effects[unitId] = new Dictionary<EffectInstanceId, EffectInstance>();
        }

        return effects;
    }
}
