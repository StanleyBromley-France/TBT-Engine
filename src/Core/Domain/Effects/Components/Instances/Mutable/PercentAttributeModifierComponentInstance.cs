namespace Core.Domain.Effects.Components.Instances.Mutable;

using Core.Domain.Effects.Components.Templates;
using Core.Domain.Types;

public sealed class PercentAttributeModifierComponentInstance
    : EffectComponentInstance<PercentAttributeModifierComponentTemplate>
{
    public PercentAttributeModifierComponentInstance(
        EffectComponentInstanceId id,
        PercentAttributeModifierComponentTemplate template)
        : base(id, template) { }
}
