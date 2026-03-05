namespace Core.Engine.Mutation.Mutators;

using Core.Domain.Effects.Instances.Mutable;
using Core.Domain.Types;

/// <summary>
/// Mutation-layer API for managing active effect instances on units.
/// </summary>
/// <remarks>
/// Reads and mutates <see cref="Game.GameState.ActiveEffects"/>, storing effects per target unit
/// keyed by <see cref="EffectInstanceId"/>. Supports adding/removing effects, ticking all
/// active effects each turn, and updating effect runtime state such as stacks and remaining ticks.
/// </remarks>
public interface IEffectsMutator
{
    void AddEffect(UnitInstanceId target, EffectInstance effect);

    void RemoveEffect(UnitInstanceId target, EffectInstanceId effectId);

    void TickAllForUnit(UnitInstanceId target);

    void IncreaseStacks(UnitInstanceId target, EffectInstanceId effectId);

    void ResetTicksToMax(UnitInstanceId target, EffectInstanceId effectId);

    void UpdateHpDelta(
        UnitInstanceId target,
        EffectInstanceId effectId,
        EffectComponentInstanceId componentId,
        int resolved);
}