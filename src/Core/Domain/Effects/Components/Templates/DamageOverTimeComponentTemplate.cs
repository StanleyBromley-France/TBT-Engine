using Core.Domain.Types;

namespace Core.Domain.Effects.Components.Templates;
public sealed class DamageOverTimeComponentTemplate : EffectComponentTemplate, IDamageComponent
{
    public int DamageAmount { get; }
    public DamageType DamageType { get; }

    public DamageOverTimeComponentTemplate(
        EffectComponentTemplateId id,
        int damage,
        DamageType damageType)
        : base(id)
    {
        DamageAmount = damage;
        DamageType = damageType;
    }
}