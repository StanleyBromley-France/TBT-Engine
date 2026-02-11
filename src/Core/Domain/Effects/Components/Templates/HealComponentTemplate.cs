using Core.Domain.Types;

namespace Core.Domain.Effects.Components.Templates;

public sealed class HealComponentTemplate : EffectComponentTemplate
{
    public int Heal { get; }

    public HealComponentTemplate(
        EffectComponentTemplateId id,
        int heal)
        : base(id)
    {
        Heal = heal;
    }
}