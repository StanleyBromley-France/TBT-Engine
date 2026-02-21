namespace Core.Engine.Effects.Components.Calculators;

using Core.Domain.Effects;
using Core.Domain.Effects.Components.Templates;
using Core.Domain.Effects.Instances.ReadOnly;
using Core.Engine.Mutation;
using Core.Game;

/// <summary>
/// Calculates resolved damage for damage component including resistance modifiers with critical hits.
/// </summary>
public sealed class CritDamageComponentCalculator : IDamageComponentCalculator
{
    public int Compute(
        GameMutationContext context,
        IReadOnlyGameState state,
        IReadOnlyEffectInstance effect,
        IDamageComponent damageTemplate)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (effect is null) throw new ArgumentNullException(nameof(effect));
        if (damageTemplate is null) throw new ArgumentNullException(nameof(damageTemplate));

        float multiplier = ComputeResistanceMultiplier(state, effect, damageTemplate.DamageType);

        if(damageTemplate is ICrittableComponentTemplate crittableTemplate)
            multiplier *= ComputeCritMultiplier(context, crittableTemplate);

        var final = damageTemplate.DamageAmount * multiplier;
        var rounded = (int)MathF.Round(final);
        return rounded < 0 ? 0 : rounded;
    }

    private static float ComputeResistanceMultiplier(
        IReadOnlyGameState state,
        IReadOnlyEffectInstance effect,
        DamageType damageType)
    {
        var target = state.UnitInstances[effect.TargetUnitId];
        var source = state.UnitInstances[effect.SourceUnitId];

        float dealtMult = source.DerivedStats.DamageDealt / 100f;

        int takenPercent = damageType == DamageType.Physical
            ? target.DerivedStats.PhysicalDamageReceived
            : target.DerivedStats.MagicDamageReceived;

        float takenMult = takenPercent / 100f;

        return dealtMult * takenMult;
    }

    private static float ComputeCritMultiplier(
        GameMutationContext context,
        ICrittableComponentTemplate critTemplate)
    {
        int chance = critTemplate.CritChance;

        if (chance <= 0) return 1f;
        if (chance >= 100) return critTemplate.CritMultiplier;

        int roll = context.Rng.RollRandom(0, 100);
        return roll < chance ? critTemplate.CritMultiplier : 1f;
    }
}