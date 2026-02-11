using Core.Domain.Types;

namespace Core.Domain.Effects.Components.Templates;
public sealed class DamageOverTimeComponentTemplate : EffectComponentTemplate
{
    public int DamagePerTick { get; }
    public DamageType DamageType { get; }

    public DamageOverTimeComponentTemplate(
        EffectComponentTemplateId id,
        int damagePerTick,
        DamageType damageType)
        : base(id)
    {
        DamagePerTick = damagePerTick;
        DamageType = damageType;
    }
}