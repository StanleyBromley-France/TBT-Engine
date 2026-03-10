namespace Core.Engine.Effects;

using Core.Domain.Types;
using Core.Domain.Units.Instances.Mutable;
using Core.Game.State.ReadOnly;

/// <summary>
/// Computes derived stats for a unit based on base stats and active effects.
/// </summary>
public interface IDerivedStatsCalculator
{
    UnitDerivedStats Compute(IReadOnlyGameState state, UnitInstanceId unitId);
}