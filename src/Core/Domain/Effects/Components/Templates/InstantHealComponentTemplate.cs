using Core.Domain.Types;

namespace Core.Domain.Effects.Components.Templates;

public sealed class InstantHealComponentTemplate : EffectComponentTemplate, IHealComponent
{
    public int HealAmount { get; }

    public InstantHealComponentTemplate(
        EffectComponentTemplateId id,
        int heal)
        : base(id)
    {
        HealAmount = heal;
    }
}