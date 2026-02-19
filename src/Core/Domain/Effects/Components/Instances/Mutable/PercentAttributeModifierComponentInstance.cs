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
        var effectivePercentAdd = TemplateTyped.Percent * stacks;

        modifierSink.ConsiderPercent(
            TemplateTyped.Stat,
            effectivePercentAdd,
            effectId);
    }
}

