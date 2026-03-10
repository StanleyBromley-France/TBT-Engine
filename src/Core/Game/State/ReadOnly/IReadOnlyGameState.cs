namespace Core.Game.State.ReadOnly;

using Core.Domain.Effects.Instances.ReadOnly;
using Core.Domain.Units.Instances.ReadOnly;
using Core.Game.State;
using Domain.Types;
using Map.Grid;

public interface IReadOnlyGameState
{
    Map Map { get; }
    IReadOnlyDictionary<UnitInstanceId, IReadOnlyUnitInstance> UnitInstances { get; }
    IReadOnlyCollection<HexCoord> OccupiedHexes { get; }
    IReadOnlyDictionary<UnitInstanceId, IReadOnlyDictionary<EffectInstanceId, IReadOnlyEffectInstance>> ActiveEffects { get; }
    Turn Turn { get; }
    RngState Rng { get; }
    ActivationPhase Phase { get; }
}