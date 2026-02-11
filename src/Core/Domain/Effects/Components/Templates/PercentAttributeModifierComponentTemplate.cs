using Core.Domain.Types;

namespace Core.Domain.Effects.Components.Templates;

public sealed class PercentAttributeModifierComponentTemplate : EffectComponentTemplate
{
    public UnitAttributeType Attribute { get; }
    public int Percent { get; }

    public PercentAttributeModifierComponentTemplate(
        EffectComponentTemplateId id,
        UnitAttributeType attribute,
        int percent)
        : base(id)
    {
        Attribute = attribute;
        Percent = percent;
    }
}