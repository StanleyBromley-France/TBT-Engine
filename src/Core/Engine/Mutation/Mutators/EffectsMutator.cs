namespace Core.Engine.Mutation.Mutators;

using Core.Domain.Effects.Instances.Mutable;
using Core.Domain.Types;
using Core.Engine.Mutation;


/// <summary>
/// Mutation-layer API for managing active effect instances on units.
/// </summary>
/// <remarks>
/// Reads and mutates <see cref="Game.GameState.ActiveEffects"/>, storing effects per target unit
/// keyed by <see cref="EffectInstanceId"/>. Supports adding/removing effects, ticking all
/// active effects each turn, and updating effect runtime state such as stacks and remaining ticks.
/// </remarks>
public sealed class EffectsMutator
{
    private readonly IGameMutationAccess _ctx;

    public EffectsMutator(GameMutationContext ctx) => _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));

    public void AddEffect(UnitInstanceId target, EffectInstance effect)
    {
        var state = _ctx.GetState();

        if (!state.ActiveEffects.TryGetValue(target, out var effectsById))
        {
            effectsById = new Dictionary<EffectInstanceId, EffectInstance>();
            state.ActiveEffects[target] = effectsById;
        }

        effectsById.Add(effect.Id, effect);

        // TODO: record undo (remove this effect)
    }


    public void RemoveEffect(UnitInstanceId target, EffectInstanceId effectId)
    {
        var state = _ctx.GetState();

        var targetUnitEffects = state.ActiveEffects[target];
        var removedEffect = targetUnitEffects[effectId];

        targetUnitEffects.Remove(effectId);

        if (targetUnitEffects.Count == 0)
            state.ActiveEffects.Remove(target);

        // TODO: Record undo step in UndoRecord
    }

    public void TickAllForUnit(UnitInstanceId target)
    {
        var state = _ctx.GetState();

        if (!state.ActiveEffects.TryGetValue(target, out var effectsById) || effectsById.Count == 0)
            return;

        foreach (var (effectId, effect) in effectsById.ToList())
        {
            var expired = UpdateTickState(target, effectId);
            if (expired)
                RemoveEffect(target, effectId);
        }
    }


    public void IncreaseStacks(UnitInstanceId target, EffectInstanceId effectId)
    {
        var state = _ctx.GetState();
        var effect = state.ActiveEffects[target][effectId];

        var beforeStacks = effect.CurrentStacks;
        effect.CurrentStacks++;

        if (effect.Template.MaxStacks > 0 && effect.CurrentStacks > effect.Template.MaxStacks)
            effect.CurrentStacks = effect.Template.MaxStacks;

        // TODO: Record undo step in UndoRecord
    }

    public void ResetTicksToMax(UnitInstanceId target, EffectInstanceId effectId)
    {
        var state = _ctx.GetState();
        var effect = state.ActiveEffects[target][effectId];

        var beforeTicks = effect.RemainingTicks;
        effect.RemainingTicks = effect.Template.TotalTicks;

        // TODO: Record undo step in UndoRecord
    }

    private bool UpdateTickState(UnitInstanceId target, EffectInstanceId effectId)
    {
        var state = _ctx.GetState();
        var effect = state.ActiveEffects[target][effectId];

        var beforeTicks = effect.RemainingTicks;
        effect.RemainingTicks--;

        // TODO: Record undo step in UndoRecord

        return effect.IsExpired();
    }

}
