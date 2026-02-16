namespace Core.Game;

using Core.Domain.Effects.Instances.Mutable;
using Core.Domain.Units.Instances.ReadOnly;
using Domain.Effects.Instances;
using Domain.Types;
using Map.Grid;

public interface IReadOnlyGameState
{
    Map Map { get; }
    IReadOnlyDictionary<UnitInstanceId, IReadOnlyUnitInstance> UnitInstances { get; }
    IReadOnlyDictionary<UnitInstanceId, IReadOnlyDictionary<EffectInstanceId, EffectInstance>> ActiveEffects { get; }
    Turn Turn { get; }
    UnitInstanceId ActiveUnitId { get; }
    RngState Rng { get; }
}