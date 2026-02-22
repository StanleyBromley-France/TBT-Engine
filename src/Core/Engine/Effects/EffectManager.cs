namespace Core.Engine.Effects;

using Core.Domain.Effects.Components.Instances;
using Core.Domain.Effects.Components.Instances.ReadOnly;
using Core.Domain.Effects.Components.Templates;
using Core.Domain.Effects.Instances.Execution;
using Core.Domain.Effects.Instances.ReadOnly;
using Core.Domain.Types;
using Core.Engine.Effects.Components.Calculators;
using Core.Engine.Effects.Factories;
using Core.Engine.Mutation;
using Core.Engine.Mutation.Mutators;
using Core.Game;
using System.ComponentModel;

internal sealed class EffectManager
{
    private readonly EffectInstanceFactory _effectFactory;
    private readonly DerivedStatsCalculator _derivedStats;
    private readonly IDamageCalculator _damageCalculator;
    private readonly IHealCalculator _healCalculator;

    public EffectManager(EffectInstanceFactory effectFactory, DerivedStatsCalculator derivedStats, IDamageCalculator damageCalculator, IHealCalculator healCalculator)
    {
        _derivedStats = derivedStats ?? throw new ArgumentNullException(nameof(derivedStats));
        _effectFactory = effectFactory ?? throw new ArgumentNullException(nameof(effectFactory));
        _healCalculator = healCalculator ?? throw new ArgumentNullException(nameof(healCalculator));
        _damageCalculator = damageCalculator ?? throw new ArgumentNullException(nameof(damageCalculator));
    }

    public void ApplyOrStackEffect(GameMutationContext context, IReadOnlyGameState state, EffectApplicationRequest request)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (request is null) throw new ArgumentNullException(nameof(request));

        // records affected units for later updating of derived stats
        var affected = new HashSet<UnitInstanceId>();

        foreach (var targetId in request.TargetUnitIds)
        {
            affected.Add(targetId);

            var existing = TryFindExistingInstanceOnUnit(
                state,
                request.TemplateId,
                request.SourceUnitId,
                targetId);

            if (existing is not null)
            {
                context.Effects.IncreaseStacks(targetId, existing.Id);
                context.Effects.ResetTicksToMax(targetId, existing.Id);

                ResolveHpDeltaComponents(context, state, existing, targetId);
            }
            else
            {
                var instance = _effectFactory.Create(context, request.TemplateId, request.SourceUnitId, targetId);

                ResolveHpDeltaComponents(context, state, instance, targetId);

                ((IEffectInstanceExecution)instance).OnApply(context);
            }
        }

        RecomputeAndWriteDerivedStats(context.Units, state, affected);
    }

    public void TickAll(
        GameMutationContext context,
        IReadOnlyGameState state)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (state is null) throw new ArgumentNullException(nameof(state));

        // records affected units for later updating of derived stats
        var affected = new HashSet<UnitInstanceId>();

        foreach (var targetId in state.ActiveEffects.Keys.ToList())
        {
            // adds target id right away
            // if target id is here, it will have at least one effect instance attached to it
            affected.Add(targetId);

            // decrement remaining ticks
            // removing of effects is handled by this method
            context.Effects.TickAllForUnit(targetId);
        }

        if (affected.Count > 0)
            RecomputeAndWriteDerivedStats(context.Units, state, affected);
    }

    // Internal 

    private static IReadOnlyEffectInstance? TryFindExistingInstanceOnUnit(
        IReadOnlyGameState state,
        EffectTemplateId templateId,
        UnitInstanceId sourceUnitId,
        UnitInstanceId targetUnitId)
    {
        if (!state.ActiveEffects.TryGetValue(targetUnitId, out var effectsById))
            return null;

        foreach (var effect in effectsById.Values)
        {
            if (effect.Template.Id != templateId) continue;
            if (effect.SourceUnitId != sourceUnitId) continue;

            // match found
            return effect;
        }

        return null;
    }

    private void RecomputeAndWriteDerivedStats(
        UnitsMutator unitMutator,
        IReadOnlyGameState state,
        HashSet<UnitInstanceId> unitIds)
    {
        foreach (var unitId in unitIds)
        {
            var computed = _derivedStats.Compute(state, unitId);
            unitMutator.SetDerivedStats(unitId, computed);
        }
    }

    private void ResolveHpDeltaComponents(
    GameMutationContext context,
    IReadOnlyGameState state,
    IReadOnlyEffectInstance instance,
    UnitInstanceId target)
    {
        foreach (var component in instance.Components)
        {
            if (component is not IReadOnlyResolvableHpDeltaComponent resolvableComponent)
                continue;

            int resolveValue = resolvableComponent.HpType switch
            {
                HpType.Heal when component.Template is IHealComponent healT =>
                        _healCalculator.Compute(context, state, instance, healT),

                HpType.Damage when component.Template is IDamageComponent dmgT =>
                    _damageCalculator.Compute(context, state, instance, dmgT),

                _ => throw new InvalidOperationException(
                    $"Template {component.Template.GetType().Name} doesn't match HpType {resolvableComponent.HpType}.")
            };

            context.Effects.UpdateHpDelta(target, instance.Id, component.Id, resolveValue);
        }
    }
}
