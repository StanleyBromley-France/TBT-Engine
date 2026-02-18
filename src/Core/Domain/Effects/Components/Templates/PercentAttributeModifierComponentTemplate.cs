using Core.Domain.Effects.Stats;
using Core.Domain.Types;

namespace Core.Domain.Effects.Components.Templates;

public sealed class PercentAttributeModifierComponentTemplate : EffectComponentTemplate
{
    public StatType Stat { get; }
    public int Percent { get; }

    public PercentAttributeModifierComponentTemplate(
        EffectComponentTemplateId id,
        StatType stat,
        int percent)
        : base(id)
    {
        Stat = stat;
        Percent = percent;
    }
}