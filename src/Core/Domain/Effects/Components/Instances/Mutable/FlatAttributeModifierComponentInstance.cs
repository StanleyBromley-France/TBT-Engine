namespace Core.Domain.Effects.Components.Instances.Mutable;

using Core.Domain.Effects.Components.Templates;
using Core.Domain.Types;

public sealed class FlatAttributeModifierComponentInstance
    : EffectComponentInstance<FlatAttributeModifierComponentTemplate>
{
    public FlatAttributeModifierComponentInstance(
        EffectComponentInstanceId id,
        FlatAttributeModifierComponentTemplate template)
        : base(id, template) { }
}
