namespace Core.Domain.Effects.Components.Instances.Mutable;

using Core.Domain.Effects.Components.Instances.ReadOnly;
using Core.Domain.Effects.Stats;
using Core.Domain.Effects.Components.Templates;
using Core.Domain.Types;

public sealed class FlatAttributeModifierComponentInstance : EffectComponentInstance<FlatAttributeModifierComponentTemplate>, IDerivedStatsContributor
{
    public FlatAttributeModifierComponentInstance(
        EffectComponentInstanceId id,
        FlatAttributeModifierComponentTemplate template)
        : base(id, template) { }

    public void Contribute(IDerivedStatsModifierSink modifierSink, EffectInstanceId effectId, int stacks)
    {
        // Flat modifiers: ADD per stack
        var delta = TemplateTyped.Amount * stacks;

        modifierSink.ConsiderFlat(
            TemplateTyped.Stat,
            delta,
            effectId);
    }
}
