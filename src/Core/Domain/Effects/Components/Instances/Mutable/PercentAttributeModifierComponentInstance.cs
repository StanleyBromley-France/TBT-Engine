namespace Core.Domain.Effects.Components.Instances.Mutable;

using Core.Domain.Effects.Components.Instances.ReadOnly;
using Core.Domain.Effects.Components.Templates;
using Core.Domain.Effects.Stats;
using Core.Domain.Types;

public sealed class PercentAttributeModifierComponentInstance : EffectComponentInstance<PercentAttributeModifierComponentTemplate>, IDerivedStatsContributor
{
    public PercentAttributeModifierComponentInstance(
        EffectComponentInstanceId id,
        PercentAttributeModifierComponentTemplate template)
        : base(id, template) { }

    public void Contribute(IDerivedStatsModifierSink modifierSink, EffectInstanceId effectId, int stacks)
    {
        // percent per stack: 0.10f = +10%, -0.10f = -10%
        float p = TemplateTyped.Percent;

        // multiply per stack
        var effectiveMultiplier = MathF.Pow(1f + p, stacks);

        // convert multiplier to percent-of-base additive
        var effectivePercentAdd = effectiveMultiplier - 1f;

        modifierSink.ConsiderPercent(
            TemplateTyped.Stat,
            effectivePercentAdd,
            effectId);
    }
}

