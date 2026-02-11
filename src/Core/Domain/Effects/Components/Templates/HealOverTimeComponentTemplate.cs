using Core.Domain.Types;

namespace Core.Domain.Effects.Components.Templates;

public sealed class HealOverTimeComponentTemplate : EffectComponentTemplate
{
    public int HealPerTick { get; }

    public HealOverTimeComponentTemplate(
        EffectComponentTemplateId id,
        int healPerTick)
        : base(id)
    {
        HealPerTick = healPerTick;
    }
}