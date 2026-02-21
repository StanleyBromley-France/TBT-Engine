namespace Core.Engine.Effects.Components.Calculators;

using Domain.Effects.Components.Templates;
using Domain.Effects.Instances.ReadOnly;
using Mutation;
using Game;

public class HealCalculator : IHealCalculator
{
    public int Compute(
        GameMutationContext context,
        IReadOnlyGameState state,
        IReadOnlyEffectInstance effect,
        IHealComponent healTemplate)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (effect is null) throw new ArgumentNullException(nameof(effect));
        if (healTemplate is null) throw new ArgumentNullException(nameof(healTemplate));

        float multiplier = ComputeMultiplier(state, effect);

        var final = healTemplate.HealAmount * multiplier;
        var rounded = (int)MathF.Round(final);
        return rounded < 0 ? 0 : rounded;
    }

    private static float ComputeMultiplier(
        IReadOnlyGameState state,
        IReadOnlyEffectInstance effect)
    {
        var target = state.UnitInstances[effect.TargetUnitId];
        var source = state.UnitInstances[effect.SourceUnitId];

        float dealtMult = source.DerivedStats.HealingDealt / 100f;

        float takenMult = target.DerivedStats.HealingReceived / 100f;

        return dealtMult * takenMult;
    }
}
