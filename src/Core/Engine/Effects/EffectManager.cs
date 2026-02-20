namespace Core.Engine.Effects;

using Core.Domain.Effects.Instances.ReadOnly;
using Core.Domain.Effects.Templates;
using Core.Domain.Types;
using Core.Engine.Mutation;
using Core.Engine.Mutation.Mutators;
using Core.Game;
public sealed class EffectManager
{
    private readonly DerivedStatsCalculator _derivedStats;

    public EffectManager(DerivedStatsCalculator derivedStats)
    {
        _derivedStats = derivedStats ?? throw new ArgumentNullException(nameof(derivedStats));
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
                request.Template,
                request.SourceUnitId,
                targetId);

            if (existing is not null)
            {
                // make new effect, set to same stacks as old, then increase stacks. this ensures resolved damage will update
                context.Effects.IncreaseStacks(targetId, existing.Id);
                context.Effects.ResetTicksToMax(targetId, existing.Id);
            }
            else
            {
                // TODO: Create EffectInstance and EffectInstanceId Factories

                // create instance per target id
                //var instance = CreateNewInstance();

                //context.Effects.AddEffect(targetId, instance);

                // apply initial effect
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
        EffectTemplate template,
        UnitInstanceId sourceUnitId,
        UnitInstanceId targetUnitId)
    {
        if (!state.ActiveEffects.TryGetValue(targetUnitId, out var effectsById))
            return null;

        foreach (var effect in effectsById.Values)
        {
            if (effect.Template.Id != template.Id) continue;
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
}
