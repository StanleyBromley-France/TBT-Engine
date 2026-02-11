using Core.Domain.Types;

namespace Core.Domain.Effects.Components.Templates;

public sealed class FlatAttributeModifierComponentTemplate : EffectComponentTemplate
{
    public UnitAttributeType Attribute { get; }
    public int Amount { get; }

    public FlatAttributeModifierComponentTemplate(
        EffectComponentTemplateId id,
        UnitAttributeType attribute,
        int amount)
        : base(id)
    {
        Attribute = attribute;
        Amount = amount;
    }
}