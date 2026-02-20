using Core.Domain.Types;

namespace Core.Domain.Effects.Components.Templates;
public sealed class DamageComponentTemplate : EffectComponentTemplate, ICrittableComponentTemplate
{
    public int Damage { get; }
    public DamageType DamageType { get; }

    public int CritChance { get; }
    public float CritMultiplier { get; }

    public DamageComponentTemplate(
        EffectComponentTemplateId id,
        int damage,
        DamageType damageType,
        int critChance,
        float critMultiplier)
        : base(id)
    {
        Damage = damage;
        DamageType = damageType;
        CritChance = critChance;
        CritMultiplier = critMultiplier;
    }
}
