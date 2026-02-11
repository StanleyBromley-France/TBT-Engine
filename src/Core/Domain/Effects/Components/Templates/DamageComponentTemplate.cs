using Core.Domain.Types;

namespace Core.Domain.Effects.Components.Templates;
public sealed class DamageComponentTemplate : EffectComponentTemplate
{
    public int Damage { get; }
    public DamageType DamageType { get; }

    public DamageComponentTemplate(
        EffectComponentTemplateId id,
        int damage,
        DamageType damageType)
        : base(id)
    {
        Damage = damage;
        DamageType = damageType;
    }
}
