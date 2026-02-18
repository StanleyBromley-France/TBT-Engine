namespace Core.Game;

using Core.Domain.Effects.Instances.ReadOnly;
using Core.Domain.Units.Instances.ReadOnly;
using Domain.Types;
using Map.Grid;

public interface IReadOnlyGameState
{
    Map Map { get; }
    IReadOnlyDictionary<UnitInstanceId, IReadOnlyUnitInstance> UnitInstances { get; }
    IReadOnlyDictionary<UnitInstanceId, IReadOnlyDictionary<EffectInstanceId, IReadOnlyEffectInstance>> ActiveEffects { get; }
    Turn Turn { get; }
    UnitInstanceId ActiveUnitId { get; }
    RngState Rng { get; }
}