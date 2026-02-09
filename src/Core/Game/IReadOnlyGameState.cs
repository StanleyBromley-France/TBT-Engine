namespace Core.Game;

using Core.Domain.Units.Instances.Mutable;
using Domain.Effects.Instances;
using Domain.Types;
using Domain.Units.Instances;
using Map.Grid;

public interface IReadOnlyGameState
{
    Map Map { get; }
    IReadOnlyList<UnitInstance> UnitInstances { get; }
    IReadOnlyDictionary<UnitInstanceId, IReadOnlyList<EffectInstance>> ActiveEffects { get; }
    Turn Turn { get; }
    UnitInstanceId ActiveUnitId { get; }
    RngState Rng { get; }
}