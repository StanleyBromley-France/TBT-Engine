using Core.Domain.Effects.Stats;
using Core.Domain.Types;

namespace Core.Domain.Effects.Components.Templates;

public sealed class FlatAttributeModifierComponentTemplate : EffectComponentTemplate
{
    public StatType Stat { get; }
    public int Amount { get; }

    public FlatAttributeModifierComponentTemplate(
        EffectComponentTemplateId id,
        StatType stat,
        int amount)
        : base(id)
    {
        Stat = stat;
        Amount = amount;
    }
}